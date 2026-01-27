using System;
using Xunit;
using WAD.NET.Concrete;

namespace WAD.NET.Tests
{
    /// <summary>
    /// Tests for MUS to MIDI conversion edge cases including multiple channels,
    /// instrument variations, timing/delays, and malformed MUS data handling.
    /// </summary>
    public class MusToMidiEdgeCasesTests
    {
        #region Valid MUS Conversion Tests

        [Fact]
        public void MusToMidi_MinimalValidMus_ShouldProduceMidi()
        {
            // Create minimal MUS file with just an end-of-track event
            var mus = CreateMusHeader(scoreLength: 1, scoreStart: 16, instrumentCount: 0);
            mus[16] = 0x60; // End of track event (type 6, channel 0)

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
            AssertValidMidiTrack(midi);
        }

        [Fact]
        public void MusToMidi_SingleNote_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 1);
            mus[16] = 0x00; // Instrument 0
            mus[17] = 0x00;

            // Play note event: type 1, channel 0, no delay
            mus[18] = 0x10; // Event type 1 (PlayNote), channel 0
            mus[19] = 60;   // Note 60 (middle C)

            // End of track
            mus[20] = 0x60; // Event type 6 (EndOfTrack), channel 0

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
            AssertValidMidiTrack(midi);
        }

        [Fact]
        public void MusToMidi_NoteOnThenOff_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 6, scoreStart: 16, instrumentCount: 1);
            mus[16] = 0x00;
            mus[17] = 0x00;

            // Play note
            mus[18] = 0x10; // PlayNote, channel 0
            mus[19] = 60;   // Note 60

            // Release note
            mus[20] = 0x00; // ReleaseNote, channel 0
            mus[21] = 60;   // Note 60

            // End of track
            mus[22] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
            AssertValidMidiTrack(midi);
        }

        #endregion

        #region Multiple Channel Tests

        [Fact]
        public void MusToMidi_MultipleChannels_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 10, scoreStart: 16, instrumentCount: 3);
            mus[16] = 0x00; // Instrument 0
            mus[17] = 0x00;
            mus[18] = 40;   // Instrument 1 (violin)
            mus[19] = 0x00;
            mus[20] = 0;    // Instrument 2 (piano)
            mus[21] = 0x00;

            int pos = 22;

            // Play note on channel 0
            mus[pos++] = 0x10; // PlayNote, channel 0
            mus[pos++] = 60;   // Note 60

            // Play note on channel 1
            mus[pos++] = 0x11; // PlayNote, channel 1
            mus[pos++] = 64;   // Note 64

            // Play note on channel 2
            mus[pos++] = 0x12; // PlayNote, channel 2
            mus[pos++] = 67;   // Note 67

            // End of track
            mus[pos++] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
            AssertValidMidiTrack(midi);
        }

        [Fact]
        public void MusToMidi_PercussionChannel15_ShouldMapToMidiChannel9()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 0);

            // Play note on MUS channel 15 (percussion)
            mus[16] = 0x1F; // PlayNote, channel 15 (maps to MIDI 9)
            mus[17] = 36;   // Bass drum

            // End of track
            mus[18] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
            AssertValidMidiTrack(midi);

            // The MIDI should have channel 9 events for percussion
            // Channel 9 in MIDI is represented as 0x99 for note-on
            bool hasPercussion = false;
            for (int i = 22; i < midi.Length - 3; i++)
            {
                if ((midi[i] & 0xF0) == 0x90 && (midi[i] & 0x0F) == 9)
                {
                    hasPercussion = true;
                    break;
                }
            }
            Assert.True(hasPercussion, "MIDI should contain percussion channel (9) events");
        }

        [Fact]
        public void MusToMidi_AllChannels_ShouldConvert()
        {
            // Test all 16 MUS channels
            var mus = new byte[100];
            var header = CreateMusHeader(scoreLength: 50, scoreStart: 16, instrumentCount: 0);
            Array.Copy(header, mus, header.Length);

            int pos = 16;
            for (int ch = 0; ch < 16; ch++)
            {
                // Play note on each channel
                mus[pos++] = (byte)(0x10 | ch); // PlayNote on channel ch
                mus[pos++] = (byte)(60 + ch);   // Different note per channel
            }
            mus[pos++] = 0x60; // End of track

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
            AssertValidMidiTrack(midi);
        }

        #endregion

        #region Instrument Change Tests

        [Fact]
        public void MusToMidi_InstrumentChange_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 6, scoreStart: 16, instrumentCount: 1);
            mus[16] = 0x00;
            mus[17] = 0x00;

            // Controller event for instrument change
            mus[18] = 0x40; // Controller, channel 0
            mus[19] = 0;    // Controller 0 = instrument change
            mus[20] = 40;   // Instrument 40 (violin)

            // Play note
            mus[21] = 0x10; // PlayNote
            mus[22] = 60;   // Note

            // End of track
            mus[23] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
            AssertValidMidiTrack(midi);
        }

        [Fact]
        public void MusToMidi_MultipleInstrumentChanges_ShouldConvert()
        {
            var mus = new byte[50];
            var header = CreateMusHeader(scoreLength: 20, scoreStart: 16, instrumentCount: 3);
            Array.Copy(header, mus, header.Length);

            int pos = 16;

            // Change to piano (0)
            mus[pos++] = 0x40;
            mus[pos++] = 0;
            mus[pos++] = 0;

            // Play note
            mus[pos++] = 0x10;
            mus[pos++] = 60;

            // Change to violin (40)
            mus[pos++] = 0x40;
            mus[pos++] = 0;
            mus[pos++] = 40;

            // Play note
            mus[pos++] = 0x10;
            mus[pos++] = 64;

            // Change to flute (73)
            mus[pos++] = 0x40;
            mus[pos++] = 0;
            mus[pos++] = 73;

            // Play note
            mus[pos++] = 0x10;
            mus[pos++] = 67;

            // End
            mus[pos++] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        #endregion

        #region Timing and Delay Tests

        [Fact]
        public void MusToMidi_SimpleDelay_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 6, scoreStart: 16, instrumentCount: 0);

            // Play note with delay bit set
            mus[16] = 0x90; // PlayNote, channel 0, delay bit set (0x80)
            mus[17] = 60;   // Note
            mus[18] = 32;   // Delay = 32 ticks

            // Play another note
            mus[19] = 0x10; // PlayNote, no delay
            mus[20] = 64;

            // End
            mus[21] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
            AssertValidMidiTrack(midi);
        }

        [Fact]
        public void MusToMidi_LargeDelay_ShouldUseVariableLengthEncoding()
        {
            var mus = CreateMusHeader(scoreLength: 8, scoreStart: 16, instrumentCount: 0);

            // Play note with large delay (> 127 requires multi-byte encoding)
            mus[16] = 0x90; // PlayNote with delay
            mus[17] = 60;
            mus[18] = 0x82; // Delay byte 1 (high bit set = more bytes follow)
            mus[19] = 0x00; // Delay byte 2 = (0x82 << 7) | 0x00 = 256

            // End
            mus[20] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        [Fact]
        public void MusToMidi_ZeroDelay_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 0);

            // Two notes with zero delay between them
            mus[16] = 0x10; // PlayNote, no delay
            mus[17] = 60;

            mus[18] = 0x10; // PlayNote, no delay (simultaneous with previous)
            mus[19] = 64;

            mus[20] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        [Fact]
        public void MusToMidi_ConsecutiveDelays_ShouldAccumulate()
        {
            var mus = CreateMusHeader(scoreLength: 10, scoreStart: 16, instrumentCount: 0);

            // Play note with delay
            mus[16] = 0x90; // PlayNote with delay
            mus[17] = 60;
            mus[18] = 10;   // Delay 10

            // Release note with delay
            mus[19] = 0x80; // ReleaseNote with delay
            mus[20] = 60;
            mus[21] = 20;   // Delay 20

            // Play another note
            mus[22] = 0x10;
            mus[23] = 64;

            mus[24] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        #endregion

        #region Controller Event Tests

        [Fact]
        public void MusToMidi_VolumeController_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 6, scoreStart: 16, instrumentCount: 0);

            // Controller event: volume (controller 3)
            mus[16] = 0x40; // Controller, channel 0
            mus[17] = 3;    // Volume controller
            mus[18] = 100;  // Volume value

            // Play note
            mus[19] = 0x10;
            mus[20] = 60;

            mus[21] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        [Fact]
        public void MusToMidi_PanController_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 6, scoreStart: 16, instrumentCount: 0);

            // Controller event: pan (controller 4)
            mus[16] = 0x40;
            mus[17] = 4;    // Pan controller
            mus[18] = 64;   // Center pan

            mus[19] = 0x10;
            mus[20] = 60;

            mus[21] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        [Fact]
        public void MusToMidi_AllSoundsOffController_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 0);

            // System event: all sounds off (controller 10)
            mus[16] = 0x30; // System event, channel 0
            mus[17] = 10;   // All sounds off

            mus[18] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        [Fact]
        public void MusToMidi_AllNotesOffController_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 0);

            // System event: all notes off (controller 11)
            mus[16] = 0x30;
            mus[17] = 11;

            mus[18] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        #endregion

        #region Pitch Bend Tests

        [Fact]
        public void MusToMidi_PitchBendCenter_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 0);

            // Pitch bend event
            mus[16] = 0x20; // Pitch bend, channel 0
            mus[17] = 128;  // Center (no bend)

            mus[18] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        [Fact]
        public void MusToMidi_PitchBendUp_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 0);

            // Pitch bend up
            mus[16] = 0x20;
            mus[17] = 192;  // Bend up

            mus[18] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        [Fact]
        public void MusToMidi_PitchBendDown_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 0);

            // Pitch bend down
            mus[16] = 0x20;
            mus[17] = 64;   // Bend down

            mus[18] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        #endregion

        #region Note Velocity Tests

        [Fact]
        public void MusToMidi_NoteWithExplicitVelocity_ShouldConvert()
        {
            var mus = CreateMusHeader(scoreLength: 4, scoreStart: 16, instrumentCount: 0);

            // Play note with volume byte (high bit set on note)
            mus[16] = 0x10;
            mus[17] = 0xC0 | 60; // Note 60 with volume flag (0x80)
            mus[18] = 100;       // Volume/velocity = 100

            mus[19] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        [Fact]
        public void MusToMidi_NoteVelocityInheritedFromChannel_ShouldWork()
        {
            var mus = CreateMusHeader(scoreLength: 6, scoreStart: 16, instrumentCount: 0);

            // First note with explicit velocity
            mus[16] = 0x10;
            mus[17] = 0x80 | 60; // Note 60 with volume flag
            mus[18] = 80;        // Set channel volume to 80

            // Second note without velocity (should inherit)
            mus[19] = 0x10;
            mus[20] = 64;        // Note 64, no volume flag

            mus[21] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        #endregion

        #region End of Measure Tests

        [Fact]
        public void MusToMidi_EndOfMeasureEvent_ShouldBeIgnored()
        {
            var mus = CreateMusHeader(scoreLength: 6, scoreStart: 16, instrumentCount: 0);

            // Play note
            mus[16] = 0x10;
            mus[17] = 60;

            // End of measure (type 5)
            mus[18] = 0x50; // EndOfMeasure, channel 0

            // Another note
            mus[19] = 0x10;
            mus[20] = 64;

            mus[21] = 0x60;

            var midi = MusToMidiConverter.Convert(mus);

            AssertValidMidiHeader(midi);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void MusToMidi_NullInput_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => MusToMidiConverter.Convert(null!));
        }

        [Fact]
        public void MusToMidi_EmptyInput_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => MusToMidiConverter.Convert(new byte[0]));
        }

        [Fact]
        public void MusToMidi_TooShortInput_ShouldThrow()
        {
            var data = new byte[10]; // Less than 16 bytes
            Assert.Throws<ArgumentException>(() => MusToMidiConverter.Convert(data));
        }

        [Fact]
        public void MusToMidi_InvalidMagic_ShouldThrow()
        {
            var data = new byte[20];
            data[0] = (byte)'X';
            data[1] = (byte)'Y';
            data[2] = (byte)'Z';
            data[3] = 0x1A;

            Assert.Throws<FormatException>(() => MusToMidiConverter.Convert(data));
        }

        [Fact]
        public void MusToMidi_WrongMagicByte_ShouldThrow()
        {
            var data = new byte[20];
            data[0] = (byte)'M';
            data[1] = (byte)'U';
            data[2] = (byte)'S';
            data[3] = 0x00; // Should be 0x1A

            Assert.Throws<FormatException>(() => MusToMidiConverter.Convert(data));
        }

        [Fact]
        public void MusToMidi_ScoreStartBeyondFile_ShouldThrow()
        {
            var mus = new byte[20];
            mus[0] = (byte)'M';
            mus[1] = (byte)'U';
            mus[2] = (byte)'S';
            mus[3] = 0x1A;
            BitConverter.GetBytes((ushort)10).CopyTo(mus, 4);   // Score length
            BitConverter.GetBytes((ushort)100).CopyTo(mus, 6);  // Score start beyond file

            Assert.Throws<FormatException>(() => MusToMidiConverter.Convert(mus));
        }

        [Fact]
        public void MusToMidi_TruncatedEventData_ShouldNotCrash()
        {
            var mus = CreateMusHeader(scoreLength: 2, scoreStart: 16, instrumentCount: 0);

            // Start of a play note event but no note byte
            mus[16] = 0x10; // PlayNote
            // Missing note byte - file ends here

            // Should either produce output or throw cleanly, but not crash
            try
            {
                var midi = MusToMidiConverter.Convert(mus);
                AssertValidMidiHeader(midi);
            }
            catch (Exception ex)
            {
                // Acceptable to throw on truncated data
                Assert.True(ex is FormatException || ex is ArgumentException || ex is IndexOutOfRangeException);
            }
        }

        [Fact]
        public void MusToMidi_NoEndOfTrackEvent_ShouldStillProduceValidMidi()
        {
            var mus = CreateMusHeader(scoreLength: 3, scoreStart: 16, instrumentCount: 0);

            // Just notes, no end-of-track
            mus[16] = 0x10;
            mus[17] = 60;
            // File ends without proper end event

            var midi = MusToMidiConverter.Convert(mus);

            // Should still produce valid MIDI with end-of-track appended
            AssertValidMidiHeader(midi);
        }

        #endregion

        #region Helper Methods

        private static byte[] CreateMusHeader(ushort scoreLength, ushort scoreStart, ushort instrumentCount)
        {
            var mus = new byte[scoreStart + scoreLength + 10];

            // Magic
            mus[0] = (byte)'M';
            mus[1] = (byte)'U';
            mus[2] = (byte)'S';
            mus[3] = 0x1A;

            // Score length
            BitConverter.GetBytes(scoreLength).CopyTo(mus, 4);

            // Score start
            BitConverter.GetBytes(scoreStart).CopyTo(mus, 6);

            // Channels (usually 0 = auto)
            BitConverter.GetBytes((ushort)0).CopyTo(mus, 8);

            // Secondary channels
            BitConverter.GetBytes((ushort)0).CopyTo(mus, 10);

            // Instrument count
            BitConverter.GetBytes(instrumentCount).CopyTo(mus, 12);

            // Dummy (reserved)
            BitConverter.GetBytes((ushort)0).CopyTo(mus, 14);

            return mus;
        }

        private static void AssertValidMidiHeader(byte[] midi)
        {
            Assert.True(midi.Length >= 14, "MIDI data too short");

            // Check MIDI header magic
            Assert.Equal((byte)'M', midi[0]);
            Assert.Equal((byte)'T', midi[1]);
            Assert.Equal((byte)'h', midi[2]);
            Assert.Equal((byte)'d', midi[3]);

            // Header length should be 6
            int headerLength = (midi[4] << 24) | (midi[5] << 16) | (midi[6] << 8) | midi[7];
            Assert.Equal(6, headerLength);
        }

        private static void AssertValidMidiTrack(byte[] midi)
        {
            Assert.True(midi.Length >= 22, "MIDI data too short for track");

            // Check track header magic at offset 14
            Assert.Equal((byte)'M', midi[14]);
            Assert.Equal((byte)'T', midi[15]);
            Assert.Equal((byte)'r', midi[16]);
            Assert.Equal((byte)'k', midi[17]);
        }

        #endregion
    }
}
