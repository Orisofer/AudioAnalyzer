using System;
using System.Collections.Generic;
using Ori.AudioAnalyzer.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ori.AudioAnalyzer.Editor.View
{
    public class FluxView : VisualElement
    {
        private const string CLASS_NAME_FLUX_ENERGY = "flux-energy";
        private const string CLASS_NAME_LOCAL_THRESHOLD = "local-threshold";
        private const string CLASS_NAME_ONSET = "onsets";
        private const string CLASS_NAME_FLUX_LEGEND_ITEM = "flux-view-legend-item";
        private const string CLASS_NAME_FLUX_LEGEND_ITEM_DOT = "flux-view-legend-item-dot";
        
        private List<int> m_Onsets;
        private float[] m_FluxData;
        private float[] m_AverageThresholds;
        private float m_HopSize;
        
        // Visual Settings
        private Color m_FluxColor;
        private Color m_ThresholdColor;
        private Color m_OnsetColor;
        private float m_LineWidth = 1.2f;

        // UI Elements
        private VisualElement m_GraphArea;
        private VisualElement m_ControlArea;
        private VisualElement m_LegendFluxEnergy;
        private VisualElement m_LegendLocalThreshold;
        private VisualElement m_LegendDetectedOnsets;
        private VisualElement m_DotFluxEnergy;
        private VisualElement m_DotLocalThreshold;
        private VisualElement m_DotDetectedOnsets;
        
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
            
            m_GraphArea.AddToClassList("flux-view-area-graph");
            
            m_GraphArea.generateVisualContent += OnGenerateVisualContent;
            
            Add(m_GraphArea);
        }

        private void SetupControlsArea()
        {
            m_ControlArea = new VisualElement();
            
            m_ControlArea.AddToClassList("flux-view-area-control");

            // 1. Create Legend
            VisualElement legendContainer = new VisualElement();
            
            legendContainer.AddToClassList("container-flux-view-legend");
            
            m_DotFluxEnergy = new VisualElement();
            m_DotLocalThreshold  = new VisualElement();
            m_DotDetectedOnsets = new VisualElement();

            m_LegendFluxEnergy = CreateLegendItem("Flux Energy", CLASS_NAME_FLUX_ENERGY, ref m_DotFluxEnergy);
            m_LegendLocalThreshold = CreateLegendItem("Local Threshold", CLASS_NAME_LOCAL_THRESHOLD, ref m_DotLocalThreshold);
            m_LegendDetectedOnsets = CreateLegendItem("Detected Onsets", CLASS_NAME_ONSET, ref m_DotDetectedOnsets);

            legendContainer.Add(m_LegendFluxEnergy);
            legendContainer.Add(m_LegendLocalThreshold);
            legendContainer.Add(m_LegendDetectedOnsets);

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

        private VisualElement CreateLegendItem(string text, string itemId, ref VisualElement dotRef)
        {
            VisualElement container = new VisualElement();
            
            container.AddToClassList(CLASS_NAME_FLUX_LEGEND_ITEM);
            
            dotRef.AddToClassList(CLASS_NAME_FLUX_LEGEND_ITEM_DOT);
            dotRef.AddToClassList($"{CLASS_NAME_FLUX_LEGEND_ITEM_DOT}-{itemId}");

            Label label = new Label(text);

            container.Add(dotRef);
            container.Add(label);

            return container;
        }

        private void NotifyParametersUpdated()
        {
            FluxParametersUpdated?.Invoke(m_CurrentParameters);
        }

        public void UpdateData(List<Flux> fluxes)
        {
            EnsureColorsCachedFromUss();
            
            if (fluxes == null || fluxes.Count == 0) return;
            
            Flux data = fluxes[0];
            
            m_FluxData = data.FluxData;
            m_AverageThresholds =  data.AverageThresholds;
            m_HopSize = data.HopSize;
            m_Onsets = data.Onsets;
            
            // Mark the GRAPH area dirty, not the whole view
            m_GraphArea.MarkDirtyRepaint();
        }

        private void EnsureColorsCachedFromUss()
        {
            m_FluxColor = m_DotFluxEnergy.resolvedStyle.backgroundColor;
            m_ThresholdColor = m_DotLocalThreshold.resolvedStyle.backgroundColor;
            m_OnsetColor = m_DotDetectedOnsets.resolvedStyle.backgroundColor;
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
            painter.BeginPath();
            painter.strokeColor = m_ThresholdColor;
            painter.lineWidth = 1.0f;

            for (int i = 0; i < m_AverageThresholds.Length; i++)
            {
                float x = i * xStep;
                float normalizedY = m_AverageThresholds[i] / maxFlux;
                float y = height - (normalizedY * height);

                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }
            
            painter.Stroke();
            
            // DRAW ONSETS
            if (m_Onsets != null && m_Onsets.Count > 0)
            {
                painter.fillColor = m_OnsetColor; 
                
                foreach (int onsetSample in m_Onsets)
                {
                    int onsetIndex = (int)(onsetSample / m_HopSize);

                    if (onsetIndex < 0 || onsetIndex >= m_FluxData.Length)
                    {
                        continue;
                    }

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