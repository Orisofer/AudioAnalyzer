using System;
using System.IO;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    public class AudioAnalyzer : IAudioAnalyzer
    {
        private const int SIGNAL_FRAMES_SIZE = 2048;
        private const int SIGNAL_FRAMES_HOP_SIZE = 1024;

        public Signal ParseAudio(string audioPath)
        {
            // open File
            byte[] audioBytes = File.ReadAllBytes(audioPath);
            
            // get file format
            string audioExtension = Path.GetExtension(audioPath);

            // allocate a decoder based on format
            IAudioDecoder decoder = GetDecoder(audioExtension, audioBytes);
            
            // decode and get a signal
            Signal signal = decoder.Decode();

            return signal;
        }

        public Spectrogram Analyze(Signal signal)
        {
            // normalize signal
            signal = NormalizeSignal(signal);

            // create signal frames
            Frame[] frames = CreateSignalFrames(signal);

            // allocate window function algorithm
            IWindowFunction windowed = new WindowFunctionHann(frames, SIGNAL_FRAMES_SIZE);

            // window frames for smoother transients
            frames = windowed.Window();

            // allocate fft client - currently have only Exocortex
            IFFTProcessor fftProcessor = new ExocortexFFTClient(
                frames, SIGNAL_FRAMES_SIZE,
                signal.SampleRate,
                SIGNAL_FRAMES_HOP_SIZE
                );
            
            // process fft
            Spectrogram spectrogram = fftProcessor.Process();
            
            // debug for printing the strongest frequencies in each spectrum
            PrintStrongestFrequencyInSpectra(spectrogram);
            
            return spectrogram;
        }

        private Frame[] CreateSignalFrames(Signal signal)
        {
            int frameNumbers = ((signal.Samples.Length - SIGNAL_FRAMES_SIZE) / SIGNAL_FRAMES_HOP_SIZE) + 1;
            
            Frame[] frames = new Frame[frameNumbers];

            for (int i = 0; i < frames.Length; i++)
            {
                float[] frameSamples = new float[SIGNAL_FRAMES_SIZE];
                int startIndex = i * SIGNAL_FRAMES_HOP_SIZE;

                for (int j = 0; j < SIGNAL_FRAMES_SIZE; j++)
                {
                    frameSamples[j] = signal[startIndex + j];
                }
                
                frames[i] = new Frame
                {
                    Index = i,
                    StartingSample = i * SIGNAL_FRAMES_HOP_SIZE,
                    Samples = frameSamples
                };
            }
            
            return frames;
        }

        private Signal NormalizeSignal(Signal signal)
        {
            float dc = 0f;
            float maxAmplitude = 0f;

            for (int i = 0; i < signal.Samples.Length; i++)
            {
                dc += signal[i];
                maxAmplitude = Math.Max(maxAmplitude, Math.Abs(signal[i]));
            }

            if (maxAmplitude == 0)
            {
                throw new Exception("Audio Analyzer: Signal of zero amplitude, nothing to analyze.");
            }
            
            float avgDc = dc / signal.Samples.Length;
            
            for (int i = 0; i < signal.Samples.Length; i++)
            {
                signal[i] -= avgDc;
                signal[i] /= maxAmplitude;
            }

            return signal;
        }

        private IAudioDecoder GetDecoder(string audioExtension, byte[] audioBytes)
        {
            IAudioDecoder decoder = null;

            switch (audioExtension)
            {
                case ".wav":
                    decoder = new WAVDecoder(audioBytes);
                    break;
                case ".mp3":
                    decoder = new MP3Decoder(audioBytes);
                    break;
                default:
                    throw new Exception("Unsupported audio format: " + audioExtension);
            }
            
            return decoder;
        }

        private void PrintStrongestFrequencyInSpectra(Spectrogram spectrogram)
        {
            for (int i = 0; i < spectrogram.Spectra.Length; i++)
            {
                Spectrum s = spectrogram.Spectra[i];
                
                Debug.Log($"strongest freq in spectrum {i} is: {s.StrongestFrequency()}");
            }
        }
    }
}

