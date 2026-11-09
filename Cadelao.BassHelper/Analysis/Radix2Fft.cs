namespace Cadelao.BassHelper.Analysis;

/// <summary>FFT radix-2 without runtime allocations; independent of NAudio.</summary>
public sealed class Radix2Fft
{
    private readonly int _size;
    private readonly int[] _reverse;
    private readonly double[] _cos;
    private readonly double[] _sin;

    public Radix2Fft(int size)
    {
        if (size < 2 || (size & (size - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(size), "FFT size must be a power of two.");
        _size = size;
        int bits = 0;
        for (int x = size; x > 1; x >>= 1) bits++;
        _reverse = new int[size];
        for (int i = 0; i < size; i++)
        {
            int x = i, r = 0;
            for (int k = 0; k < bits; k++) { r = (r << 1) | (x & 1); x >>= 1; }
            _reverse[i] = r;
        }
        _cos = new double[size / 2];
        _sin = new double[size / 2];
        for (int k = 0; k < size / 2; k++)
        {
            double angle = -2 * Math.PI * k / size;
            _cos[k] = Math.Cos(angle);
            _sin[k] = Math.Sin(angle);
        }
    }

    public void Forward(double[] real, double[] imag)
    {
        if (real.Length != _size || imag.Length != _size) throw new ArgumentException("Incorrect FFT buffer size.");
        for (int i = 0; i < _size; i++)
        {
            int j = _reverse[i];
            if (j <= i) continue;
            (real[i], real[j]) = (real[j], real[i]);
            (imag[i], imag[j]) = (imag[j], imag[i]);
        }
        for (int len = 2; len <= _size; len <<= 1)
        {
            int half = len >> 1, step = _size / len;
            for (int i = 0; i < _size; i += len)
            {
                for (int j = 0; j < half; j++)
                {
                    int k = j * step, u = i + j, v = u + half;
                    double tr = real[v] * _cos[k] - imag[v] * _sin[k];
                    double ti = real[v] * _sin[k] + imag[v] * _cos[k];
                    real[v] = real[u] - tr;
                    imag[v] = imag[u] - ti;
                    real[u] += tr;
                    imag[u] += ti;
                }
            }
        }
    }
}
