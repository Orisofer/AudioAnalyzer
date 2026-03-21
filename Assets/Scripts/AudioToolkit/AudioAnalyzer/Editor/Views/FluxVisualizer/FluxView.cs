using System;
using System.Collections.Generic;
using Ori.AudioAnalyzer.Core;
using UnityEditor.UIElements; // Required for SliderInt and Slider
using UnityEngine;
using UnityEngine.UIElements;

namespace Ori.AudioAnalyzer.Editor
{
    public class FluxView : VisualElement
    {
        private float[] m_FluxData;
        private List<int> m_Onsets;
        private float m_MedianEnergy;

        // Visual Settings
        private readonly Color m_FluxColor = Color.cyan;
        private readonly Color m_ThresholdColor = Color.red;
        private readonly Color m_OnsetColor = Color.yellow;
        private readonly float m_LineWidth = 1.2f;

        // UI Elements
        private VisualElement m_GraphArea;
        private VisualElement m_ControlArea;
        
        // Current Parameters State
        private FluxCreatorParameters m_CurrentParameters;

        public event Action<FluxCreatorParameters> FluxParametersUpdated;

        public FluxView()
        {
            // Initialize default parameters to match your algorithm's defaults
            m_CurrentParameters = new FluxCreatorParameters
            {
                ThresholdSensitivityMultiplier = 1.65f,
                RegionAverageEnergyMultiplier = 110f,
                FluxTimelineWindowSize = 20
            };

            SetupLayout();
            SetupGraphArea();
            SetupControlsArea();
        }

        private void SetupLayout()
        {
            // Make the main FluxView a flex column
            style.flexDirection = FlexDirection.Column;
            style.flexGrow = 1;
        }

        private void SetupGraphArea()
        {
            m_GraphArea = new VisualElement();
            m_GraphArea.style.flexGrow = 1; // Take up all available space
            m_GraphArea.style.minHeight = 150; // Ensure it doesn't collapse
            m_GraphArea.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Optional dark background
            
            // Subscribe to the drawing phase ON THE GRAPH AREA, not the main view
            m_GraphArea.generateVisualContent += OnGenerateVisualContent;
            
            Add(m_GraphArea);
        }

        private void SetupControlsArea()
        {
            m_ControlArea = new VisualElement();
            m_ControlArea.style.flexShrink = 0; // Don't let the controls get squished
            m_ControlArea.style.paddingTop = 10;
            m_ControlArea.style.paddingBottom = 10;

            // 1. Create Legend
            VisualElement legendContainer = new VisualElement();
            legendContainer.style.flexDirection = FlexDirection.Row;
            legendContainer.style.justifyContent = Justify.Center;
            legendContainer.style.marginBottom = 10;

            legendContainer.Add(CreateLegendItem(m_FluxColor, "Flux Energy"));
            legendContainer.Add(CreateLegendItem(m_ThresholdColor, "Local Threshold"));
            legendContainer.Add(CreateLegendItem(m_OnsetColor, "Detected Onsets"));

            // 2. Create Sliders
            Slider sensitivitySlider = new Slider("Sensitivity Mult.", 0.1f, 5.0f) { value = m_CurrentParameters.ThresholdSensitivityMultiplier };
            Slider regionEnergySlider = new Slider("Region Energy Mult.", 10f, 300f) { value = m_CurrentParameters.RegionAverageEnergyMultiplier };
            SliderInt windowSizeSlider = new SliderInt("Window Size", 5, 100) { value = m_CurrentParameters.FluxTimelineWindowSize };

            // 3. Bind Events
            sensitivitySlider.RegisterValueChangedCallback(evt => 
            {
                m_CurrentParameters.ThresholdSensitivityMultiplier = evt.newValue;
                NotifyParametersUpdated();
            });

            regionEnergySlider.RegisterValueChangedCallback(evt => 
            {
                m_CurrentParameters.RegionAverageEnergyMultiplier = evt.newValue;
                NotifyParametersUpdated();
            });

            windowSizeSlider.RegisterValueChangedCallback(evt => 
            {
                m_CurrentParameters.FluxTimelineWindowSize = evt.newValue;
                NotifyParametersUpdated();
            });

            m_ControlArea.Add(legendContainer);
            m_ControlArea.Add(sensitivitySlider);
            m_ControlArea.Add(regionEnergySlider);
            m_ControlArea.Add(windowSizeSlider);

            Add(m_ControlArea);
        }

        private VisualElement CreateLegendItem(Color color, string text)
        {
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginRight = 15;

            VisualElement dot = new VisualElement();
            dot.style.width = 10;
            dot.style.height = 10;
            dot.style.backgroundColor = color;
            dot.style.borderTopLeftRadius = 5;
            dot.style.borderTopRightRadius = 5;
            dot.style.borderBottomLeftRadius = 5;
            dot.style.borderBottomRightRadius = 5;
            dot.style.marginRight = 5;

            Label label = new Label(text);

            container.Add(dot);
            container.Add(label);

            return container;
        }

        private void NotifyParametersUpdated()
        {
            FluxParametersUpdated?.Invoke(m_CurrentParameters);
        }

        public void UpdateData(List<Flux> fluxes)
        {
            if (fluxes == null || fluxes.Count == 0) return;
            
            Flux data = fluxes[0];
            
            m_FluxData = data.FluxData;
            m_MedianEnergy = data.MedianEnergy;
            m_Onsets = data.Onsets;
            
            // Mark the GRAPH area dirty, not the whole view
            m_GraphArea.MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (m_FluxData == null || m_FluxData.Length < 2) return;

            Painter2D painter = mgc.painter2D;
            float width = m_GraphArea.contentRect.width;
            float height = m_GraphArea.contentRect.height;

            float maxFlux = 0.001f; 
            for (int i = 0; i < m_FluxData.Length; i++)
            {
                if (m_FluxData[i] > maxFlux) maxFlux = m_FluxData[i];
            }
            maxFlux *= 1.1f; 

            // DRAW FLUX
            painter.BeginPath();
            painter.strokeColor = m_FluxColor;
            painter.lineWidth = m_LineWidth;
            painter.lineJoin = LineJoin.Round;

            float xStep = width / (m_FluxData.Length - 1);

            for (int i = 0; i < m_FluxData.Length; i++)
            {
                float x = i * xStep;
                float normalizedY = m_FluxData[i] / maxFlux;
                float y = height - (normalizedY * height);

                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke(); 

            // DRAW THRESHOLD 
            // Note: Now using the synced parameter from the struct instead of a disconnected variable
            float currentThreshold = m_MedianEnergy * m_CurrentParameters.ThresholdSensitivityMultiplier;
            float normalizedThresholdY = currentThreshold / maxFlux;
            float thresholdY = height - (normalizedThresholdY * height);

            painter.BeginPath();
            painter.strokeColor = m_ThresholdColor;
            painter.lineWidth = 1.0f;
            painter.MoveTo(new Vector2(0, thresholdY));
            painter.LineTo(new Vector2(width, thresholdY));
            painter.Stroke();

            // DRAW ONSETS
            if (m_Onsets != null && m_Onsets.Count > 0)
            {
                painter.fillColor = m_OnsetColor; 
                
                foreach (int onsetIndex in m_Onsets)
                {
                    if (onsetIndex < 0 || onsetIndex >= m_FluxData.Length) continue;

                    float x = onsetIndex * xStep;
                    float normalizedY = m_FluxData[onsetIndex] / maxFlux;
                    float y = height - (normalizedY * height);

                    painter.BeginPath();
                    painter.Arc(new Vector2(x, y), 3f, 0f, 360f);
                    painter.Fill(); 
                }
            }
        }

        public void Unbind()
        {
            m_GraphArea.generateVisualContent -= OnGenerateVisualContent;
        }
    }
}