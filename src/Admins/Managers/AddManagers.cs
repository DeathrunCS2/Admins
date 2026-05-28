using Admins.Commands;
using Admins.Extensions.DependencyInjectionExtensions;
using Admins.Interfaces;
using Admins.Interfaces.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Admins.Managers;


// ReSharper disable once InconsistentNaming
public static class ManagersDI
{
    public static IServiceCollection AddManagers(this IServiceCollection services)
    {
        services.AddAdminCommands();
        
        services.AddSingleton<IBaseInterface, IManager, AdminsManager>();

        return services;
    }
}