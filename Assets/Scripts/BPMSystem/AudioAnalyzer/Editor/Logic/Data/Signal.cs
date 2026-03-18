namespace Ori.AudioAnalyzer.Core
{
    public class Signal
    {
        public float[] Samples;
        public int Channels;
        public int SampleRate;

        public float this[int index]
        {
            get => Samples[index];
            set => Samples[index] = value;
        }
    }
}

