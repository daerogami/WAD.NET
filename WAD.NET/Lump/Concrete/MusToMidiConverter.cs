using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WAD.NET.Concrete
{
    /// <summary>
    /// Converts DOOM MUS format music to standard MIDI format.
    /// </summary>
    public static class MusToMidiConverter
    {
        /// <summary>
        /// MUS event types.
        /// </summary>
        private enum MusEventType
        {
            ReleaseNote = 0,
            PlayNote = 1,
            PitchBend = 2,
            SystemEvent = 3,
            Controller = 4,
            EndOfMeasure = 5,
            EndOfTrack = 6
        }

        /// <summary>
        /// MUS controller types mapped to MIDI controllers.
        /// </summary>
        private static readonly byte[] MusToMidiController = new byte[]
        {
            0,      // 0: Not used
            0,      // 1: Bank select (not used in DOOM)
            1,      // 2: Modulation
            7,      // 3: Volume
            10,     // 4: Pan
            11,     // 5: Expression
            91,     // 6: Reverb
            93,     // 7: Chorus
            64,     // 8: Sustain pedal
            67,     // 9: Soft pedal
            120,    // 10: All sounds off
            123,    // 11: All notes off
            126,    // 12: Mono
            127,    // 13: Poly
            121     // 14: Reset all controllers
        };

        /// <summary>
        /// MUS to MIDI channel mapping.
        /// Channel 15 in MUS maps to MIDI channel 9 (percussion).
        /// </summary>
        private static readonly int[] MusToMidiChannel =
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 9 };

        /// <summary>
        /// Converts MUS data to MIDI format.
        /// </summary>
        /// <param name="musData">Raw MUS file data.</param>
        /// <returns>MIDI file data.</returns>
        public static byte[] Convert(byte[] musData)
        {
            if (musData == null || musData.Length < 16)
                throw new ArgumentException("Invalid MUS data", nameof(musData));

            // Validate MUS magic
            if (musData[0] != 'M' || musData[1] != 'U' ||
                musData[2] != 'S' || musData[3] != 0x1A)
                throw new FormatException("Invalid MUS magic");

            var span = musData.AsSpan();

            var scoreLength = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(4));
            var scoreStart = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(6));

            if (scoreStart >= musData.Length)
                throw new FormatException("Invalid MUS score start offset");

            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);

            // Write MIDI header
            writer.Write(Encoding.ASCII.GetBytes("MThd"));
            WriteBigEndian32(writer, 6);        // Header length
            WriteBigEndian16(writer, 0);        // Format 0 (single track)
            WriteBigEndian16(writer, 1);        // 1 track
            WriteBigEndian16(writer, 70);       // Ticks per beat

            // Write track header
            writer.Write(Encoding.ASCII.GetBytes("MTrk"));
            var trackLengthPos = output.Position;
            writer.Write(0);  // Placeholder for track length

            var trackStartPos = output.Position;

            // Channel volumes (for note-on velocity)
            var channelVolumes = new byte[16];
            for (int i = 0; i < 16; i++)
                channelVolumes[i] = 127;

            // Track last note played per channel for note-off
            var lastNotes = new int[16];
            for (int i = 0; i < 16; i++)
                lastNotes[i] = -1;

            // Convert MUS events to MIDI
            int pos = scoreStart;
            int delta = 0;

            while (pos < musData.Length)
            {
                byte eventByte = musData[pos++];
                bool hasDelay = (eventByte & 0x80) != 0;
                int eventType = (eventByte >> 4) & 0x07;
                int musChannel = eventByte & 0x0F;
                int midiChannel = MusToMidiChannel[musChannel];

                switch ((MusEventType)eventType)
                {
                    case MusEventType.ReleaseNote:
                        if (pos >= musData.Length) break;
                        {
                            byte note = (byte)(musData[pos++] & 0x7F);
                            WriteVarLength(writer, delta);
                            delta = 0;
                            // Note off
                            writer.Write((byte)(0x80 | midiChannel));
                            writer.Write(note);
                            writer.Write((byte)64);  // Release velocity
                        }
                        break;

                    case MusEventType.PlayNote:
                        if (pos >= musData.Length) break;
                        {
                            byte noteData = musData[pos++];
                            byte note = (byte)(noteData & 0x7F);
                            byte velocity = channelVolumes[midiChannel];

                            // If high bit set, volume follows
                            if ((noteData & 0x80) != 0)
                            {
                                if (pos >= musData.Length) break;
                                velocity = (byte)(musData[pos++] & 0x7F);
                                channelVolumes[midiChannel] = velocity;
                            }

                            WriteVarLength(writer, delta);
                            delta = 0;
                            // Note on
                            writer.Write((byte)(0x90 | midiChannel));
                            writer.Write(note);
                            writer.Write(velocity);
                        }
                        break;

                    case MusEventType.PitchBend:
                        if (pos >= musData.Length) break;
                        {
                            byte bend = musData[pos++];
                            // MUS pitch bend is 0-255, MIDI is 14-bit (0-16383)
                            // Center is 128 in MUS, 8192 in MIDI
                            int midiBend = bend * 64;
                            WriteVarLength(writer, delta);
                            delta = 0;
                            writer.Write((byte)(0xE0 | midiChannel));
                            writer.Write((byte)(midiBend & 0x7F));
                            writer.Write((byte)((midiBend >> 7) & 0x7F));
                        }
                        break;

                    case MusEventType.SystemEvent:
                        if (pos >= musData.Length) break;
                        {
                            byte controller = (byte)(musData[pos++] & 0x7F);
                            // System events: 10=all sounds off, 11=all notes off, etc.
                            if (controller >= 10 && controller <= 14)
                            {
                                WriteVarLength(writer, delta);
                                delta = 0;
                                writer.Write((byte)(0xB0 | midiChannel));
                                writer.Write(MusToMidiController[controller]);
                                writer.Write((byte)0);
                            }
                        }
                        break;

                    case MusEventType.Controller:
                        if (pos >= musData.Length) break;
                        {
                            byte controller = (byte)(musData[pos++] & 0x7F);
                            if (pos >= musData.Length) break;
                            byte value = (byte)(musData[pos++] & 0x7F);

                            WriteVarLength(writer, delta);
                            delta = 0;

                            if (controller == 0)
                            {
                                // Instrument change
                                writer.Write((byte)(0xC0 | midiChannel));
                                writer.Write(value);
                            }
                            else if (controller < MusToMidiController.Length)
                            {
                                // Controller change
                                writer.Write((byte)(0xB0 | midiChannel));
                                writer.Write(MusToMidiController[controller]);
                                writer.Write(value);
                            }
                        }
                        break;

                    case MusEventType.EndOfMeasure:
                        // No MIDI equivalent, just skip
                        break;

                    case MusEventType.EndOfTrack:
                        // Write end of track meta event
                        WriteVarLength(writer, delta);
                        writer.Write((byte)0xFF);
                        writer.Write((byte)0x2F);
                        writer.Write((byte)0x00);
                        goto done;
                }

                // Read delay if present
                if (hasDelay)
                {
                    int delayValue = 0;
                    byte delayByte;
                    do
                    {
                        if (pos >= musData.Length) break;
                        delayByte = musData[pos++];
                        delayValue = (delayValue << 7) | (delayByte & 0x7F);
                    } while ((delayByte & 0x80) != 0);
                    delta += delayValue;
                }
            }

        done:
            // Ensure we have end of track if loop exited early
            if (output.Position == trackStartPos ||
                musData.Length == 0 ||
                musData[Math.Min(pos - 1, musData.Length - 1)] != 6)
            {
                WriteVarLength(writer, 0);
                writer.Write((byte)0xFF);
                writer.Write((byte)0x2F);
                writer.Write((byte)0x00);
            }

            // Write track length
            var trackLength = (int)(output.Position - trackStartPos);
            output.Position = trackLengthPos;
            WriteBigEndian32(writer, trackLength);

            return output.ToArray();
        }

        private static void WriteBigEndian32(BinaryWriter writer, int value)
        {
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteBigEndian16(BinaryWriter writer, int value)
        {
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteVarLength(BinaryWriter writer, int value)
        {
            // MIDI variable-length encoding
            if (value < 0)
                value = 0;

            var buffer = new Stack<byte>();
            buffer.Push((byte)(value & 0x7F));
            value >>= 7;

            while (value > 0)
            {
                buffer.Push((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }

            while (buffer.Count > 0)
                writer.Write(buffer.Pop());
        }
    }
}
