using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using DeathrunManager.Shared;
using DeathrunManager.Shared.DeathrunObjects;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using ExampleModule.Interfaces.Services;
using ExampleModule.Config;
using Sharp.Shared;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Managers;

namespace ExampleModule.Services;

internal sealed class ExampleService(
    IModSharp modSharp,
    ILogger<ExampleService> logger,
    IClientManager clientManager,
    IDeathrunManager deathrunManagerApi) : IService
{
    private ExampleModuleConfig _config = new();

    public bool Init()
    {
        //_config = LoadConfig();
        
        clientManager.InstallCommandCallback("test", OnCommand);
        
        deathrunManagerApi.Managers.PlayersManager.ThinkPost += OnDeathrunPlayerThinkPost;
        deathrunManagerApi.Managers.GameplayManager.DeathrunPlayerSpawned += OnDeathrunPlayerSpawned;
        deathrunManagerApi.Managers.GameplayManager.DeathrunPlayerKilled += OnDeathrunPlayerKilled;
        deathrunManagerApi.Managers.GameplayManager.RoundEnded += OnDeathrunRoundEnded;
        
        logger.LogInformation("Example module initialized!");
        
        return true;
    }

    public void Shutdown()
    {
        clientManager.RemoveCommandCallback("test", OnCommand);
        
        deathrunManagerApi.Managers.PlayersManager.ThinkPost -= OnDeathrunPlayerThinkPost;
        deathrunManagerApi.Managers.GameplayManager.DeathrunPlayerSpawned -= OnDeathrunPlayerSpawned;
        deathrunManagerApi.Managers.GameplayManager.DeathrunPlayerKilled -= OnDeathrunPlayerKilled;
        deathrunManagerApi.Managers.GameplayManager.RoundEnded -= OnDeathrunRoundEnded;
        
        logger.LogInformation("Shutdown Example module!");
    }
    
    #region Listeners
    
    private static void OnDeathrunPlayerThinkPost(IDeathrunPlayer deathrunPlayer) { }
    
    private static void OnDeathrunPlayerSpawned(IDeathrunPlayer deathrunPlayer) { }
    
    private static void OnDeathrunPlayerKilled(IDeathrunPlayer victimDPlayer, 
                                               IDeathrunPlayer attackerDPlayer, 
                                               IBaseEntity? attackerWeaponEntity, 
                                               float damageTaken, float damageTakenTotal) { }
    
    private static void OnDeathrunRoundEnded() { }
    
    #endregion
    
    #region Commands
    
    private ECommandAction OnCommand(IGameClient client, StringCommand command)
    {
        var deathrunPlayer = deathrunManagerApi.Managers.PlayersManager.GetDeathrunPlayer(client);
        if (deathrunPlayer is null) return ECommandAction.Stopped;

        //
        
        return ECommandAction.Stopped;
    }

    #endregion
    
    #region Config
    
    private ExampleModuleConfig LoadConfig()
    {
        try
        {
            var sharpPath = Path.Combine(modSharp.GetGamePath(), "../sharp");
            var configPathConstruct = Path.GetFullPath(Path.Combine(sharpPath, "configs"));
            
            //
            var moduleName = Assembly.GetExecutingAssembly().GetName().Name;
            const string configFileName = "config.json";
            //
            
            if (Directory.Exists(configPathConstruct + $"/Deathrun.Manager/modules/{moduleName}") is not true) 
                Directory.CreateDirectory(configPathConstruct + $"/Deathrun.Manager/modules/{moduleName}");
        
            var configPath = Path.Combine(configPathConstruct, $"Deathrun.Manager/modules/{moduleName}/{configFileName}");
            if (File.Exists(configPath) is not true) CreateConfig(configPath);
    
            return JsonSerializer.Deserialize<ExampleModuleConfig>(File.ReadAllText(configPath))!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Deathrun ExampleModule config. Falling back to defaults.");
            return new ExampleModuleConfig();
        }
    }
    
    private static void CreateConfig(string configPath)
    {
        var config = new ExampleModuleConfig ();
            
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    public void ReloadExampleModuleConfig() { _config = LoadConfig(); }
    
    #endregion
}
