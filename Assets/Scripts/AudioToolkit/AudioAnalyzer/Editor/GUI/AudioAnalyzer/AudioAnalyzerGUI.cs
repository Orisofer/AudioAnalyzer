using System;
using System.Collections.Generic;
using Ori.AudioAnalyzer.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ori.AudioAnalyzer.Editor
{
    public class AudioAnalyzerGUI : EditorWindow
    {
        private readonly string[] AUDIO_EXTENSIONS = { "Audio-Files", "mp3,ogg,wav" };
        
        private const string UXML_FILE_NAME = "AudioAnalyzerXML.uxml";
        private const string USS_FILE_NAME = "AudioAnalyzerUSS.uss";
        private const string NO_AUDIO_TEXT = "No Audio File";

        private Orchestrator m_Orchestrator;
        private WaveformView m_WaveformView;
        private FluxView m_FluxView;

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
    
        [MenuItem("Tools/AudioAnalyzer/AudioAnalyzerWindow")]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioAnalyzerGUI>();
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
            CreateWaveformView();
            CreateFluxVisualizer();
            AddListeners();
            CreateOrchestrator();
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
            
            // 1. Tell it to take up all available space in the container
            m_WaveformView.style.flexGrow = 1;
    
            // 2. Or, explicitly match the parent's height
            m_WaveformView.style.height = Length.Percent(100);
            m_WaveformView.style.width = Length.Percent(100);
            
            m_WaveformViewSection.Add(m_WaveformView);
        }
        
        private void CreateFluxVisualizer()
        {
            m_FluxView = new FluxView();
            
            m_FluxView.style.flexGrow = 1;
    
            m_FluxView.style.height = Length.Percent(100);
            m_FluxView.style.width = Length.Percent(100);
            
            m_FluxViewSection.Add(m_FluxView);
        }
        
        private void AddListeners()
        {
            m_LoadAudioButton.clicked += OnLoadAudioFilePressed;
            m_RemoveAudioButton.clicked += OnRemoveAudioClicked;
            m_AnalyzeAudioButton.clicked += OnAnalyzeAudioClicked;
            m_CreateFluxButton.clicked += OnCreateFluxButtonClicked;
        }
        
        private void CreateOrchestrator()
        {
            m_Orchestrator = new Orchestrator();
        }
        
        private void OnCreateFluxButtonClicked()
        {
            try
            {
                List<Flux> fluxes = m_Orchestrator.CreateFlux();
                
                m_FluxView.UpdateData(fluxes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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
                Console.WriteLine(e);
                throw;
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
                Console.WriteLine(e);
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
            }
        }
        
        private void DisplayNoAudioLoadedState()
        {
            m_LoadAudioSection.style.display = DisplayStyle.Flex;
            
            m_AudioLoadedSection.style.display = DisplayStyle.None;
            m_AudioAnalyzedSection.style.display = DisplayStyle.None;
        }
        
        private void DisplayAudioLoadedState()
        {
            m_AudioLoadedSection.style.display = DisplayStyle.Flex;
            
            m_LoadAudioSection.style.display = DisplayStyle.None;
            m_AudioAnalyzedSection.style.display = DisplayStyle.None;
        }

        private void DisplayAudioAnalyzedState()
        {
            m_AudioAnalyzedSection.style.display = DisplayStyle.Flex;
            m_AudioLoadedSection.style.display = DisplayStyle.Flex;
            
            m_LoadAudioSection.style.display = DisplayStyle.None;
        }
        
        private void RemoveListeners()
        {
            m_LoadAudioButton.clicked -= OnLoadAudioFilePressed;
            m_RemoveAudioButton.clicked -= OnRemoveAudioClicked;
            m_AnalyzeAudioButton.clicked -= OnAnalyzeAudioClicked;
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

