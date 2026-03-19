using System;

namespace Ori.AudioAnalyzer.Core
{
    public class WindowFunctionHann : WindowFunction
    {
        public WindowFunctionHann(Frame[] frames, int frameSize) : base(frames, frameSize) { }

        public override Frame[] Window()
        {
            for (int i = 0; i < m_Frames.Length; i++)
            {
                Frame currentFrame = m_Frames[i];

                for (int j = 0; j < m_FrameSize; j++)
                {
                    double hannsCoefficient = 0.5 * (1 - Math.Cos(2 * Math.PI * j / (m_FrameSize - 1)));
                    currentFrame[j] *= (float)hannsCoefficient;
                }
            }
            
            return m_Frames;
        }
    }
}

