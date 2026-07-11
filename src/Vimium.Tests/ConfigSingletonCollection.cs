using Xunit;

namespace Vimium.Tests;

/// <summary>
/// Groups all test classes that mutate the <see cref="Vimium.Services.ConfigService"/>
/// singleton (which auto-saves to a single %APPDATA%\Vimium\config.json on every
/// change). Running them in one collection with parallelization disabled prevents
/// concurrent File.WriteAllText calls to the same file from throwing IOExceptions.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
#pragma warning disable RCS1102 // Make class static — xUnit collection-definition marker classes must be non-static.
public class ConfigSingletonCollection
#pragma warning restore RCS1102
{
    public const string Name = "ConfigService singleton";
}
