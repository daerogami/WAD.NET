using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using WAD.NET.Abstract;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Represents a sound effect lump (DS* prefix) in DMX format.
    /// </summary>
    /// <remarks>
    /// DMX Sound Format:
    /// - 2 bytes: Format (always 3)
    /// - 2 bytes: Sample rate (typically 11025)
    /// - 4 bytes: Number of samples (including padding)
    /// - 16 bytes: Padding (zeros)
    /// - N bytes: 8-bit unsigned PCM samples
    /// - 16 bytes: Padding (zeros)
    /// </remarks>
    public sealed class SoundLump : Lump
    {
        /// <summary>
        /// Expected DMX format identifier.
        /// </summary>
        public const ushort DmxFormat = 3;

        /// <summary>
        /// Sound format identifier (should be 3 for DMX).
        /// </summary>
        public ushort Format { get; }

        /// <summary>
        /// Sample rate in Hz (typically 11025).
        /// </summary>
        public ushort SampleRate { get; }

        /// <summary>
        /// Raw 8-bit unsigned PCM samples (without padding).
        /// </summary>
        public byte[] Samples { get; }

        /// <summary>
        /// Duration of the sound in seconds.
        /// </summary>
        public double Duration => SampleRate > 0 ? Samples.Length / (double)SampleRate : 0;

        public SoundLump(string lumpName, string sourceWadFileName, ReadOnlySpan<byte> data)
            : base(lumpName, sourceWadFileName)
        {
            if (data.Length < 8)
            {
                throw new FormatException(
                    $"Sound lump '{lumpName}' must be at least 8 bytes, got {data.Length}");
            }

            Format = BinaryPrimitives.ReadUInt16LittleEndian(data);
            SampleRate = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2));
            var totalSamples = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4));

            // Validate format
            if (Format != DmxFormat)
            {
                // Store empty samples for unknown formats
                Samples = Array.Empty<byte>();
                return;
            }

            // Skip header padding (16 bytes), extract samples, skip end padding (16 bytes)
            const int headerSize = 8 + 16;
            const int paddingSize = 32; // 16 bytes before, 16 bytes after

            int actualSamples = (int)totalSamples - paddingSize;
            if (actualSamples > 0 && headerSize + actualSamples <= data.Length)
            {
                Samples = data.Slice(headerSize, actualSamples).ToArray();
            }
            else
            {
                Samples = Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Converts 8-bit unsigned samples to 16-bit signed PCM.
        /// </summary>
        public short[] ToSigned16Bit()
        {
            var output = new short[Samples.Length];
            for (int i = 0; i < Samples.Length; i++)
            {
                // Convert 8-bit unsigned (0-255, centered at 128) to 16-bit signed
                output[i] = (short)((Samples[i] - 128) * 256);
            }
            return output;
        }

        /// <summary>
        /// Exports the sound as a WAV file.
        /// </summary>
        public void ExportWav(Stream output)
        {
            using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);

            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + Samples.Length);  // File size - 8
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // Format chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);           // Chunk size
            writer.Write((ushort)1);    // PCM format
            writer.Write((ushort)1);    // Mono
            writer.Write((int)SampleRate);
            writer.Write((int)SampleRate);   // Byte rate (mono 8-bit)
            writer.Write((ushort)1);    // Block align
            writer.Write((ushort)8);    // Bits per sample

            // Data chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(Samples.Length);
            writer.Write(Samples);
        }

        /// <summary>
        /// Exports the sound as a WAV file to the specified path.
        /// </summary>
        public void ExportWav(string filePath)
        {
            using var fs = File.Create(filePath);
            ExportWav(fs);
        }
    }
}
