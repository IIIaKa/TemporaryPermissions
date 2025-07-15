# Temporary Permissions  

Useful plugin for managing **temporary permissions**, **temporary groups** and **temporary permissions for groups**. This is done through **chat commands**, built-in **Oxide commands**, and **API methods**.  
**Note:** The dates is in **UTC** format.  

## Contacts  

- **Telegram:** https://t.me/iiiaka  
- **Discord:** @iiiaka  
- **GitHub:** https://github.com/IIIaKa  
- **uMod:** https://umod.org/user/IIIaKa  
- **Codefling:** https://codefling.com/iiiaka  
- **LoneDesign:** https://lone.design/vendor/iiiaka/  
- **GitHub repository page:** https://github.com/IIIaKa/TemporaryPermissions  

## Donations  

- **USDT TRC20:** `TLN9Tsrdmt96yFCXZTfh4NLtyzWYGkqTA3`  
- **USDT TON:** `UQDma5Ovkk7M9Qve-4P2njmrgSXZQdACU0gLCGNEgkXDlngn`  
- **TON:** `UQDma5Ovkk7M9Qve-4P2njmrgSXZQdACU0gLCGNEgkXDlngn`  

## Features  

- The ability to **grant players temporary permissions** by specifying either **the number of seconds**, an **exact expiration date** or **until the wipe** occurs;  
- The ability to **add players to temporary groups** by specifying either **the number of seconds**, an **exact expiration date** or **until the wipe** occurs;  
- The ability to **grant groups temporary permissions** by specifying either **the number of seconds**, an **exact expiration date** or **until the wipe** occurs;  
- The ability to **revoke temporary permissions** from **players** and **groups** prematurely;  
- The ability to **remove players from groups** prematurely;  
- The ability to **perform all the above actions** using **existing and familiar console commands**(e.g., **`o.grant`**), simply by adding **the number of seconds**, **the expiration date** or the word "**wipe**" at the end;  
- The ability to **perform all the above actions** using a **chat command** (by default **`/tperm`**);  
- The ability to **perform all the above actions** using **API methods**;  
- The ability to **remove all temporary permissions** and **groups** upon **wipe** detection.  

## Permissions  

- **`temporarypermissions.admin`** - Grants access to **the admin command**(by default **`/tperm`**).  

## Default Configuration  

```json
{
  "Chat command": "myperm",
  "Chat admin command": "tperm",
  "Is it worth enabling GameTips for messages?": true,
  "Is it worth saving logs to a file?": true,
  "Is it worth using console logging?": true,
  "List of language keys for creating language files": [
    "en"
  ],
  "Interval in seconds for expiration check": 1.0,
  "Interval in seconds for checking the presence of temporary permissions and temporary groups. A value of 0 disables the check": 600.0,
  "Is it worth restoring removed temporary permissions and temporary groups if the timer hasn't expired? There are cases where removal cannot be tracked in the usual way": true,
  "Is it worth revoking temporary permissions and temporary groups when unloading the plugin, without removing them from the data file?": true,
  "Is it worth revoking temporary permissions and temporary groups that haven't expired yet upon detecting a wipe?": false,
  "Custom wipe date(detected only during initialization). Only required if you're experiencing issues with the Wipe ID. Leave empty to use the Wipe ID. Example: 2025-06-25 13:00": "",
  "Wipe ID": null,
  "Version": {
    "Major": 0,
    "Minor": 1,
    "Patch": 6
  }
}
```  

## Localization  

### EN  
```json
{
  "CmdAdmin": "Available admin commands:\n\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *nameOrId* realpve.vip wipe</color> - Grants or extends the specified permission for the specified player until the end of the current wipe\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *nameOrId* realpve.vip *intValue* *boolValue*(optional)</color> - Grants or extends the specified permission for the specified player for the given number of seconds\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *nameOrId* realpve.vip *expirationDate* *assignmentDate*(optional)</color> - Grants or extends the specified permission for the specified player until the given date\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *groupName* realpve.vip wipe</color> - Grants or extends the specified permission for the specified group until the end of the current wipe\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *groupName* realpve.vip *intValue* *boolValue*(optional)</color> - Grants or extends the specified permission for the specified group for the given number of seconds\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *groupName* realpve.vip *expirationDate* *assignmentDate*(optional)</color> - Grants or extends the specified permission for the specified group until the given date\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>revoke user *nameOrId* realpve.vip</color> - Revokes the specified permission from the specified player\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>revoke group *groupName* realpve.vip</color> - Revokes the specified permission from the specified group\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *nameOrId* *groupName* wipe</color> - Adds or extends the specified player's membership in the specified group until the end of the current wipe\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *nameOrId* *groupName* *intValue* *boolValue*(optional)</color> - Adds or extends the specified player's membership in the specified group for the given number of seconds\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *nameOrId* *groupName* *expirationDate* *assignmentDate*(optional)</color> - Adds or extends the specified player's membership in the specified group until the given date\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>remove *nameOrId* *groupName*</color> - Removes the specified player from the specified group\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>wipe *wipeDate*</color> - Set a custom wipe date. Used in case of issues with the Wipe ID. Format: yyyy-MM-dd HH:mm\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>wipe reset</color> - Reset the custom wipe date\n\n<color=#D1CBCB>Optional values:</color>\n*boolValue* - If false(default) and an existing permission or group membership has not expired, the specified time will be added to the existing time. Otherwise, including when true, the specified time will be counted from the current time\n*assignmentDate* - If the assignment date is not specified and there is no existing permission or group membership, the assignment date will be set to the current time. If the assignment date is specified, it will be applied regardless of existing permissions or group memberships\n\n--------------------------------------------------",
  "CmdPermissionNotFound": "Permission '{0}' not found!",
  "CmdPlayerNotFound": "Player '{0}' not found! You must provide the player's name or ID.",
  "CmdMultiplePlayers": "Multiple players found for '{0}': {1}",
  "CmdGroupNotFound": "Group '{0}' not found!",
  "CmdGrantWrongFormat": "Incorrect command format! Example: /tperm grant user/group *nameOrId* realpve.vip *secondsOrDateTime*",
  "CmdRevokeWrongFormat": "Incorrect command format! Example: /tperm revoke user/group *nameOrId* realpve.vip",
  "CmdUserGroupWrongFormat": "Incorrect command format! Example: /tperm group add/remove *nameOrId* *groupName*",
  "CmdUserGranted": "Permission '{0}' granted to player '{1}'.",
  "CmdGroupGranted": "Permission '{0}' granted to group '{1}'.",
  "CmdUserGroupAdded": "Player '{0}' has been added to group '{1}'.",
  "CmdUserRevoked": "Permission '{0}' has been revoked for player '{1}'.",
  "CmdGroupRevoked": "Permission '{0}' has been revoked for group '{1}'.",
  "CmdUserGroupRemoved": "Player '{0}' has been removed from group '{1}'.",
  "CmdWipeNew": "New wipe date successfully set to '{0}'. The wipe will take effect only after the plugin is loaded following this date.",
  "CmdWipeReset": "The custom wipe date has been reset. The wipe is now determined by the Wipe ID.",
  "CmdWipeFailed": "The specified date '{0}' has an invalid format(yyyy-MM-dd HH:mm) or has already passed. Example: '{1}'.",
  "CmdCheckNoActive": "You have no active temporary permissions or temporary groups!",
  "CmdCheckTargetNoActive": "Player '{0}' has no active temporary permissions or temporary groups!",
  "CmdCheckPermissions": "<color=#D1AB9A>You have {0} temporary permissions(time in UTC):</color>\n{1}",
  "CmdCheckGroups": "<color=#D1AB9A>You have {0} temporary groups(time in UTC):</color>\n{1}",
  "CmdCheckTargetPermissions": "<color=#D1AB9A>Player '{2}' has {0} temporary permissions(time in UTC):</color>\n{1}",
  "CmdCheckTargetGroups": "<color=#D1AB9A>Player '{2}' has {0} temporary groups(time in UTC):</color>\n{1}",
  "CmdCheckFormatPermissions": "'{0}' - {1}({2})",
  "CmdCheckFormatGroups": "'{0}' - {1}({2})",
  "CmdUntilWipe": "Until Wipe"
}
```  

### RU  
```json
{
  "CmdAdmin": "Доступные админ команды:\n\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *имяИлиАйди* realpve.vip wipe</color> - Выдать или продлить указанный пермишен указанному игроку до конца текущего вайпа\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *имяИлиАйди* realpve.vip *числовоеЗначение* *булевоеЗначение*(опционально)</color> - Выдать или продлить указанный пермишен указанному игроку на указанное количество секунд\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *имяИлиАйди* realpve.vip *датаИстечения* *датаНазначения*(опционально)</color> - Выдать или продлить указанный пермишен указанному игроку до указанной даты\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *имяГруппы* realpve.vip wipe</color> - Выдать или продлить указанный пермишен указанной группе до конца текущего вайпа\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *имяГруппы* realpve.vip *числовоеЗначение* *булевоеЗначение*(опционально)</color> - Выдать или продлить указанный пермишен указанной группе на указанное количество секунд\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *имяГруппы* realpve.vip *датаИстечения* *датаНазначения*(опционально)</color> - Выдать или продлить указанный пермишен указанной группе до указанной даты\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>revoke user *имяИлиАйди* realpve.vip</color> - Снять указанный пермишен у указанного игрока\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>revoke group *имяГруппы* realpve.vip</color> - Снять указанный пермишен у указанной группы\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *имяИлиАйди* *имяГруппы* wipe</color> - Добавить или продлить пребывание в указанной группе указанному игроку до конца текущего вайпа\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *имяИлиАйди* *имяГруппы* *числовоеЗначение* *булевоеЗначение*(опционально)</color> - Добавить или продлить пребывание в указанной группе указанному игроку на указанное количество секунд\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *имяИлиАйди* *имяГруппы* *датаИстечения* *датаНазначения*(опционально)</color> - Добавить или продлить пребывание в указанной группе указанному игроку до указанной даты\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>remove *имяИлиАйди* *имяГруппы*</color> - Отменить пребывание в указанной группе указанному игроку\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>wipe *датаВайпа*</color> - Установка кастомной даты вайпа. Используется при проблемах с Wipe ID. Формат: yyyy-MM-dd HH:mm\n<color=#D1CBCB>/tperm</color> <color=#D1AB9A>wipe reset</color> - Сброс кастомной даты вайпа\n\n<color=#D1CBCB>Опциональные значения:</color>\n*булевоеЗначение* - Если false(по умолчанию) и существующий пермишен или группа не истекли, указанное время будет добавлено к существующему времени. В противном случае, в т.ч. при true, указанное время будет отсчитываться от текущего времени\n*датаНазначения* - Если дата назначения не указана и нет существующего пермишена или группы, дата назначения будет равна текущей. Если дата назначения указана, то вне зависимости от существования пермишенов или групп, присвоится указанная дата\n\n--------------------------------------------------",
  "CmdPermissionNotFound": "Пермишен '{0}' не найден!",
  "CmdPlayerNotFound": "Игрок '{0}' не найден! Вы должны указать имя или ID игрока.",
  "CmdMultiplePlayers": "По значению '{0}' найдено несколько игроков: {1}",
  "CmdGroupNotFound": "Группа '{0}' не найдена!",
  "CmdGrantWrongFormat": "Не верный формат команды! Пример: /tperm grant user/group *имяИлиАйди* realpve.vip *секундыИлиДата*",
  "CmdRevokeWrongFormat": "Не верный формат команды! Пример: /tperm revoke user/group *имяИлиАйди* realpve.vip",
  "CmdUserGroupWrongFormat": "Не верный формат команды! Пример: /tperm group add/remove *имяИлиАйди* *имяГруппы*",
  "CmdUserGranted": "Пермишен '{0}' выдан игроку '{1}'.",
  "CmdGroupGranted": "Пермишен '{0}' выдан группе '{1}'.",
  "CmdUserGroupAdded": "Игрок '{0}' был добавлен в группу '{1}'.",
  "CmdUserRevoked": "Пермишен '{0}' был удален для игрока '{1}'.",
  "CmdGroupRevoked": "Пермишен '{0}' был удален для группы '{1}'.",
  "CmdUserGroupRemoved": "Игрок '{0}' был удален из группы '{1}'.",
  "CmdWipeNew": "Новая дата вайпа успешно установлена на '{0}'. Вайп вступит в силу только при загрузке плагина после этой даты.",
  "CmdWipeReset": "Кастомная дата вайпа была сброшена. Вайп снова определяется по Wipe ID.",
  "CmdWipeFailed": "Указанная дата '{0}' имеет неверный формат(yyyy-MM-dd HH:mm) или уже прошла. Пример: '{1}'.",
  "CmdCheckFormatGroups": "'{0}' - {1}({2})",
  "CmdCheckNoActive": "У вас нет активных временных пермишенов или временных групп!",
  "CmdCheckTargetNoActive": "У игрока '{0}' нет активных временных пермишенов или временных групп!",
  "CmdCheckPermissions": "<color=#D1AB9A>У вас есть {0} временных пермишенов(время по UTC):</color>\n{1}",
  "CmdCheckGroups": "<color=#D1AB9A>У вас есть {0} временных групп(время по UTC):</color>\n{1}",
  "CmdCheckTargetPermissions": "<color=#D1AB9A>У игрока '{2}' есть {0} временных пермишенов(время по UTC):</color>\n{1}",
  "CmdCheckTargetGroups": "<color=#D1AB9A>У игрока '{2}' есть {0} временных групп(время по UTC):</color>\n{1}",
  "CmdCheckFormatPermissions": "'{0}' - {1}({2})",
  "CmdUntilWipe": "До вайпа"
}
```  

## Commands  

- **/myperm** - Displays a list of **all <ins>your</ins> temporary permissions** and **temporary groups**;  
- **/myperm <displayName|userID>** - Displays a list of **all temporary permissions** and **temporary groups** of the **<ins>specified player</ins>**. Permission "**temporarypermissions.admin**" required.  

Admin commands(**/tperm**). Permission "**temporarypermissions.admin**" required:  
- **grant** - **Grants** a temporary permission to a **player** or **group**.
  - **user**
    - **<displayName|userID> realpve.vip wipe** - **Grants** a temporary permission to a player until the next wipe by specifying **the player's name** or **Id**, **the permission name** and the word "**wipe**";
    - **<displayName|userID> realpve.vip 3600 true/false** - **Grants** a temporary permission to a player by specifying **the player's name** or **Id**, **the permission name**, **the number of seconds** and **true/false**(optional).  
    If **false**(default) and an existing permission has not expired, the specified time will be added to the existing time. Otherwise, including when **true**, the specified time will be counted from the current time;
    - **<displayName|userID> realpve.vip "2024-08-19 17:57" "2024-08-19 16:57"** - **Grants** a temporary permission to a player by specifying **the player's name** or **Id**, **the permission name**, **the expiration date** and **the assigned date**(optional).  
    If the assignment date is **not specified** and there is no existing permission, the assignment date will be set to **the current time**. If the assignment date is **specified**, it will be applied regardless of existing permissions.
  - **group**
    - **<groupName> realpve.vip wipe** - **Grants** a temporary permission to a group until the next wipe by specifying **the group's name**, **the permission name** and the word "**wipe**";
    - **<groupName> realpve.vip 3600 true/false** - **Grants** a temporary permission to a group by specifying **the group's name**, **the permission name**, **the number of seconds**, and **true/false**(optional).  
    If **false**(default) and an existing group membership has not expired, the specified time will be added to the existing time. Otherwise, including when **true**, the specified time will be counted from the current time;
    - **<groupName> realpve.vip "2024-08-19 17:57" "2024-08-19 16:57"** - **Grants** a temporary permission to a group by specifying **the group's name**, **the permission name**, **the expiration date** and **the assigned date**(optional).  
    If the assignment date is **not specified** and there is no existing group membership, the assignment date will be set to **the current time**. If the assignment date is **specified**, it will be applied regardless of existing group memberships.
- **revoke** - **Revokes** a temporary permission from a **player** or **group**.
  - **user <displayName|userID> realpve.vip** - **Revokes** a temporary permission from a player by specifying **the player's name** or **Id** and **the permission name**;
  - **group <groupName> realpve.vip** - **Revokes** a temporary permission from a group by specifying **the group's name** and **the permission name**.
- **add** - Temporary **addition** of a **player to a group**.
  - **<displayName|userID> <groupName> wipe** - Temporary **addition** of a player to a group until the next wipe by specifying **the player's name** or **Id**, **the group name** and the word "**wipe**";
  - **<displayName|userID> <groupName> 3600 true/false** - Temporary **addition** of a player to a group by specifying **the player's name** or **Id**, **the group name**, **the number of seconds**, and **true/false**(optional).  
  If **true**, the specified seconds will count from **the current moment**, otherwise(default), they will be added to the existing time;
  - **<displayName|userID> <groupName> "2024-08-19 17:57" "2024-08-19 16:57"** - Temporary **addition** of a player to a group by specifying **the player's name** or **Id**, **the group name**, **the expiration date** and **the assigned date**(optional).  
  If **not specified**, the assigned date will default to **the current date**, otherwise, it will be set to the provided date.
- **remove <displayName|userID> <groupName>** - **Removal** of a **player from a temporary group** by specifying **the player's name** or **Id** and **the group name**.
- **wipe** - Setting a custom wipe date. Used if you're experiencing issues with using the Wipe ID.
  - **<wipeDate>** - Set a custom wipe date(yyyy-MM-dd HH:mm). Used in case of issues with the Wipe ID;
  - **reset** - Reset the custom wipe date.

**Example:**
- **/tperm grant user iiiaka realpve.vip wipe**
- **/tperm grant user iiiaka realpve.vip 3600 true**
- **/tperm grant user iiiaka realpve.vip "2024-08-19 17:57" "2024-08-19 16:57"**
- **/tperm wipe "2025-06-25 13:00"**

**Note:** To access the commands, the player must be an admin(**console** or **owner**) or have the **`temporarypermissions.admin`** permission.  

**P.S.** Templates for the commands above can also be used with **existing console commands**.  
For example: **`o.grant user iiiaka realpve.vip 3600 true`**

## Developer Hooks

### OnTemporaryPermissionsLoaded
Called after the **TemporaryPermissions** plugin is **fully loaded** and **ready**.  
No return behaviour.

```csharp
void OnTemporaryPermissionsLoaded(VersionNumber version = default)
{
  Puts("The TemporaryPermissions plugin is loaded and ready to go!");
}
```

### OnTemporaryPermissionGranted
Called after the **player** has been **granted** a temporary permission.  
No return behaviour.

```csharp
void OnTemporaryPermissionGranted(string userID, string perm, DateTime expireDate, DateTime assignedDate)
{
  Puts($"Player {userID} has been granted the temporary permission {perm} from {assignedDate} until {expireDate}.");
}
```

### OnTemporaryPermissionUpdated
Called after the **player's** temporary permission has been **updated**.  
No return behaviour.

```csharp
void OnTemporaryPermissionUpdated(string userID, string perm, DateTime expireDate, DateTime assignedDate)
{
  Puts($"Player {userID}'s temporary permission {perm} has been updated. New expiration date: {expireDate}. Assigned date: {assignedDate}.");
}
```

### OnTemporaryPermissionRevoked
Called after the **player's** temporary permission has **expired** or been **revoked**.  
No return behaviour.

```csharp
void OnTemporaryPermissionRevoked(string userID, string perm, bool isExpired)
{
  Puts($"Player {userID} has had the temporary permission {perm} revoked. Permission expired: {isExpired}.");
}
```

### OnTemporaryGroupAdded
Called after the **player** has been temporarily **added to the group**.  
No return behaviour.

```csharp
void OnTemporaryGroupAdded(string userID, string groupName, DateTime expireDate, DateTime assignedDate)
{
  Puts($"Player {userID} has been added to the temporary group {groupName} from {assignedDate} until {expireDate}.");
}
```

### OnTemporaryGroupUpdated
Called after the **player's** temporary **group** has been **updated**.  
No return behaviour.

```csharp
void OnTemporaryGroupUpdated(string userID, string groupName, DateTime expireDate, DateTime assignedDate)
{
  Puts($"Player {userID}'s temporary group {groupName} has been updated. New expiration date: {expireDate}. Assigned date: {assignedDate}.");
}
```

### OnTemporaryGroupRemoved
Called after the **player's** temporary **group** has **expired** or been **removed**.  
No return behaviour.

```csharp
void OnTemporaryGroupRemoved(string userID, string groupName, bool isExpired)
{
  Puts($"Player {userID} has had the temporary group {groupName} revoked. Group expired: {isExpired}.");
}
```

### OnGroupTemporaryPermissionGranted
Called after the **group** has been **granted** a temporary permission.  
No return behaviour.

```csharp
void OnGroupTemporaryPermissionGranted(string groupName, string perm, DateTime expireDate, DateTime assignedDate)
{
  Puts($"Group {groupName} has been granted the temporary permission {perm}, valid from {assignedDate} until {expireDate}.");
}
```

### OnGroupTemporaryPermissionUpdated
Called after the **group's** temporary permission has been **updated**.  
No return behaviour.

```csharp
void OnGroupTemporaryPermissionUpdated(string groupName, string perm, DateTime expireDate, DateTime assignedDate)
{
  Puts($"Group {groupName}'s temporary permission {perm} has been updated. New expiration date: {expireDate}. Assigned date: {assignedDate}.");
}
```

### OnGroupTemporaryPermissionRevoked
Called after the **group's** temporary permission has **expired** or been **revoked**.  
No return behaviour.

```csharp
void OnGroupTemporaryPermissionRevoked(string groupName, string perm, bool isExpired)
{
  Puts($"Group {groupName} has had the temporary permission {perm} revoked. Permission expired: {isExpired}.");
}
```

## Developer API

```csharp
[PluginReference]
private Plugin TemporaryPermissions;
```

There are 30 methods:
- **IsReady**
- _User's Permissions:_
  - **GrantUserPermission**
  - **RevokeUserPermission**
  - **UserHasPermission**
  - **GrantActiveUsersPermission**
  - **GrantAllUsersPermission**
  - **RevokeActiveUsersPermission**
  - **RevokeAllUsersPermission**
  - **UserGetAllPermissions**
  - **UserGetPermissionByExpiry**
  - **UserGetAllPermissionsByExpiry**
  - **ActiveUsersGetAllPermissions**
  - **AllUsersGetAllPermissions**
- _User's Groups:_
  - **AddUserGroup**
  - **RemoveUserGroup**
  - **UserHasGroup**
  - **AddActiveUsersGroup**
  - **AddAllUsersGroup**
  - **RemoveActiveUsersGroup**
  - **RemoveAllUsersGroup**
  - **UserGetAllGroups**
  - **ActiveUsersGetAllGroups**
  - **AllUsersGetAllGroups**
- _Group's Permissions:_
  - **GrantGroupPermission**
  - **RevokeGroupPermission**
  - **GroupHasPermission**
  - **GrantAllGroupsPermission**
  - **RevokeAllGroupsPermission**
  - **GroupGetAllPermissions**
  - **AllGroupsGetAllPermissions**

### IsReady
Used to check if the **TemporaryPermissions** plugin is **loaded** and **ready** to work.  
The **IsReady** method returns **true if it is ready**, or **null if it is not**.

```csharp
(bool)TemporaryPermissions?.Call("IsReady");//Calling the IsReady method. If the result is not null(bool true), the plugin is ready.
```  

### GrantUserPermission
Used to **grant a temporary permission** to a **player**.  
Returns **true if the grant was successful**.  
To call the **GrantUserPermission** method, you need to pass 5 parameters, 3 of which are optional:
1. **IPlayer** or **BasePlayer** or <string>**playerID** - **The player object** or their **Id**;
2. <string>**permName** - **The name of the permission**;
3. <int>**secondsToAdd** or <DateTime>**expireDate** - **Optional**. The time in **seconds to add**, or **the end date**. If a **number less than 1** is specified, or **if this parameter is not specified**, **the permission** will be valid **until the wipe**;
4. <bool>**fromNow** or <DateTime>**assignedDate** - **Optional**. **true/false** to specify whether the seconds should be added to the current date or to the existing time, or an exact assignment date. Defaults to the current date;
5. <bool>**checkExistence** - **Optional**. Whether to check for the existence of the permission.

 ```csharp
(bool)TemporaryPermissions?.Call("GrantUserPermission", player.UserIDString, "realpve.vip");//Calling the GrantUserPermission method without specifying the third parameter, to grant temporary permission until the wipe.
(bool)TemporaryPermissions?.Call("GrantUserPermission", player.UserIDString, "realpve.vip", 0);//Calling the GrantUserPermission method with the specified number less than 1, to grant temporary permission until the wipe.
(bool)TemporaryPermissions?.Call("GrantUserPermission", player.UserIDString, "realpve.vip", 3600, true, true);//Calling the GrantUserPermission method with the specified number of seconds to add.
(bool)TemporaryPermissions?.Call("GrantUserPermission", player.UserIDString, "realpve.vip", expireDate, assignedDate, true);//Calling the GrantUserPermission method with the specified DateTime for the end and start of the temporary permission.
```  

### RevokeUserPermission
Used to **revoke a temporary permission** from a **player**.  
Returns **true** if the **revoke was successful**.  
To call the **RevokeUserPermission** method, you need to pass 2 parameters:
1. **IPlayer** or **BasePlayer** or <string>**playerID** - **The player object** or their **Id**;
2. <string>**permName** - **The name of the permission**.

```csharp
(bool)TemporaryPermissions?.Call("RevokeUserPermission", player.UserIDString, "realpve.vip");
```  

### UserHasPermission
Used to **check** if a **player has a temporary permission**.  
Returns **true** if the **player has the specified temporary permission**.  
To call the **UserHasPermission** method, you need to pass 2 parameters:
1. **IPlayer** or **BasePlayer** or <string>**playerID** - **The player object** or their **Id**;
2. <string>**permName** - **The name of the permission**.

```csharp
(bool)TemporaryPermissions?.Call("UserHasPermission", player.UserIDString, "realpve.vip");
```  

### GrantActiveUsersPermission
Used to **temporarily grant a permission** to **all online players**.  
Returns the <int>**number of successful grants of temporary permissions to players**.  
To call the **GrantActiveUsersPermission** method, you need to pass 3 parameters, 2 of which is optional:
1. <string>**permName** - **The name of the permission**;
2. <int>**secondsToAdd** or <DateTime>**expireDate** - **Optional**. The time in **seconds to add**, or **the end date**. If a **number less than 1** is specified, or **if this parameter is not specified**, **the permission** will be valid **until the wipe**;
3. <bool>**fromNow** or <DateTime>**assignedDate** - **Optional**. **true/false** to specify whether the seconds should be added to the current date or to the existing time, or an exact assignment date. Defaults to the current date.

```csharp
(int)TemporaryPermissions?.Call("GrantActiveUsersPermission", "realpve.vip");//Calling the GrantActiveUsersPermission method without specifying the second parameter, to grant all online players temporary permission until the wipe.
(int)TemporaryPermissions?.Call("GrantActiveUsersPermission", "realpve.vip", 0);//Calling the GrantActiveUsersPermission method with the specified number less than 1, to grant all online players temporary permission until the wipe.
(int)TemporaryPermissions?.Call("GrantActiveUsersPermission", "realpve.vip", 3600, true);//Calling the GrantActiveUsersPermission method with the specified number of seconds to add.
(int)TemporaryPermissions?.Call("GrantActiveUsersPermission", "realpve.vip", expireDate, assignedDate);//Calling the GrantActiveUsersPermission method with the specified DateTime for the end and start of the temporary permission.
```  

### GrantAllUsersPermission
Used to **grant a temporary permission** to **all players**.  
Returns the <int>**number of successful grants of temporary permissions to players**.  
To call the **GrantAllUsersPermission** method, you need to pass 3 parameters, 2 of which is optional:
1. <string>**permName** - **The name of the permission**;
2. <int>**secondsToAdd** or <DateTime>**expireDate** - **Optional**. The time in **seconds to add**, or **the end date**. If a **number less than 1** is specified, or **if this parameter is not specified**, **the permission** will be valid **until the wipe**;
3. <bool>**fromNow** or <DateTime>**assignedDate** - **Optional**. **true/false** to specify whether the seconds should be added to the current date or to the existing time, or an exact assignment date. Defaults to the current date.

```csharp
(int)TemporaryPermissions?.Call("GrantAllUsersPermission", "realpve.vip");//Calling the GrantAllUsersPermission method without specifying the second parameter, to grant all players temporary permission until the wipe.
(int)TemporaryPermissions?.Call("GrantAllUsersPermission", "realpve.vip", 0);//Calling the GrantAllUsersPermission method with the specified number less than 1, to grant all players temporary permission until the wipe.
(int)TemporaryPermissions?.Call("GrantAllUsersPermission", "realpve.vip", 3600, true);//Calling the GrantAllUsersPermission method with the specified number of seconds to add.
(int)TemporaryPermissions?.Call("GrantAllUsersPermission", "realpve.vip", expireDate, assignedDate);//Calling the GrantAllUsersPermission method with the specified DateTime for the end and start of the temporary permission.
```  

### RevokeActiveUsersPermission
Used to **revoke a temporary permission** from **all online players**.  
Returns the <int>**number of successful revokes of temporary permissions to players**.  
To call the **RevokeActiveUsersPermission** method, you need to pass 1 parameter:
1. <string>**permName** - **The name of the permission**.

```csharp
(int)TemporaryPermissions?.Call("RevokeActiveUsersPermission", "realpve.vip");
```  

### RevokeAllUsersPermission
Used to **revoke a temporary permission** from **all players**.  
Returns the <int>**number of successful revokes of temporary permissions to players**.  
To call the **RevokeAllUsersPermission** method, you need to pass 1 parameter:
1. <string>**permName** - **The name of the permission**.

```csharp
(int)TemporaryPermissions?.Call("RevokeAllUsersPermission", "realpve.vip");
```  

### UserGetAllPermissions
Used to **retrieve all temporary permissions** of a **player**.  
Returns a **Dictionary<string, DateTime[]>** where the **key is the permission name** and **the value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the permission.  
If **the expiration date** is set to **default**, it means the permission is valid **until the wipe**.  
To call the **UserGetAllPermissions** method, you need to pass 1 parameter:
1. <string>**playerID** - The player's **Id**.

```csharp
(Dictionary<string, DateTime[]>)TemporaryPermissions?.Call("UserGetAllPermissions", player.UserIDString);
```  

### UserGetPermissionByExpiry  
Used to get **the expiration date** of a player's temporary permission, considering **the temporary permission for the group** they may belong to. The latest date is taken.  
Returns a **DateTime[]**: **the first date is the assignment date** and **the second date is the expiration date** of the permission.  
If **the expiration date** is set to **default**, it means the permission is valid **until the wipe**.  
To call the **UserGetPermissionByExpiry** method, you need to pass 2 parameters:
1. **IPlayer** or **BasePlayer** or <string>**playerID** - **The player object** or their **Id**;
2. <string>**permName** - **The name of the permission**.

```csharp
(DateTime[])TemporaryPermissions?.Call("UserGetPermissionByExpiry", player.UserIDString, "realpve.vip");
```  

### UserGetAllPermissionsByExpiry  
Used to get **the expiration date** of **all** temporary permissions for a player, considering **the temporary permissions for the group** they may belong to. The latest date is taken.  
Returns a **Dictionary<string, DateTime[]>** where the **key is the permission name** and **the value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the permission.  
If **the expiration date** is set to **default**, it means the permission is valid **until the wipe**.  
To call the **UserGetAllPermissionsByExpiry** method, you need to pass 2 parameters:
1. **IPlayer** or **BasePlayer** or <string>**playerID** - **The player object** or their **Id**.

```csharp
(Dictionary<string, DateTime[]>)TemporaryPermissions?.Call("UserGetAllPermissionsByExpiry", player.UserIDString);
```  

### ActiveUsersGetAllPermissions
Used to **retrieve all temporary permissions** of **all online players** who **have temporary permissions**.  
Returns a **Dictionary<string, Dictionary<string, DateTime[]>>** where the **key is userID** and the **value is another Dictionary<string, DateTime[]>**, where the **key is the permission name** and the **value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the permission.  
If **the expiration date** is set to **default**, it means the permission is valid **until the wipe**.

```csharp
(Dictionary<string, Dictionary<string, DateTime[]>>)TemporaryPermissions?.Call("ActiveUsersGetAllPermissions");
```  

### AllUsersGetAllPermissions
Used to **retrieve all temporary permissions** of **all players** who **have temporary permissions**.  
Returns a **Dictionary<string, Dictionary<string, DateTime[]>>** where the **key is userID** and the **value is another Dictionary<string, DateTime[]>**, where the **key is the permission name** and the **value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the permission.  
If **the expiration date** is set to **default**, it means the permission is valid **until the wipe**.

```csharp
(Dictionary<string, Dictionary<string, DateTime[]>>)TemporaryPermissions?.Call("AllUsersGetAllPermissions");
```  

### AddUserGroup
Used to **temporarily add a player to a group**.  
Returns **true** if the **addition was successful**.  
To call the **AddUserGroup** method, you need to pass 5 parameters, 3 of which are optional:
1. **IPlayer** or **BasePlayer** or <string>**playerID** - **The player object** or their **Id**;
2. <string>**groupName** - **The name of the group**;
3. <int>**secondsToAdd** or <DateTime>**expireDate** - **Optional**. The time in **seconds to add**, or **the end date**. If a **number less than 1** is specified, or **if this parameter is not specified**, **the group** will be valid **until the wipe**;
4. <bool>**fromNow** or <DateTime>**assignedDate** - **Optional**. **true/false** to specify whether the seconds should be added to the current date or to the existing time, or an exact assignment date. Defaults to the current date;
5. <bool>**checkExistence** - **Optional**. Whether to check for the existence of the group.

```csharp
(bool)TemporaryPermissions?.Call("AddUserGroup", player.UserIDString, "vip");//Calling the AddUserGroup method without specifying the third parameter to temporarily add a player, to a group until the wipe.
(bool)TemporaryPermissions?.Call("AddUserGroup", player.UserIDString, "vip", 0);//Calling the AddUserGroup method with the specified number less than 1, to temporarily add a player to a group until the wipe.
(bool)TemporaryPermissions?.Call("AddUserGroup", player.UserIDString, "vip", 3600, true, true);//Calling the AddUserGroup method with the specified number of seconds to add.
(bool)TemporaryPermissions?.Call("AddUserGroup", player.UserIDString, "vip", expireDate, assignedDate, true);//Calling the AddUserGroup method with the specified DateTime for the end and start of the temporary permission.
```  

### RemoveUserGroup
Used to **remove a temporary group from a player**.  
Returns **true** if the **removal was successful**.  
To call the **RemoveUserGroup** method, you need to pass 2 parameters:
1. **IPlayer** or **BasePlayer** or <string>**playerID** - **The player object** or their **Id**;
2. <string>**groupName** - **The name of the group**.

```csharp
(bool)TemporaryPermissions?.Call("RemoveUserGroup", player.UserIDString, "vip");
```  

### UserHasGroup
Used to **check** if a **player has a temporary group**.  
Returns **true** if the **player has the specified temporary group**.  
To call the **UserHasGroup** method, you need to pass 2 parameters:
1. **IPlayer** or **BasePlayer** or <string>**playerID** - **The player object** or their **Id**;
2. <string>**groupName** - **The name of the group**.

```csharp
(bool)TemporaryPermissions?.Call("UserHasGroup", player.UserIDString, "vip");
```  

### AddActiveUsersGroup
Used to **temporarily add a group** to **all online players**.  
Returns the <int>**number of successful additions of the temporary group to players**.  
To call the **AddActiveUsersGroup** method, you need to pass 3 parameters, 2 of which is optional:
1. <string>**groupName** - **The name of the group**;
2. <int>**secondsToAdd** or <DateTime>**expireDate** - **Optional**. The time in **seconds to add**, or **the end date**. If a **number less than 1** is specified, or **if this parameter is not specified**, **the group** will be valid **until the wipe**;
3. <bool>**fromNow** or <DateTime>**assignedDate** - **Optional**. **true/false** to specify whether the seconds should be added to the current date or to the existing time, or an exact assignment date. Defaults to the current date.

```csharp
(int)TemporaryPermissions?.Call("AddActiveUsersGroup", "vip");//Calling the AddActiveUsersGroup method without specifying the second parameter to temporarily add all online players to a group until the wipe.
(int)TemporaryPermissions?.Call("AddActiveUsersGroup", "vip", 0);//Calling the AddActiveUsersGroup method with the specified number less than 1, to temporarily add all online players to a group until the wipe.
(int)TemporaryPermissions?.Call("AddActiveUsersGroup", "vip", 3600, true);//Calling the AddActiveUsersGroup method with the specified number of seconds to add.
(int)TemporaryPermissions?.Call("AddActiveUsersGroup", "vip", expireDate, assignedDate);//Calling the AddActiveUsersGroup method with the specified DateTime for the end and start of the temporary permission.
```  

### AddAllUsersGroup
Used to **temporarily add a group** to **all players**.  
Returns the <int>**number of successful additions of the temporary group to players**.  
To call the **AddAllUsersGroup** method, you need to pass 3 parameters, 2 of which is optional:
1. <string>**groupName** - **The name of the group**;
2. <int>**secondsToAdd** or <DateTime>**expireDate** - **Optional**. The time in **seconds to add**, or **the end date**. If a **number less than 1** is specified, or **if this parameter is not specified**, **the group** will be valid **until the wipe**;
3. <bool>**fromNow** or <DateTime>**assignedDate** - **Optional**. **true/false** to specify whether the seconds should be added to the current date or to the existing time, or an exact assignment date. Defaults to the current date.

```csharp
(int)TemporaryPermissions?.Call("AddAllUsersGroup", "vip");//Calling the AddAllUsersGroup method without specifying the second parameter to temporarily add all players to a group until the wipe.
(int)TemporaryPermissions?.Call("AddAllUsersGroup", "vip", 0);//Calling the AddAllUsersGroup method with the specified number less than 1, to temporarily add all players to a group until the wipe.
(int)TemporaryPermissions?.Call("AddAllUsersGroup", "vip", 3600, true);//Calling the AddAllUsersGroup method with the specified number of seconds to add.
(int)TemporaryPermissions?.Call("AddAllUsersGroup", "vip", expireDate, assignedDate);//Calling the AddAllUsersGroup method with the specified DateTime for the end and start of the temporary permission.
```  

### RemoveActiveUsersGroup
Used to **remove a temporary group** from **all online players**.  
Returns the <int>**number of successful removals of temporary groups from players**.  
To call the **RemoveActiveUsersGroup** method, you need to pass 1 parameter:
1. <string>**groupName** - **The name of the group**.

```csharp
(int)TemporaryPermissions?.Call("RemoveActiveUsersGroup", "vip");
```  

### RemoveAllUsersGroup
Used to **remove a temporary group** from **all players**.  
Returns the <int>**number of successful removals of temporary groups from players**.  
To call the **RemoveAllUsersGroup** method, you need to pass 1 parameter:
1. <string>**permName** - **The name of the permission**.

```csharp
(int)TemporaryPermissions?.Call("RemoveAllUsersGroup", "vip");
```  

### UserGetAllGroups
Used to **retrieve all temporary groups** of a **player**.  
Returns a **Dictionary<string, DateTime[]>** where the **key is the group name** and the **value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the group.  
If **the expiration date** is set to **default**, it means the permission is valid **until the wipe**.  
To call the **UserGetAllGroups** method, you need to pass 1 parameter:
1. <string>**permName** - **The name of the permission**.

```csharp
(Dictionary<string, DateTime[]>)TemporaryPermissions?.Call("UserGetAllGroups", player.UserIDString);
```  

### ActiveUsersGetAllGroups
Used to **retrieve all temporary groups** of a **player**.  
Returns a **Dictionary<string, Dictionary<string, DateTime[]>>** where the **key is the group name** and the **value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the group.  
If **the expiration date** is set to **default**, it means the group is valid **until the wipe**.

```csharp
(Dictionary<string, Dictionary<string, DateTime[]>>)TemporaryPermissions?.Call("ActiveUsersGetAllGroups");
```  

### ActiveUsersGetAllGroups
Used to **retrieve all temporary groups** of **all players** who **have temporary groups**.  
Returns a **Dictionary<string, Dictionary<string, DateTime[]>>** where the **key is userID** and the **value is another Dictionary<string, DateTime[]>**, where the **key is the group name** and the **value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the group.  
If **the expiration date** is set to **default**, it means the group is valid **until the wipe**.

```csharp
(Dictionary<string, Dictionary<string, DateTime[]>>)TemporaryPermissions?.Call("ActiveUsersGetAllGroups");
```  

### AllUsersGetAllGroups
Used to **retrieve all temporary groups** of **all players** who **have temporary groups**.  
Returns a **Dictionary<string, Dictionary<string, DateTime[]>>** where the **key is userID** and the **value is another Dictionary<string, DateTime[]>**, where the **key is the group name** and the **value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the group.  
If **the expiration date** is set to **default**, it means the group is valid **until the wipe**.

```csharp
(Dictionary<string, Dictionary<string, DateTime[]>>)TemporaryPermissions?.Call("AllUsersGetAllGroups");
```  

### GrantGroupPermission
Used to **grant a temporary permission** to a **group**.  
Returns **true** if the **grant was successful**.  
To call the **GrantGroupPermission** method, you need to pass 5 parameters, 3 of which are optional:
1. <string>**groupName** - **The name of the group**;
2. <string>**permName** - **The name of the permission**;
3. <int>**secondsToAdd** or <DateTime>**expireDate** - **Optional**. The time in **seconds to add**, or **the end date**. If a **number less than 1** is specified, or **if this parameter is not specified**, **the permission** will be valid **until the wipe**;
4. <bool>**fromNow** or <DateTime>**assignedDate** - **Optional**. **true/false** to specify whether the seconds should be added to the current date or to the existing time, or an exact assignment date. Defaults to the current date;
5. <bool>**checkExistence** - **Optional**. Whether to check for the existence of the permission.

```csharp
(bool)TemporaryPermissions?.Call("GrantGroupPermission", "vip", "realpve.vip");//Calling the GrantGroupPermission method without specifying the third parameter, to grant temporary permission until the wipe.
(bool)TemporaryPermissions?.Call("GrantGroupPermission", "vip", "realpve.vip", 0);//Calling the GrantGroupPermission method with the specified number less than 1, to grant temporary permission until the wipe.
(bool)TemporaryPermissions?.Call("GrantGroupPermission", "vip", "realpve.vip", 3600, true, true);//Calling the GrantGroupPermission method with the specified number of seconds to add.
(bool)TemporaryPermissions?.Call("GrantGroupPermission", "vip", "realpve.vip", expireDate, assignedDate, true);//Calling the GrantGroupPermission method with the specified DateTime for the end and start of the temporary permission.
```  

### RevokeGroupPermission
Used to **revoke a temporary permission** from a **group**.  
Returns **true** if the **revoke was successful**.  
To call the **RevokeGroupPermission** method, you need to pass 2 parameters:
1. <string>**groupName** - **The name of the group**;
2. <string>**permName** - **The name of the permission**.

```csharp
(bool)TemporaryPermissions?.Call("RevokeGroupPermission", "vip", "realpve.vip");
```  

### GroupHasPermission
Used to **check** if a **group has a temporary permission**.  
Returns **true** if the **group has the specified temporary permission**.  
To call the **GroupHasPermission** method, you need to pass 2 parameters:
1. <string>**groupName** - **The name of the group**;
2. <string>**permName** - **The name of the permission**.

```csharp
(bool)TemporaryPermissions?.Call("GroupHasPermission", "vip", "realpve.vip");
```  

### GrantAllGroupsPermission
Used to **temporarily grant a permission** to **all groups**.  
Returns the <int>**number of successful grants of temporary permissions to groups**.  
To call the **GrantAllGroupsPermission** method, you need to pass 3 parameters, 2 of which is optional:
1. <string>**permName** - **The name of the permission**;
2. <int>**secondsToAdd** or <DateTime>**expireDate** - **Optional**. The time in **seconds to add**, or **the end date**. If a **number less than 1** is specified, or **if this parameter is not specified**, **the permission** will be valid **until the wipe**;
3. <bool>**fromNow** or <DateTime>**assignedDate** - **Optional**. **true/false** to specify whether the seconds should be added to the current date or to the existing time, or an exact assignment date. Defaults to the current date.

```csharp
(int)TemporaryPermissions?.Call("GrantAllGroupsPermission", "realpve.vip");//Calling the GrantAllGroupsPermission method without specifying the second parameter, to grant all groups temporary permission until the wipe.
(int)TemporaryPermissions?.Call("GrantAllGroupsPermission", "realpve.vip", 0);//Calling the GrantAllGroupsPermission method with the specified number less than 1, to grant all groups temporary permission until the wipe.
(int)TemporaryPermissions?.Call("GrantAllGroupsPermission", "realpve.vip", 3600, true);//Calling the GrantAllGroupsPermission method with the specified number of seconds to add.
(int)TemporaryPermissions?.Call("GrantAllGroupsPermission", "realpve.vip", expireDate, assignedDate);//Calling the GrantAllGroupsPermission method with the specified DateTime for the end and start of the temporary permission.
```  

### RevokeAllGroupsPermission
Used to **revoke a temporary permission** from **all groups**.  
Returns the <int>**number of successful revokes of temporary permissions to groups**.  
To call the **RevokeAllGroupsPermission** method, you need to pass 1 parameter:
1. <string>**permName** - **The name of the permission**.

```csharp
(int)TemporaryPermissions?.Call("RevokeAllGroupsPermission", "realpve.vip");
```  

### GroupGetAllPermissions
Used to **retrieve all temporary permissions** of a **group**.  
Returns a **Dictionary<string, DateTime[]>** where **the key is the permission name** and the **value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the permission.  
If **the expiration date** is set to **default**, it means the permission is valid **until the wipe**.  
To call the **GroupGetAllPermissions** method, you need to pass 1 parameter:
1. <string>**groupName** - **The name of the group**.

```csharp
(Dictionary<string, DateTime[]>)TemporaryPermissions?.Call("GroupGetAllPermissions", "vip");
```  

### AllGroupsGetAllPermissions
Used to **retrieve all temporary permissions** of **all groups** that **have temporary permissions**.  
Returns a **Dictionary<string, Dictionary<string, DateTime[]>>** where the **key is the group name** and the **value is another Dictionary<string, DateTime[]>**, where the **key is the permission name** and the **value is an array of 2 DateTimes**: **the first date is the assignment date** and **the second date is the expiration date** of the permission.  
If **the expiration date** is set to **default**, it means the permission is valid **until the wipe**.

```csharp
(Dictionary<string, Dictionary<string, DateTime[]>>)TemporaryPermissions?.Call("AllGroupsGetAllPermissions");
```
