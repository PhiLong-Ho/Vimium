using System.Runtime.Versioning;

// The Vimium.Tests project targets a Windows-specific TFM and exercises the
// Windows-only Vimium assembly. Because GenerateAssemblyInfo is disabled, the SDK
// does not emit the SupportedOSPlatform attribute automatically; declaring it here
// tells the platform-compatibility analyzer (CA1416) that these tests are Windows-only.
[assembly: SupportedOSPlatform("windows")]
