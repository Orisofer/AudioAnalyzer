using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class MultibandFluxCreator : IFluxCreator
    {
        private const float ENERGY_THRESHOLD = 4.5f;
        private const float KICK_FREQUENCY_MAX = 150;
        private const float SNARE_FREQUENCY_MAX = 2000;
        private const float HIHAT_FREQUENCY_MAX = 15000;
        
        private int[] m_Kicks;
        private int[] m_Snares;
        private int[] m_HiHats;

        public void CreateFlux(Spectrogram spectrogram)
        {
            if (spectrogram == null || spectrogram.Spectra == null || spectrogram.Spectra.Length == 0)
            {
                Debug.LogWarning("No Spectrogram specified for Multiband FluxCreator");
                return;
            }
            
            // 1. separate bins into instruments
            
            float sampleRate = spectrogram.SampleRate;
            int binsLength = spectrogram.Spectra[0].Bins.Length;
            int fftSize = binsLength * 2;

            int kickBinsEndIndex = GetBinIndex(KICK_FREQUENCY_MAX, sampleRate, fftSize);
            int snareBinsEndIndex = GetBinIndex(SNARE_FREQUENCY_MAX, sampleRate, fftSize);
            int hihatBinsEndIndex = GetBinIndex(HIHAT_FREQUENCY_MAX, sampleRate, fftSize);
            
            // 2. loop over the bins for each spectrum
            
            m_Kicks = new int[spectrogram.Spectra.Length];
            m_Snares = new int[spectrogram.Spectra.Length];
            m_HiHats = new int[spectrogram.Spectra.Length];
            
            for (int i = 1; i < spectrogram.Spectra.Length; i++)
            {
                Spectrum prev =  spectrogram.Spectra[i - 1];
                Spectrum curr = spectrogram.Spectra[i];
                
                int binIndexPointer = 0;
                
                // evaluate kick region
                EvaluateRegion(ref m_Kicks,  kickBinsEndIndex, prev, curr, ref binIndexPointer, i);
                
                // evaluate snare region
                EvaluateRegion(ref m_Snares,  snareBinsEndIndex, prev, curr, ref binIndexPointer, i);
                
                // evaluate hihat region
                EvaluateRegion(ref m_HiHats,  hihatBinsEndIndex, prev, curr, ref binIndexPointer, i);
            }
        }

        private void EvaluateRegion(ref int[] instrument, int binsEndIndex, Spectrum prev, Spectrum curr, ref int binIndexPointer, int spectrumIndex)
        {
            float regionDeltaSum = 0f;
            
            while (binIndexPointer < binsEndIndex)
            {
                float delta = BinDelta(prev, curr, binIndexPointer);
                    
                regionDeltaSum += delta;
                    
                binIndexPointer++;
            }

            if (regionDeltaSum >= ENERGY_THRESHOLD)
            {
                instrument[spectrumIndex] = curr.StartingSample;
            }
        }

        private float BinDelta(Spectrum prev, Spectrum curr, int binIndex)
        {
            float delta = curr[binIndex] - prev[binIndex];

            if (delta < 0)
            {
                return 0f;
            }

            return delta;
        }

        private int GetBinIndex(float frequency, float sampleRate, int fftSize)
        {
            int binIndex = (int)((fftSize * frequency) / sampleRate);
            return binIndex;
        }
    }
}

