using System;
using System.IO;
using System.Text;
using UnityEditor;

public static class WavTestGenerator
{
    private const int WAV_DURATION_SECONDS = 10;
    private const int SAMPLE_RATE = 44100;
    private const short BITS_PER_SAMPLE = 16;
    private const short CHANNELS = 1;

    [MenuItem("Tools/AudioAnalyzer/Tests/Wav Generator")]
    public static void GenerateAll()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        WriteWav(
            Path.Combine(desktop, "silence_10s.wav"),
            GenerateSilence()
        );

        WriteWav(
            Path.Combine(desktop, "sine_440hz_10s.wav"),
            GenerateSine(440f)
        );

        WriteWav(
            Path.Combine(desktop, "sine_220hz_10s.wav"),
            GenerateSine(220f)
        );
    }

    static float[] GenerateSilence()
    {
        int sampleCount = SAMPLE_RATE * WAV_DURATION_SECONDS;
        return new float[sampleCount];
    }

    static float[] GenerateSine(float frequency)
    {
        int sampleCount = SAMPLE_RATE * WAV_DURATION_SECONDS;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SAMPLE_RATE;
            samples[i] = (float)Math.Sin(2.0 * Math.PI * frequency * t);
        }

        return samples;
    }

    static void WriteWav(string path, float[] samples)
    {
        using (var stream = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            int byteRate = SAMPLE_RATE * CHANNELS * BITS_PER_SAMPLE / 8;
            short blockAlign = (short)(CHANNELS * BITS_PER_SAMPLE / 8);
            int dataSize = samples.Length * blockAlign;

            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write(CHANNELS);
            writer.Write(SAMPLE_RATE);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(BITS_PER_SAMPLE);

            // data chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (float sample in samples)
            {
                short pcm = (short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(pcm);
            }
        }
    }
}