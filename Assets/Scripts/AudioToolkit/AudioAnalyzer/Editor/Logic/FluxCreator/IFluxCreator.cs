using System.Collections.Generic;

namespace Ori.AudioAnalyzer.Core
{
    public interface IFluxCreator
    {
        public List<Flux> CreateFlux(Spectrogram spectrogram);
        
        public void SetParameters(FluxCreatorParameters parameters);
    }
}

