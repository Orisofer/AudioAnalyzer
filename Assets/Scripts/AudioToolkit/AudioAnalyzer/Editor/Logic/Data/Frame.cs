namespace Ori.AudioAnalyzer.Core
{
    public class Frame
    {
        public float[] Samples;
        public int Index;
        public int StartingSample;

        public float this[int index]
        {
            get => Samples[index];
            set => Samples[index] = value;
        }
    }
}
