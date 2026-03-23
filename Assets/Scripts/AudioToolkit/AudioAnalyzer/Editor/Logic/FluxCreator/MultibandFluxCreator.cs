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
        
        private List<int> m_KicksOnsets;
        private List<int> m_SnaresOnsets;
        private List<int> m_HiHatsOnsets;
        
        private float m_ThresholdSensitivityMultiplier;
        private float m_NoiseFloorMultiplier;
        private int m_FluxTimelineWindowSize;
        
        private float[] m_KicksFlux;
        private float[] m_SnaresFlux;
        private float[] m_HiHatsFlux;

        public MultibandFluxCreator()
        {
            m_KicksOnsets = new List<int>();
            m_SnaresOnsets = new List<int>();
            m_HiHatsOnsets = new List<int>();

            m_ThresholdSensitivityMultiplier = 1.65f;
            m_NoiseFloorMultiplier = 110f;
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
            
            List<Flux> result = new List<Flux>();
            
            // 1. separate bins into instruments
            
            Spectrum[] spectra = spectrogram.Spectra;
            float sampleRate = spectrogram.SampleRate;
            int spectraLength = spectra.Length;
            int binsLength = spectra[0].Bins.Length;
            int fftSize = binsLength * 2;

            int kickBinsEndIndex = GetBinIndex(KICK_FREQUENCY_MAX, sampleRate, fftSize);
            int snareBinsEndIndex = GetBinIndex(SNARE_FREQUENCY_MAX, sampleRate, fftSize);
            int hihatBinsEndIndex = GetBinIndex(HIHAT_FREQUENCY_MAX, sampleRate, fftSize);
            
            // 2. loop over the bins for each spectrum to create the flux timeline
            
            m_KicksFlux = new float[spectraLength];
            m_SnaresFlux = new float[spectraLength];
            m_HiHatsFlux = new float[spectraLength];
            
            for (int i = 1; i < spectraLength; i++)
            {
                Spectrum prev =  spectrogram.Spectra[i - 1];
                Spectrum curr = spectrogram.Spectra[i];
                
                int binIndexPointer = 0;
                
                // evaluate kick region
                EvaluateRegionFlux(ref m_KicksFlux,  kickBinsEndIndex, prev, curr, ref binIndexPointer, i);
                
                // evaluate snare region
                EvaluateRegionFlux(ref m_SnaresFlux,  snareBinsEndIndex, prev, curr, ref binIndexPointer, i);
                
                // evaluate hihat region
                EvaluateRegionFlux(ref m_HiHatsFlux,  hihatBinsEndIndex, prev, curr, ref binIndexPointer, i);
            }
            
            // 3. go over the total flux with local average creating onsets
            
            float[] averageThresholdsKicks = new float[m_KicksFlux.Length];
            float[] averageThresholdsSnares = new float[m_KicksFlux.Length];
            float[] averageThresholdsHiHats = new float[m_KicksFlux.Length];
            
            float averageEnergyInRegion = CalculateMedian(m_KicksFlux);
            float noiseFloor = averageEnergyInRegion * m_NoiseFloorMultiplier;
            int excludeWindowInAverage = 1;

            m_KicksOnsets = CreateOnsets(ref m_KicksFlux, ref averageThresholdsKicks, spectraLength, noiseFloor,
                excludeWindowInAverage, spectra);
            
            m_SnaresOnsets = CreateOnsets(ref m_SnaresFlux, ref averageThresholdsSnares, spectraLength, noiseFloor,
                excludeWindowInAverage, spectra);
            
            m_HiHatsOnsets = CreateOnsets(ref m_HiHatsFlux, ref averageThresholdsHiHats, spectraLength, noiseFloor,
                excludeWindowInAverage, spectra);

            Flux kickFlux = new Flux("Kicks", m_KicksFlux, averageThresholdsKicks, m_KicksOnsets, noiseFloor, spectrogram.HopSize);
            Flux snareFlux = new Flux("Snares", m_SnaresFlux, averageThresholdsSnares, m_SnaresOnsets, noiseFloor, spectrogram.HopSize);
            Flux hihatFlux = new Flux("HiHats", m_HiHatsFlux, averageThresholdsHiHats, m_HiHatsOnsets, noiseFloor, spectrogram.HopSize);
            
            result.Add(kickFlux);
            result.Add(snareFlux);
            result.Add(hihatFlux);

            return result;
        }

        private List<int> CreateOnsets(ref float[] fluxArray, ref float[] averageThresholds, int spectraLength,
            float noiseFloor, int excludeWindowInAverage, Spectrum[] spectra)
        {
            List<int> onsets = new List<int>();
            
            int halfWindowSize = (int)(m_FluxTimelineWindowSize * 0.5f);
            
            for (int i = 0; i < spectraLength; i++)
            {
                if (fluxArray[i] < noiseFloor)
                {
                    continue;
                }
                
                int leftPointer = Mathf.Max(0, i - halfWindowSize);
                int rightPointer =  Mathf.Min(i + halfWindowSize, fluxArray.Length - 1);

                float localAverageThreshold = CalculateLocalAverage(leftPointer,  rightPointer, i, fluxArray,
                    excludeWindowInAverage, averageThresholds);

                // if it passes the threshold we validate its not local maxima
                if (fluxArray[i] > localAverageThreshold)
                {
                    if (i > 0 && i < fluxArray.Length - 1)
                    {
                        float leftValue = fluxArray[i - 1];
                        float rightValue = fluxArray[i + 1];
                        
                        if (fluxArray[i] > leftValue && fluxArray[i] > rightValue)
                        {
                            // it's a local maximum, add it to onset
                            onsets.Add(spectra[i].StartingSample);
                        }
                    }
                    else
                    {
                        if (i == 0)
                        {
                            float rightValue = fluxArray[i + 1];
                            
                            if (fluxArray[i] > rightValue)
                            {
                                // it's a local maximum, add it to onset
                                onsets.Add(spectra[i].StartingSample);

                                continue;
                            }
                        }

                        if (i == fluxArray.Length - 1)
                        {
                            float leftValue = fluxArray[i - 1];
                            
                            if (fluxArray[i] > leftValue)
                            {
                                // it's a local maximum, add it to onset
                                onsets.Add(spectra[i].StartingSample);
                            }
                        }
                    }
                }
            }

            return onsets;
        }

        private float CalculateLocalAverage(int leftPointer, int rightPointer, int fluxIndex,
            float[] fluxArray, int excludeWindowInAverage, float[] averageThresholds)
        {
            float localAverageThreshold = 0;
            int windowElementCount = rightPointer - leftPointer + 1;
            
            for (int j = leftPointer; j < rightPointer + 1; j++)
            {
                // exclude the current position itself and surrounding area
                if (j < fluxIndex - excludeWindowInAverage || j > fluxIndex + excludeWindowInAverage)
                {
                    localAverageThreshold += fluxArray[j];
                }
            }

            // average the window - subtract the exclude surrounding area so it won't affect the locale average
            localAverageThreshold /= (windowElementCount - ((excludeWindowInAverage * 2) + 1));
                
            // add multiplier
            localAverageThreshold *= m_ThresholdSensitivityMultiplier;
                
            averageThresholds[fluxIndex] = localAverageThreshold;
            
            return localAverageThreshold;
        }

        public void SetParameters(FluxCreatorParameters parameters)
        {
            m_ThresholdSensitivityMultiplier =  parameters.ThresholdSensitivityMultiplier;
            m_NoiseFloorMultiplier = parameters.NoiseFloorMultiplier;
            m_FluxTimelineWindowSize  = parameters.FluxTimelineWindowSize;
        }

        private void EvaluateRegionFlux(ref float[] instrument, int binsEndIndex, Spectrum prev,Spectrum curr,
                ref int binIndexPointer, int spectrumIndex)
        {
            float regionDeltaSum = 0f;
            
            while (binIndexPointer < binsEndIndex)
            {
                float delta = BinDelta(prev, curr, binIndexPointer);
                regionDeltaSum += delta;
                binIndexPointer++;
            }

            instrument[spectrumIndex] = regionDeltaSum;
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
            m_KicksFlux = null;
            m_SnaresFlux = null;
            m_HiHatsFlux = null;
            
            m_KicksOnsets.Clear();
            m_SnaresOnsets.Clear();
            m_HiHatsOnsets.Clear();
        }
    }
}