using System;
using System.Collections.Generic;
using Ori.AudioAnalyzer.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ori.AudioAnalyzer.Editor
{
    public class FluxView : VisualElement
    {
        private float[] m_FluxData;
        private List<int> m_Onsets;
        private float m_MedianEnergy;
        private float m_ThresholdMultiplier = 1.65f;

        // Visual Settings
        private readonly Color m_FluxColor = Color.cyan;
        private readonly Color m_ThresholdColor = Color.red;
        private readonly Color m_OnsetColor = Color.yellow;
        private readonly float m_LineWidth = 1.2f;

        public event Action<FluxCreatorParameters> FluxParametersUpdated;

        public FluxView()
        {
            // Subscribe to the drawing phase
            generateVisualContent += OnGenerateVisualContent;
        }

        // The public API to feed data from your GUI/Orchestrator
        public void UpdateData(List<Flux> fluxes)
        {
            if (fluxes == null || fluxes.Count == 0)
            {
                Debug.Log("No Flux data available");
                return;
            }
            
            Flux data = fluxes[0];
            
            m_FluxData = data.FluxData;
            m_MedianEnergy = data.MedianEnergy;
            m_Onsets = data.Onsets;
            
            // This tells UI Toolkit to re-run the generateVisualContent callback next frame
            MarkDirtyRepaint();
        }

        // Overload to quickly update just the slider value while scrubbing
        public void UpdateThreshold(float thresholdMultiplier)
        {
            m_ThresholdMultiplier = thresholdMultiplier;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (m_FluxData == null || m_FluxData.Length < 2) return;

            Painter2D painter = mgc.painter2D;
            float width = contentRect.width;
            float height = contentRect.height;

            // 1. Find the local maximum to normalize the Y-Axis
            float maxFlux = 0.001f; // Prevent divide-by-zero
            for (int i = 0; i < m_FluxData.Length; i++)
            {
                if (m_FluxData[i] > maxFlux) maxFlux = m_FluxData[i];
            }

            // Optional padding so the highest peak doesn't touch the absolute top pixel
            maxFlux *= 1.1f; 

            // 2. DRAW THE FLUX LINE (CYAN)
            painter.BeginPath();
            painter.strokeColor = m_FluxColor;
            painter.lineWidth = m_LineWidth;
            painter.lineJoin = LineJoin.Round;

            float xStep = width / (m_FluxData.Length - 1);

            for (int i = 0; i < m_FluxData.Length; i++)
            {
                float x = i * xStep;
                // Normalize the value and invert Y (0 is top in UI Toolkit)
                float normalizedY = m_FluxData[i] / maxFlux;
                float y = height - (normalizedY * height);

                if (i == 0)
                {
                    painter.MoveTo(new Vector2(x, y));
                }
                else
                {
                    painter.LineTo(new Vector2(x, y));
                }
            }
            
            painter.Stroke(); // Commit the flux line to the mesh

            // 3. DRAW THE DYNAMIC THRESHOLD LINE (RED)
            float currentThreshold = m_MedianEnergy * m_ThresholdMultiplier;
            float normalizedThresholdY = currentThreshold / maxFlux;
            float thresholdY = height - (normalizedThresholdY * height);

            painter.BeginPath();
            painter.strokeColor = m_ThresholdColor;
            painter.lineWidth = 1.0f;
            
            painter.MoveTo(new Vector2(0, thresholdY));
            painter.LineTo(new Vector2(width, thresholdY));
            painter.Stroke();

            // 4. DRAW DETECTED ONSETS (YELLOW DOTS)
            if (m_Onsets != null && m_Onsets.Count > 0)
            {
                painter.fillColor = m_OnsetColor; // Note: fillColor, not strokeColor
                
                foreach (int onsetIndex in m_Onsets)
                {
                    // Boundary check just in case
                    if (onsetIndex < 0 || onsetIndex >= m_FluxData.Length) continue;

                    float x = onsetIndex * xStep;
                    float normalizedY = m_FluxData[onsetIndex] / maxFlux;
                    float y = height - (normalizedY * height);

                    painter.BeginPath();
                    // Draw a 360 degree arc to create a solid circle (radius 3f)
                    painter.Arc(new Vector2(x, y), 3f, 0f, 360f);
                    painter.Fill(); 
                }
            }
        }

        // Cleanup
        public void Unbind()
        {
            generateVisualContent -= OnGenerateVisualContent;
        }
    }
}