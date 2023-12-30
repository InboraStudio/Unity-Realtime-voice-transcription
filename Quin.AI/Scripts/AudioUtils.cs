using System;
using System.IO;

namespace LeastSquares.Undertone
{
    public class AudioUtils
    {
        const int HEADER_SIZE = 44;

        public static byte[] FloatArrayToWavBytes(float[] samples, int channels = 1, int sampleRate = 16000)
        {
            int bytesPerSample = 2;
            int subChunk2Size = samples.Length * bytesPerSample;
            byte[] wavFile = new byte[HEADER_SIZE + subChunk2Size];

            int byteRate = sampleRate * channels * bytesPerSample;

            byte[] header = new byte[44];

            // RIFF header
            WriteStringToBytes(header, 0, "RIFF");
            BitConverter.GetBytes(subChunk2Size + 36).CopyTo(header, 4);
            WriteStringToBytes(header, 8, "WAVE");

            // fmt sub-chunk
            WriteStringToBytes(header, 12, "fmt ");
            BitConverter.GetBytes(16).CopyTo(header, 16);   // SubChunk1Size
            BitConverter.GetBytes((short)1).CopyTo(header, 20); // AudioFormat (1 = PCM)
            BitConverter.GetBytes((short)channels).CopyTo(header, 22);
            BitConverter.GetBytes(sampleRate).CopyTo(header, 24);
            BitConverter.GetBytes(byteRate).CopyTo(header, 28);
            BitConverter.GetBytes((short)(channels * bytesPerSample)).CopyTo(header, 32); // BlockAlign
            BitConverter.GetBytes((short)(8 * bytesPerSample)).CopyTo(header, 34); // BitsPerSample

            // data sub-chunk
            WriteStringToBytes(header, 36, "data");
            BitConverter.GetBytes(subChunk2Size).CopyTo(header, 40);

            Buffer.BlockCopy(header, 0, wavFile, 0, HEADER_SIZE);

            int offset = HEADER_SIZE;
            for (int i = 0; i < samples.Length; i++)
            {
                short sample = (short)(samples[i] * (float)Int16.MaxValue);
                BitConverter.GetBytes(sample).CopyTo(wavFile, offset);
                offset += 2;
            }

            return wavFile;
        }

        static void WriteStringToBytes(byte[] byteArray, int offset, string s)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(s);
            Buffer.BlockCopy(bytes, 0, byteArray, offset, bytes.Length);
        }
    }
}