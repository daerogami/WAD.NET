# WAD.NET Test Suite

This project contains unit tests and real-world validation tests for the WAD.NET library.

## Running Unit Tests

Unit tests run without any configuration:

```bash
dotnet test
```

## Real-World WAD Tests

The test suite includes tests against actual WAD files (DOOM.WAD, DOOM2.WAD, community megawads, etc.). These tests are **automatically skipped** if the WAD files are not configured.

### Setting Up WAD Paths

WAD file paths are configured using .NET User Secrets, which keeps your local paths out of source control.

#### Step 1: Initialize User Secrets

From the `Wad.NET.Tests` directory:

```bash
cd Wad.NET.Tests
dotnet user-secrets init  # Already done, but safe to run again
```

#### Step 2: Configure Your WAD Paths

Set paths to your WAD files using the `dotnet user-secrets set` command:

```bash
# IWADs
dotnet user-secrets set "WadPaths:Doom" "C:\Games\DOOM\DOOM.WAD"
dotnet user-secrets set "WadPaths:Doom2" "C:\Games\DOOM\DOOM2.WAD"
dotnet user-secrets set "WadPaths:Plutonia" "C:\Games\DOOM\PLUTONIA.WAD"
dotnet user-secrets set "WadPaths:Tnt" "C:\Games\DOOM\TNT.WAD"
dotnet user-secrets set "WadPaths:Heretic" "C:\Games\Heretic\HERETIC.WAD"
dotnet user-secrets set "WadPaths:Hexen" "C:\Games\Hexen\HEXEN.WAD"
dotnet user-secrets set "WadPaths:Strife" "C:\Games\Strife\STRIFE1.WAD"
dotnet user-secrets set "WadPaths:FreeDoom1" "C:\Games\FreeDoom\freedoom1.wad"
dotnet user-secrets set "WadPaths:FreeDoom2" "C:\Games\FreeDoom\freedoom2.wad"

# PWADs (Community Megawads)
dotnet user-secrets set "WadPaths:Pwads:Eviternity" "C:\DOOM\Wads\Eviternity.wad"
dotnet user-secrets set "WadPaths:Pwads:AncientAliens" "C:\DOOM\Wads\aaliens.wad"
dotnet user-secrets set "WadPaths:Pwads:Sunlust" "C:\DOOM\Wads\sunlust.wad"
dotnet user-secrets set "WadPaths:Pwads:Scythe2" "C:\DOOM\Wads\scythe2.wad"

# PK3s (Modern Mods)
dotnet user-secrets set "WadPaths:Pk3s:BrutalDoom" "C:\DOOM\Mods\brutalv21.pk3"
dotnet user-secrets set "WadPaths:Pk3s:Gzdoom_Pk3" "C:\Games\GZDoom\gzdoom.pk3"
dotnet user-secrets set "WadPaths:Pk3s:CustomMod" "C:\DOOM\Mods\mymod.pk3"
```

#### Step 3: Verify Configuration

List your configured secrets:

```bash
dotnet user-secrets list
```

### Available Test Categories

| Category | Description | Required Files |
|----------|-------------|----------------|
| IWAD Tests | Tests core game WAD parsing | DOOM.WAD, DOOM2.WAD, etc. |
| PWAD Tests | Tests community megawad parsing | Eviternity, Sunlust, etc. |
| PK3 Tests | Tests modern ZIP-based mods | Brutal Doom, etc. |

### Test Behavior

- Tests for unconfigured WADs are **skipped** (not failed)
- Configure only the WADs you have available
- The more WADs configured, the more comprehensive the testing

### Example Output

When running tests with partial configuration:

```
Passed!  - Failed: 0, Passed: 176, Skipped: 45
```

The 45 skipped tests are real-world WAD tests for files not configured.

## Where to Get WAD Files

### Official IWADs
- **DOOM/DOOM2**: Purchase from Steam, GOG, or Bethesda
- **FreeDoom**: Free at https://freedoom.github.io/

### Community PWADs
- **idgames Archive**: https://www.doomworld.com/idgames/
- **Doomworld Forums**: https://www.doomworld.com/forum/

### Popular Megawads for Testing
- [Eviternity](https://www.doomworld.com/idgames/levels/doom2/Ports/megawads/eviternity)
- [Ancient Aliens](https://www.doomworld.com/idgames/levels/doom2/Ports/megawads/aaliens)
- [Sunlust](https://www.doomworld.com/idgames/levels/doom2/Ports/megawads/sunlust)
- [Scythe 2](https://www.doomworld.com/idgames/levels/doom2/Ports/megawads/scythe2)

## User Secrets Location

User secrets are stored at:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\wadnet-tests-12345678-1234-1234-1234-123456789012\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/wadnet-tests-12345678-1234-1234-1234-123456789012/secrets.json`

You can edit this file directly if preferred:

```json
{
  "WadPaths": {
    "Doom": "C:\\Games\\DOOM\\DOOM.WAD",
    "Doom2": "C:\\Games\\DOOM\\DOOM2.WAD",
    "Pwads": {
      "Eviternity": "C:\\DOOM\\Wads\\Eviternity.wad"
    },
    "Pk3s": {
      "BrutalDoom": "C:\\DOOM\\Mods\\brutalv21.pk3"
    }
  }
}
```
