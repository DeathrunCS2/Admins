using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Admins.Config;
using Admins.Extensions;
using Admins.Interfaces;
using Admins.Interfaces.Managers;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Shared;

namespace Admins.Managers;

internal class AdminsManager(
    IModSharp modSharp,
    ILogger<AdminsManager> logger) : IManager
{
    public static string ConnectionString { get; set; } = "";
    public static AdminsConfig AdminsConfig { get; set; } = null!;
    private static CommandsConfig? CommandsConfig { get; set; } = null;
    public static AdminsManager Instance { get; private set; } = null!;

    private static ILogger<AdminsManager> Logger { get; set; } = null!;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new () { WriteIndented = true };
    
    public bool Init()
    {
        Instance = this;
        Logger = logger;
        
        AdminsConfig = new();
        CommandsConfig = LoadCommandsConfig();
        
        //make commands callable from chat, console, server console
        RegisterCommands();
        
        GetDatabaseConfig();
        BuildDbConnectionString();
        SetupDatabaseTables();
        
        LoadPermissionsCollections();
        LoadAdminRolesList();
        GetAdminsListFromDatabase();
        
        return true;
    }
    
    public void Shutdown() { }
    
    #region Admin Table Manifest
    
    private static void GetDatabaseConfig()
    {
        try
        {
            var moduleName = Assembly.GetExecutingAssembly().GetName().Name;
            
            if (Directory.Exists(Admins.Instance.ConfigsPath + $"/Deathrun.Manager/modules/{moduleName}") is not true) 
                Directory.CreateDirectory(Admins.Instance.ConfigsPath + $"/Deathrun.Manager/modules/{moduleName}");
            
            {
                const string configFileName = "database.json";
                var databaseConfigPath = Path.Combine(Admins.Instance.ConfigsPath, $"Deathrun.Manager/modules/{moduleName}/{configFileName}");
                
                if (File.Exists(databaseConfigPath) is not true)
                    File.WriteAllText(databaseConfigPath, JsonSerializer
                        .Serialize(GetDefaultAdminsConfig().Storage, JsonSerializerOptions));
                
                AdminsConfig.Storage = JsonSerializer.Deserialize<AdminsStorage>(File.ReadAllText(databaseConfigPath)) ?? throw new Exception("Failed to load roles list");
                
                if (AdminsConfig.Storage.Database is "database_name")
                    throw new Exception("Database details not found in database.json. Please fill in the config file and try again.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load Deathrun Admins config. Falling back to defaults.");
        }
    }
    
    private static void LoadPermissionsCollections()
    {
        try
        {
            var moduleName = Assembly.GetExecutingAssembly().GetName().Name;
            
            if (Directory.Exists(Admins.Instance.ConfigsPath + $"/Deathrun.Manager/modules/{moduleName}") is not true) 
                Directory.CreateDirectory(Admins.Instance.ConfigsPath + $"/Deathrun.Manager/modules/{moduleName}");
            
            const string configFileName = "permissions.json";
            var permissionsCollectionsConfigPath = Path.Combine(Admins.Instance.ConfigsPath, $"Deathrun.Manager/modules/{moduleName}/{configFileName}");

            var permissions = GetDefaultAdminsConfig().Permissions;

            foreach (var command in CommandsConfig?.Commands ?? [])
            {
                if (permissions.Collections.TryGetValue(permissions.PermissionRegistryIdentity, out var cmdPermissions))
                    cmdPermissions.Add(permissions.PermissionRegistryIdentity + ":" + command.Value.Permission);
                else
                    permissions.Collections.TryAdd(permissions.PermissionRegistryIdentity, 
                        [ permissions.PermissionRegistryIdentity + ":" + command.Value.Permission ]);
            }
            
            File.WriteAllText(permissionsCollectionsConfigPath, JsonSerializer
                .Serialize(permissions, new JsonSerializerOptions { WriteIndented = true }));
            
            AdminsConfig.Permissions = JsonSerializer.Deserialize<PermissionCollections>(File.ReadAllText(permissionsCollectionsConfigPath)) ?? throw new Exception("Failed to load permissions collections");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load Deathrun Admins config. Falling back to defaults.");
        }
    }
    
    private static void LoadAdminRolesList()
    {
        try
        {
            var moduleName = Assembly.GetExecutingAssembly().GetName().Name;
            
            if (Directory.Exists(Admins.Instance.ConfigsPath + $"/Deathrun.Manager/modules/{moduleName}") is not true) 
                Directory.CreateDirectory(Admins.Instance.ConfigsPath + $"/Deathrun.Manager/modules/{moduleName}");
            
            {
                const string configFileName = "roles.json";
                var rolesConfigPath = Path.Combine(Admins.Instance.ConfigsPath, $"Deathrun.Manager/modules/{moduleName}/{configFileName}");
                
                if (File.Exists(rolesConfigPath) is not true)
                    File.WriteAllText(rolesConfigPath, JsonSerializer.Serialize(GetDefaultAdminsConfig().Roles, JsonSerializerOptions));
                
                AdminsConfig.Roles = JsonSerializer.Deserialize<List<RoleManifest>>(File.ReadAllText(rolesConfigPath)) ?? throw new Exception("Failed to load roles list");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load Deathrun Admins config. Falling back to defaults.");
        }
    }
    
    private void GetAdminsListFromDatabase()
    {
        try
        {
            var moduleName = Assembly.GetExecutingAssembly().GetName().Name;
            
            if (Directory.Exists(Admins.Instance.ConfigsPath + $"/Deathrun.Manager/modules/{moduleName}") is not true) 
                Directory.CreateDirectory(Admins.Instance.ConfigsPath + $"/Deathrun.Manager/modules/{moduleName}");
            
            Task.Run(async () =>
            {
                var adminsList = await GetAdminsFromDb() ?? throw new Exception("Failed to get admins list from database!");
                var serializedAdminList = JsonSerializer.Serialize(adminsList, JsonSerializerOptions);
                AdminsConfig.Admins = JsonSerializer.Deserialize<List<AdminManifest>>(serializedAdminList) ?? throw new Exception("Failed to load serialized Admin List");
                
                //Logger.LogInformation("Loaded {adminsCount} admins from database", AdminsConfig.Admins.Count);
                await modSharp.InvokeFrameActionAsync(MountAdminsManifest);
                
                //info: fixed an issue where the admin list needed 2 or more manual `refresh` commands to be loaded into the admin manager
                //this is a temporary fix until I figure out how to properly reload the admin list reliably
                //tried: delayed call via `PushTimer`
                ReloadAdmins();
            });

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load Deathrun Admins from database!");
        }
    }
    
    public void ReloadAdmins()
    {
        LoadPermissionsCollections();
        LoadAdminRolesList();
        GetAdminsListFromDatabase();
    }
    
    private static void MountAdminsManifest()
    {
        Admins
            .Instance
            .DeathrunManager
            .Managers
            .AdminManager
            .MountAdminManifest(Admins.BaseAdminModuleIdentity, BuildAdminsManifest );
    }

    private static AdminTableManifest BuildAdminsManifest() => new (AdminsConfig.Permissions.Collections, 
                                                                    AdminsConfig.Roles, 
                                                                    AdminsConfig.Admins);
    
    private static AdminsConfig GetDefaultAdminsConfig()
    {
        return new AdminsConfig()
        {
            Permissions =
            {
                Collections = new Dictionary<string, HashSet<string>>()
                {
                    {
                        "admin", new HashSet<string>()
                        {
                            "admin:ban",
                            "admin:unban",
                            "admin:mute",
                            "admin:silence",
                            "admin:gag",
                            "admin:kick",
                            "admin:say",
                            "admin:csay",
                            "admin:hsay",
                            "admin:psay",
                            "admin:noclip",
                            "admin:speed",
                            "admin:gravity",
                            "admin:tp",
                            "admin:bring",
                            "admin:freeze",
                            "admin:unfreeze",
                            "admin:slay",
                            "admin:slap",
                            "admin:hp",
                            "admin:respawn",
                            "admin:god",
                            "admin:give",
                            "admin:strip",
                            "admin:rename",
                            "admin:team",
                            "admin:money",
                            "admin:map",
                            "admin:rcon",
                            "admin:cvar"
                        }
                    }
                }
            }, 
            Roles = 
            [
                new ("root", 255, [ "*" ]),
                new ("serveradmin", 80, [ "@admin", "admin:ban", "admin:unban" ]),
                new ("admin", 60, 
                [ 
                    "admin:mute", "admin:silence", "admin:gag", "admin:kick",
                    "admin:say", "admin:csay", "admin:hsay", "admin:psay",
                    "admin:slay", "admin:slap", "admin:team", "admin:map"
                ])
            ],
            Admins = [],
            Storage = new AdminsStorage()
            {
                Host = "localhost",
                Database = "database_name",
                User = "database_user",
                Password = "",
                Port = 3306,
                TableName = "deathrun_admins"
            }
        };
    }
    
    #endregion
    
    #region Commands
    
    private CommandsConfig? LoadCommandsConfig()
    {
        try
        {
            var sharpPath = Path.Combine(modSharp.GetGamePath(), "../sharp");
            var configPathConstruct = Path.GetFullPath(Path.Combine(sharpPath, "configs"));
            
            //
            var moduleName = Assembly.GetExecutingAssembly().GetName().Name;
            const string configFileName = "commands.json";
            //
            
            if (Directory.Exists(configPathConstruct + $"/Deathrun.Manager/modules/{moduleName}") is not true) 
                Directory.CreateDirectory(configPathConstruct + $"/Deathrun.Manager/modules/{moduleName}");
        
            var configPath = Path.Combine(configPathConstruct, $"Deathrun.Manager/modules/{moduleName}/{configFileName}");
            if (File.Exists(configPath) is not true)
            {
                var loadedAdmins = new Dictionary<string, CommandInfo>();

                foreach (var adminCommand in Admins.Instance.ServiceProvider.GetServices<IAdminCommand>())
                    loadedAdmins.Add(adminCommand.CommandString, adminCommand.CommandInfo);
        
                File.WriteAllText(configPath, JsonSerializer.Serialize(new CommandsConfig() { Commands = loadedAdmins }, new JsonSerializerOptions { WriteIndented = true }));
            }
    
            return JsonSerializer.Deserialize<CommandsConfig>(File.ReadAllText(configPath))!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Deathrun Admins config. Falling back to defaults.");
            return null;
        }
    }

    private static void RegisterCommands()
    {
        var registeredCommands = new List<IAdminCommand>();

        foreach (var adminCommand in Admins.Instance.ServiceProvider.GetServices<IAdminCommand>())
        {
            //proceed only if the command is documented in config
            if (CommandsConfig?.Commands.ContainsKey(adminCommand.CommandString) is not true) continue;

            // Get command info from config (takes priority over C# object values)
            var commandInfoFromConfig = CommandsConfig.Commands[adminCommand.CommandString];

            var commandRegistry = Admins.Instance
                .DeathrunManager
                .Managers
                .AdminManager.GetCommandRegistry(Admins.BaseAdminModuleIdentity);

            commandRegistry.RegisterAdminCommand(adminCommand.CommandString, adminCommand.OnCommandExecute, [AdminsConfig.Permissions.PermissionRegistryIdentity + ":" + commandInfoFromConfig.Permission]);

            //check for aliases and register them if any
            if (commandInfoFromConfig.Aliases.Length > 0)
                foreach (var alias in commandInfoFromConfig.Aliases)
                    commandRegistry.RegisterAdminCommand(alias, adminCommand.OnCommandExecute, [AdminsConfig.Permissions.PermissionRegistryIdentity + ":" + commandInfoFromConfig.Permission]);

            commandRegistry.RegisterPermissions([AdminsConfig.Permissions.PermissionRegistryIdentity + ":" + commandInfoFromConfig.Permission]);

            registeredCommands.Add(adminCommand);
        }
        
        // var allRegisteredCommandsString = registeredCommands
        //     .Select(command =>
        //     {
        //         var aliases = command.CommandInfo.Aliases.Length > 0
        //             ? $" ({command.CommandInfo.Aliases.Aggregate((current, next) => $"{current}, {next}")})"
        //             : "";
        //
        //         return $"{command.CommandString}{aliases}";
        //     })
        //     .Aggregate((current, next) => $"{current} | {next}");
        var iterator = 0;
        var allRegisteredCommandsString = string.Join("   ", registeredCommands.Select(command =>
        {
            // Use config values for display
            var configInfo = CommandsConfig?.Commands[command.CommandString];

            if (configInfo?.Aliases.Length is 0 or null)
                return command.CommandString;

            iterator++;

            var commandString = AnsiColorMapExtension.Peach + command.CommandString + AnsiColorMapExtension.Reset;
            var aliases = $"{AnsiColorMapExtension.Gray}[{AnsiColorMapExtension.Reset} " + string.Join(", ", configInfo.Aliases) + $" {AnsiColorMapExtension.Gray}]{AnsiColorMapExtension.Reset}";

            return iterator % 4 is 0 ? $"\n{commandString} {aliases}" : $"{commandString} {aliases}";
        }));
        
        Console.WriteLine($"Registered admin commands: \n{allRegisteredCommandsString ?? "none"}", allRegisteredCommandsString);
    }
    
    #endregion
    
    #region Async methods

    private static async Task<List<AdminManifest>?> GetAdminsFromDb()
    {
        try
        {
            await using var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            var adminRows = await connection.QueryAsync<(ulong, int, string)>(
                $@"SELECT `steamid64`, `immunity`, `permissions`
                       FROM `{AdminsConfig.Storage.TableName}`
                       WHERE 1"

            );
            
            var adminsList = new List<AdminManifest>();
            
            foreach (var admin in adminRows.ToList())
            {
                var adminRecord = new AdminManifest
                    (admin.Item1, (byte) admin.Item2, admin.Item3.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).ToHashSet());
                
                adminsList.Add(adminRecord);

                // var immunityNum = Admins.Instance.DeathrunManager.Managers.AdminManager.GetAdmin(admin.Item1)?.Immunity ?? 0;
                // if (adminObject is null) return null;
                // Logger.LogInformation("Loaded Admin: {steamid} | Immunity: {immunity} | Permissions: {perms}", adminRecord.Identity, immunityNum, adminRecord.Permissions);
            }
            
            return adminsList;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    #endregion
    
    #region ConnectionString

    private static void BuildDbConnectionString() 
    {
        //build connection string
        ConnectionString = new MySqlConnectionStringBuilder
        {
            Database = AdminsConfig.Storage.Database,
            UserID = AdminsConfig.Storage.User,
            Password = AdminsConfig.Storage.Password,
            Server = AdminsConfig.Storage.Host,
            Port = (uint)AdminsConfig.Storage.Port,
        }.ConnectionString;
    }

    #endregion
    
    #region Tables

    private static void SetupDatabaseTables()
    {
        Task.Run(() => CreateDatabaseTable($@" CREATE TABLE IF NOT EXISTS `{AdminsConfig.Storage.TableName}` 
                                               (
                                                   `id` BIGINT NOT NULL AUTO_INCREMENT,
                                                   `steamid64` BIGINT(255) NOT NULL UNIQUE,
                                                   `immunity` INT(8) DEFAULT 0,
                                                   `permissions` TEXT NOT NULL,
                                                   `added_on` TEXT DEFAULT CURRENT_TIMESTAMP,
                                                    
                                                   PRIMARY KEY (id)
                                               )"));
    }
    
    private static async Task CreateDatabaseTable(string databaseTableStringStructure)
    {
        try
        {
            await using var dbConnection = new MySqlConnection(ConnectionString);
            dbConnection.Open();
            
            await dbConnection.ExecuteAsync(databaseTableStringStructure);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    #endregion
}