using System.Collections.Generic;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class Flux
    {
        private readonly List<int> m_Onsets;
        private readonly float[] m_FluxData;
        private readonly float[] m_AverageThresholds;
        private readonly int m_HopSize;
        private readonly string m_Id;
        
        public List<int>  Onsets => m_Onsets;
        public float[]  FluxData => m_FluxData;
        public float[]  AverageThresholds => m_AverageThresholds;
        public float  HopSize => m_HopSize;
        public string Id => m_Id;

        public Flux(string id, float[] fluxData, float[] averageThresholds, List<int> onsets, int hopSize)
        {
            m_Onsets = onsets;
            m_FluxData = fluxData;
            m_AverageThresholds  = averageThresholds;
            m_HopSize = hopSize;
            m_Id = id;
        }
    }
}

