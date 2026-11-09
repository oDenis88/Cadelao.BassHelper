using NAudio.Wave;

namespace Cadelao.BassHelper.Audio;

public sealed class AudioPlayer : IDisposable
{
    private AudioFileReader? _reader;
    private WaveOut? _output;
    private float _volume = 0.8f;
    private double _clockAnchorSeconds;
    private long _deviceAnchorBytes;

    public event EventHandler? PlaybackFinished;
    public string? FilePath { get; private set; }
    public bool IsLoaded => _reader != null;
    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;
    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;
    /// <summary>
    /// Actual samples reported played by the WaveOut device, not the reader
    /// cursor (which leads audible playback by the buffered-audio duration).
    /// </summary>
    public TimeSpan Position
    {
        get
        {
            if (_reader is null || _output is null) return TimeSpan.Zero;
            if (_output.PlaybackState == PlaybackState.Stopped)
                return TimeSpan.FromSeconds(Math.Clamp(_clockAnchorSeconds, 0, Duration.TotalSeconds));
            try
            {
                long bytes = Math.Max(0, _output.GetPosition() - _deviceAnchorBytes);
                double s = _clockAnchorSeconds + bytes / (double)_reader.WaveFormat.AverageBytesPerSecond;
                return TimeSpan.FromSeconds(Math.Clamp(s, 0, Duration.TotalSeconds));
            }
            catch { return _reader.CurrentTime; }
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_reader != null) _reader.Volume = _volume;
        }
    }

    public void Open(string path)
    {
        Close();
        try
        {
            _reader = new AudioFileReader(path) { Volume = _volume };
            // NAudio 3.x: WaveOutEvent foi renomeado para WaveOut.
            // BufferMilliseconds e a duracao de CADA buffer (2 buffers por padrao).
            // 80 ms x 1,5 ~= 120 ms de latencia media, compativel com o SYNC inicial.
            _output = new WaveOut { BufferMilliseconds = 80 };
            _output.Init(_reader);
            _output.PlaybackStopped += HandlePlaybackStopped;
            FilePath = path;
            _clockAnchorSeconds = 0;
            _deviceAnchorBytes = 0;
        }
        catch
        {
            Close();
            throw;
        }
    }

    public void Play()
    {
        if (_reader is null || _output is null) return;
        if (_output.PlaybackState == PlaybackState.Playing) return;
        if (_output.PlaybackState == PlaybackState.Paused)
        {
            _output.Play();
            return; // preserve hardware clock anchor across pause
        }
        if (_reader.CurrentTime >= _reader.TotalTime - TimeSpan.FromMilliseconds(100))
            _reader.CurrentTime = TimeSpan.Zero;
        _clockAnchorSeconds = _reader.CurrentTime.TotalSeconds;
        _deviceAnchorBytes = _output.GetPosition();
        _output.Play();
    }
    public void Pause() => _output?.Pause();
    public void Stop()
    {
        if (_output is null || _reader is null) return;
        _output.Stop();
        _reader.CurrentTime = TimeSpan.Zero;
        _clockAnchorSeconds = 0;
        _deviceAnchorBytes = _output.GetPosition();
    }
    public void Seek(double seconds)
    {
        if (_reader is null || _output is null) return;
        bool wasPlaying = _output.PlaybackState == PlaybackState.Playing;
        // Flush queued old audio. Setting AudioFileReader.CurrentTime alone does
        // NOT flush WaveOut's buffered data and causes wrong visible sync.
        if (_output.PlaybackState != PlaybackState.Stopped) _output.Stop();
        double t = Math.Clamp(seconds, 0, Duration.TotalSeconds);
        _reader.CurrentTime = TimeSpan.FromSeconds(t);
        _clockAnchorSeconds = t;
        _deviceAnchorBytes = _output.GetPosition();
        if (wasPlaying) _output.Play();
    }
    private void HandlePlaybackStopped(object? sender, StoppedEventArgs e)
    {
        var reader = _reader;
        if (e.Exception == null && reader != null &&
            reader.CurrentTime >= reader.TotalTime - TimeSpan.FromMilliseconds(120))
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
    }
    public void Close()
    {
        if (_output != null)
        {
            _output.PlaybackStopped -= HandlePlaybackStopped;
            _output.Stop();
            _output.Dispose();
            _output = null;
        }
        _reader?.Dispose();
        _reader = null;
        FilePath = null;
        _clockAnchorSeconds = 0;
        _deviceAnchorBytes = 0;
    }
    public void Dispose() => Close();
}
