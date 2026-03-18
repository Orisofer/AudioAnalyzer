namespace Ori.AudioAnalyzer.Core
{
    public abstract class AudioDecoder : IAudioDecoder
    {
        protected byte[] m_AudioBytes;

        protected AudioDecoder(byte[] bytes)
        {
            m_AudioBytes = bytes;
        }
        
        public abstract Signal Decode();
    }
}