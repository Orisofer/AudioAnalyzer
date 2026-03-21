using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public struct FluxCreatorParameters
    {
        public float ThresholdSensitivityMultiplier;
        public float RegionAverageEnergyMultiplier;
        public int FluxTimelineWindowSize;
    }
}
