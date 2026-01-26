using System;
using System.Text;
using Xunit;
using WAD.NET.Concrete.Maps;
using WAD.NET.Definitions.Classic.Map;

namespace WAD.NET.Tests
{
    public class MapLumpTests
    {
        #region ThingsLump Tests

        [Fact]
        public void ThingsLump_ShouldParseMultipleThings()
        {
            // Create 3 things
            var data = new byte[30]; // 3 * 10 bytes

            // Thing 0: Player 1 start
            BitConverter.GetBytes((short)100).CopyTo(data, 0);
            BitConverter.GetBytes((short)200).CopyTo(data, 2);
            BitConverter.GetBytes((ushort)90).CopyTo(data, 4);
            BitConverter.GetBytes((ushort)1).CopyTo(data, 6);
            BitConverter.GetBytes((ushort)0x07).CopyTo(data, 8);

            // Thing 1: Imp
            BitConverter.GetBytes((short)300).CopyTo(data, 10);
            BitConverter.GetBytes((short)400).CopyTo(data, 12);
            BitConverter.GetBytes((ushort)270).CopyTo(data, 14);
            BitConverter.GetBytes((ushort)3001).CopyTo(data, 16);
            BitConverter.GetBytes((ushort)0x07).CopyTo(data, 18);

            // Thing 2: Shotgun
            BitConverter.GetBytes((short)150).CopyTo(data, 20);
            BitConverter.GetBytes((short)250).CopyTo(data, 22);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 24);
            BitConverter.GetBytes((ushort)2001).CopyTo(data, 26);
            BitConverter.GetBytes((ushort)0x07).CopyTo(data, 28);

            var lump = new ThingsLump("THINGS", "TEST.WAD", data);

            Assert.Equal(3, lump.Count);
            Assert.Equal(100, lump[0].X);
            Assert.Equal(1, lump[0].Type);
            Assert.Equal(3001, lump[1].Type);
            Assert.Equal(2001, lump[2].Type);
        }

        [Fact]
        public void ThingsLump_ShouldHandleEmptyData()
        {
            var data = Array.Empty<byte>();
            var lump = new ThingsLump("THINGS", "TEST.WAD", data);

            Assert.Equal(0, lump.Count);
        }

        #endregion

        #region VertexesLump Tests

        [Fact]
        public void VertexesLump_ShouldParseMultipleVertices()
        {
            var data = new byte[16]; // 4 vertices * 4 bytes
            BitConverter.GetBytes((short)0).CopyTo(data, 0);
            BitConverter.GetBytes((short)0).CopyTo(data, 2);
            BitConverter.GetBytes((short)128).CopyTo(data, 4);
            BitConverter.GetBytes((short)0).CopyTo(data, 6);
            BitConverter.GetBytes((short)128).CopyTo(data, 8);
            BitConverter.GetBytes((short)128).CopyTo(data, 10);
            BitConverter.GetBytes((short)0).CopyTo(data, 12);
            BitConverter.GetBytes((short)128).CopyTo(data, 14);

            var lump = new VertexesLump("VERTEXES", "TEST.WAD", data);

            Assert.Equal(4, lump.Count);
            Assert.Equal(0, lump[0].X);
            Assert.Equal(0, lump[0].Y);
            Assert.Equal(128, lump[1].X);
            Assert.Equal(128, lump[2].Y);
        }

        #endregion

        #region LinedefsLump Tests

        [Fact]
        public void LinedefsLump_ShouldParseLinedefs()
        {
            var data = new byte[28]; // 2 linedefs * 14 bytes

            // Linedef 0
            BitConverter.GetBytes((ushort)0).CopyTo(data, 0);
            BitConverter.GetBytes((ushort)1).CopyTo(data, 2);
            BitConverter.GetBytes((ushort)1).CopyTo(data, 4);  // Impassable
            BitConverter.GetBytes((ushort)0).CopyTo(data, 6);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 8);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 10);
            BitConverter.GetBytes((ushort)0xFFFF).CopyTo(data, 12);

            // Linedef 1
            BitConverter.GetBytes((ushort)1).CopyTo(data, 14);
            BitConverter.GetBytes((ushort)2).CopyTo(data, 16);
            BitConverter.GetBytes((ushort)5).CopyTo(data, 18);  // Impassable + TwoSided
            BitConverter.GetBytes((ushort)0).CopyTo(data, 20);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 22);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 24);
            BitConverter.GetBytes((ushort)1).CopyTo(data, 26);

            var lump = new LinedefsLump("LINEDEFS", "TEST.WAD", data);

            Assert.Equal(2, lump.Count);
            Assert.False(lump[0].HasBackSide);
            Assert.True(lump[1].HasBackSide);
            Assert.True(lump[1].IsTwoSided);
        }

        #endregion

        #region SidedefsLump Tests

        [Fact]
        public void SidedefsLump_ShouldParseSidedefs()
        {
            var data = new byte[30];
            BitConverter.GetBytes((short)0).CopyTo(data, 0);
            BitConverter.GetBytes((short)0).CopyTo(data, 2);
            Encoding.ASCII.GetBytes("STARTAN1").CopyTo(data, 4);
            Encoding.ASCII.GetBytes("-\0\0\0\0\0\0\0").CopyTo(data, 12);
            Encoding.ASCII.GetBytes("STARTAN1").CopyTo(data, 20);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 28);

            var lump = new SidedefsLump("SIDEDEFS", "TEST.WAD", data);

            Assert.Single(lump.Sidedefs);
            Assert.Equal("STARTAN1", lump[0].UpperTexture);
            Assert.Equal("-", lump[0].LowerTexture);
            Assert.Equal("STARTAN1", lump[0].MiddleTexture);
        }

        #endregion

        #region SectorsLump Tests

        [Fact]
        public void SectorsLump_ShouldParseSectors()
        {
            var data = new byte[26];
            BitConverter.GetBytes((short)0).CopyTo(data, 0);
            BitConverter.GetBytes((short)128).CopyTo(data, 2);
            Encoding.ASCII.GetBytes("FLOOR4_8").CopyTo(data, 4);
            Encoding.ASCII.GetBytes("CEIL3_5\0").CopyTo(data, 12);
            BitConverter.GetBytes((ushort)160).CopyTo(data, 20);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 22);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 24);

            var lump = new SectorsLump("SECTORS", "TEST.WAD", data);

            Assert.Single(lump.Sectors);
            Assert.Equal(0, lump[0].FloorHeight);
            Assert.Equal(128, lump[0].CeilingHeight);
            Assert.Equal("FLOOR4_8", lump[0].FloorTexture);
            Assert.Equal("CEIL3_5", lump[0].CeilingTexture);
            Assert.Equal(160, lump[0].LightLevel);
        }

        #endregion

        #region SegsLump Tests

        [Fact]
        public void SegsLump_ShouldParseSegs()
        {
            var data = new byte[24]; // 2 segs * 12 bytes

            // Seg 0
            BitConverter.GetBytes((ushort)0).CopyTo(data, 0);
            BitConverter.GetBytes((ushort)1).CopyTo(data, 2);
            BitConverter.GetBytes((short)0).CopyTo(data, 4);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 6);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 8);
            BitConverter.GetBytes((short)0).CopyTo(data, 10);

            // Seg 1
            BitConverter.GetBytes((ushort)1).CopyTo(data, 12);
            BitConverter.GetBytes((ushort)2).CopyTo(data, 14);
            BitConverter.GetBytes((short)16384).CopyTo(data, 16);  // 90 degrees
            BitConverter.GetBytes((ushort)1).CopyTo(data, 18);
            BitConverter.GetBytes((ushort)1).CopyTo(data, 20);
            BitConverter.GetBytes((short)64).CopyTo(data, 22);

            var lump = new SegsLump("SEGS", "TEST.WAD", data);

            Assert.Equal(2, lump.Count);
            Assert.False(lump[0].IsBackSide);
            Assert.True(lump[1].IsBackSide);
            Assert.Equal(90.0, lump[1].AngleDegrees, 5);
        }

        #endregion

        #region SubsectorsLump Tests

        [Fact]
        public void SubsectorsLump_ShouldParseSubsectors()
        {
            var data = new byte[8]; // 2 subsectors * 4 bytes
            BitConverter.GetBytes((ushort)4).CopyTo(data, 0);
            BitConverter.GetBytes((ushort)0).CopyTo(data, 2);
            BitConverter.GetBytes((ushort)3).CopyTo(data, 4);
            BitConverter.GetBytes((ushort)4).CopyTo(data, 6);

            var lump = new SubsectorsLump("SSECTORS", "TEST.WAD", data);

            Assert.Equal(2, lump.Count);
            Assert.Equal(4, lump[0].SegCount);
            Assert.Equal(0, lump[0].FirstSeg);
            Assert.Equal(3, lump[1].SegCount);
            Assert.Equal(4, lump[1].FirstSeg);
        }

        #endregion

        #region NodesLump Tests

        [Fact]
        public void NodesLump_ShouldParseNodes()
        {
            var data = new byte[28];
            // Partition line
            BitConverter.GetBytes((short)64).CopyTo(data, 0);
            BitConverter.GetBytes((short)64).CopyTo(data, 2);
            BitConverter.GetBytes((short)128).CopyTo(data, 4);
            BitConverter.GetBytes((short)0).CopyTo(data, 6);
            // Right bbox
            BitConverter.GetBytes((short)192).CopyTo(data, 8);
            BitConverter.GetBytes((short)0).CopyTo(data, 10);
            BitConverter.GetBytes((short)64).CopyTo(data, 12);
            BitConverter.GetBytes((short)192).CopyTo(data, 14);
            // Left bbox
            BitConverter.GetBytes((short)192).CopyTo(data, 16);
            BitConverter.GetBytes((short)0).CopyTo(data, 18);
            BitConverter.GetBytes((short)0).CopyTo(data, 20);
            BitConverter.GetBytes((short)64).CopyTo(data, 22);
            // Children
            BitConverter.GetBytes((ushort)0x8000).CopyTo(data, 24);  // Subsector 0
            BitConverter.GetBytes((ushort)0x8001).CopyTo(data, 26);  // Subsector 1

            var lump = new NodesLump("NODES", "TEST.WAD", data);

            Assert.Single(lump.Nodes);
            Assert.NotNull(lump.Root);
            Assert.True(lump[0].RightIsSubsector);
            Assert.True(lump[0].LeftIsSubsector);
            Assert.Equal(0, lump[0].RightSubsectorIndex);
            Assert.Equal(1, lump[0].LeftSubsectorIndex);
        }

        #endregion

        #region RejectLump Tests

        [Fact]
        public void RejectLump_ShouldParseBitArray()
        {
            // 3x3 sector reject table = 9 bits = 2 bytes
            // Bit pattern: 0b10100101 0b00000001
            // Means: sectors can't see certain other sectors
            var data = new byte[] { 0xA5, 0x01 };

            var lump = new RejectLump("REJECT", "TEST.WAD", data, 3);

            Assert.Equal(3, lump.SectorCount);
            Assert.True(lump.IsRejected(0, 0));   // Bit 0
            Assert.False(lump.IsRejected(0, 1));  // Bit 1
            Assert.True(lump.IsRejected(0, 2));   // Bit 2
            Assert.False(lump.CanSee(0, 0));
            Assert.True(lump.CanSee(0, 1));
        }

        #endregion

        #region BlockmapLump Tests

        [Fact]
        public void BlockmapLump_ShouldParseHeader()
        {
            var data = new byte[16];
            BitConverter.GetBytes((short)-128).CopyTo(data, 0);  // Origin X
            BitConverter.GetBytes((short)-128).CopyTo(data, 2);  // Origin Y
            BitConverter.GetBytes((short)4).CopyTo(data, 4);     // Columns
            BitConverter.GetBytes((short)4).CopyTo(data, 6);     // Rows

            var lump = new BlockmapLump("BLOCKMAP", "TEST.WAD", data);

            Assert.Equal(-128, lump.OriginX);
            Assert.Equal(-128, lump.OriginY);
            Assert.Equal(4, lump.Columns);
            Assert.Equal(4, lump.Rows);
            Assert.Equal(16, lump.BlockCount);
        }

        [Fact]
        public void BlockmapLump_GetBlockIndex_ShouldReturnCorrectIndex()
        {
            var data = new byte[8];
            BitConverter.GetBytes((short)0).CopyTo(data, 0);
            BitConverter.GetBytes((short)0).CopyTo(data, 2);
            BitConverter.GetBytes((short)4).CopyTo(data, 4);
            BitConverter.GetBytes((short)4).CopyTo(data, 6);

            var lump = new BlockmapLump("BLOCKMAP", "TEST.WAD", data);

            Assert.Equal(0, lump.GetBlockIndex(0, 0));
            Assert.Equal(0, lump.GetBlockIndex(64, 64));  // Still in block 0
            Assert.Equal(1, lump.GetBlockIndex(128, 0));  // Column 1
            Assert.Equal(4, lump.GetBlockIndex(0, 128));  // Row 1
            Assert.Equal(-1, lump.GetBlockIndex(-128, 0));  // Outside (more than one block before origin)
            Assert.Equal(-1, lump.GetBlockIndex(512, 0));  // Outside (beyond the grid)
        }

        #endregion

        #region BehaviorLump Tests

        [Fact]
        public void BehaviorLump_ShouldDetectOldFormat()
        {
            var data = new byte[12];
            Encoding.ASCII.GetBytes("ACS\0").CopyTo(data, 0);
            BitConverter.GetBytes(8).CopyTo(data, 4);  // Directory offset
            BitConverter.GetBytes(0).CopyTo(data, 8);  // Script count

            var lump = new BehaviorLump("BEHAVIOR", "TEST.WAD", data);

            Assert.True(lump.IsValid);
            Assert.Equal(AcsFormat.Old, lump.Format);
            Assert.Equal(8, lump.DirectoryOffset);
            Assert.Equal(0, lump.ScriptCount);
        }

        [Fact]
        public void BehaviorLump_ShouldDetectEnhancedFormat()
        {
            var data = new byte[8];
            Encoding.ASCII.GetBytes("ACSE").CopyTo(data, 0);
            BitConverter.GetBytes(0).CopyTo(data, 4);

            var lump = new BehaviorLump("BEHAVIOR", "TEST.WAD", data);

            Assert.True(lump.IsValid);
            Assert.Equal(AcsFormat.Enhanced, lump.Format);
        }

        [Fact]
        public void BehaviorLump_ShouldRejectInvalidData()
        {
            var data = new byte[8];
            Encoding.ASCII.GetBytes("NOPE").CopyTo(data, 0);

            var lump = new BehaviorLump("BEHAVIOR", "TEST.WAD", data);

            Assert.False(lump.IsValid);
            Assert.Equal(AcsFormat.Unknown, lump.Format);
        }

        #endregion

        #region TextMapLump Tests

        [Fact]
        public void TextMapLump_ShouldStoreText()
        {
            var text = "namespace = \"doom\"; vertex { x = 0; y = 0; }";
            var data = Encoding.UTF8.GetBytes(text);

            var lump = new TextMapLump("TEXTMAP", "TEST.WAD", data);

            Assert.Equal(text, lump.Text);
        }

        [Fact]
        public void TextMapLump_ShouldLazyParseMap()
        {
            var text = @"
                namespace = ""zdoom"";
                vertex { x = 100; y = 200; }
            ";
            var data = Encoding.UTF8.GetBytes(text);

            var lump = new TextMapLump("TEXTMAP", "TEST.WAD", data);
            var map = lump.Map;

            Assert.Equal("zdoom", map.Namespace);
            Assert.Single(map.Vertices);
            Assert.Equal(100, map.Vertices[0].X);
            Assert.Equal(200, map.Vertices[0].Y);
        }

        #endregion
    }
}
