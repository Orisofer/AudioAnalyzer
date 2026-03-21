using UnityEngine;

namespace Ori.AudioAnalyzer.Settings
{
    [CreateAssetMenu(fileName =  "AudioToolkitSettings", menuName = "AudioToolkit/AudioToolkitSettings")]
    public class AudioToolkitSettings : ScriptableObject
    {
        [Header("Audio Analyzer")] [Space(20)]
        
        [Tooltip("FFT Size")]
        public int FFTSize = 2048;
        
        [Tooltip("Overlaps between frames")]
        public int HopSize = 1024;
        
        [Header("Flux Creator")] [Space(20)]
        
        [Tooltip("Threshold for passing flux peaks")]
        public float ThresholdSensitivityMultiplier = 1.65f;
        
        [Tooltip("Multiplier to raise threshold for eliminating background noises")]
        public float RegionAverageMultiplier = 110f;
        
        [Tooltip("Multiplier to raise threshold for eliminating background noises")]
        public int FluxTimelineWindowSize = 20;
    }
}
