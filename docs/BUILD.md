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

## Code signing & publishing

Downloaded releases are unsigned, so Microsoft Defender SmartScreen shows an
"unrecognized app" warning. See **[docs/SIGNING.md](SIGNING.md)** for how to
resolve it — Microsoft Store (Microsoft signs it, free), Azure Trusted Signing,
EV/OV certificates, or a self-signed certificate for internal enterprise
deployment.

The [`release`](../.github/workflows/release.yml) GitHub Actions workflow builds
the portable exe, optionally signs it, and publishes a GitHub release on tag push.
