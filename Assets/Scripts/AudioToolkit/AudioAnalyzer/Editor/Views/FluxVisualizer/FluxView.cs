using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string CLASS_NAME_NOISE_FLOOR = "noise-floor";
        private const string CLASS_NAME_FLUX_LEGEND_ITEM = "flux-view-legend-item";
        private const string CLASS_NAME_FLUX_LEGEND_ITEM_DOT = "flux-view-legend-item-dot";
        private const string CLASS_NAME_SLIDER = "container-slider";
        private const string CLASS_NAME_SLIDER_THRESHOLD = "threshold";
        private const string CLASS_NAME_SLIDER_NOISE_FLOOR = "noise-floor";
        private const string CLASS_NAME_SLIDER_WINDOW_SIZE = "window-size";
        private const string CLASS_NAME_SLIDER_LABEL = "slider-value-label";
        
        // --- Data ---
        private List<int> m_Onsets;
        private float[] m_FluxData;
        private float[] m_AverageThresholds;
        private float m_NoiseFloor;
        private float m_HopSize;
        
        // --- Flux creator parameters ranges ---
        private float m_SensitivityMin = 0f;
        private float m_SensitivityMax = 10f;
        private int m_WindowSizeMin = 0;
        private int m_WindowSizeMax = 150;
        
        // --- Visual Settings ---
        private Color m_FluxColor;
        private Color m_ThresholdColor;
        private Color m_OnsetColor;
        private Color m_NoiseFloorColor;
        private float m_LineWidth = 1.2f;

        // --- UI Elements ---
        private VisualElement m_GraphArea;
        private VisualElement m_ControlArea;
        
        // Legend
        private VisualElement m_LegendContainer;
        private VisualElement m_LegendFluxEnergy;
        private VisualElement m_LegendLocalThreshold;
        private VisualElement m_LegendDetectedOnsets;
        private VisualElement m_LegendNoiseFloor;
        private VisualElement m_DotFluxEnergy;
        private VisualElement m_DotLocalThreshold;
        private VisualElement m_DotDetectedOnsets;
        private VisualElement m_DotNoiseFloor;
        
        // Sliders
        private Slider m_ThresholdSlider;
        private Slider m_NoiseFloorSlider;
        private Slider m_WindowSizeSlider;
        
        // Current Parameters State
        private FluxCreatorParameters m_CurrentParameters;

        public event Action<FluxCreatorParameters, bool> FluxParametersUpdated;

        public FluxView()
        {
            // Initialize default parameters to match your algorithm's defaults
            m_CurrentParameters = new FluxCreatorParameters
            {
                ThresholdSensitivityMultiplier = 1.65f,
                NoiseFloorMultiplier = 110f,
                FluxTimelineWindowSize = 20
            };

            SetupGraphArea();
            SetupControlsArea();
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

            m_LegendContainer = CreateLegend();

            CreateSliders();
            
            Add(m_ControlArea);
        }

        private void CreateSliders()
        {
            VisualElement sliderContainerSensitivity = new VisualElement();
            VisualElement sliderContainerRegionEnerdyMult = new VisualElement();
            VisualElement sliderContainerWindowSize = new VisualElement();
            
            sliderContainerSensitivity.AddToClassList(CLASS_NAME_SLIDER);
            sliderContainerRegionEnerdyMult.AddToClassList(CLASS_NAME_SLIDER);
            sliderContainerWindowSize.AddToClassList(CLASS_NAME_SLIDER);
            
            m_ThresholdSlider = new Slider("Threshold Sensitivity Mult.", m_SensitivityMin, m_SensitivityMax) { value = m_CurrentParameters.ThresholdSensitivityMultiplier };
            m_NoiseFloorSlider = new Slider("Noise Floor Mult.", 10f, 300f) { value = m_CurrentParameters.NoiseFloorMultiplier };
            m_WindowSizeSlider = new Slider("Window Size", m_WindowSizeMin, m_WindowSizeMax) { value = m_CurrentParameters.FluxTimelineWindowSize };
            
            m_ThresholdSlider.AddToClassList($"{CLASS_NAME_SLIDER}-{CLASS_NAME_SLIDER_THRESHOLD}");
            m_NoiseFloorSlider.AddToClassList($"{CLASS_NAME_SLIDER}-{CLASS_NAME_SLIDER_NOISE_FLOOR}");
            m_WindowSizeSlider.AddToClassList($"{CLASS_NAME_SLIDER}-{CLASS_NAME_SLIDER_WINDOW_SIZE}");
            
            EventCallback<PointerCaptureOutEvent> onDragEnd = evt => NotifyParametersUpdatedAndRecalculate();
            EventCallback<KeyUpEvent> onKeyUp = evt => NotifyParametersUpdatedAndRecalculate();
            
            m_ThresholdSlider.RegisterCallback(onDragEnd);
            m_ThresholdSlider.RegisterCallback(onKeyUp);

            m_NoiseFloorSlider.RegisterCallback(onDragEnd);
            m_NoiseFloorSlider.RegisterCallback(onKeyUp);

            m_WindowSizeSlider.RegisterCallback(onDragEnd);
            m_WindowSizeSlider.RegisterCallback(onKeyUp);
            
            Label sensitivityValueLabel = new Label(m_CurrentParameters.ThresholdSensitivityMultiplier.ToString(CultureInfo.InvariantCulture));
            Label regionEnergyMultiplierValueLabel = new Label(m_CurrentParameters.NoiseFloorMultiplier.ToString(CultureInfo.InvariantCulture));
            Label windowSizeValueLabel = new Label(m_CurrentParameters.FluxTimelineWindowSize.ToString(CultureInfo.InvariantCulture));
            
            sensitivityValueLabel.AddToClassList(CLASS_NAME_SLIDER_LABEL);
            regionEnergyMultiplierValueLabel.AddToClassList(CLASS_NAME_SLIDER_LABEL);
            windowSizeValueLabel.AddToClassList(CLASS_NAME_SLIDER_LABEL);
            
            // 3. Bind Events
            m_ThresholdSlider.RegisterValueChangedCallback(evt =>
            {
                float newValue = evt.newValue;
                m_CurrentParameters.ThresholdSensitivityMultiplier = newValue;
                sensitivityValueLabel.text = newValue.ToString(CultureInfo.InvariantCulture);
                NotifyParametersUpdated();
            });

            m_NoiseFloorSlider.RegisterValueChangedCallback(evt => 
            {
                float newValue = evt.newValue;
                m_CurrentParameters.NoiseFloorMultiplier = evt.newValue;
                regionEnergyMultiplierValueLabel.text = newValue.ToString(CultureInfo.InvariantCulture);
                NotifyParametersUpdated();
            });

            m_WindowSizeSlider.RegisterValueChangedCallback(evt => 
            {
                int newValue = (int)evt.newValue;
                m_CurrentParameters.FluxTimelineWindowSize = (int)evt.newValue;
                windowSizeValueLabel.text = newValue.ToString(CultureInfo.InvariantCulture);
                NotifyParametersUpdated();
            });
            
            sliderContainerSensitivity.Add(m_ThresholdSlider);
            sliderContainerRegionEnerdyMult.Add(m_NoiseFloorSlider);
            sliderContainerWindowSize.Add(m_WindowSizeSlider);
            
            sliderContainerSensitivity.Add(sensitivityValueLabel);
            sliderContainerRegionEnerdyMult.Add(regionEnergyMultiplierValueLabel);
            sliderContainerWindowSize.Add(windowSizeValueLabel);
            
            m_ControlArea.Add(sliderContainerSensitivity);
            m_ControlArea.Add(sliderContainerRegionEnerdyMult);
            m_ControlArea.Add(sliderContainerWindowSize);
        }

        private VisualElement CreateLegend()
        {
            VisualElement legendContainer = new VisualElement();
            
            legendContainer.AddToClassList("container-flux-view-legend");
            
            m_DotFluxEnergy = new VisualElement();
            m_DotLocalThreshold  = new VisualElement();
            m_DotDetectedOnsets = new VisualElement();
            m_DotNoiseFloor = new VisualElement();

            m_LegendFluxEnergy = CreateLegendItem("Flux Energy", CLASS_NAME_FLUX_ENERGY, ref m_DotFluxEnergy);
            m_LegendLocalThreshold = CreateLegendItem("Local Threshold", CLASS_NAME_LOCAL_THRESHOLD, ref m_DotLocalThreshold);
            m_LegendDetectedOnsets = CreateLegendItem("Detected Onsets", CLASS_NAME_ONSET, ref m_DotDetectedOnsets);
            m_LegendNoiseFloor = CreateLegendItem("Noise Floor", CLASS_NAME_NOISE_FLOOR, ref m_DotNoiseFloor);

            legendContainer.Add(m_LegendFluxEnergy);
            legendContainer.Add(m_LegendLocalThreshold);
            legendContainer.Add(m_LegendDetectedOnsets);
            legendContainer.Add(m_LegendNoiseFloor);
            
            m_ControlArea.Add(legendContainer);

            return legendContainer;
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
            FluxParametersUpdated?.Invoke(m_CurrentParameters, false);
        }
        
        private void NotifyParametersUpdatedAndRecalculate()
        {
            FluxParametersUpdated?.Invoke(m_CurrentParameters, true);
        }

        public void UpdateData(List<Flux> fluxes)
        {
            EnsureColorsCachedFromUss();
            
            if (fluxes == null || fluxes.Count == 0) return;
            
            Flux data = fluxes[0];
            
            m_FluxData = data.FluxData;
            m_AverageThresholds =  data.AverageThresholds;
            m_NoiseFloor = data.NoiseFloor;
            m_HopSize = data.HopSize;
            m_Onsets = data.Onsets;
            
            m_GraphArea.MarkDirtyRepaint();
        }

        private void EnsureColorsCachedFromUss()
        {
            m_FluxColor = m_DotFluxEnergy.resolvedStyle.backgroundColor;
            m_ThresholdColor = m_DotLocalThreshold.resolvedStyle.backgroundColor;
            m_OnsetColor = m_DotDetectedOnsets.resolvedStyle.backgroundColor;
            m_NoiseFloorColor = m_DotNoiseFloor.resolvedStyle.backgroundColor;
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
            
            // DRAW NOISE FLOOR
            painter.BeginPath();
            painter.strokeColor = m_NoiseFloorColor;
            painter.lineWidth = m_LineWidth;

            float noiseXstart = 0;
            float noiseXend = width;
            float noiseNormalizedY = m_NoiseFloor / maxFlux;
            float noiseY = height - (noiseNormalizedY * height);
            
            painter.MoveTo(new Vector2(noiseXstart, noiseY));
            painter.LineTo(new Vector2(noiseXend, noiseY));
            
            painter.Stroke();
        }

        public void Unbind()
        {
            m_GraphArea.generateVisualContent -= OnGenerateVisualContent;
        }
    }
}