using DeathrunManager.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using ExampleModule.Extensions;
using ExampleModule.Interfaces;
using ExampleModule.Interfaces.Services;
using ExampleModule.Services;

namespace ExampleModule;

public sealed class ExampleModule(ISharedSystem sharedSystem, IDeathrunManager deathrunManagerApi) : IDeathrunModule
{
    public string Name                                                 => "Environment Modifier for Deathrun mode";
    public string Author                                               => "AquaVadis";

    public IDeathrunManager DeathrunManager { get; }                   = deathrunManagerApi;
    public required ServiceProvider ServiceProvider                    { get; set; }

    private ILogger<ExampleModule> Logger { get; set; }                = sharedSystem.GetLoggerFactory().CreateLogger<ExampleModule>();

    public bool Init(bool hotReload)
    {
        var services = new ServiceCollection();

        services.AddSingleton(this);
        services.AddSingleton(DeathrunManager);
        services.AddSingleton(sharedSystem);
        services.AddSingleton(sharedSystem.GetModSharp());
        services.AddSingleton(sharedSystem.GetHookManager());
        services.AddSingleton(sharedSystem.GetEntityManager());
        services.AddSingleton(sharedSystem.GetClientManager());
        services.AddSingleton(sharedSystem.GetTransmitManager());
        services.AddSingleton(sharedSystem.GetLoggerFactory());
        
        services.AddSingleton<IBaseInterface, IService, ExampleService>();
        
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
        
        ServiceProvider = services.BuildServiceProvider();

        return DependencyInjectionExtensions.CallInit(ServiceProvider, Logger) >= 0;
    }

    public void Shutdown(bool hotReload)
    {
        DependencyInjectionExtensions.CallShutdown(ServiceProvider, Logger);
    }
}
