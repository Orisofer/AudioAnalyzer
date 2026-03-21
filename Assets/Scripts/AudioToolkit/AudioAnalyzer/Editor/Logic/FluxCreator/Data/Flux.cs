using System.Collections.Generic;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class Flux
    {
        private readonly List<int> m_Onsets;
        private readonly float[] m_FluxData;
        private readonly float m_FluxTotalSum;
        private readonly float m_MedianEnergy;
        private readonly string m_Id;
        
        public List<int>  Onsets => m_Onsets;
        public float[]  FluxData => m_FluxData;
        public float  FluxTotalSum => m_FluxTotalSum;
        public float  MedianEnergy => m_MedianEnergy;
        public string Id => m_Id;

        public Flux(string id, float[] fluxData, List<int> onsets, float fluxTotalSum, float medianEnergy)
        {
            m_FluxData = fluxData;
            m_Onsets = onsets;
            m_FluxTotalSum = fluxTotalSum;
            m_MedianEnergy = medianEnergy;
            m_Id = id;
        }
    }
}

