namespace Ori.AudioAnalyzer.Core
{
    public class Spectrum
    {
        private readonly float[] m_Bins;
        private readonly float m_SampleRate;
        private readonly int m_StartingSample;
        
        internal int StartingSample => m_StartingSample;
        internal float[] Bins => m_Bins;
        

        internal Spectrum(float[] bins, int startingSample, float sampleRate)
        {
            m_Bins = bins;
            m_StartingSample = startingSample;
            m_SampleRate = sampleRate;
        }

        internal float this[int index]
        {
            get => m_Bins[index];
            set => m_Bins[index] = value;
        }

        internal float CalculateStrongestFrequency()
        {
            int strongestIndex = 0;
            float strongestMagnitude = 0f;

            // only look at positive frequencies
            for (int i = 0; i < m_Bins.Length; i++)
            {
                float mag = m_Bins[i];
                
                if (mag > strongestMagnitude)
                {
                    strongestMagnitude = mag;
                    strongestIndex = i;
                }
            }
            
            // Convert bin index -> Hz
            int fftSize = m_Bins.Length * 2;
            float strongestFrequency = (strongestIndex * m_SampleRate) / fftSize;
            
            return strongestFrequency;
        }
    }
}
