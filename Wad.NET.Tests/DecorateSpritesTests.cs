using Xunit;
using WAD.NET.Detection;

namespace WAD.NET.Tests
{
    public class DecorateSpritesTests
    {
        [Fact]
        public void Scan_PopulatesReferencedSprites_OnActors()
        {
            var decorate = @"
                Actor MyMonster : DoomImp
                {
                    States
                    {
                        Spawn:
                            TROO AB 10 A_Look
                            Loop
                    }
                }";

            var info = DecorateScanner.Scan(decorate);

            Assert.Single(info.Actors);
            Assert.Contains("TROO", info.Actors[0].ReferencedSprites);
        }

        [Fact]
        public void Scan_MultipleActors_EachGetOwnSprites()
        {
            var decorate = @"
                Actor Monster1 : DoomImp
                {
                    States
                    {
                        Spawn:
                            POSS A 10
                            Loop
                    }
                }
                Actor Monster2 : DoomImp
                {
                    States
                    {
                        Spawn:
                            SARG A 10
                            Loop
                    }
                }";

            var info = DecorateScanner.Scan(decorate);

            Assert.Equal(2, info.Actors.Count);
            Assert.Contains("POSS", info.Actors[0].ReferencedSprites);
            Assert.DoesNotContain("SARG", info.Actors[0].ReferencedSprites);
            Assert.Contains("SARG", info.Actors[1].ReferencedSprites);
            Assert.DoesNotContain("POSS", info.Actors[1].ReferencedSprites);
        }

        [Fact]
        public void Scan_ActorWithNoStates_HasEmptyReferencedSprites()
        {
            var decorate = @"
                Actor SimpleActor : DoomImp
                {
                    Health 100
                    Radius 20
                }";

            var info = DecorateScanner.Scan(decorate);

            Assert.Single(info.Actors);
            Assert.Empty(info.Actors[0].ReferencedSprites);
        }

        [Fact]
        public void Scan_ExcludesTNT1FromReferencedSprites()
        {
            var decorate = @"
                Actor InvisibleThing : Actor
                {
                    States
                    {
                        Spawn:
                            TNT1 A 0
                            POSS A 10
                            Loop
                    }
                }";

            var info = DecorateScanner.Scan(decorate);

            Assert.Single(info.Actors);
            Assert.DoesNotContain("TNT1", info.Actors[0].ReferencedSprites);
            Assert.Contains("POSS", info.Actors[0].ReferencedSprites);
        }
    }
}
