using Xunit;
using WAD.NET.Detection;

namespace WAD.NET.Tests
{
    public class ZScriptSpritesTests
    {
        [Fact]
        public void Scan_PopulatesReferencedSprites_OnClasses()
        {
            var zscript = @"
                version ""4.0""
                class MyMonster : Actor
                {
                    Default
                    {
                        Health 100;
                    }
                    States
                    {
                        Spawn:
                            POSS AB 10 A_Look;
                            Loop;
                    }
                }";

            var info = ZScriptScanner.Scan(zscript);

            Assert.Single(info.Classes);
            Assert.Contains("POSS", info.Classes[0].ReferencedSprites);
        }

        [Fact]
        public void Scan_ClassWithStatesBlock_ExtractsSprites()
        {
            var zscript = @"
                version ""4.0""
                class CoolEnemy : Actor
                {
                    Default
                    {
                        Health 200;
                    }
                    States
                    {
                        Spawn:
                            TROO A 10;
                            Loop;
                        Death:
                            BOS2 AB 5;
                            Stop;
                    }
                }";

            var info = ZScriptScanner.Scan(zscript);

            Assert.Single(info.Classes);
            Assert.Contains("TROO", info.Classes[0].ReferencedSprites);
            Assert.Contains("BOS2", info.Classes[0].ReferencedSprites);
        }

        [Fact]
        public void Scan_ClassWithNoStates_HasEmptyReferencedSprites()
        {
            var zscript = @"
                version ""4.0""
                class MyHandler : EventHandler
                {
                }";

            var info = ZScriptScanner.Scan(zscript);

            Assert.Single(info.Classes);
            Assert.Empty(info.Classes[0].ReferencedSprites);
        }

        [Fact]
        public void Scan_ExcludesTNT1()
        {
            var zscript = @"
                version ""4.0""
                class Spawner : Actor
                {
                    States
                    {
                        Spawn:
                            TNT1 A 0;
                            POSS A 10;
                            Loop;
                    }
                }";

            var info = ZScriptScanner.Scan(zscript);

            Assert.DoesNotContain("TNT1", info.Classes[0].ReferencedSprites);
            Assert.Contains("POSS", info.Classes[0].ReferencedSprites);
        }
    }
}
