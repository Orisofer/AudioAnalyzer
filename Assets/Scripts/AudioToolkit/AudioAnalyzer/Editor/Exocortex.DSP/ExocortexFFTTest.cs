using UnityEngine;
using UnityEditor;
using Exocortex.DSP;

namespace Ori.Audio.AudioAnalyzer.Editor
{
    public static class ExocortexFFTTest
    {
        [MenuItem("Tools/AudioAnalyzer/Tests/Run Exocortex Test")]
        public static void RunTest()
        {
            Debug.Log("=== Exocortex FFT Test Start ===");

            int size = 1024;                    // FFT size
            float frequency = 440f;             // Generate 440Hz test tone
            float sampleRate = 44100f;
        
            ComplexF[] complex = new ComplexF[size];

            // Create a clean sine wave so FFT has something obvious to detect
            for (int i = 0; i < size; i++)
            {
                var real = Mathf.Sin(2f * Mathf.PI * frequency * (i / sampleRate));
                var imag = 0f;
            
                complex[i] = new ComplexF(real, imag);
            }

            // Run FFT
            Fourier.FFT(complex, FourierDirection.Forward);

            // Find the strongest frequency bin
            int strongestIndex = 0;
            float strongestMagnitude = 0f;

            for (int i = 0; i < size / 2; i++)   // only look at positive frequencies
            {
                float mag = Mathf.Sqrt(complex[i].Re * complex[i].Re + complex[i].Im * complex[i].Im);
                if (mag > strongestMagnitude)
                {
                    strongestMagnitude = mag;
                    strongestIndex = i;
                }
            }

            // Convert bin index → Hz
            float detectedFrequency = (strongestIndex * sampleRate) / size;

            Debug.Log($"Strongest frequency bin: {strongestIndex}");
            Debug.Log($"Detected frequency: {detectedFrequency} Hz (expected ~440 Hz)");
            Debug.Log("=== Exocortex FFT Test Complete ===");
        }
    }
}