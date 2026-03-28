using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ori.AudioAnalyzer.Core
{
    internal class Orchestrator
    {
        private const string KICK_FLUX_ID = "Kick";
        private const string SNARE_FLUX_ID = "Snare";
        private const string HIHAT_FLUX_ID = "Hihat";
        
        private const int KICK_FREQUENCY_MIN = 40;
        private const int KICK_FREQUENCY_MAX = 150;
        
        private const int SNARE_FREQUENCY_MIN = 150;
        private const int SNARE_FREQUENCY_MAX = 2000;
        
        private const int HIHAT_FREQUENCY_MIN = 2000;
        private const int HIHAT_FREQUENCY_MAX = 15000;
        
        private readonly IAudioAnalyzer m_AudioAnalyzer;
        private readonly IFluxCreator m_FluxCreator;
        
        private Spectrogram m_Spectrogram;
        private Dictionary<string, FluxResult> m_Fluxes;
        private Signal m_Signal;
        private string m_AudioPath;
        
        public Dictionary<string, FluxResult> Fluxes => m_Fluxes;
        
        internal Orchestrator()
        {
            m_AudioAnalyzer = new AudioAnalyzer();
            m_Fluxes = new Dictionary<string, FluxResult>();
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

        internal Dictionary<string, FluxResult> CreateFluxes(Spectrogram spectrogram = null)
        {
            m_Fluxes.Clear();
            
            IFluxCreator fluxCreatorBass = new WindowedFluxCreator(KICK_FREQUENCY_MIN, KICK_FREQUENCY_MAX);
            IFluxCreator fluxCreatorSnare = new WindowedFluxCreator(SNARE_FREQUENCY_MIN , SNARE_FREQUENCY_MAX);
            IFluxCreator fluxCreatorHihat = new WindowedFluxCreator(HIHAT_FREQUENCY_MIN, HIHAT_FREQUENCY_MAX);

            FluxResult fluxBass = new FluxResult();
            FluxResult fluxSnare = new FluxResult();
            FluxResult fluxHihat = new FluxResult();
            
            if (spectrogram == null)
            {
                if (m_Spectrogram != null)
                {
                    fluxBass = fluxCreatorBass.CreateFlux(KICK_FLUX_ID, m_Spectrogram);
                    fluxSnare = fluxCreatorSnare.CreateFlux(SNARE_FLUX_ID, m_Spectrogram);
                    fluxHihat = fluxCreatorHihat.CreateFlux(HIHAT_FLUX_ID, m_Spectrogram);
                }
            }
            else
            {
                fluxBass = fluxCreatorBass.CreateFlux(KICK_FLUX_ID, spectrogram);
                fluxSnare = fluxCreatorSnare.CreateFlux(SNARE_FLUX_ID, spectrogram);
                fluxHihat = fluxCreatorHihat.CreateFlux(HIHAT_FLUX_ID, spectrogram);
            }
            
            m_Fluxes.Add(fluxBass.ID, fluxBass);
            m_Fluxes.Add(fluxSnare.ID, fluxSnare);
            m_Fluxes.Add(fluxHihat.ID, fluxHihat);

            return m_Fluxes;
        }
        
        private void NormalizeSignal(Signal signal)
        {
            m_AudioAnalyzer.NormalizeSignal(signal);
        }
        
        internal void UpdateAudioPath(string audioPath)
        {
            m_AudioPath = audioPath;
        }

        internal void UpdateFluxParameters(string fluxKey, FluxCreatorParameters parameters)
        {
            if (m_Fluxes.TryGetValue(fluxKey, out FluxResult fluxResult))
            {
                fluxResult.FluxCreatorParameters = parameters;
            }
        }
        
        internal FluxResult UpdateFlux(string fluxKey)
        {
            if (m_Fluxes.TryGetValue(fluxKey, out FluxResult fluxResult))
            {
                fluxResult = m_FluxCreator.UpdateFlux(fluxResult);
                
                return fluxResult;
            }
            
            Debug.LogError("Could not find flux for " + fluxKey);

            return null;
        }

        internal void Reset()
        {
            m_Signal = null;
            m_Spectrogram = null;
            m_AudioPath = null;
        }
    }
}