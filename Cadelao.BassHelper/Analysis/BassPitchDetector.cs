using Cadelao.BassHelper.Models;

namespace Cadelao.BassHelper.Analysis;

/// <summary>
/// Monophonic bass-pitch ESTIMATOR over a full mix. Harmonic-summation FFT with
/// sub-bin interpolation, a low-frequency prior and confidence estimation.
/// It intentionally reports no note for silence/uncertain material.
/// </summary>
public sealed class BassPitchDetector
{
    public const int SampleRate = 11025;
    public const int WindowSize = 8192;
    public const int HopSize = 1102; // approximately 100ms

    private readonly Radix2Fft _fft = new(WindowSize);
    private readonly double[] _real = new double[WindowSize];
    private readonly double[] _imag = new double[WindowSize];
    private readonly double[] _magnitudes = new double[WindowSize / 2 + 1];
    private readonly double[] _hann = new double[WindowSize];
    private readonly double[] _scores = new double[80];
    private readonly int _minMidi;
    private readonly int _maxMidi;
    private static readonly double[] HarmonicWeight = { 2.5, 1.05, 0.72, 0.40, 0.26, 0.17 };

    public BassPitchDetector(int minMidi, int maxMidi)
    {
        if (minMidi < 18 || maxMidi >= _scores.Length || minMidi >= maxMidi)
            throw new ArgumentOutOfRangeException(nameof(minMidi));
        _minMidi = minMidi;
        _maxMidi = maxMidi;
        for (int i = 0; i < WindowSize; i++)
            _hann[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (WindowSize - 1));
    }

    public NoteFrame Detect(ReadOnlySpan<float> window, double time)
    {
        if (window.Length != WindowSize) throw new ArgumentException("Incorrect analysis window.");
        double sum = 0;
        for (int i = 0; i < WindowSize; i++)
        {
            double x = window[i];
            sum += x * x;
            _real[i] = x * _hann[i];
            _imag[i] = 0;
        }
        double rms = Math.Sqrt(sum / WindowSize);
        if (rms < 0.0032)
            return new NoteFrame(time, -1, 0, 0, rms);

        _fft.Forward(_real, _imag);
        double normalization = 4.0 / WindowSize;
        for (int i = 0; i < _magnitudes.Length; i++)
            _magnitudes[i] = Math.Sqrt(_real[i] * _real[i] + _imag[i] * _imag[i]) * normalization;

        double best = 0, runnerUp = 0;
        int bestNote = -1;
        double bestFreq = 0, bestFundamental = 0;
        for (int midi = _minMidi; midi <= _maxMidi; midi++)
        {
            double baseFrequency = MusicNotes.Frequency(midi);
            double score = 0, baseAmplitude = 0, baseRefinedFreq = baseFrequency;
            for (int harmonic = 1; harmonic <= HarmonicWeight.Length; harmonic++)
            {
                double frequency = baseFrequency * harmonic;
                if (frequency >= SampleRate * 0.49) break;
                (double amplitude, double measuredFrequency) = HarmonicAmplitude(frequency);
                if (harmonic == 1) { baseAmplitude = amplitude; baseRefinedFreq = measuredFrequency; }
                score += HarmonicWeight[harmonic - 1] * amplitude;
            }
            // Requiring some fundamental evidence reduces octave-down hallucinations.
            if (baseAmplitude < 0.0045 && score > 0.0) score *= 0.56;
            // Mild preference for the low instrument range in a polyphonic recording.
            if (baseFrequency > 175.0) score *= 0.87;
            _scores[midi] = score;
            if (score > best)
            {
                runnerUp = best;
                best = score;
                bestNote = midi;
                bestFreq = baseRefinedFreq;
                bestFundamental = baseAmplitude;
            }
            else if (score > runnerUp)
            {
                runnerUp = score;
            }
        }

        if (bestNote < 0 || best < 0.014 || bestFundamental < 0.0020)
            return new NoteFrame(time, -1, 0, 0, rms);

        // Adjacent semitones can share an FFT lobe. Compare excluding neighbors
        // to make the ambiguity estimate sensitive to other notes, not just leakage.
        double remote = 0;
        for (int midi = _minMidi; midi <= _maxMidi; midi++)
            if (Math.Abs(midi - bestNote) >= 2) remote = Math.Max(remote, _scores[midi]);

        double uniqueness = Math.Clamp((best - runnerUp) / Math.Max(0.00001, best), 0, 1);
        double remoteSeparation = Math.Clamp((best - remote) / Math.Max(0.00001, best), 0, 1);
        double strength = Math.Clamp(bestFundamental / Math.Max(0.002, rms * 0.8), 0, 1);
        double confidence = Math.Clamp(0.20 + 0.42 * uniqueness + 0.28 * remoteSeparation + 0.15 * strength, 0, 1);
        return new NoteFrame(time, bestNote, bestFreq, confidence, rms);
    }

    private (double amplitude, double frequency) HarmonicAmplitude(double desiredFrequency)
    {
        double expectedBin = desiredFrequency * WindowSize / SampleRate;
        int center = (int)Math.Round(expectedBin);
        int lo = Math.Max(2, center - 2);
        int hi = Math.Min(_magnitudes.Length - 3, center + 2);
        double best = 0, frequency = desiredFrequency;
        for (int k = lo; k <= hi; k++)
        {
            // Interpolate log magnitude, not raw magnitude, to locate sub-bin peaks.
            double l = Math.Log(_magnitudes[k - 1] + 1e-10);
            double c = Math.Log(_magnitudes[k] + 1e-10);
            double r = Math.Log(_magnitudes[k + 1] + 1e-10);
            double denominator = l - 2 * c + r;
            double offset = Math.Abs(denominator) > 1e-10 ? 0.5 * (l - r) / denominator : 0;
            offset = Math.Clamp(offset, -0.5, 0.5);
            double interpolatedFrequency = (k + offset) * SampleRate / WindowSize;
            double cents = 1200.0 * Math.Log2(Math.Max(0.1, interpolatedFrequency) / desiredFrequency);
            double weight = Math.Exp(-0.5 * Math.Pow(cents / 42.0, 2));
            double amp = _magnitudes[k] * weight;
            if (amp > best)
            {
                best = amp;
                frequency = interpolatedFrequency;
            }
        }
        return (best, frequency);
    }
}
