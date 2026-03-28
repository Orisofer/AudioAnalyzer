using System;
using System.Collections.Generic;
using System.Linq;
using Ori.AudioAnalyzer.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ori.AudioAnalyzer.Editor.View
{
    public class AudioAnalyzerView : EditorWindow
    {
        private readonly string[] AUDIO_EXTENSIONS = { "Audio-Files", "mp3,ogg,wav" };
        
        private const string UXML_FILE_NAME = "AudioAnalyzerXML.uxml";
        private const string USS_FILE_NAME = "AudioAnalyzerUSS.uss";
        private const string NO_AUDIO_TEXT = "No Audio File";

        private Orchestrator m_Orchestrator;
        private WaveformView m_WaveformView;
        private FluxView m_FluxView;
        private List<FluxResult> m_FluxResults;

        private VisualTreeAsset m_VisualTree;
        private StyleSheet m_StyleSheet;
        private VisualElement m_HeaderSection;
        private VisualElement m_LoadAudioSection;
        private VisualElement m_AudioLoadedSection;
        private VisualElement m_AudioAnalyzedSection;
        private VisualElement m_WaveformViewSection;
        private VisualElement m_FluxViewSection;
        private Label m_HeaderLabel;
        private Label m_PathLabel;
        private Button m_LoadAudioButton;
        private Button m_RemoveAudioButton;
        private Button m_AnalyzeAudioButton;
        private Button m_CreateFluxButton;

        private AudioAnalyzerWindowState m_State;
        
        private string m_AudioPath;
        private int CurrentFluxIndex;
    
        [MenuItem("Tools/AudioAnalyzer/AudioAnalyzerWindow")]
        public static void ShowWindow()
        {
            Type inspectorType = System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
            
            AudioAnalyzerView window = GetWindow<AudioAnalyzerView>(inspectorType);
            
            window.titleContent = new GUIContent("Audio Analyzer");
        }

        private void CreateGUI()
        {
            var script = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string folderPath = System.IO.Path.GetDirectoryName(scriptPath);

            if (!string.IsNullOrEmpty(folderPath))
            {
                string xmlPath = folderPath + "/" + UXML_FILE_NAME;
                string ussPath = folderPath + "/" + USS_FILE_NAME;

                m_VisualTree = LoadXML(xmlPath);
                m_StyleSheet = LoadUSS(ussPath);
            }

            if (m_VisualTree == null || m_StyleSheet == null) return;

            QueryWindowElements();
            CreateOrchestrator();
            CreateWaveformView();
            CreateFluxVisualizer();
            AddListeners();
            UpdateAudioPath(m_AudioPath);

            ChangeState(AudioAnalyzerWindowState.NO_AUDIO_LOADED);
        }

        private void QueryWindowElements()
        {
            m_HeaderSection = rootVisualElement.Q<VisualElement>("HeaderSection");
            m_LoadAudioSection = rootVisualElement.Q<VisualElement>("LoadAudioSection");
            m_AudioLoadedSection = rootVisualElement.Q<VisualElement>("AudioLoadedSection");
            m_AudioAnalyzedSection = rootVisualElement.Q<VisualElement>("AudioAnalyzedSection");
            m_WaveformViewSection = rootVisualElement.Q<VisualElement>("WaveformViewSection");
            
            m_HeaderLabel = rootVisualElement.Q<Label>("HeaderLabel");
            m_PathLabel = rootVisualElement.Q<Label>("FilePathLabel");
            
            m_LoadAudioButton = rootVisualElement.Q<Button>("LoadAudioButton");
            m_RemoveAudioButton = rootVisualElement.Q<Button>("RemoveAudioButton");
            m_AnalyzeAudioButton = rootVisualElement.Q<Button>("AnalyzeButton");
            m_CreateFluxButton = rootVisualElement.Q<Button>("CreateFluxButton");
            m_FluxViewSection =  rootVisualElement.Q<VisualElement>("FluxViewSection");
        }
        
        private void CreateWaveformView()
        {
            m_WaveformView = new WaveformView();
            
            m_WaveformView.AddToClassList("waveform-view");
            
            m_WaveformViewSection.Add(m_WaveformView);
        }
        
        private void CreateFluxVisualizer()
        {
            m_FluxView = new FluxView();
            
            m_FluxView.AddToClassList("flux-view");
            
            m_FluxViewSection.Add(m_FluxView);

            m_FluxResults = new List<FluxResult>();
            CurrentFluxIndex = 0;
        }
        
        private void AddListeners()
        {
            m_LoadAudioButton.clicked += OnLoadAudioFilePressed;
            m_RemoveAudioButton.clicked += OnRemoveAudioClicked;
            m_AnalyzeAudioButton.clicked += OnAnalyzeAudioClicked;
            m_CreateFluxButton.clicked += OnCreateFluxButtonClicked;
            m_FluxView.FluxParametersUpdated += OnFluxParametersUpdated;
            m_FluxView.NavButtonClicked += OnFluxNavButtonClicked;
        }

        private void OnFluxNavButtonClicked(int dir)
        {
            int numFluxes = m_FluxResults.Count;
            
            int nextIndex = (CurrentFluxIndex + dir + numFluxes) % numFluxes;
            
            FluxResult toDisplay = m_FluxResults[nextIndex];
            
            m_FluxView.UpdateData(toDisplay);
            m_FluxView.CurrentFluxIndex = nextIndex;
        }

        private void CreateOrchestrator()
        {
            m_Orchestrator = new Orchestrator();
        }
        
        private void OnCreateFluxButtonClicked()
        {
            try
            {
                Dictionary<string, FluxResult> fluxesDic = m_Orchestrator.CreateFluxes();
                
                m_FluxResults.Clear();

                foreach (KeyValuePair<string, FluxResult> kvp in fluxesDic)
                {
                    m_FluxResults.Add(kvp.Value);
                }
                
                m_FluxView.UpdateData(m_FluxResults[CurrentFluxIndex]);
                
                ChangeState(AudioAnalyzerWindowState.FLUX_CREATED);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        private void OnFluxParametersUpdated(string fluxKey, FluxCreatorParameters newParameters, bool recalculate)
        {
            m_Orchestrator.UpdateFluxParameters(fluxKey, newParameters);

            if (recalculate)
            {
                FluxResult result = m_Orchestrator.UpdateFlux(fluxKey);
                
                m_FluxView.UpdateData(result);
            }
        }

        private void OnAnalyzeAudioClicked()
        {
            try
            {
                Spectrogram spectrogram = m_Orchestrator.AnalyzeAudio();

                if (spectrogram != null)
                {
                    ChangeState(AudioAnalyzerWindowState.AUDIO_ANALYZED);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnRemoveAudioClicked()
        {
            m_AudioPath = null;
            
            UpdateAudioPath(m_AudioPath);
            
            ChangeState(AudioAnalyzerWindowState.NO_AUDIO_LOADED);
        }

        private void OnLoadAudioFilePressed()
        {
            string audioPath = EditorUtility.OpenFilePanelWithFilters("Audio Analyzer","/Users/orisofer/Desktop", AUDIO_EXTENSIONS);

            if (string.IsNullOrEmpty(audioPath))
            {
                return;
            }

            try
            {
                UpdateAudioPath(audioPath);
                
                Signal signal = m_Orchestrator.ParseAudio(audioPath);
                
                DrawWaveform(signal);
                
                ChangeState(AudioAnalyzerWindowState.AUDIO_LOADED);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ChangeState(AudioAnalyzerWindowState.NO_AUDIO_LOADED);
            }
        }

        private void DrawWaveform(Signal signal)
        {
            m_WaveformView.SetSignal(signal);
        }

        private VisualTreeAsset LoadXML(string path)
        {
            VisualTreeAsset root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);

            if (root == null)
            {
                Debug.Log("XML root wasnt found");
                return null;
            }
            
            VisualElement rootFromUxml = root.Instantiate();
            rootVisualElement.Add(rootFromUxml);
            
            return root;
        }
        
        private StyleSheet LoadUSS(string ussPath)
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            
            if (styleSheet == null)
            {
                Debug.Log("USS root wasnt found");
                return null;
            }
            
            rootVisualElement.styleSheets.Add(styleSheet);
            
            return styleSheet;
        }
        
        private void ChangeState(AudioAnalyzerWindowState state)
        {
            switch (state)
            {
                case AudioAnalyzerWindowState.NO_AUDIO_LOADED:
                    DisplayNoAudioLoadedState();
                    break;
                case AudioAnalyzerWindowState.AUDIO_LOADED:
                    DisplayAudioLoadedState();
                    break;
                case AudioAnalyzerWindowState.AUDIO_ANALYZED:
                    DisplayAudioAnalyzedState();
                    break;
                case AudioAnalyzerWindowState.FLUX_CREATED:
                    DisplayFluxCreatedState();
                    break;
            }
        }
        
        private void DisplayNoAudioLoadedState()
        {
            m_LoadAudioSection.style.display = DisplayStyle.Flex;
            
            m_AudioLoadedSection.style.display = DisplayStyle.None;
            m_AudioAnalyzedSection.style.display = DisplayStyle.None;
            m_FluxViewSection.style.display = DisplayStyle.None;
        }
        
        private void DisplayAudioLoadedState()
        {
            m_AudioLoadedSection.style.display = DisplayStyle.Flex;
            
            m_LoadAudioSection.style.display = DisplayStyle.None;
            m_AudioAnalyzedSection.style.display = DisplayStyle.None;
            m_FluxViewSection.style.display = DisplayStyle.None;
        }

        private void DisplayAudioAnalyzedState()
        {
            m_AudioAnalyzedSection.style.display = DisplayStyle.Flex;
            m_AudioLoadedSection.style.display = DisplayStyle.Flex;
            
            m_LoadAudioSection.style.display = DisplayStyle.None;
            m_FluxViewSection.style.display = DisplayStyle.None;
        }

        private void DisplayFluxCreatedState()
        {
            m_AudioAnalyzedSection.style.display = DisplayStyle.Flex;
            m_AudioLoadedSection.style.display = DisplayStyle.Flex;
            m_FluxViewSection.style.display = DisplayStyle.Flex;
            
            m_LoadAudioSection.style.display = DisplayStyle.None;
        }
        
        private void RemoveListeners()
        {
            m_LoadAudioButton.clicked -= OnLoadAudioFilePressed;
            m_RemoveAudioButton.clicked -= OnRemoveAudioClicked;
            m_AnalyzeAudioButton.clicked -= OnAnalyzeAudioClicked;
            m_CreateFluxButton.clicked -= OnCreateFluxButtonClicked;
        }
        
        private void UpdateAudioPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                m_AudioPath = path;
                m_Orchestrator.UpdateAudioPath(path);
                m_PathLabel.text = path;
            }
            else
            {
                m_PathLabel.text = NO_AUDIO_TEXT;
            }
        }


        private void OnDisable()
        {
            RemoveListeners();
        }
    }
}

