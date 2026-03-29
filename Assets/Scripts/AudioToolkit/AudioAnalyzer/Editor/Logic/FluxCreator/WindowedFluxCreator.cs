using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class WindowedFluxCreator : IFluxCreator
    {
        private readonly int m_MinFrequency;
        private readonly int m_MaxFrequency;
        // flux data
        private List<int> m_Onsets;
        private float[] m_FluxData;
        
        public WindowedFluxCreator(int minFrequency, int maxFrequency)
        {
            m_MinFrequency = minFrequency;
            m_MaxFrequency = maxFrequency;
            
            m_Onsets = new List<int>();
        }
        
        public FluxResult CreateFlux(string fluxID, Spectrogram spectrogram)
        {
            if (spectrogram == null || spectrogram.Spectra == null || spectrogram.Spectra.Length == 0)
            {
                Debug.LogWarning("No Spectrogram specified for Multiband FluxCreator");
                return null;
            }

            Reset();
            
            FluxResult result = new FluxResult();
            
            result.ID = fluxID;
            result.FrequencyWindowMin = m_MinFrequency;
            result.FrequencyWindowMax = m_MaxFrequency;
            result.FluxCreatorParameters = CreateParameters();
            
            return UpdateFlux(spectrogram, result);
        }

        public FluxResult UpdateFlux(Spectrogram spectrogram, FluxResult sourceFlux)
        {
            // 1. separate bins into instruments
            FluxCreatorParameters parameters = sourceFlux.FluxCreatorParameters;
            Spectrum[] spectra = spectrogram.Spectra;
            float sampleRate = spectrogram.SampleRate;
            int spectraLength = spectra.Length;
            int binsLength = spectra[0].Bins.Length;
            int fftSize = binsLength * 2;

            int binsStartIndex = GetBinIndex(m_MinFrequency, sampleRate, fftSize);
            int binsEndIndex = GetBinIndex(m_MaxFrequency, sampleRate, fftSize);
            
            m_FluxData = new float[spectraLength];
            
            for (int i = 1; i < spectraLength; i++)
            {
                Spectrum prev =  spectrogram.Spectra[i - 1];
                Spectrum curr = spectrogram.Spectra[i];
                
                // evaluate kick region
                m_FluxData[i] = EvaluateRegionFlux(binsStartIndex, binsEndIndex, prev, curr);
            }
            
            // 3. go over the total flux with local average creating onsets
            
            float[] averageThresholds = new float[m_FluxData.Length];
            
            float averageEnergyInRegion = CalculateMedian(m_FluxData);
            float noiseFloor = averageEnergyInRegion * parameters.NoiseFloorMultiplier;
            int excludeWindowInAverage = 1;
            
            m_Onsets = CreateOnsets(ref m_FluxData, ref averageThresholds, spectraLength, noiseFloor,
                excludeWindowInAverage, spectra, parameters);
            
            Flux flux = new Flux(m_FluxData,  averageThresholds, m_Onsets, noiseFloor, spectrogram.HopSize);
            
            sourceFlux.Flux = flux;

            return sourceFlux;
        }
        
        private List<int> CreateOnsets(ref float[] fluxArray, ref float[] averageThresholds, int spectraLength,
            float noiseFloor, int excludeWindowInAverage, Spectrum[] spectra, FluxCreatorParameters parameters)
        {
            List<int> onsets = new List<int>();
            
            int halfWindowSize = (int)(parameters.FluxTimelineWindowSize * 0.5f);
            
            for (int i = 0; i < spectraLength; i++)
            {
                if (fluxArray[i] < noiseFloor)
                {
                    continue;
                }
                
                int leftPointer = Mathf.Max(0, i - halfWindowSize);
                int rightPointer =  Mathf.Min(i + halfWindowSize, fluxArray.Length - 1);

                float localAverageThreshold = CalculateLocalAverage(leftPointer,  rightPointer, i, fluxArray,
                    excludeWindowInAverage, averageThresholds, parameters);

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
            float[] fluxArray, int excludeWindowInAverage, float[] averageThresholds, FluxCreatorParameters parameters)
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
            localAverageThreshold *= parameters.ThresholdSensitivityMultiplier;
                
            averageThresholds[fluxIndex] = localAverageThreshold;
            
            return localAverageThreshold;
        }
        
        private float EvaluateRegionFlux(int binsStartIndex, int binsEndIndex,
            Spectrum prev,Spectrum curr)
        {
            float regionDeltaSum = 0f;

            for (int i = binsStartIndex; i < binsEndIndex; i++)
            {
                float delta = BinDelta(prev, curr, i);
                regionDeltaSum += delta;
            }

            return regionDeltaSum;
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

        private FluxCreatorParameters CreateParameters()
        {
            FluxCreatorParameters newParameters = new FluxCreatorParameters();
            
            newParameters.ThresholdSensitivityMultiplier = 1.65f;
            newParameters.NoiseFloorMultiplier = 110f;
            newParameters.FluxTimelineWindowSize = 20;

            return newParameters;
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
            m_FluxData = null;
            m_Onsets.Clear();
        }
    }
}

