## Admins extension for [Deathrun Manager](https://github.com/DeathrunCS2/DeathrunManager)

An abstraction layer for the ModSharp's AdminManager FPM module that provides database-backed admin permissions and commands for managing players, economy, and lives systems etc..

## Features

- **Database storage** - MySQL storage for persistent admin data
- **Permission-based commands** - Granular permission system with roles and immunity levels
- **Economy management** - Add/remove credits from players
- **Lives system management** - Modify player lives
- **Runtime admin management** - Add/remove admins without server restart
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

## Admin Roles

Admin roles provide a convenient way to group permissions together. Instead of assigning individual permissions to each admin, you can assign them a role that contains multiple permissions.

### Role Configuration

Roles are defined in `sharp/configs/Deathrun.Manager/modules/Admins/roles.json`:

```json
[
  {
    "Identity": "root",
    "Immunity": 255,
    "Permissions": [
      "*"
    ]
  },
  {
    "Identity": "serveradmin",
    "Immunity": 80,
    "Permissions": [
      "@admin",
      "admin:ban",
      "admin:unban"
    ]
  },
  {
    "Identity": "admin",
    "Immunity": 60,
    "Permissions": [
      "admin:mute",
      "admin:silence",
      "admin:gag",
      "admin:kick",
      "admin:say",
      "admin:csay",
      "admin:hsay",
      "admin:psay",
      "admin:slay",
      "admin:slap",
      "admin:team",
      "admin:map"
    ]
  }
]
```

### Default Roles

The module includes three default roles:

| Role | Immunity | Description |
|------|----------|-------------|
| **root** | 255 | Full access - all permissions (`*`) |
| **serveradmin** | 80 | High-level admin with ban/unban capabilities plus all `@admin` permissions |
| **admin** | 60 | Standard admin with moderation and utility commands |

### Using Roles

When adding an admin, you can reference roles using the `@` prefix:

```bash
# Grant the "admin" role
/addadmin 76561198012345678 60 @admin

# Grant multiple roles
/addadmin 76561198012345678 80 @admin,@serveradmin

# Mix roles and individual permissions
/addadmin 76561198012345678 70 @admin,deathrun.manager:addcredits
```

### Immunity System

Immunity determines the hierarchy between admins:
- Higher immunity can target lower immunity admins
- Admins cannot target other admins with equal or higher immunity
- Immunity range: `0-255` (higher = more powerful)

### Permission Format

Permissions follow the format: `registry:permission`

- **Wildcard**: `*` grants all permissions
- **Role reference**: `@rolename` grants all permissions from that role
- **Specific permission**: `deathrun.manager:addcredits` grants one specific permission

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
