using ExampleModule.Models;

namespace ExampleModule.Config;

internal sealed class ExampleModuleConfig
{
    public ExampleModuleConfigModel Data { get; init; } = new();
}
