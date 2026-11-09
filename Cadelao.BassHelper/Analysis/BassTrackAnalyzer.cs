using System.Diagnostics;
using NAudio.Wave;
using Cadelao.BassHelper.Models;

namespace Cadelao.BassHelper.Analysis;

public sealed class BassTrackAnalyzer
{
    public Task<AnalysisResult> AnalyzeAsync(
        string fileName, IProgress<AnalysisProgress>? progress, CancellationToken cancellationToken)
    {
        return Task.Run(() => Analyze(fileName, progress, cancellationToken), cancellationToken);
    }

    public AnalysisResult Analyze(
        string fileName, IProgress<AnalysisProgress>? progress, CancellationToken cancellationToken)
    {
        using var reader = new AudioFileReader(fileName);
        // AudioFileReader implementa ISampleProvider (amostras float intercaladas).
        // O NAudio 3 usa Read(Span<float>) em vez de Read(float[], offset, count).
        ISampleProvider sampleReader = reader;
        var detector = new BassPitchDetector(28, 55); // 4 cordas: E1-G3
        var chordDetector = new ChordDetector();
        double totalSeconds = reader.TotalTime.TotalSeconds;
        int channels = reader.WaveFormat.Channels;
        int inputRate = reader.WaveFormat.SampleRate;
        if (channels < 1 || inputRate < 8000)
            throw new NotSupportedException("Formato de audio nao suportado.");

        // Stream decode; never load an entire song into RAM.
        float[] decoded = new float[16384 - (16384 % channels)];
        var bassSamples = new float[BassPitchDetector.WindowSize];
        var window = new float[BassPitchDetector.WindowSize];
        var result = new List<NoteFrame>(Math.Max(128, (int)(totalSeconds * 10)));
        var chordFrames = new List<ChordFrame>(Math.Max(128, (int)(totalSeconds * 10)));
        long downsampledCount = 0;
        int ringPosition = 0;
        int sinceLastFrame = 0;
        var lowpass = new FourPoleLowpass(inputRate, 1750.0);
        int phase = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();
        int read;

        while ((read = sampleReader.Read(decoded.AsSpan())) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int frames = read / channels;
            for (int i = 0; i < frames; i++)
            {
                double mono = 0;
                int ix = i * channels;
                for (int ch = 0; ch < channels; ch++) mono += decoded[ix + ch];
                mono /= channels;
                float filtered = (float)lowpass.Process(mono);
                phase += BassPitchDetector.SampleRate;
                // Common MP3/WAV sampling rates >= 11025. This also handles 8 kHz inputs.
                while (phase >= inputRate)
                {
                    phase -= inputRate;
                    bassSamples[ringPosition] = filtered;
                    ringPosition = (ringPosition + 1) % BassPitchDetector.WindowSize;
                    downsampledCount++;
                    sinceLastFrame++;
                    if (downsampledCount >= BassPitchDetector.WindowSize &&
                        (downsampledCount == BassPitchDetector.WindowSize || sinceLastFrame >= BassPitchDetector.HopSize))
                    {
                        sinceLastFrame = 0;
                        int remaining = BassPitchDetector.WindowSize - ringPosition;
                        Array.Copy(bassSamples, ringPosition, window, 0, remaining);
                        if (ringPosition > 0) Array.Copy(bassSamples, 0, window, remaining, ringPosition);
                        double timestamp = (downsampledCount - BassPitchDetector.WindowSize / 2.0)
                                         / BassPitchDetector.SampleRate;
                        result.Add(detector.Detect(window, timestamp));
                        chordFrames.Add(chordDetector.Detect(window, timestamp));
                    }
                }
            }
            if (progress != null && stopwatch.ElapsedMilliseconds >= 300)
            {
                stopwatch.Restart();
                double seconds = Math.Min(totalSeconds, (double)downsampledCount / BassPitchDetector.SampleRate);
                progress.Report(new AnalysisProgress(seconds, totalSeconds, result.Count));
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        var complete = new AnalysisResult(result.ToArray(), totalSeconds, true, chordFrames.ToArray());
        progress?.Report(new AnalysisProgress(totalSeconds, totalSeconds, result.Count));
        return complete;
    }

    private sealed class FourPoleLowpass
    {
        private readonly Biquad _a;
        private readonly Biquad _b;
        public FourPoleLowpass(int fs, double cutoff)
        {
            cutoff = Math.Min(cutoff, fs * 0.35);
            _a = new Biquad(fs, cutoff, 0.5411961);
            _b = new Biquad(fs, cutoff, 1.3065630);
        }
        public double Process(double sample) => _b.Process(_a.Process(sample));

        private sealed class Biquad
        {
            private readonly double b0, b1, b2, a1, a2;
            private double z1, z2;
            public Biquad(double sampleRate, double cutoff, double q)
            {
                double w = 2.0 * Math.PI * cutoff / sampleRate;
                double c = Math.Cos(w), s = Math.Sin(w), alpha = s / (2.0 * q);
                double d = 1.0 + alpha;
                b0 = (1.0 - c) / (2.0 * d);
                b1 = (1.0 - c) / d;
                b2 = b0;
                a1 = -2.0 * c / d;
                a2 = (1.0 - alpha) / d;
            }
            public double Process(double input)
            {
                double output = b0 * input + z1;
                z1 = b1 * input - a1 * output + z2;
                z2 = b2 * input - a2 * output;
                return output;
            }
        }
    }
}
