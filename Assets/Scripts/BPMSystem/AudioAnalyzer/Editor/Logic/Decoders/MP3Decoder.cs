namespace Ori.AudioAnalyzer.Core
{
    public class MP3Decoder : AudioDecoder
    {
        public MP3Decoder(byte[] bytes) : base(bytes)
        {
        }

        public override Signal Decode()
        {
            return new Signal();
        }
    }
}

