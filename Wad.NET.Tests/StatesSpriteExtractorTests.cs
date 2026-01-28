using System.Collections.Generic;
using Xunit;
using WAD.NET.Detection;

namespace WAD.NET.Tests
{
    public class StatesSpriteExtractorTests
    {
        [Fact]
        public void ExtractsSpritePrefixesFromStatesBody()
        {
            var states = @"
                Spawn:
                    POSS AB 10 A_Look
                    Loop
                Death:
                    POSS H 5
                    BOS2 A 5
                    Stop";

            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

            Assert.Contains("POSS", sprites);
            Assert.Contains("BOS2", sprites);
        }

        [Fact]
        public void ExcludesTNT1()
        {
            var states = @"
                Spawn:
                    TNT1 A 0
                    POSS A 10
                    Loop";

            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

            Assert.DoesNotContain("TNT1", sprites);
            Assert.Contains("POSS", sprites);
        }

        [Fact]
        public void ExcludesDashes()
        {
            var states = @"
                Spawn:
                    ---- A 0
                    TROO A 10
                    Loop";

            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

            Assert.DoesNotContain("----", sprites);
            Assert.Contains("TROO", sprites);
        }

        [Fact]
        public void SkipsLabels()
        {
            var states = @"
                Spawn:
                    POSS A 10
                    Loop
                Death:
                    BOS2 A 5
                    Stop";

            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

            // Labels like "Spawn:" and "Death:" should not appear as sprites
            Assert.DoesNotContain("Spaw", sprites);
            Assert.DoesNotContain("SPAW", sprites);
            Assert.DoesNotContain("Deat", sprites);
            Assert.Equal(2, sprites.Count);
        }

        [Fact]
        public void SkipsFlowControlKeywords()
        {
            var states = @"
                Spawn:
                    POSS A 10
                    Loop
                    Stop
                    Goto See
                    Wait
                    Fail";

            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

            Assert.DoesNotContain("LOOP", sprites);
            Assert.DoesNotContain("STOP", sprites);
            Assert.DoesNotContain("GOTO", sprites);
            Assert.DoesNotContain("WAIT", sprites);
            Assert.DoesNotContain("FAIL", sprites);
            Assert.Single(sprites);
        }

        [Fact]
        public void UppercasesResults()
        {
            var states = @"
                Spawn:
                    poss AB 10
                    Loop";

            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

            Assert.Contains("POSS", sprites);
            Assert.DoesNotContain("poss", sprites);
        }

        [Fact]
        public void DeduplicatesResults()
        {
            var states = @"
                Spawn:
                    POSS A 10
                    POSS B 10
                    POSS C 10
                    Loop";

            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

            Assert.Single(sprites);
            Assert.Contains("POSS", sprites);
        }

        [Fact]
        public void EmptyStatesBody_ReturnsEmptySet()
        {
            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes("");
            Assert.Empty(sprites);
        }

        [Fact]
        public void NullStatesBody_ReturnsEmptySet()
        {
            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(null!);
            Assert.Empty(sprites);
        }

        [Fact]
        public void HandlesMixedIndentation()
        {
            var states = "Spawn:\n\tPOSS A 10\n    BOS2 B 5\n\t\tTROO C 8\n\tLoop";

            var sprites = StatesSpriteExtractor.ExtractSpritePrefixes(states);

            Assert.Contains("POSS", sprites);
            Assert.Contains("BOS2", sprites);
            Assert.Contains("TROO", sprites);
            Assert.Equal(3, sprites.Count);
        }
    }
}
