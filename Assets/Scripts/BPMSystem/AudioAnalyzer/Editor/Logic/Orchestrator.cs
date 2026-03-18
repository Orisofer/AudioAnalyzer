using System;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    internal class Orchestrator
    {
        private readonly IAudioAnalyzer m_AudioAnalyzer;
        
        private Spectrogram m_Spectrogram;
        private Signal m_Signal;
        
        private string m_AudioPath;
        
        internal Orchestrator()
        {
            m_AudioAnalyzer = new AudioAnalyzer();
        }

        internal Signal ParseAudio(string audioPath)
        {
            if (string.IsNullOrEmpty(m_AudioPath))
            {
                Debug.Log("Orchestrator: No audio path provided");
                return null;
            }
            
            m_Signal = m_AudioAnalyzer.ParseAudio(audioPath);
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