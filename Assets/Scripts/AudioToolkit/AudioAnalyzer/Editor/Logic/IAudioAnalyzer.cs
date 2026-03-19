namespace Ori.AudioAnalyzer.Core
{
    public interface IAudioAnalyzer
    {
        public Signal ParseAudio(string audioPath);
        
        public Spectrogram Analyze(Signal signal);

        public Signal NormalizeSignal(Signal signal);
    }
}

