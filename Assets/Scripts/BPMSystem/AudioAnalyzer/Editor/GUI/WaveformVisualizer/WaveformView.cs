using Ori.AudioAnalyzer.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ori.AudioAnalyzer.Editor
{
    internal class WaveformView : VisualElement
    {
        private readonly Color m_WaveformColor = new Color(0.8f, 0.8f, 0.8f);
        private LevelPoint[] m_PrecomputedLevels;
        private Signal m_Signal;

        internal WaveformView()
        {
            // Register the drawing callback
            generateVisualContent += OnGenerateVisualContent;
        }
        
        internal void SetSignal(Signal signal)
        {
            m_Signal = signal;
            Precompute(1024); // Precompute for a standard max width
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (m_PrecomputedLevels == null) return;

            Painter2D painter = mgc.painter2D;
            float width = contentRect.width;
            float height = contentRect.height;
            float centerY = height / 2f;

            painter.BeginPath();
            painter.strokeColor = m_WaveformColor;
            painter.lineWidth = 1f;

            for (int x = 0; x < (int)width; x++)
            {
                // Map the current UI pixel to our precomputed data index
                int dataIdx = Mathf.FloorToInt((x / width) * (m_PrecomputedLevels.Length - 1));
                var level = m_PrecomputedLevels[dataIdx];

                float yMin = centerY - (level.Min * centerY * 0.9f);
                float yMax = centerY - (level.Max * centerY * 0.9f);

                painter.MoveTo(new Vector2(x, yMin));
                painter.LineTo(new Vector2(x, yMax));
            }
            painter.Stroke();
        }
        
        private void Precompute(int width)
        {
            if (m_Signal == null || m_Signal.Samples.Length == 0) return;

            m_PrecomputedLevels = new LevelPoint[width];
            float samplesPerPixel = (float)m_Signal.Samples.Length / width;

            for (int i = 0; i < width; i++)
            {
                int start = Mathf.FloorToInt(i * samplesPerPixel);
                int end = Mathf.FloorToInt((i + 1) * samplesPerPixel);
            
                float min = 0;
                float max = 0;
                for (int s = start; s < end && s < m_Signal.Samples.Length; s++)
                {
                    float val = m_Signal.Samples[s];
                    if (val < min) min = val;
                    if (val > max) max = val;
                }
                m_PrecomputedLevels[i] = new LevelPoint { Min = min, Max = max };
            }
        }

        internal void Unbind()
        {
            generateVisualContent -= OnGenerateVisualContent;
        }
    }
}