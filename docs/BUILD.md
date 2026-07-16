# Building Vimium from source

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Windows.

## Build

```powershell
dotnet build src\Vimium.sln
```

Run the tests:

```powershell
dotnet test src\Vimium.sln
```

## Portable single-file build

Produce a self-contained, single-file `Vimium.exe` (no .NET runtime needed on the
target machine — native WPF libraries are bundled and self-extracted at runtime):

```powershell
dotnet publish src\Vimium\Vimium.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -o publish\win-x64
# → publish\win-x64\Vimium.exe
```

## Versioning

The single source of truth for the version is `src/SolutionInfo.cs`:

```
[assembly: AssemblyVersionAttribute("1.4.2.0")]
[assembly: AssemblyFileVersionAttribute("1.4.2.0")]
internal const string Version = "1.4.2.0";   // ← canonical version
```

Version format: `MAJOR.MINOR.PATCH.REVISION` (e.g. `1.4.2.0`). The revision is
always `0` after a bump. The version is bumped automatically by the release
workflow — there's no need to edit it by hand.

## Releasing

**Releases are done through GitHub Actions.** Go to the **Actions** tab →
**bump-version** → **Run workflow**, choose the bump type, and run.

| Bump type | Example (from 1.4.2.0) | When to use |
|---|---|---|
| `minor` (default) | `1.5.0.0` | New features, routine releases |
| `patch` | `1.4.3.0` | Bug fixes only |
| `major` | `2.0.0.0` | Breaking changes |

The workflow will:
1. Bump the version in `src/SolutionInfo.cs`
2. Build the portable `Vimium.exe`
3. Commit the version change and push it
4. Create a GitHub release with the exe attached

The release tag follows the convention `vMAJOR.MINOR.PATCH` (e.g. `v1.5.0`).

### Manual tag-based release (fallback)

If you need to release from a tag manually (e.g. for signed builds when signing
is configured), push a version tag and the `release` workflow handles the rest:

```powershell
git tag v1.4.3
git push origin v1.4.3
```

This triggers [`.github/workflows/release.yml`](../.github/workflows/release.yml).

## Code signing

Downloaded releases are unsigned, so Microsoft Defender SmartScreen shows an
"unrecognized app" warning. See **[docs/SIGNING.md](SIGNING.md)** for how to
resolve it — Microsoft Store (Microsoft signs it, free), Azure Trusted Signing,
EV/OV certificates, or a self-signed certificate for internal enterprise
deployment.
