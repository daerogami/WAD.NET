using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using WAD.NET.Concrete;
using WAD.NET.Concrete.Maps;
using WAD.NET.Definitions.Classic.Map;
using WAD.NET.Enums;
using WAD.NET.Interfaces;
using WAD.NET.Validation;

namespace WAD.NET.Tests
{
    public class WadValidatorTests
    {
        private const string TestWadName = "TEST.WAD";

        #region ValidationResult Tests

        [Fact]
        public void ValidationResult_Success_ReturnsValidResultWithNoErrors()
        {
            var result = ValidationResult.Success();

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Equal(0, result.ErrorCount);
            Assert.Equal(0, result.WarningCount);
            Assert.Equal(0, result.TotalIssueCount);
        }

        [Fact]
        public void ValidationResult_WithError_AddsErrorAndMakesResultInvalid()
        {
            var result = ValidationResult.WithError("TEST_ERROR", "Test error message", "TESTLUMP", "E1M1");

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Equal(1, result.ErrorCount);
            Assert.Equal(0, result.WarningCount);
            Assert.Equal(1, result.TotalIssueCount);

            var error = result.Errors[0];
            Assert.Equal("TEST_ERROR", error.Code);
            Assert.Equal("Test error message", error.Message);
            Assert.Equal("TESTLUMP", error.LumpName);
            Assert.Equal("E1M1", error.MapName);
        }

        [Fact]
        public void ValidationResult_Constructor_WithErrorsAndWarnings()
        {
            var errors = new List<ValidationMessage>
            {
                new ValidationMessage("ERROR1", "Error 1"),
                new ValidationMessage("ERROR2", "Error 2")
            };
            var warnings = new List<ValidationMessage>
            {
                new ValidationMessage("WARN1", "Warning 1")
            };

            var result = new ValidationResult(errors, warnings);

            Assert.False(result.IsValid);
            Assert.Equal(2, result.ErrorCount);
            Assert.Equal(1, result.WarningCount);
            Assert.Equal(3, result.TotalIssueCount);
        }

        [Fact]
        public void ValidationResult_Constructor_WithNullCollections()
        {
            var result = new ValidationResult(null, null);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void ValidationResult_Constructor_WithWarningsOnly_RemainsValid()
        {
            var warnings = new List<ValidationMessage>
            {
                new ValidationMessage("WARN1", "Warning 1")
            };

            var result = new ValidationResult(null, warnings);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Single(result.Warnings);
        }

        [Fact]
        public void ValidationResult_Merge_CombinesMultipleResults()
        {
            var result1 = ValidationResult.WithError("ERROR1", "Error from result 1");
            var result2 = new ValidationResult(
                new[] { new ValidationMessage("ERROR2", "Error from result 2") },
                new[] { new ValidationMessage("WARN1", "Warning from result 2") }
            );

            // Create a result using the public constructor, then use reflection or re-create
            // Actually, Merge is internal - let's test through Validate() with IWadConfig
            // For now, test via ValidateConfig which uses Merge internally
            var validator = new WadValidator();

            var iwad = CreateWadWithMapMissingLumps("E1M1");
            var pwad = CreateWadWithMapMissingLumps("E1M2");

            var config = new TestWadConfig(iwad, pwad);

            var result = validator.ValidateConfig(config);

            // Both WADs should contribute errors
            Assert.False(result.IsValid);
            Assert.True(result.ErrorCount >= 2); // At least one error from each WAD
        }

        [Fact]
        public void ValidationResult_ToString_NoIssues()
        {
            var result = ValidationResult.Success();

            var str = result.ToString();

            Assert.Contains("No errors or warnings", str);
        }

        [Fact]
        public void ValidationResult_ToString_WithErrors()
        {
            var result = ValidationResult.WithError("ERR", "Error");

            var str = result.ToString();

            Assert.Contains("failed", str);
            Assert.Contains("1 error", str);
        }

        #endregion

        #region ValidationMessage Tests

        [Fact]
        public void ValidationMessage_Constructor_SetsAllProperties()
        {
            var msg = new ValidationMessage("CODE", "Message", "LUMP", "MAP", "Context");

            Assert.Equal("CODE", msg.Code);
            Assert.Equal("Message", msg.Message);
            Assert.Equal("LUMP", msg.LumpName);
            Assert.Equal("MAP", msg.MapName);
            Assert.Equal("Context", msg.Context);
        }

        [Fact]
        public void ValidationMessage_Constructor_ThrowsOnNullCode()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ValidationMessage(null!, "Message"));
        }

        [Fact]
        public void ValidationMessage_Constructor_ThrowsOnNullMessage()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ValidationMessage("CODE", null!));
        }

        [Fact]
        public void ValidationMessage_ToString_IncludesAllComponents()
        {
            var msg = new ValidationMessage("TEST_CODE", "Test message", "TESTLUMP", "E1M1", "Additional context");

            var str = msg.ToString();

            Assert.Contains("[TEST_CODE]", str);
            Assert.Contains("Test message", str);
            Assert.Contains("TESTLUMP", str);
            Assert.Contains("E1M1", str);
            Assert.Contains("Additional context", str);
        }

        [Fact]
        public void ValidationMessage_ToString_WithMinimalData()
        {
            var msg = new ValidationMessage("CODE", "Message");

            var str = msg.ToString();

            Assert.Contains("[CODE]", str);
            Assert.Contains("Message", str);
        }

        #endregion

        #region Structural Validation Tests

        [Fact]
        public void Validate_ValidWadWithProperMapStructure_Passes()
        {
            var wad = CreateValidMinimalMap("E1M1");
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            // Should have no errors (may have warnings about player starts)
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_MapMarkerWithoutRequiredLumps_Fails()
        {
            var wad = CreateWadWithMapMissingLumps("E1M1");
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.MapMissingLumps);
            Assert.Contains(result.Errors, e => e.MapName == "E1M1");
        }

        [Fact]
        public void Validate_Doom2MapMarkerWithoutRequiredLumps_Fails()
        {
            var wad = CreateWadWithMapMissingLumps("MAP01");
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.MapMissingLumps);
            Assert.Contains(result.Errors, e => e.MapName == "MAP01");
        }

        [Fact]
        public void Validate_UnpairedStartMarker_GeneratesError()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            // Add F_START without F_END
            wad.Lumps.Enqueue(new BinaryLump("F_START", TestWadName, Array.Empty<byte>()));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.MarkerUnpaired);
            Assert.Contains(result.Errors, e => e.LumpName == "F_START");
        }

        [Fact]
        public void Validate_UnpairedEndMarker_GeneratesError()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            // Add F_END without F_START
            wad.Lumps.Enqueue(new BinaryLump("F_END", TestWadName, Array.Empty<byte>()));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.MarkerUnpaired);
        }

        [Fact]
        public void Validate_MismatchedMarkers_GeneratesError()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            // Add F_START with S_END (mismatched)
            wad.Lumps.Enqueue(new BinaryLump("F_START", TestWadName, Array.Empty<byte>()));
            wad.Lumps.Enqueue(new BinaryLump("S_END", TestWadName, Array.Empty<byte>()));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.MarkerUnpaired);
        }

        [Fact]
        public void Validate_PairedMarkersWithContent_Passes()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            wad.Lumps.Enqueue(new BinaryLump("F_START", TestWadName, Array.Empty<byte>()));
            wad.Lumps.Enqueue(new FlatLump("FLOOR1", TestWadName, new byte[4096]));
            wad.Lumps.Enqueue(new BinaryLump("F_END", TestWadName, Array.Empty<byte>()));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            // Should have warnings about missing PLAYPAL, but no marker errors
            Assert.DoesNotContain(result.Errors, e => e.Code == ValidationCodes.MarkerUnpaired);
        }

        [Fact]
        public void Validate_EmptyMarkerSection_GeneratesWarning()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            wad.Lumps.Enqueue(new BinaryLump("F_START", TestWadName, Array.Empty<byte>()));
            wad.Lumps.Enqueue(new BinaryLump("F_END", TestWadName, Array.Empty<byte>()));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            Assert.Contains(result.Warnings, w => w.Code == ValidationCodes.MarkerEmpty);
        }

        [Fact]
        public void Validate_EmptyWad_IsValid()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        #endregion

        #region Cross-Reference Validation Tests

        [Fact]
        public void Validate_LinedefWithInvalidStartVertex_Fails()
        {
            var wad = CreateWadWithInvalidVertexReference(invalidStart: true, invalidEnd: false);
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.LinedefInvalidStartVertex);
        }

        [Fact]
        public void Validate_LinedefWithInvalidEndVertex_Fails()
        {
            var wad = CreateWadWithInvalidVertexReference(invalidStart: false, invalidEnd: true);
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.LinedefInvalidEndVertex);
        }

        [Fact]
        public void Validate_LinedefWithInvalidFrontSidedef_Fails()
        {
            var wad = CreateWadWithInvalidSidedefReference(invalidFront: true, invalidBack: false);
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.LinedefInvalidFrontSidedef);
        }

        [Fact]
        public void Validate_LinedefWithInvalidBackSidedef_Fails()
        {
            var wad = CreateWadWithInvalidSidedefReference(invalidFront: false, invalidBack: true);
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.LinedefInvalidBackSidedef);
        }

        [Fact]
        public void Validate_LinedefMissingFrontSidedef_Fails()
        {
            var wad = CreateWadWithMissingFrontSidedef();
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.LinedefMissingFrontSidedef);
        }

        [Fact]
        public void Validate_SidedefWithInvalidSector_Fails()
        {
            var wad = CreateWadWithInvalidSectorReference();
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == ValidationCodes.SidedefInvalidSector);
        }

        [Fact]
        public void Validate_ValidMapCrossReferences_Passes()
        {
            var wad = CreateValidMinimalMap("E1M1");
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            // Should pass validation (may have warnings about player starts)
            Assert.True(result.IsValid);
            Assert.DoesNotContain(result.Errors, e =>
                e.Code == ValidationCodes.LinedefInvalidStartVertex ||
                e.Code == ValidationCodes.LinedefInvalidEndVertex ||
                e.Code == ValidationCodes.LinedefInvalidFrontSidedef ||
                e.Code == ValidationCodes.LinedefInvalidBackSidedef ||
                e.Code == ValidationCodes.SidedefInvalidSector);
        }

        #endregion

        #region Resource Validation Tests

        [Fact]
        public void Validate_WadWithGraphicsButMissingPlaypal_GeneratesWarning()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            // Add a flat (which requires PLAYPAL)
            wad.Lumps.Enqueue(new FlatLump("FLOOR1", TestWadName, new byte[4096]));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            Assert.Contains(result.Warnings, w => w.Code == ValidationCodes.ResourceMissingPlaypal);
        }

        [Fact]
        public void Validate_WadWithGraphicsAndPlaypal_NoPlaypalWarning()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            wad.Lumps.Enqueue(new PaletteLump("PLAYPAL", TestWadName, new byte[PaletteLump.ExpectedSize]));
            wad.Lumps.Enqueue(new FlatLump("FLOOR1", TestWadName, new byte[4096]));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            Assert.DoesNotContain(result.Warnings, w => w.Code == ValidationCodes.ResourceMissingPlaypal);
        }

        [Fact]
        public void Validate_WadWithSpriteSectionButMissingPlaypal_GeneratesWarning()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            wad.Lumps.Enqueue(new BinaryLump("S_START", TestWadName, Array.Empty<byte>()));
            wad.Lumps.Enqueue(new BinaryLump("SPRT1", TestWadName, new byte[100]));
            wad.Lumps.Enqueue(new BinaryLump("S_END", TestWadName, Array.Empty<byte>()));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            Assert.Contains(result.Warnings, w => w.Code == ValidationCodes.ResourceMissingPlaypal);
        }

        #endregion

        #region Interface Compliance Tests

        [Fact]
        public void IsValid_Wad_ReturnsCorrectBoolean()
        {
            var validator = new WadValidator();

            // Valid empty wad
            var validWad = new Wad { Name = TestWadName, WadType = WadType.PWAD };
            Assert.True(validator.IsValid(validWad));

            // Invalid wad with unpaired marker
            var invalidWad = new Wad { Name = TestWadName, WadType = WadType.PWAD };
            invalidWad.Lumps.Enqueue(new BinaryLump("F_END", TestWadName, Array.Empty<byte>()));
            Assert.False(validator.IsValid(invalidWad));
        }

        [Fact]
        public void IsValid_IWadConfig_WorksCorrectly()
        {
            var validator = new WadValidator();

            // Valid config with valid wads
            var validConfig = new TestWadConfig(
                new Wad { Name = "DOOM.WAD", WadType = WadType.IWAD },
                new Wad { Name = "MOD.WAD", WadType = WadType.PWAD }
            );
            Assert.True(validator.IsValid(validConfig));

            // Invalid config with invalid wad
            var invalidWad = new Wad { Name = "BAD.WAD", WadType = WadType.PWAD };
            invalidWad.Lumps.Enqueue(new BinaryLump("F_END", TestWadName, Array.Empty<byte>()));

            var invalidConfig = new TestWadConfig(invalidWad);
            Assert.False(validator.IsValid(invalidConfig));
        }

        [Fact]
        public void Validate_NullWad_ThrowsArgumentNullException()
        {
            var validator = new WadValidator();

            Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));
        }

        [Fact]
        public void ValidateConfig_NullConfig_ThrowsArgumentNullException()
        {
            var validator = new WadValidator();

            Assert.Throws<ArgumentNullException>(() => validator.ValidateConfig(null!));
        }

        [Fact]
        public void ValidateConfig_WithNullIWad_StillValidatesPWads()
        {
            var validator = new WadValidator();

            var invalidPwad = new Wad { Name = "BAD.WAD", WadType = WadType.PWAD };
            invalidPwad.Lumps.Enqueue(new BinaryLump("F_END", TestWadName, Array.Empty<byte>()));

            var config = new TestWadConfig(null!, invalidPwad);

            var result = validator.ValidateConfig(config);

            Assert.False(result.IsValid);
        }

        #endregion

        #region Map Format Detection Tests

        [Theory]
        [InlineData("E1M1")]
        [InlineData("E4M9")]
        [InlineData("e2m3")]  // lowercase
        public void Validate_DoomMapMarkers_AreRecognized(string mapName)
        {
            var wad = CreateWadWithMapMissingLumps(mapName);
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            // Should detect as map and report missing lumps
            Assert.Contains(result.Errors, e =>
                e.Code == ValidationCodes.MapMissingLumps &&
                e.MapName != null &&
                e.MapName.Equals(mapName, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("MAP01")]
        [InlineData("MAP32")]
        [InlineData("map15")]  // lowercase
        public void Validate_Doom2MapMarkers_AreRecognized(string mapName)
        {
            var wad = CreateWadWithMapMissingLumps(mapName);
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.Contains(result.Errors, e =>
                e.Code == ValidationCodes.MapMissingLumps &&
                e.MapName != null &&
                e.MapName.Equals(mapName, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("E10M1")]  // Invalid episode
        [InlineData("E1M10")]  // Invalid map
        [InlineData("MAP001")] // Too many digits
        [InlineData("LEVEL1")] // Wrong format
        public void Validate_NonMapMarkers_AreNotRecognizedAsMaps(string lumpName)
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            wad.Lumps.Enqueue(new BinaryLump(lumpName, TestWadName, Array.Empty<byte>()));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            // Should not report map missing lumps error for non-map markers
            Assert.DoesNotContain(result.Errors, e => e.Code == ValidationCodes.MapMissingLumps);
        }

        #endregion

        #region Things Validation Tests

        [Fact]
        public void Validate_MapWithNoThings_GeneratesWarning()
        {
            var wad = CreateMinimalMapWithEmptyThings("E1M1");
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.Contains(result.Warnings, w => w.Code == ValidationCodes.MapNoThings);
        }

        [Fact]
        public void Validate_MapWithNoPlayerStart_GeneratesWarning()
        {
            var wad = CreateMinimalMapWithThingsButNoPlayerStart("E1M1");
            var validator = new WadValidator();

            var result = validator.Validate(wad);

            Assert.Contains(result.Warnings, w => w.Code == ValidationCodes.MapNoPlayerStart);
        }

        #endregion

        #region Multiple Marker Pair Tests

        [Theory]
        [InlineData("F_START", "F_END")]
        [InlineData("FF_START", "FF_END")]
        [InlineData("S_START", "S_END")]
        [InlineData("SS_START", "SS_END")]
        [InlineData("P_START", "P_END")]
        [InlineData("PP_START", "PP_END")]
        public void Validate_AllSupportedMarkerPairs_WorkCorrectly(string startMarker, string endMarker)
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            wad.Lumps.Enqueue(new BinaryLump(startMarker, TestWadName, Array.Empty<byte>()));
            wad.Lumps.Enqueue(new BinaryLump("CONTENT", TestWadName, new byte[10]));
            wad.Lumps.Enqueue(new BinaryLump(endMarker, TestWadName, Array.Empty<byte>()));

            var validator = new WadValidator();
            var result = validator.Validate(wad);

            // Should not have marker unpaired errors
            Assert.DoesNotContain(result.Errors, e => e.Code == ValidationCodes.MarkerUnpaired);
        }

        #endregion

        #region Helper Methods

        private static Wad CreateValidMinimalMap(string mapName)
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };

            // Map marker
            wad.Lumps.Enqueue(new BinaryLump(mapName, TestWadName, Array.Empty<byte>()));

            // Create valid vertex data (4 vertices forming a square)
            var vertexData = new byte[16]; // 4 vertices * 4 bytes each
            WriteInt16LE(vertexData, 0, 0);    // v0.x
            WriteInt16LE(vertexData, 2, 0);    // v0.y
            WriteInt16LE(vertexData, 4, 256);  // v1.x
            WriteInt16LE(vertexData, 6, 0);    // v1.y
            WriteInt16LE(vertexData, 8, 256);  // v2.x
            WriteInt16LE(vertexData, 10, 256); // v2.y
            WriteInt16LE(vertexData, 12, 0);   // v3.x
            WriteInt16LE(vertexData, 14, 256); // v3.y
            wad.Lumps.Enqueue(new VertexesLump("VERTEXES", TestWadName, vertexData));

            // Create valid sector data (1 sector)
            var sectorData = new byte[26];
            WriteInt16LE(sectorData, 0, 0);    // floor height
            WriteInt16LE(sectorData, 2, 128);  // ceiling height
            WriteString8(sectorData, 4, "FLOOR1");  // floor texture
            WriteString8(sectorData, 12, "CEIL1");  // ceiling texture
            WriteUInt16LE(sectorData, 20, 192);  // light level
            WriteUInt16LE(sectorData, 22, 0);    // special
            WriteUInt16LE(sectorData, 24, 0);    // tag
            wad.Lumps.Enqueue(new SectorsLump("SECTORS", TestWadName, sectorData));

            // Create valid sidedef data (4 sidedefs)
            var sidedefData = new byte[120]; // 4 sidedefs * 30 bytes each
            for (int i = 0; i < 4; i++)
            {
                int offset = i * 30;
                WriteInt16LE(sidedefData, offset, 0);      // x offset
                WriteInt16LE(sidedefData, offset + 2, 0);  // y offset
                WriteString8(sidedefData, offset + 4, "-");     // upper
                WriteString8(sidedefData, offset + 12, "-");    // lower
                WriteString8(sidedefData, offset + 20, "WALL1"); // middle
                WriteUInt16LE(sidedefData, offset + 28, 0);     // sector 0
            }
            wad.Lumps.Enqueue(new SidedefsLump("SIDEDEFS", TestWadName, sidedefData));

            // Create valid linedef data (4 linedefs forming a box)
            var linedefData = new byte[56]; // 4 linedefs * 14 bytes each
            for (int i = 0; i < 4; i++)
            {
                int offset = i * 14;
                WriteUInt16LE(linedefData, offset, (ushort)i);          // start vertex
                WriteUInt16LE(linedefData, offset + 2, (ushort)((i + 1) % 4)); // end vertex
                WriteUInt16LE(linedefData, offset + 4, 1);              // flags (impassable)
                WriteUInt16LE(linedefData, offset + 6, 0);              // special
                WriteUInt16LE(linedefData, offset + 8, 0);              // tag
                WriteUInt16LE(linedefData, offset + 10, (ushort)i);     // front sidedef
                WriteUInt16LE(linedefData, offset + 12, 0xFFFF);        // no back sidedef
            }
            wad.Lumps.Enqueue(new LinedefsLump("LINEDEFS", TestWadName, linedefData));

            // Create valid things data (1 player start)
            var thingData = new byte[10];
            WriteInt16LE(thingData, 0, 128);   // x
            WriteInt16LE(thingData, 2, 128);   // y
            WriteUInt16LE(thingData, 4, 90);   // angle
            WriteUInt16LE(thingData, 6, 1);    // type 1 = player 1 start
            WriteUInt16LE(thingData, 8, 7);    // flags (all skills)
            wad.Lumps.Enqueue(new ThingsLump("THINGS", TestWadName, thingData));

            return wad;
        }

        private static Wad CreateWadWithMapMissingLumps(string mapName)
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };
            // Just the map marker, no actual map lumps
            wad.Lumps.Enqueue(new BinaryLump(mapName, TestWadName, Array.Empty<byte>()));
            return wad;
        }

        private static Wad CreateWadWithInvalidVertexReference(bool invalidStart, bool invalidEnd)
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };

            wad.Lumps.Enqueue(new BinaryLump("E1M1", TestWadName, Array.Empty<byte>()));

            // Only 2 vertices (indices 0 and 1 are valid)
            var vertexData = new byte[8];
            wad.Lumps.Enqueue(new VertexesLump("VERTEXES", TestWadName, vertexData));

            // 1 sector
            var sectorData = CreateMinimalSectorData(1);
            wad.Lumps.Enqueue(new SectorsLump("SECTORS", TestWadName, sectorData));

            // 1 sidedef pointing to sector 0
            var sidedefData = CreateMinimalSidedefData(1, 0);
            wad.Lumps.Enqueue(new SidedefsLump("SIDEDEFS", TestWadName, sidedefData));

            // 1 linedef with potentially invalid vertex references
            var linedefData = new byte[14];
            WriteUInt16LE(linedefData, 0, invalidStart ? (ushort)99 : (ushort)0);  // start vertex
            WriteUInt16LE(linedefData, 2, invalidEnd ? (ushort)99 : (ushort)1);    // end vertex
            WriteUInt16LE(linedefData, 4, 1);       // flags
            WriteUInt16LE(linedefData, 6, 0);       // special
            WriteUInt16LE(linedefData, 8, 0);       // tag
            WriteUInt16LE(linedefData, 10, 0);      // front sidedef
            WriteUInt16LE(linedefData, 12, 0xFFFF); // no back sidedef
            wad.Lumps.Enqueue(new LinedefsLump("LINEDEFS", TestWadName, linedefData));

            // Empty things
            wad.Lumps.Enqueue(new ThingsLump("THINGS", TestWadName, Array.Empty<byte>()));

            return wad;
        }

        private static Wad CreateWadWithInvalidSidedefReference(bool invalidFront, bool invalidBack)
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };

            wad.Lumps.Enqueue(new BinaryLump("E1M1", TestWadName, Array.Empty<byte>()));

            // 2 vertices
            var vertexData = new byte[8];
            wad.Lumps.Enqueue(new VertexesLump("VERTEXES", TestWadName, vertexData));

            // 1 sector
            var sectorData = CreateMinimalSectorData(1);
            wad.Lumps.Enqueue(new SectorsLump("SECTORS", TestWadName, sectorData));

            // 1 sidedef (index 0 is valid)
            var sidedefData = CreateMinimalSidedefData(1, 0);
            wad.Lumps.Enqueue(new SidedefsLump("SIDEDEFS", TestWadName, sidedefData));

            // 1 linedef with potentially invalid sidedef references
            var linedefData = new byte[14];
            WriteUInt16LE(linedefData, 0, 0);       // start vertex
            WriteUInt16LE(linedefData, 2, 1);       // end vertex
            WriteUInt16LE(linedefData, 4, 1);       // flags
            WriteUInt16LE(linedefData, 6, 0);       // special
            WriteUInt16LE(linedefData, 8, 0);       // tag
            WriteUInt16LE(linedefData, 10, invalidFront ? (ushort)99 : (ushort)0);  // front sidedef
            WriteUInt16LE(linedefData, 12, invalidBack ? (ushort)99 : (ushort)0xFFFF); // back sidedef
            wad.Lumps.Enqueue(new LinedefsLump("LINEDEFS", TestWadName, linedefData));

            // Empty things
            wad.Lumps.Enqueue(new ThingsLump("THINGS", TestWadName, Array.Empty<byte>()));

            return wad;
        }

        private static Wad CreateWadWithMissingFrontSidedef()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };

            wad.Lumps.Enqueue(new BinaryLump("E1M1", TestWadName, Array.Empty<byte>()));

            // 2 vertices
            var vertexData = new byte[8];
            wad.Lumps.Enqueue(new VertexesLump("VERTEXES", TestWadName, vertexData));

            // 1 sector
            var sectorData = CreateMinimalSectorData(1);
            wad.Lumps.Enqueue(new SectorsLump("SECTORS", TestWadName, sectorData));

            // 1 sidedef
            var sidedefData = CreateMinimalSidedefData(1, 0);
            wad.Lumps.Enqueue(new SidedefsLump("SIDEDEFS", TestWadName, sidedefData));

            // 1 linedef with no front sidedef (0xFFFF)
            var linedefData = new byte[14];
            WriteUInt16LE(linedefData, 0, 0);       // start vertex
            WriteUInt16LE(linedefData, 2, 1);       // end vertex
            WriteUInt16LE(linedefData, 4, 1);       // flags
            WriteUInt16LE(linedefData, 6, 0);       // special
            WriteUInt16LE(linedefData, 8, 0);       // tag
            WriteUInt16LE(linedefData, 10, 0xFFFF); // NO front sidedef
            WriteUInt16LE(linedefData, 12, 0xFFFF); // no back sidedef
            wad.Lumps.Enqueue(new LinedefsLump("LINEDEFS", TestWadName, linedefData));

            // Empty things
            wad.Lumps.Enqueue(new ThingsLump("THINGS", TestWadName, Array.Empty<byte>()));

            return wad;
        }

        private static Wad CreateWadWithInvalidSectorReference()
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };

            wad.Lumps.Enqueue(new BinaryLump("E1M1", TestWadName, Array.Empty<byte>()));

            // 2 vertices
            var vertexData = new byte[8];
            wad.Lumps.Enqueue(new VertexesLump("VERTEXES", TestWadName, vertexData));

            // 1 sector (index 0 is valid)
            var sectorData = CreateMinimalSectorData(1);
            wad.Lumps.Enqueue(new SectorsLump("SECTORS", TestWadName, sectorData));

            // 1 sidedef pointing to invalid sector 99
            var sidedefData = CreateMinimalSidedefData(1, 99);
            wad.Lumps.Enqueue(new SidedefsLump("SIDEDEFS", TestWadName, sidedefData));

            // 1 linedef
            var linedefData = new byte[14];
            WriteUInt16LE(linedefData, 0, 0);       // start vertex
            WriteUInt16LE(linedefData, 2, 1);       // end vertex
            WriteUInt16LE(linedefData, 4, 1);       // flags
            WriteUInt16LE(linedefData, 6, 0);       // special
            WriteUInt16LE(linedefData, 8, 0);       // tag
            WriteUInt16LE(linedefData, 10, 0);      // front sidedef
            WriteUInt16LE(linedefData, 12, 0xFFFF); // no back sidedef
            wad.Lumps.Enqueue(new LinedefsLump("LINEDEFS", TestWadName, linedefData));

            // Empty things
            wad.Lumps.Enqueue(new ThingsLump("THINGS", TestWadName, Array.Empty<byte>()));

            return wad;
        }

        private static Wad CreateMinimalMapWithEmptyThings(string mapName)
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };

            wad.Lumps.Enqueue(new BinaryLump(mapName, TestWadName, Array.Empty<byte>()));

            // Minimal valid vertices
            var vertexData = new byte[8];
            wad.Lumps.Enqueue(new VertexesLump("VERTEXES", TestWadName, vertexData));

            // 1 sector
            var sectorData = CreateMinimalSectorData(1);
            wad.Lumps.Enqueue(new SectorsLump("SECTORS", TestWadName, sectorData));

            // 1 sidedef
            var sidedefData = CreateMinimalSidedefData(1, 0);
            wad.Lumps.Enqueue(new SidedefsLump("SIDEDEFS", TestWadName, sidedefData));

            // 1 linedef
            var linedefData = new byte[14];
            WriteUInt16LE(linedefData, 0, 0);       // start
            WriteUInt16LE(linedefData, 2, 1);       // end
            WriteUInt16LE(linedefData, 4, 1);       // flags
            WriteUInt16LE(linedefData, 10, 0);      // front
            WriteUInt16LE(linedefData, 12, 0xFFFF); // no back
            wad.Lumps.Enqueue(new LinedefsLump("LINEDEFS", TestWadName, linedefData));

            // Empty things
            wad.Lumps.Enqueue(new ThingsLump("THINGS", TestWadName, Array.Empty<byte>()));

            return wad;
        }

        private static Wad CreateMinimalMapWithThingsButNoPlayerStart(string mapName)
        {
            var wad = new Wad
            {
                Name = TestWadName,
                WadType = WadType.PWAD
            };

            wad.Lumps.Enqueue(new BinaryLump(mapName, TestWadName, Array.Empty<byte>()));

            // Minimal valid vertices
            var vertexData = new byte[8];
            wad.Lumps.Enqueue(new VertexesLump("VERTEXES", TestWadName, vertexData));

            // 1 sector
            var sectorData = CreateMinimalSectorData(1);
            wad.Lumps.Enqueue(new SectorsLump("SECTORS", TestWadName, sectorData));

            // 1 sidedef
            var sidedefData = CreateMinimalSidedefData(1, 0);
            wad.Lumps.Enqueue(new SidedefsLump("SIDEDEFS", TestWadName, sidedefData));

            // 1 linedef
            var linedefData = new byte[14];
            WriteUInt16LE(linedefData, 0, 0);       // start
            WriteUInt16LE(linedefData, 2, 1);       // end
            WriteUInt16LE(linedefData, 4, 1);       // flags
            WriteUInt16LE(linedefData, 10, 0);      // front
            WriteUInt16LE(linedefData, 12, 0xFFFF); // no back
            wad.Lumps.Enqueue(new LinedefsLump("LINEDEFS", TestWadName, linedefData));

            // Things with non-player start types (e.g., imp = type 3001)
            var thingData = new byte[10];
            WriteInt16LE(thingData, 0, 128);   // x
            WriteInt16LE(thingData, 2, 128);   // y
            WriteUInt16LE(thingData, 4, 90);   // angle
            WriteUInt16LE(thingData, 6, 3001); // type = imp (not player start)
            WriteUInt16LE(thingData, 8, 7);    // flags
            wad.Lumps.Enqueue(new ThingsLump("THINGS", TestWadName, thingData));

            return wad;
        }

        private static byte[] CreateMinimalSectorData(int count)
        {
            var data = new byte[count * 26];
            for (int i = 0; i < count; i++)
            {
                int offset = i * 26;
                WriteInt16LE(data, offset, 0);      // floor height
                WriteInt16LE(data, offset + 2, 128); // ceiling height
                WriteString8(data, offset + 4, "FLOOR1");
                WriteString8(data, offset + 12, "CEIL1");
                WriteUInt16LE(data, offset + 20, 192);
            }
            return data;
        }

        private static byte[] CreateMinimalSidedefData(int count, ushort sectorIndex)
        {
            var data = new byte[count * 30];
            for (int i = 0; i < count; i++)
            {
                int offset = i * 30;
                WriteString8(data, offset + 4, "-");
                WriteString8(data, offset + 12, "-");
                WriteString8(data, offset + 20, "WALL1");
                WriteUInt16LE(data, offset + 28, sectorIndex);
            }
            return data;
        }

        private static void WriteInt16LE(byte[] buffer, int offset, short value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static void WriteUInt16LE(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static void WriteString8(byte[] buffer, int offset, string value)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(value);
            for (int i = 0; i < 8 && i < bytes.Length; i++)
            {
                buffer[offset + i] = bytes[i];
            }
        }

        #endregion

        #region Test Helper Classes

        private class TestWadConfig : IWadConfig
        {
            public Engine CompatibleEngines { get; set; } = null!;
            public Wad IWad { get; set; }
            public Queue<Wad> PWads { get; }
            public IDictionary<string, string> AdditionalParamsCollection { get; set; } = new Dictionary<string, string>();

            public TestWadConfig(Wad? iwad = null, params Wad[] pwads)
            {
                IWad = iwad!;
                PWads = new Queue<Wad>(pwads ?? Array.Empty<Wad>());
            }
        }

        #endregion
    }
}
