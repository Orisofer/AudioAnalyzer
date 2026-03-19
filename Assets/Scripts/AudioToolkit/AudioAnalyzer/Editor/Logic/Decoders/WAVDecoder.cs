using System;
using System.IO;

namespace Ori.AudioAnalyzer.Core
{
    public class WAVDecoder : AudioDecoder
    {
        public WAVDecoder(byte[] bytes) : base(bytes) { }

        public override Signal Decode()
        {
            using MemoryStream memoryStream = new MemoryStream(m_AudioBytes);
            using BinaryReader binaryReader = new BinaryReader(memoryStream);
            
            string riff = new string(binaryReader.ReadChars(4));
            int fileSize = binaryReader.ReadInt32();
            string wave = new string(binaryReader.ReadChars(4));

            if (riff != "RIFF" || wave != "WAVE")
            {
                throw new Exception("Invalid WAV file.");
            }
            
            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            int audioFormat = 0;

            byte[] pcmBytes = null;
            
            // --- Chunk Loop ---
            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
            {
                string chunkId = new string(binaryReader.ReadChars(4));
                int chunkSize = binaryReader.ReadInt32();

                switch (chunkId)
                {
                    case "fmt ":
                    {
                        // PCM = 1, Float = 3
                        audioFormat = binaryReader.ReadInt16();
                        channels = binaryReader.ReadInt16();
                        sampleRate = binaryReader.ReadInt32();
                        int byteRate = binaryReader.ReadInt32();
                        int blockAlign = binaryReader.ReadInt16();
                        bitsPerSample = binaryReader.ReadInt16();

                        // Skip any extra fmt bytes
                        if (chunkSize > 16)
                        {
                            binaryReader.ReadBytes(chunkSize - 16);
                        }

                        break;
                    }
                    case "data":
                    {
                        pcmBytes = binaryReader.ReadBytes(chunkSize);
                        break;
                    }
                    default:
                        // Unhandled chunk – must skip it
                        binaryReader.ReadBytes(chunkSize);
                        break;
                }
            }

            if (pcmBytes == null)
            {
                throw new Exception("No PCM data chunk found in WAV.");
            }
            
            // --- Convert PCM to float[] ---
            float[] samples = ConvertPCMToFloat(pcmBytes, audioFormat, bitsPerSample);

            // --- Convert stereo to mono (recommended for analysis) ---
            if (channels == 2)
            {
                samples = ConvertStereoToMono(samples);
                channels = 1;
            }

            Signal result = new Signal()
            {
                Samples = samples,
                SampleRate = sampleRate,
                Channels = channels
            };

            return result;
        }
        
        private float[] ConvertPCMToFloat(byte[] data, int audioFormat, int bits)
        {
            // Minimal starter logic
            if (audioFormat == 1 && bits == 16)
            {
                return Convert16Bit(data);
            }
            
            if (audioFormat == 3 && bits == 32)
            {
                return ConvertFloat32(data);
            }
            
            throw new Exception($"Unsupported WAV format. Format={audioFormat}, Bits={bits}");
        }
        
        private float[] Convert16Bit(byte[] data)
        {
            int sampleCount = data.Length / 2;
            float[] result = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short raw = BitConverter.ToInt16(data, i * 2);
                result[i] = raw / 32768f;
            }

            return result;
        }

        private float[] ConvertFloat32(byte[] data)
        {
            int count = data.Length / 4;
            float[] result = new float[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = BitConverter.ToSingle(data, i * 4);
            }

            return result;
        }

        private float[] ConvertStereoToMono(float[] samples)
        {
            int frames = samples.Length / 2;
            float[] mono = new float[frames];

            for (int i = 0; i < frames; i++)
            {
                float L = samples[i * 2];
                float R = samples[i * 2 + 1];
                mono[i] = 0.5f * (L + R);
            }

            return mono;
        }
    }
}
