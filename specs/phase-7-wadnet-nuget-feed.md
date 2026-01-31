# Phase 7: WAD.NET NuGet Package Publishing

## Overview

Set up GitHub Actions to build, test, and publish WAD.NET to nuget.org as a public NuGet package.

## Why nuget.org?

- **Public access**: Anyone can discover and use the package
- **Industry standard**: The official .NET package registry
- **Free**: No cost for open source packages
- **Integrated tooling**: Works with `dotnet add package` out of the box

## Part 1: NuGet.org Setup (Manual Steps)

### 1.1 Create NuGet.org Account

1. Go to [nuget.org](https://www.nuget.org/)
2. Sign in or create an account (Microsoft account or username/password)
3. Verify your email address

### 1.2 Generate API Key

1. Go to **Account Settings** → **API Keys**
2. Click **Create**
3. Configure:
   - **Key Name**: `WAD.NET GitHub Actions`
   - **Expiration**: 365 days (maximum)
   - **Glob Pattern**: `WAD.NET*` (or `*` for all packages)
   - **Scopes**: Push new packages and package versions
4. Click **Create**
5. **Copy the API key immediately** (it won't be shown again)

### 1.3 Add GitHub Secret

1. Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Name: `NUGET_API_KEY`
4. Value: Paste the API key from nuget.org
5. Click **Add secret**

## Part 2: WAD.NET Project Configuration

### 2.1 Update WAD.NET.csproj

Add NuGet package metadata to the project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>

    <!-- Package Metadata -->
    <PackageId>WAD.NET</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>A .NET Standard library for reading and analyzing DOOM WAD files, PK3 archives, and related formats</Description>
    <PackageTags>doom;wad;idtech;modding;games;heretic;hexen;pk3</PackageTags>
    <PackageProjectUrl>https://github.com/yourorg/WAD.NET</PackageProjectUrl>
    <RepositoryUrl>https://github.com/yourorg/WAD.NET</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- Package Settings -->
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>

    <!-- Enable Source Link for debugging -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <!-- Include README in package -->
  <ItemGroup>
    <None Include="..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

  <!-- Source Link for GitHub -->
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>

</Project>
```

### 2.2 Create/Update README.md

Ensure WAD.NET has a README.md at the repository root. This will be displayed on the nuget.org package page.

## Part 3: GitHub Actions Workflow

### 3.1 Create `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main, master, dev]
  pull_request:
    branches: [main, master]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore WAD.NET.sln

      - name: Build
        run: dotnet build WAD.NET.sln --configuration Release --no-restore

      - name: Test
        run: dotnet test WAD.NET.sln --configuration Release --no-build --verbosity normal
```

### 3.2 Create `.github/workflows/publish.yml`

```yaml
name: Publish to NuGet

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:
    inputs:
      version:
        description: 'Version to publish (without v prefix)'
        required: true
        type: string

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Determine version
        id: version
        run: |
          if [ "${{ github.event_name }}" == "workflow_dispatch" ]; then
            echo "VERSION=${{ inputs.version }}" >> $GITHUB_OUTPUT
          else
            # Extract version from tag (remove 'v' prefix)
            echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT
          fi

      - name: Restore dependencies
        run: dotnet restore WAD.NET.sln

      - name: Build
        run: dotnet build WAD.NET.sln --configuration Release --no-restore

      - name: Test
        run: dotnet test WAD.NET.sln --configuration Release --no-build --verbosity normal

      - name: Pack
        run: dotnet pack WAD.NET/WAD.NET.csproj --configuration Release --no-build -p:PackageVersion=${{ steps.version.outputs.VERSION }} --output ./nupkg

      - name: Push to NuGet
        run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate

      - name: Upload package artifact
        uses: actions/upload-artifact@v4
        with:
          name: nuget-package
          path: ./nupkg/*
```

### 3.3 Alternative: Combined Workflow with Preview Releases

For preview releases on every main branch push:

```yaml
name: CI/CD

on:
  push:
    branches: [main, master]
    tags:
      - 'v*'
  pull_request:
    branches: [main, master]

env:
  DOTNET_VERSION: '8.0.x'

jobs:
  build:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.version.outputs.VERSION }}
      is_release: ${{ steps.version.outputs.IS_RELEASE }}

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for versioning

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Determine version
        id: version
        run: |
          if [[ "$GITHUB_REF" == refs/tags/v* ]]; then
            VERSION="${GITHUB_REF#refs/tags/v}"
            echo "VERSION=$VERSION" >> $GITHUB_OUTPUT
            echo "IS_RELEASE=true" >> $GITHUB_OUTPUT
          else
            # Preview version: 1.0.0-preview.{run_number}
            VERSION="1.0.0-preview.${{ github.run_number }}"
            echo "VERSION=$VERSION" >> $GITHUB_OUTPUT
            echo "IS_RELEASE=false" >> $GITHUB_OUTPUT
          fi

      - name: Restore
        run: dotnet restore WAD.NET.sln

      - name: Build
        run: dotnet build WAD.NET.sln --configuration Release --no-restore

      - name: Test
        run: dotnet test WAD.NET.sln --configuration Release --no-build --verbosity normal

      - name: Pack
        run: dotnet pack WAD.NET/WAD.NET.csproj --configuration Release --no-build -p:PackageVersion=${{ steps.version.outputs.VERSION }} --output ./nupkg

      - name: Upload package artifact
        uses: actions/upload-artifact@v4
        with:
          name: nuget-package
          path: ./nupkg/*

  publish:
    needs: build
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && (github.ref == 'refs/heads/main' || github.ref == 'refs/heads/master' || startsWith(github.ref, 'refs/tags/v'))

    steps:
      - name: Download package artifact
        uses: actions/download-artifact@v4
        with:
          name: nuget-package
          path: ./nupkg

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Push to NuGet
        run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Part 4: Consuming WAD.NET

### 4.1 Install the Package

Once published, anyone can install WAD.NET:

```bash
dotnet add package WAD.NET
```

Or add to a `.csproj`:

```xml
<PackageReference Include="WAD.NET" Version="1.0.0" />
```

### 4.2 Using Prerelease Versions

To use preview versions:

```bash
dotnet add package WAD.NET --prerelease
```

Or specify the version explicitly:

```xml
<PackageReference Include="WAD.NET" Version="1.0.0-preview.42" />
```

## Part 5: Versioning Strategy

### Semantic Versioning (SemVer)

- **Main branch builds**: `1.0.0-preview.{run_number}` (prerelease)
- **Tagged releases**: `v1.2.3` tag → `1.2.3` package (stable)

### Release Workflow

1. Develop on `dev` or feature branches
2. Merge to `main`/`master` for preview releases (optional)
3. Create a git tag for stable releases:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The workflow will automatically:
- Build and test
- Create the package with the tag version
- Push to nuget.org

### Version Bumping Guidelines

- **Patch** (1.0.x): Bug fixes, no API changes
- **Minor** (1.x.0): New features, backwards compatible
- **Major** (x.0.0): Breaking API changes

## Part 6: GitHub Release Integration

### 6.1 Auto-create GitHub Releases

Add this job to create GitHub releases when tags are pushed:

```yaml
  release:
    needs: [build, publish]
    runs-on: ubuntu-latest
    if: startsWith(github.ref, 'refs/tags/v')
    permissions:
      contents: write

    steps:
      - name: Download package artifact
        uses: actions/download-artifact@v4
        with:
          name: nuget-package
          path: ./nupkg

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v1
        with:
          files: ./nupkg/*
          generate_release_notes: true
```

## Checklist

### NuGet.org Setup
- [ ] Create nuget.org account
- [ ] Verify email
- [ ] Generate API key with push permissions
- [ ] Reserve package name (optional, push first package to claim)

### GitHub Repository
- [ ] Add `NUGET_API_KEY` secret
- [ ] Update WAD.NET.csproj with package metadata
- [ ] Create/update README.md
- [ ] Create `.github/workflows/ci.yml`
- [ ] Create `.github/workflows/publish.yml`

### First Release
- [ ] Push to main to verify CI workflow
- [ ] Create first tag: `git tag v1.0.0 && git push origin v1.0.0`
- [ ] Verify package appears on nuget.org
- [ ] Test installing package: `dotnet add package WAD.NET`

### Verification
- [ ] CI runs on PRs
- [ ] Package published on tag push
- [ ] Package page looks correct on nuget.org
- [ ] Source Link works (can step into source when debugging)

## Troubleshooting

### "403 Forbidden" on Push

- Verify API key hasn't expired
- Check API key has correct scope (Push)
- Ensure glob pattern matches package name

### "409 Conflict" - Package Already Exists

- NuGet packages are immutable; you cannot overwrite a version
- Bump the version number and try again
- The `--skip-duplicate` flag handles this gracefully

### Package Not Showing on nuget.org

- New packages take a few minutes to index
- Check the package validation status in your nuget.org account
- Ensure README.md path is correct in csproj

### Source Link Not Working

- Ensure `Microsoft.SourceLink.GitHub` package is included
- Repository must be public for Source Link to work
- Check that `PublishRepositoryUrl` is true

## Future Enhancements

- [ ] Add package icon (`<PackageIcon>icon.png</PackageIcon>`)
- [ ] Add XML documentation (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`)
- [ ] Set up Dependabot for dependency updates
- [ ] Add code coverage reporting
- [ ] Add build status badges to README
- [ ] Configure branch protection rules
