using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class MultibandFluxCreator : IFluxCreator
    {
        private const float KICK_FREQUENCY_MAX = 120;
        private const float SNARE_FREQUENCY_MAX = 2000;
        private const float HIHAT_FREQUENCY_MAX = 15000;
        
        private float m_ThresholdSensitivityMultiplier;
        private float m_RegionAverageEnergyMultiplier;
        private int m_FluxTimelineWindowSize;
        
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

            m_ThresholdSensitivityMultiplier = 1.65f;
            m_RegionAverageEnergyMultiplier = 110f;
            m_FluxTimelineWindowSize = 20;
        }

        public List<Flux> CreateFlux(Spectrogram spectrogram)
        {
            if (spectrogram == null || spectrogram.Spectra == null || spectrogram.Spectra.Length == 0)
            {
                Debug.LogWarning("No Spectrogram specified for Multiband FluxCreator");
                return null;
            }

            Reset();
            
            // create result variable
            List<Flux> result = new List<Flux>();
            
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
            
            Spectrum[] spectra = spectrogram.Spectra;

            int halfWindowSize = (int)(m_FluxTimelineWindowSize * 0.5f);
            int positionExcludeSurrounding = 1;

            float averageEnergyInRegion = CalculateMedian(m_KicksFlux);
            float noiseFloor = averageEnergyInRegion * m_RegionAverageEnergyMultiplier;
            
            float[] averageThresholds = new float[m_KicksFlux.Length];

            for (int i = 0; i < m_KicksFlux.Length; i++)
            {
                if (m_KicksFlux[i] < noiseFloor)
                {
                    continue;
                }
                
                int leftPointer = Mathf.Max(0, i - halfWindowSize);
                int rightPointer =  Mathf.Min(i + halfWindowSize, m_KicksFlux.Length - 1);
                int windowElementCount = rightPointer - leftPointer + 1;

                float localAverageThreshold = 0f;

                for (int j = leftPointer; j < rightPointer + 1; j++)
                {
                    // exclude the current position itself and surrounding area
                    if (j < i - positionExcludeSurrounding || j > i + positionExcludeSurrounding)
                    {
                        localAverageThreshold += m_KicksFlux[j];
                    }
                }

                // average the window - subtract the exclude surrounding area so it won't affect the locale average
                localAverageThreshold /= (windowElementCount - ((positionExcludeSurrounding * 2) + 1));
                
                // add multiplier
                localAverageThreshold *= m_ThresholdSensitivityMultiplier;
                
                averageThresholds[i] = localAverageThreshold;

                // if it passes the threshold we validate its not local maxima
                if (m_KicksFlux[i] > localAverageThreshold)
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

            Flux kickFlux = new Flux("Kicks", m_KicksFlux, averageThresholds, m_KicksOnsets, noiseFloor, spectrogram.HopSize);
            
            result.Add(kickFlux);

            return result;
        }

        public void SetParameters(FluxCreatorParameters parameters)
        {
            m_ThresholdSensitivityMultiplier =  parameters.ThresholdSensitivityMultiplier;
            m_RegionAverageEnergyMultiplier = parameters.RegionAverageEnergyMultiplier;
            m_FluxTimelineWindowSize  = parameters.FluxTimelineWindowSize;
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
        
        private float CalculateMedian(float[] sourceArray)
        {
            if (sourceArray == null || sourceArray.Length == 0)
            {
                return 0f;
            }
            
            int arrayLength = sourceArray.Length;

            // Create a copy so we don't mutate the original timeline's order
            float[] sortedArray = new float[arrayLength];
            Array.Copy(sourceArray, sortedArray, arrayLength);
            Array.Sort(sortedArray);

            int halfLengthIndex = arrayLength / 2;

            // If the length is even, the median is the average of the two middle elements
            if (arrayLength % 2 == 0)
            {
                return (sortedArray[halfLengthIndex - 1] + sortedArray[halfLengthIndex]) * 0.5f;
            }
    
            // If odd, it's exactly the middle element
            return sortedArray[halfLengthIndex];
        }

        private void Reset()
        {
            m_KicksFluxTotalSum = 0f;
            m_SnaresFluxTotalSum = 0f;
            m_HiHatsFluxTotalSum = 0f;

            m_KicksFlux = null;
            m_SnaresFlux = null;
            m_HiHatsFlux = null;
            
            m_KicksOnsets.Clear();
            m_SnaresOnsets.Clear();
            m_HiHatsOnsets.Clear();
        }
    }
}

