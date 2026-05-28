using System.Collections.Generic;
using Admins.Managers;
using Sharp.Modules.AdminManager.Shared;

namespace Admins.Config;

public class AdminsConfig
{
    public PermissionCollections Permissions { get; set; } = new ();
    public List<RoleManifest> Roles { get; set; } = [];
    public List<AdminManifest> Admins { get; set; } = [];
    public AdminsStorage Storage { get; set; } = new();
}

public class PermissionCollections
{
    public string PermissionRegistryIdentity { get; init; } = "deathrun.manager";
    public Dictionary<string, HashSet<string>> Collections { get; set; } = [];
}

public class AdminsStorage
{
    public string Host { get; init; } = "localhost";
    public string Database { get; init; } = "database_name";
    public string User { get; init; } = "database_user";
    public string Password { get; init; } = "database_password";
    public int Port { get; init; } = 3306;
    public string TableName { get; init; } = "deathrun_admins";
}