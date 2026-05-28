using DeathrunManager.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Admins.Extensions.DependencyInjectionExtensions;
using Admins.Managers;
using DeathrunManager.Shared.Objects;

namespace Admins;

public sealed class Admins(ISharedSystem sharedSystem, IDeathrunManager deathrunManagerApi) : IDeathrunModule
{
    public string Name                                                 => "Admins";
    public string Author                                               => "AquaVadis";

    public IDeathrunManager DeathrunManager { get; }                   = deathrunManagerApi;
    public required ServiceProvider ServiceProvider                    { get; set; }

    public static Admins Instance { get; private set; }                = null!;
    public static string BaseAdminModuleIdentity { get; private set; } = ""; 
    public string ConfigsPath { get; private set; }                    = deathrunManagerApi.CommonVars.ConfigsPath;
    
    private ILogger<Admins> Logger { get; }                            = sharedSystem.GetLoggerFactory().CreateLogger<Admins>();

    public bool Init(bool hotReload)
    {
        Instance = this;
        
        //target the identity of the module that is the setting the default admin config
        BaseAdminModuleIdentity = "Sharp.Modules.AdminManager";
        
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

        services.AddManagers();
        
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
        
        ServiceProvider = services.BuildServiceProvider();

        return DependencyInjectionExtensions.CallInit(ServiceProvider, Logger) >= 0;
    }

    public void Shutdown(bool hotReload)
    {
        DependencyInjectionExtensions.CallShutdown(ServiceProvider, Logger);
    }
}
