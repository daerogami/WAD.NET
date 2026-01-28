# waddb-agent: Local WAD Analysis Agent for waddb

## Overview

A lightweight, cross-platform .NET console application that runs as a background service on the user's machine. It provides local WAD file analysis capabilities to the waddb web application (ASP.NET Core + Angular 18) via a localhost SignalR hub, eliminating the need for users to upload large WAD files.

## Problem Statement

WAD configurations can reference gigabytes of files across multiple IWADs, PWADs, and PK3 archives. Users need to inspect cascaded lump state (final monsters, weapons, levels, overwrite counts, broken resources) while building configs. Uploading these files to a server is impractical due to size, bandwidth, and security concerns.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│  User's Machine                                     │
│                                                     │
│  ┌─────────────────────────────────────────────┐    │
│  │  waddb-agent (localhost:9666)                │    │
│  │  ├── Kestrel server + SignalR hub           │    │
│  │  ├── WAD.NET library                        │    │
│  │  ├── File system watcher                    │    │
│  │  ├── WAD index (in-memory or SQLite)        │    │
│  │  └── Analysis engine (CompositeArchive)     │    │
│  └──────────────┬──────────────────────────────┘    │
│                 │ localhost SignalR                   │
│  ┌──────────────┴──────────────────────────────┐    │
│  │  Browser (Angular app served from waddb)    │    │
│  │  ├── AgentService (SignalR client)          │    │
│  │  ├── Config builder UI                      │    │
│  │  └── Cascade inspection views               │    │
│  └──────────────┬──────────────────────────────┘    │
│                 │ HTTPS                              │
└─────────────────┼───────────────────────────────────┘
                  │
┌─────────────────┴───────────────────────────────────┐
│  waddb Server (ASP.NET Core API)                    │
│  ├── Config storage (metadata only, no WAD bytes)   │
│  ├── User accounts & sharing                        │
│  └── Community config browsing                      │
└─────────────────────────────────────────────────────┘
```

### Data Flow

Only lightweight JSON metadata and small image blobs (map thumbnails) cross the localhost boundary. WAD file bytes never leave the user's machine.

```
User adds "brutal.pk3" to config in Angular UI
  → SignalR to agent: { files: ["doom2.wad", "brutal.pk3"] }

Agent:
  CompositeArchive.Load("doom2.wad", "brutal.pk3")
  → resolve effective lumps
  → categorize (things, weapons, textures, maps, etc.)
  → count overrides per lump
  → run CompatibilityAnalyzer
  → generate map thumbnails
  → return JSON summary (~KB, not GB)

Angular: renders cascade view
  → "THINGS (MAP01): overridden by brutal.pk3"
  → "68 DECORATE actors (42 replacements, 26 new)"
  → "Minimum port: GZDoom"

User saves config
  → Angular sends config metadata to waddb server (file names, load order, analysis summary)
  → No WAD bytes sent to server
```

## Graceful Degradation

The waddb web app must function with or without the local agent:

| Feature | Agent Available | Agent Unavailable |
|---------|----------------|-------------------|
| Browse shared configs | Yes | Yes |
| Create configs (file list + order) | Yes | Yes |
| Inspect cascaded lumps | Yes | No — show install prompt |
| View override counts | Yes | No |
| Detect broken resources | Yes | No |
| Map thumbnails | Yes | No |
| Compatibility analysis | Yes | No |
| Save/share configs | Yes | Yes (metadata only) |

When the agent is not detected, the Angular app should display a non-intrusive prompt: "Install the waddb agent to inspect WAD contents locally."

---

## waddb-agent Project

### Project Structure

```
waddb-agent/
├── waddb-agent.csproj          # .NET 8+ console app, references WAD.NET
├── Program.cs                  # Host builder setup
├── Configuration/
│   ├── AgentConfig.cs          # Watch directories, port, index settings
│   └── appsettings.json        # Default configuration
├── Services/
│   ├── WadIndexService.cs      # Indexes WADs in watched directories
│   ├── FileWatcherService.cs   # Monitors directories for changes
│   └── AnalysisService.cs      # Runs CompositeArchive analysis
├── Hubs/
│   └── WadHub.cs               # SignalR hub for Angular communication
├── Models/
│   ├── WadIndexEntry.cs        # Indexed WAD metadata
│   ├── AnalysisRequest.cs      # Config to analyze
│   ├── AnalysisResult.cs       # Full analysis response
│   ├── LumpSummary.cs          # Individual lump metadata
│   ├── CascadeReport.cs        # Override chain details
│   └── ResourceHealthReport.cs # Broken resource detection
└── Middleware/
    └── CorsMiddleware.cs       # CORS for localhost Angular app
```

### Distribution

The agent should be distributable via:
1. **dotnet tool**: `dotnet tool install -g waddb-agent`
2. **Single-file executable**: self-contained publish for users without the .NET SDK
3. **Platform installers** (future): optional Windows service, systemd unit, launchd plist

### Configuration

```json
{
  "Agent": {
    "Port": 9666,
    "WatchDirectories": [
      "C:/Games/DOOM",
      "C:/Games/WADs"
    ],
    "IndexOnStartup": true,
    "ThumbnailCacheDirectory": null
  }
}
```

Users configure their WAD directories on first run. The agent indexes all recognized files (*.wad, *.pk3, *.pk7, *.zip) and watches for changes.

---

## SignalR Hub API

### Hub: `/wad-hub`

The Angular app connects to `http://localhost:9666/wad-hub`.

### Client → Agent (Invocations)

#### `GetStatus`
Returns agent status and index summary.

```typescript
// Request
hub.invoke("GetStatus");

// Response
{
  "version": "1.0.0",
  "indexed": 47,
  "watchDirectories": ["C:/Games/DOOM", "C:/Games/WADs"],
  "ready": true
}
```

#### `GetIndex`
Returns all indexed WADs with basic metadata.

```typescript
// Request
hub.invoke("GetIndex");

// Response
[
  {
    "id": "sha256hash",
    "fileName": "doom2.wad",
    "fullPath": "C:/Games/DOOM/doom2.wad",
    "type": "IWAD",
    "format": "WAD",
    "sizeBytes": 14604584,
    "lumpCount": 2919,
    "categories": {
      "maps": 32,
      "sprites": 483,
      "flats": 64,
      "textures": 162,
      "sounds": 109,
      "music": 32
    }
  }
]
```

#### `AnalyzeConfig`
Runs CompositeArchive analysis on a config (ordered list of files).

```typescript
// Request
hub.invoke("AnalyzeConfig", {
  files: [
    "C:/Games/DOOM/doom2.wad",
    "C:/Games/WADs/brutal.pk3",
    "C:/Games/WADs/extra_maps.wad"
  ]
});

// Response: AnalysisResult (see Models section)
```

#### `GetLumpDetails`
Returns detailed info for a specific lump in a config context.

```typescript
// Request
hub.invoke("GetLumpDetails", {
  files: ["doom2.wad", "brutal.pk3"],
  lumpName: "THINGS",
  mapContext: "MAP01"
});

// Response
{
  "name": "THINGS",
  "category": "Map",
  "size": 4620,
  "source": "brutal.pk3",
  "resolution": "Override",
  "overrideChain": [
    { "source": "doom2.wad", "size": 3080 },
    { "source": "brutal.pk3", "size": 4620 }
  ]
}
```

#### `GetMapThumbnail`
Returns a PNG thumbnail for a specific map in a config context.

```typescript
// Request
hub.invoke("GetMapThumbnail", {
  files: ["doom2.wad", "brutal.pk3"],
  mapName: "MAP01",
  width: 512,
  height: 512
});

// Response: base64-encoded PNG string
```

#### `SearchLumps`
Searches the effective lump set with filtering.

```typescript
// Request
hub.invoke("SearchLumps", {
  files: ["doom2.wad", "brutal.pk3"],
  query: "POSS",
  categories: ["Sprites", "Things"],
  resolutionFilter: "Override"
});
```

#### `ValidateConfig`
Runs resource health checks on a config.

```typescript
// Request
hub.invoke("ValidateConfig", {
  files: ["doom2.wad", "my_mod.wad"]
});

// Response: ResourceHealthReport (see Models section)
```

### Agent → Client (Events)

#### `IndexUpdated`
Pushed when the file watcher detects changes.

```typescript
hub.on("IndexUpdated", (entry: WadIndexEntry) => {
  // A WAD was added, modified, or removed
});
```

#### `AnalysisProgress`
Pushed during long-running analysis operations.

```typescript
hub.on("AnalysisProgress", (progress: { current: number, total: number, file: string }) => {
  // Update progress bar
});
```

---

## Response Models

### AnalysisResult

The primary response from `AnalyzeConfig`. This is what drives the cascade inspection UI.

```typescript
interface AnalysisResult {
  // Load order as provided
  loadOrder: string[];

  // Compatibility
  compatibility: {
    minimumPort: string;         // "GZDoom", "Boom", "Vanilla", etc.
    requiredFeatures: string[];  // ["ZScript", "DECORATE", "MAPINFO"]
    warnings: string[];
  };

  // Summary counts
  summary: {
    totalEffectiveLumps: number;
    totalOverrides: number;
    totalAdditions: number;
    totalConflicts: number;
  };

  // Lumps grouped by category
  categories: {
    [category: string]: CategorySummary;
  };

  // Conflicts (2+ non-base archives providing same lump)
  conflicts: LumpConflict[];

  // Resource health issues
  health: ResourceHealthIssue[];
}

interface CategorySummary {
  category: string;           // "Maps", "Sprites", "Things", etc.
  totalLumps: number;
  overrides: number;
  additions: number;
  lumps: LumpSummary[];
}

interface LumpSummary {
  name: string;
  category: string;
  size: number;
  source: string;              // Which archive provides the effective version
  sourceIndex: number;
  resolution: "Base" | "Override" | "Added";
  overrideCount: number;       // How many times this lump was overridden
}

interface LumpConflict {
  lumpName: string;
  sources: {
    sourceIndex: number;
    sourcePath: string;
    size: number;
  }[];
}

interface ResourceHealthIssue {
  severity: "Error" | "Warning";
  lumpName: string;
  source: string;
  message: string;
  // e.g., "Weapon 'SuperShotgun' references sprite 'SHT2A0' which is not present in the effective lump set"
}
```

### WadIndexEntry

Represents a single indexed WAD in the local library.

```typescript
interface WadIndexEntry {
  id: string;                   // Content hash for identity
  fileName: string;
  fullPath: string;
  type: "IWAD" | "PWAD";
  format: "WAD" | "PK3" | "PK7" | "Folder";
  sizeBytes: number;
  lumpCount: number;
  lastModified: string;         // ISO 8601
  categories: { [key: string]: number };
}
```

---

## Agent Services

### WadIndexService

Maintains an index of all WADs in watched directories.

```csharp
public class WadIndexService
{
    // Index all files in watched directories
    public Task IndexAsync(CancellationToken ct);

    // Get all indexed entries
    public IReadOnlyList<WadIndexEntry> GetEntries();

    // Find by filename (for resolving config references)
    public WadIndexEntry? FindByFileName(string fileName);

    // Find by full path
    public WadIndexEntry? FindByPath(string fullPath);

    // Re-index a single file (called by file watcher)
    public Task ReindexFileAsync(string path);
}
```

**Indexing strategy:** On startup, scan all watched directories for WAD/PK3/PK7 files. For each file, open with `ArchiveReaderFactory`, extract metadata (type, lump count, category counts), compute content hash, store in index. Do not retain parsed lump data in memory — just metadata. Full parsing happens on-demand during `AnalyzeConfig`.

### FileWatcherService

Monitors watched directories using `FileSystemWatcher`.

```csharp
public class FileWatcherService : BackgroundService
{
    // Watches for Created, Changed, Deleted, Renamed events
    // Debounces rapid changes (e.g., file being copied)
    // Calls WadIndexService.ReindexFileAsync on changes
    // Pushes IndexUpdated events to connected SignalR clients
}
```

### AnalysisService

Orchestrates WAD.NET analysis for config requests.

```csharp
public class AnalysisService
{
    // Core analysis using CompositeArchive
    public Task<AnalysisResult> AnalyzeAsync(string[] filePaths, CancellationToken ct);

    // Detailed lump inspection
    public Task<LumpDetail> GetLumpDetailAsync(string[] filePaths, string lumpName);

    // Map thumbnail generation
    public Task<byte[]> GenerateMapThumbnailAsync(string[] filePaths, string mapName,
        int width = 512, int height = 512);

    // Resource health validation
    public Task<ResourceHealthReport> ValidateAsync(string[] filePaths);
}
```

**Resource health checks to implement:**
1. DECORATE/ZScript actors referencing sprites not in the effective lump set
2. SNDINFO entries referencing missing sound lumps
3. Texture patches (PNAMES) referencing missing patch lumps
4. Map linedefs referencing out-of-range sidedef/vertex indices (already in WadValidator)
5. Missing required lumps (PLAYPAL, COLORMAP) when graphics are present

---

## Angular Integration

### AgentService

```typescript
@Injectable({ providedIn: 'root' })
export class AgentService {
  private connection: HubConnection;

  // Observable connection state
  connected$: Observable<boolean>;

  // Observable WAD index (updated in real-time)
  index$: Observable<WadIndexEntry[]>;

  constructor() {
    this.connection = new HubConnectionBuilder()
      .withUrl('http://localhost:9666/wad-hub')
      .withAutomaticReconnect()
      .build();
  }

  async connect(): Promise<boolean> {
    try {
      await this.connection.start();
      return true;
    } catch {
      return false; // Agent not running
    }
  }

  analyzeConfig(files: string[]): Promise<AnalysisResult> {
    return this.connection.invoke('AnalyzeConfig', { files });
  }

  getMapThumbnail(files: string[], mapName: string): Promise<string> {
    return this.connection.invoke('GetMapThumbnail', { files, mapName });
  }

  validateConfig(files: string[]): Promise<ResourceHealthReport> {
    return this.connection.invoke('ValidateConfig', { files });
  }

  searchLumps(files: string[], query: string, categories?: string[]): Promise<LumpSummary[]> {
    return this.connection.invoke('SearchLumps', { files, query, categories });
  }
}
```

### Connection Detection

On app startup, the Angular app attempts to connect to the local agent. If unavailable, inspection features are hidden and a prompt is shown.

```typescript
// app.component.ts or a resolver
ngOnInit() {
  this.agentService.connect().then(connected => {
    if (!connected) {
      this.showAgentPrompt = true;
    }
  });
}
```

### Config Builder Component

The config builder UI references WADs by filename. When the agent is connected, it resolves filenames to the local index and enables inspection. When saving/sharing a config to the server, only the metadata is sent:

```typescript
interface SharedConfig {
  name: string;
  description: string;
  iwad: string;                  // filename only, e.g., "doom2.wad"
  files: string[];               // filenames in load order
  compatibility?: {
    minimumPort: string;
    requiredFeatures: string[];
  };
  analysisSummary?: {            // Optional snapshot from last local analysis
    totalOverrides: number;
    totalAdditions: number;
    mapCount: number;
  };
}
```

---

## CORS Configuration

The agent must allow connections from the waddb web app origin.

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("WadDb", policy =>
    {
        policy.WithOrigins(
            "https://waddb.example.com",
            "http://localhost:4200"       // Angular dev server
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();             // Required for SignalR
    });
});
```

---

## Security Considerations

1. **Localhost only.** The agent binds to `127.0.0.1`, not `0.0.0.0`. It is not accessible from the network.
2. **No file uploads.** The agent reads local files. The Angular app never sends WAD bytes.
3. **Path validation.** The agent should only serve files within configured watch directories. Reject requests for paths outside those directories to prevent path traversal.
4. **No code execution.** WAD.NET parses data structures. ACS bytecode and scripts are analyzed, not executed.
5. **Origin validation.** CORS restricts which origins can connect to the SignalR hub.

---

## Implementation Order

1. **Scaffold the project** — .NET 8 console app with Kestrel, SignalR, references WAD.NET
2. **WadIndexService + FileWatcherService** — index local WADs, watch for changes
3. **WadHub with GetStatus and GetIndex** — basic connectivity, Angular can list local WADs
4. **AgentService in Angular** — connection management, index display
5. **AnalysisService + AnalyzeConfig** — CompositeArchive integration, cascade analysis
6. **Cascade inspection UI** — category views, override counts, lump details
7. **Map thumbnails** — MapThumbnailRenderer integration, thumbnail caching
8. **Resource health validation** — broken resource detection
9. **Config save/share flow** — metadata-only sharing to waddb server
10. **Distribution** — dotnet tool packaging, single-file publish

---

## Dependencies

### waddb-agent
- WAD.NET (project reference or NuGet)
- Microsoft.AspNetCore.SignalR (included in ASP.NET Core)
- Microsoft.Extensions.Hosting (for BackgroundService)

### waddb Angular app
- @microsoft/signalr (npm package for SignalR client)
