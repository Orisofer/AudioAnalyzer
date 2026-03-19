namespace Ori.AudioAnalyzer.Core
{
    public abstract class WindowFunction : IWindowFunction
    {
        protected Frame[] m_Frames;
        protected int m_FrameSize;
        
        protected WindowFunction(Frame[] frames, int frameSize)
        {
            m_Frames = frames;
            m_FrameSize = frameSize;
        }

        public abstract Frame[] Window();
    }
}


