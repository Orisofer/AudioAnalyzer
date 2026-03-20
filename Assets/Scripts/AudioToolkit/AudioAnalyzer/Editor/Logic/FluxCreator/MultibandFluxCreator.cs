using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class MultibandFluxCreator : IFluxCreator
    {
        private const float KICK_FREQUENCY_MAX = 150;
        private const float SNARE_FREQUENCY_MAX = 2000;
        private const float HIHAT_FREQUENCY_MAX = 15000;
        private const float THRESHOLD_SENSITIVITY_MULTIPLIER = 1.65f;
        private const float REGION_AVERAGE_ENERGY_MULTIPLIER = 5f;

        private const int FLUX_TIMELINE_WINDOW_SIZE = 20;

        private List<int> m_KicksOnsets;
        private List<int> m_SnaresOnsets;
        private List<int> m_HiHatsOnsets;
        
        private float[] m_KicksFlux;
        private float[] m_SnaresFlux;
        private float[] m_HiHatsFlux;
        
        private float m_KicksFluxTotalSum;
        private float m_SnaresFluxTotalSum;
        private float m_HiHatsFluxTotalSum;

        public MultibandFluxCreator()
        {
            m_KicksOnsets = new List<int>();
            m_SnaresOnsets = new List<int>();
            m_HiHatsOnsets = new List<int>();
        }

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
            
            // 2. loop over the bins for each spectrum to create the flux timeline
            
            m_KicksFlux = new float[spectrogram.Spectra.Length];
            m_SnaresFlux = new float[spectrogram.Spectra.Length];
            m_HiHatsFlux = new float[spectrogram.Spectra.Length];
            
            for (int i = 1; i < spectrogram.Spectra.Length; i++)
            {
                Spectrum prev =  spectrogram.Spectra[i - 1];
                Spectrum curr = spectrogram.Spectra[i];
                
                int binIndexPointer = 0;
                
                // evaluate kick region
                EvaluateRegionFlux(ref m_KicksFlux,  kickBinsEndIndex, prev, curr, ref binIndexPointer, i, ref m_KicksFluxTotalSum);
                
                // evaluate snare region
                EvaluateRegionFlux(ref m_SnaresFlux,  snareBinsEndIndex, prev, curr, ref binIndexPointer, i, ref m_SnaresFluxTotalSum);
                
                // evaluate hihat region
                EvaluateRegionFlux(ref m_HiHatsFlux,  hihatBinsEndIndex, prev, curr, ref binIndexPointer, i, ref m_HiHatsFluxTotalSum);
            }
            
            // 3. go over the total flux with local average creating onsets
            
            m_KicksOnsets.Clear();
            m_SnaresOnsets.Clear();
            m_HiHatsOnsets.Clear();
            
            Spectrum[] spectra = spectrogram.Spectra;

            int halfWindowSize = (int)(FLUX_TIMELINE_WINDOW_SIZE * 0.5f);

            float averageEnergyInRegion = m_KicksFluxTotalSum / m_KicksFlux.Length;
            float noiseThreshold = averageEnergyInRegion * REGION_AVERAGE_ENERGY_MULTIPLIER;

            for (int i = 0; i < m_KicksFlux.Length; i++)
            {
                int leftPointer = Mathf.Max(0, i - halfWindowSize);
                int rightPointer =  Mathf.Min(i + halfWindowSize, m_KicksFlux.Length - 1);
                int windowElementCount = rightPointer - leftPointer + 1;

                float localAverageThreshold = 0f;

                for (int j = leftPointer; j < rightPointer + 1; j++)
                {
                    localAverageThreshold += m_KicksFlux[j];
                }

                // average the window
                localAverageThreshold /= windowElementCount;
                
                // add multiplier
                localAverageThreshold *= THRESHOLD_SENSITIVITY_MULTIPLIER;

                // if it passes the threshold we validate its not local maxima
                if (m_KicksFlux[i] > noiseThreshold && m_KicksFlux[i] > localAverageThreshold)
                {
                    if (i > 0 && i < m_KicksFlux.Length - 1)
                    {
                        float leftValue = m_KicksFlux[i - 1];
                        float rightValue = m_KicksFlux[i + 1];
                        
                        if (m_KicksFlux[i] > leftValue && m_KicksFlux[i] > rightValue)
                        {
                            // it's a local maximum, add it to onset
                            m_KicksOnsets.Add(spectra[i].StartingSample);
                        }
                    }
                    else
                    {
                        if (i == 0)
                        {
                            float rightValue = m_KicksFlux[i + 1];
                            
                            if (m_KicksFlux[i] > rightValue)
                            {
                                // it's a local maximum, add it to onset
                                m_KicksOnsets.Add(spectra[i].StartingSample);

                                continue;
                            }
                        }

                        if (i == m_KicksFlux.Length - 1)
                        {
                            float leftValue = m_KicksFlux[i - 1];
                            
                            if (m_KicksFlux[i] > leftValue)
                            {
                                // it's a local maximum, add it to onset
                                m_KicksOnsets.Add(spectra[i].StartingSample);
                            }
                        }
                    }
                }
            }
        }

        private void EvaluateRegionFlux(ref float[] instrument, int binsEndIndex, Spectrum prev,Spectrum curr,
                ref int binIndexPointer, int spectrumIndex, ref float fluxTotalSum)
        {
            float regionDeltaSum = 0f;
            
            while (binIndexPointer < binsEndIndex)
            {
                float delta = BinDelta(prev, curr, binIndexPointer);
                regionDeltaSum += delta;
                binIndexPointer++;
            }

            instrument[spectrumIndex] = regionDeltaSum;
            fluxTotalSum +=  regionDeltaSum;
        }

        private float BinDelta(Spectrum prev, Spectrum curr, int binIndex)
        {
            float delta = curr[binIndex] - prev[binIndex];
            return Mathf.Max(0, delta);
        }

        private int GetBinIndex(float frequency, float sampleRate, int fftSize)
        {
            int binIndex = (int)((fftSize * frequency) / sampleRate);
            return binIndex;
        }
    }
}

