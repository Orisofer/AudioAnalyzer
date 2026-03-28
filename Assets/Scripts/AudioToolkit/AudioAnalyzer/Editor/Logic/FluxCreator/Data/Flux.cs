using System.Collections.Generic;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class Flux
    {
        private readonly List<int> m_Onsets;
        private readonly float[] m_FluxData;
        private readonly float[] m_AverageThresholds;
        private readonly float m_NoiseFloor;
        private readonly int m_HopSize;
        
        public List<int>  Onsets => m_Onsets;
        public float[]  FluxData => m_FluxData;
        public float[]  AverageThresholds => m_AverageThresholds;
        public float NoiseFloor => m_NoiseFloor;
        public float  HopSize => m_HopSize;

        public Flux(float[] fluxData, float[] averageThresholds, List<int> onsets, float noiseFloor, int hopSize)
        {
            m_Onsets = onsets;
            m_FluxData = fluxData;
            m_NoiseFloor  = noiseFloor;
            m_AverageThresholds  = averageThresholds;
            m_HopSize = hopSize;
        }
    }
}

