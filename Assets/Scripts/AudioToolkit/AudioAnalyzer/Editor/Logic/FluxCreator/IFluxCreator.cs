using System.Collections.Generic;

namespace Ori.AudioAnalyzer.Core
{
    public interface IFluxCreator
    {
        public FluxResult CreateFlux(string fluxID, Spectrogram spectrogram);

        public FluxResult UpdateFlux(Spectrogram spectrogram, FluxResult source);
    }
}

