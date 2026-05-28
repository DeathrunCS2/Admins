using Admins.Commands.Admins;
using Admins.Commands.EconomySystem;
using Admins.Commands.LivesSystem;
using Admins.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Admins.Commands;


// ReSharper disable once InconsistentNaming
public static class AdminCommandsDI
{
    public static IServiceCollection AddAdminCommands(this IServiceCollection services)
    {
        //lives system
        services.AddSingleton<IAdminCommand, AddLivesCommand>();
        services.AddSingleton<IAdminCommand, TakeLivesCommand>();

        //economy system
        services.AddSingleton<IAdminCommand, AddCreditsCommand>();
        services.AddSingleton<IAdminCommand, TakeCreditsCommand>();
        
        //admins
        services.AddSingleton<IAdminCommand, AddAdminsCommand>();
        services.AddSingleton<IAdminCommand, RemoveAdminsCommand>();
        services.AddSingleton<IAdminCommand, ReloadAdminsCommand>();
        
        return services;
    }
}