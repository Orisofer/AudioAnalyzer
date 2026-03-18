using Exocortex.DSP;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class ExocortexFFTClient : IFFTProcessor
    {
        private readonly Frame[] m_Frames;
        private readonly float m_SampleRate;
        private readonly int m_FFTSize;
        private readonly int m_HopSize;

        public ExocortexFFTClient(Frame[] frames, int fftSize, int sampleRate, int hopSize)
        {
            m_Frames = frames;
            m_SampleRate = sampleRate;
            m_FFTSize = fftSize;
            m_HopSize = hopSize;
        }

        public Spectrogram Process()
        {
            Spectrum[] spectrums = new Spectrum[m_Frames.Length];
                
            for (int i = 0; i < m_Frames.Length; i++)
            {
                Frame frame = m_Frames[i];
                spectrums[i] = ProcessFrame(frame);
            }
            
            Spectrogram result = new Spectrogram(
                m_SampleRate,
                m_FFTSize,
                m_HopSize,
                spectrums
                );
            
            return result;
        }

        private Spectrum ProcessFrame(Frame frame)
        {
            if (m_FFTSize != frame.Samples.Length)
            {
                throw new System.Exception("FFT Client: FFT size not matching frame size, cannot keep processing");
            }
            
            ComplexF[] complex = new ComplexF[m_FFTSize];
            
            for (int i = 0; i < m_FFTSize; i++)
            {
                var real = frame[i];
                var imag = 0f;
            
                complex[i] = new ComplexF(real, imag);
            }
            
            Fourier.FFT(complex, FourierDirection.Forward);
            
            // we take only positive frequencies
            int binsCount = m_FFTSize / 2;
            float[] bins = new float[binsCount];

            for (int i = 0; i < binsCount; i++)
            {
                bins[i] = Mathf.Sqrt(complex[i].Re * complex[i].Re + complex[i].Im * complex[i].Im);
            }

            Spectrum newSpectrum = new Spectrum(bins, frame.StartingSample, m_SampleRate);
            
            return newSpectrum;
        }
    }
}