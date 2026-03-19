using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class MultibandFluxCreator : IFluxCreator
    {
        private const float ENERGY_THRESHOLD = 4.5f;
        private const float KICK_FREQUENCY_MAX = 110;
        private const float SNARE_FREQUENCY_MAX = 2000;
        private const float HIHAT_FREQUENCY_MAX = 13000;
        
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
                while (binIndexPointer < kickBinsEndIndex)
                {
                    SetOnset(prev, curr, ref m_Kicks, binIndexPointer, i);
                    binIndexPointer++;
                }
                
                // evaluate kick region
                while (binIndexPointer < snareBinsEndIndex)
                {
                    SetOnset(prev, curr, ref m_Snares, binIndexPointer, i);
                    binIndexPointer++;
                }
                
                // evaluate kick region
                while (binIndexPointer < hihatBinsEndIndex)
                {
                    SetOnset(prev, curr, ref m_HiHats, binIndexPointer, i);
                    binIndexPointer++;
                }
            }
        }

        private void SetOnset(Spectrum prev, Spectrum curr, ref int[] instrument, int binIndex, int timeLineIndex)
        {
            float delta = curr[binIndex] - prev[binIndex];

            if (delta < 0) return;

            if (delta > ENERGY_THRESHOLD)
            {
                instrument[timeLineIndex] = curr.StartingSample;
            }
        }

        private int GetBinIndex(float frequency, float sampleRate, int fftSize)
        {
            int binIndex = (int)((fftSize * frequency) / sampleRate);
            return binIndex;
        }
    }
}

