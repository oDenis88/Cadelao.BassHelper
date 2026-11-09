using Cadelao.BassHelper.Models;

namespace Cadelao.BassHelper.Analysis;

/// <summary>
/// A full-mix harmonic/pitch-class analyzer. Separates harmony (all notes)
/// from the monophonic bass pitch used by BassPitchDetector.
/// Never labels an isolated D as Dm: the minor third F must contribute.
/// </summary>
public sealed class ChordDetector
{
    private readonly Radix2Fft _fft = new(BassPitchDetector.WindowSize);
    private readonly double[] _real = new double[BassPitchDetector.WindowSize];
    private readonly double[] _imag = new double[BassPitchDetector.WindowSize];
    private readonly double[] _hann = new double[BassPitchDetector.WindowSize];

    public ChordDetector()
    {
        for (int i = 0; i < _hann.Length; i++)
            _hann[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (_hann.Length - 1));
    }

    public ChordFrame Detect(ReadOnlySpan<float> window, double timestamp)
    {
        if (window.Length != _real.Length)
            throw new ArgumentException("Tamanho incorreto da janela de audio.", nameof(window));
        double sumSquares = 0;
        for (int i = 0; i < window.Length; i++)
        {
            double x = window[i];
            sumSquares += x * x;
            _real[i] = x * _hann[i];
            _imag[i] = 0;
        }
        double rms = Math.Sqrt(sumSquares / window.Length);
        float[] result = new float[12], bass = new float[12];
        if (rms < 0.0015) return new ChordFrame(timestamp, result, bass, rms);
        _fft.Forward(_real, _imag);

        double[] mid = new double[12], combo = new double[12], low = new double[12];
        int maxBin = Math.Min(_real.Length / 2 - 1,
            (int)(1600.0 * _real.Length / BassPitchDetector.SampleRate));
        for (int bin = 2; bin <= maxBin; bin++)
        {
            double freq = (double)bin * BassPitchDetector.SampleRate / _real.Length;
            if (freq < 40.0) continue;
            double magnitude = Math.Sqrt(_real[bin] * _real[bin] + _imag[bin] * _imag[bin]);
            if (magnitude < 1e-10) continue;
            double pitch = 69.0 + 12.0 * Math.Log2(freq / 440.0);
            int midi = (int)Math.Round(pitch);
            int chroma = (midi % 12 + 12) % 12;
            double contribution = Math.Pow(magnitude, 0.70);
            if (freq >= 110.0 && freq <= 1300.0) mid[chroma] += contribution;
            if (freq >= 50.0 && freq <= 1000.0) combo[chroma] += contribution;
            if (freq <= 240.0) low[chroma] += contribution;
        }

        Normalize(mid);
        Normalize(combo);
        Normalize(low);
        for (int i = 0; i < 12; i++)
        {
            result[i] = (float)(0.70 * mid[i] + 0.30 * combo[i]);
            bass[i] = (float)low[i];
        }
        return new ChordFrame(timestamp, result, bass, rms);
    }

    private static void Normalize(double[] values)
    {
        double sum = values.Sum();
        if (sum <= 1e-10) return;
        for (int i = 0; i < values.Length; i++) values[i] /= sum;
    }
}
