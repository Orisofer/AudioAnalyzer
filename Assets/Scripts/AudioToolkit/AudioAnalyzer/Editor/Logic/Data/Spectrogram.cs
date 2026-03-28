namespace Ori.AudioAnalyzer.Core
{
    public class Spectrogram
    {
        private readonly Spectrum[] m_Spectra;
        private readonly float m_SampleRate;
        private readonly int m_FFTSize;
        private readonly int m_HopSize;
        
        internal Spectrum[] Spectra => m_Spectra;
        internal float SampleRate => m_SampleRate;
        internal int HopSize => m_HopSize;
        
        internal Spectrogram(float sampleRate, int fftSize, int hopSize, Spectrum[] spectra)
        {
            m_Spectra = spectra;
            m_SampleRate = sampleRate;
            m_FFTSize = fftSize;
            m_HopSize = hopSize;
        }
    }
}
