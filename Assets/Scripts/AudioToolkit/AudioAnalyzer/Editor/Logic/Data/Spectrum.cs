namespace Ori.AudioAnalyzer.Core
{
    public class Spectrum
    {
        private readonly float[] m_Bins;
        private readonly float m_SampleRate;
        private readonly int m_StartingSample;

        private float m_StrongestFrequency;
        
        public int StartingSample => m_StartingSample;
        public float StrongestFrequency => m_StrongestFrequency;

        public Spectrum(float[] bins, int startingSample, float sampleRate)
        {
            m_Bins = bins;
            m_StartingSample = startingSample;
            m_SampleRate = sampleRate;
        }

        public float this[int index]
        {
            get => m_Bins[index];
            set => m_Bins[index] = value;
        }

        public float CalculateStrongestFrequency()
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
            m_StrongestFrequency = (strongestIndex * m_SampleRate) / fftSize;
            
            return m_StrongestFrequency;
        }
    }
}
