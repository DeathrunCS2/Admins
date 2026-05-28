## Admins extension for [Deathrun Manager](https://github.com/DeathrunCS2/DeathrunManager)

A comprehensive admin management module for Deathrun Manager that provides database-backed admin permissions and commands for managing players, economy, and lives systems.

## Features

- **Database-backed admin system** - MySQL storage for persistent admin data
- **Permission-based commands** - Granular permission system with roles and immunity levels
- **Economy management** - Add/remove credits from players
- **Lives system management** - Modify player lives
- **Runtime admin management** - Add/remove admins without server restart
- **Command aliases** - Multiple aliases for each command
- **Player targeting** - Support for targeting players by name or pattern

## Configuration

Most importantly you need to configure the database connection in `sharp/configs/Deathrun.Manager/modules/Admins/database.json`.

> [!WARNING]
> The database driver must be *mysql*.

```json

{
  "Host": "localhost",
  "Database": "mysqldbname",
  "User": "dbuser",
  "Password": "dbpassword",
  "Port": 3306,
  "TableName": "deathrun_admins"
}

```

## Commands

### Admin Management

#### `/addadmin` (aliases: `giveadmin`)
**Permission:** `addadmin`
**Usage:** `/addadmin <steamid64> <immunity> <permissions>`
**Description:** Adds a new admin to the database with specified immunity level and permissions.
**Remark:** Permissions are comma-separated. Use `*` to grant all permissions.
**Remark:** Accepts roles and specific strings as permissions if defined in the config.
**Example:**
```
/addadmin 76561198012345678 100 @admin,deathrun.manager:addcredits,deathrun.manager:takecredits
```

#### `/removeadmin` (aliases: `takeadmin`)
**Permission:** `removeadmin`
**Usage:** `/removeadmin <steamid64>`
**Description:** Removes admin privileges from the specified SteamID64.

**Example:**
```
/removeadmin 76561198012345678
```

#### `/reloadadmins` (aliases: `refreshadmins`)
**Permission:** `reloadadmins`
**Usage:** `/reloadadmins`
**Description:** Reloads the admin cache from the database without restarting the server.

### Economy System

#### `/addcredits` (aliases: `givecredits`)
**Permission:** `addcredits`
**Usage:** `/addcredits <name> <amount>`
**Description:** Adds credits to the target player's account.

**Example:**
```
/addcredits PlayerName 1000
```

#### `/takecredits` (aliases: `removecredits`)
**Permission:** `takecredits`
**Usage:** `/takecredits <name> <amount>`
**Description:** Removes credits from the target player's account.

**Example:**
```
/takecredits PlayerName 500
```

### Lives System

#### `/addlives` (aliases: `givelives`)
**Permission:** `addlives`
**Usage:** `/addlives <name> <amount>`
**Description:** Adds lives to the target player.

**Example:**
```
/addlives PlayerName 3
```

#### `/takelives` (aliases: `removelives`)
**Permission:** `takelives`
**Usage:** `/takelives <name> <amount>`
**Description:** Removes lives from the target player.

**Example:**
```
/takelives PlayerName 2
```
