using System;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    internal class Orchestrator
    {
        private readonly IAudioAnalyzer m_AudioAnalyzer;
        private readonly IFluxCreator m_FluxCreator;
        
        private Spectrogram m_Spectrogram;
        private Signal m_Signal;
        
        private string m_AudioPath;
        
        internal Orchestrator()
        {
            m_AudioAnalyzer = new AudioAnalyzer();
            m_FluxCreator = new MultibandFluxCreator();
        }

        internal Signal ParseAudio(string audioPath = null, bool normalized = true)
        {
            if (string.IsNullOrEmpty(audioPath))
            {
                if (m_AudioPath != null)
                {
                    m_Signal = m_AudioAnalyzer.ParseAudio(m_AudioPath);
                }
                else
                {
                    Debug.Log("Orchestrator: No audio path provided");
                    return null;
                }
            }
            else
            {
                UpdateAudioPath(audioPath);
                
                m_Signal = m_AudioAnalyzer.ParseAudio(audioPath);
            }

            if (normalized)
            {
                NormalizeSignal(m_Signal);
            }
            
            return m_Signal;
        }

        internal Spectrogram AnalyzeAudio(Signal signal = null)
        {
            if (signal == null)
            {
                if (m_Signal != null)
                {
                    m_Spectrogram = m_AudioAnalyzer.Analyze(m_Signal);
                }
            }
            else
            {
                m_Spectrogram = m_AudioAnalyzer.Analyze(signal);
            }
            
            return m_Spectrogram;
        }
        
        private void NormalizeSignal(Signal signal)
        {
            m_AudioAnalyzer.NormalizeSignal(signal);
        }
        
        internal void UpdateAudioPath(string audioPath)
        {
            m_AudioPath = audioPath;
        }

        internal void Reset()
        {
            m_Signal = null;
            m_Spectrogram = null;
            m_AudioPath = null;
        }
    }
}