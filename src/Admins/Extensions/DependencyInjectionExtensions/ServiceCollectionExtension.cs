using System;
using Admins.Commands.Admins;
using Admins.Commands.EconomySystem;
using Admins.Commands.LivesSystem;
using Admins.Interfaces;
using Admins.Interfaces.Managers;
using Admins.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Admins.Extensions.DependencyInjectionExtensions;

internal static class DependencyInjectionExtensions
{
    internal class DiCallers
    {
        public DiCallers()
        {
            
        }
    }
    
    public static IServiceCollection AddLogging(this IServiceCollection services, ILoggerFactory factory)
    {
        services.AddSingleton(factory);
        services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        return services;
    }

    public static IServiceCollection AddSingleton<TService1, TService2, T>(this IServiceCollection services)
        where T : class, TService1, TService2
        where TService1 : class
        where TService2 : class
    {
        services.AddSingleton<T>();
        services.AddSingleton<TService1, T>(x => x.GetRequiredService<T>());
        services.AddSingleton<TService2, T>(x => x.GetRequiredService<T>());

        return services;
    }

    #region Init/Shutdown DI Callers

    public static int CallInit(IServiceProvider serviceProvider, ILogger logger)
    {
        var init = 0;

        foreach (var service in serviceProvider.GetServices<IBaseInterface>())
        {
            if (service.Init() is not true)
            {
                logger.LogCritical(service.GetType().Name, "Failed to init service!");
                return -1;
            }

            init++;
        }

        return init;
    }

    public static void CallShutdown(IServiceProvider serviceProvider, ILogger logger)
    {
        foreach (var service in serviceProvider.GetServices<IBaseInterface>())
        {
            try
            {
                service.Shutdown();
            }
            catch (Exception e)
            {
                logger.LogCritical(service.GetType().Name, $"Shutdown error | {e}");
            }
        }
    }

    #endregion
}
