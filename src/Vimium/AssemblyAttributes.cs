using System.Resources;
using System.Runtime.Versioning;

// Vimium is a Windows-only WPF/WinForms application. Because GenerateAssemblyInfo
// is disabled (version metadata is hand-maintained in SolutionInfo.cs), the SDK does
// not emit the SupportedOSPlatform attribute automatically. Declaring it here tells
// the platform-compatibility analyzer (CA1416) that every call site is Windows-only.
[assembly: SupportedOSPlatform("windows")]

// Declares English as the neutral (fallback) resource language so the resource
// manager skips the culture-specific satellite lookup for the default culture (CA1824).
[assembly: NeutralResourcesLanguage("en")]

