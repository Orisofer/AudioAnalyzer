
using System.Collections.Generic;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    internal class FluxManager
    {
        private List<Flux> m_FluxData;
        
        internal List<Flux> FluxData => m_FluxData;

        internal FluxManager()
        {
            m_FluxData  = new List<Flux>();
        }

        internal void SetFluxes(List<Flux> fluxes)
        {
            m_FluxData.Clear();
            
            m_FluxData  = fluxes;
        }
    }
}
