/*
*  < ----- End-User License Agreement ----->
*  
*  You may not copy, modify, merge, publish, distribute, sublicense, or sell copies of this software without the developer’s consent.
*
*  THIS SOFTWARE IS PROVIDED BY IIIaKa AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
*  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS 
*  BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
*  GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
*  LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
*  Developer: IIIaKa
*      https://t.me/iiiaka
*      Discord: @iiiaka
*      https://github.com/IIIaKa
*      https://umod.org/user/IIIaKa
*      https://codefling.com/iiiaka
*      https://lone.design/vendor/iiiaka/
*      https://www.patreon.com/iiiaka
*      https://boosty.to/iiiaka
*  GitHub repository page: https://github.com/IIIaKa/TemporaryPermissions
*  
*  uMod plugin page: https://umod.org/plugins/temporary-permissions
*  uMod license: https://umod.org/plugins/temporary-permissions#license
*  
*  Codefling plugin page: https://codefling.com/plugins/temporary-permissions
*  Codefling license: https://codefling.com/plugins/temporary-permissions?tab=downloads_field_4
*  
*  Lone.Design plugin page: https://lone.design/product/temporary-permissions/
*
*  Copyright © 2024-2025 IIIaKa
*/

using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using Facepunch;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Temporary Permissions", "IIIaKa", "0.1.6")]
    [Description("Useful plugin for managing temporary permissions, temporary groups and temporary permissions for groups. This is done through chat commands, built-in Oxide commands and API methods.")]
    class TemporaryPermissions : RustPlugin
    {
        #region ~Variables~
        private static TemporaryPermissions Instance { get; set; }
        private bool _isReady = false;
        private const string PERMISSION_ADMIN = "temporarypermissions.admin", TimeFormat = "yyyy-MM-dd HH:mm:ss", CommandAdd = "add", CommandRemove = "remove", CommandUser = "user", CommandGroup = "group",
            Str_Showtoast = "gametip.showtoast", Str_Wipe = "wipe", Log_UserPerms = "user_permissions", Log_UserGroups = "user_groups", Log_GroupPerms = "group_permissions", Hooks_OnLoaded = "OnTemporaryPermissionsLoaded",
            Hooks_UserPermissionGranted = "OnTemporaryPermissionGranted", Hooks_UserPermissionUpdated = "OnTemporaryPermissionUpdated", Hooks_UserPermissionRevoked = "OnTemporaryPermissionRevoked",
            Hooks_UserGroupAdded = "OnTemporaryGroupAdded", Hooks_UserGroupUpdated = "OnTemporaryGroupUpdated", Hooks_UserGroupRemoved = "OnTemporaryGroupRemoved",
            Hooks_GroupPermissionGranted = "OnGroupTemporaryPermissionGranted", Hooks_GroupPermissionUpdated = "OnGroupTemporaryPermissionUpdated", Hooks_GroupPermissionRevoked = "OnGroupTemporaryPermissionRevoked";
        private readonly string[] CommandsGrant = new string[] { "oxide.grant", "o.grant", "perm.grant" },
            CommandsRevoke = new string[] { "oxide.revoke", "o.revoke", "perm.revoke" },
            CommandsUserGroup = new string[] { "oxide.usergroup", "o.usergroup", "perm.usergroup" };
        private Timer _expirationTimer, _presenceTimer;
        private HashSet<string> _existingPerms, _existingGroups;
        #endregion

        #region ~Configuration~
        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Chat command")]
            public string Command = string.Empty;
            
            [JsonProperty(PropertyName = "Chat admin command")]
            public string AdminCommand = string.Empty;
            
            [JsonProperty(PropertyName = "Is it worth enabling GameTips for messages?")]
            public bool GameTips_Enabled = true;
            
            [JsonProperty(PropertyName = "Is it worth saving logs to a file?")]
            public bool FileLogs = true;
            
            [JsonProperty(PropertyName = "Is it worth using console logging?")]
            public bool ConsoleLogs = true;
            
            [JsonProperty(PropertyName = "List of language keys for creating language files")]
            public List<string> LanguageKeys;
            
            [JsonProperty(PropertyName = "Interval in seconds for expiration check")]
            public float ExpirationInterval = 1f;
            
            [JsonProperty(PropertyName = "Interval in seconds for checking the presence of temporary permissions and temporary groups. A value of 0 disables the check")]
            public float PresenceInterval = 600f;
            
            [JsonProperty(PropertyName = "Is it worth restoring removed temporary permissions and temporary groups if the timer hasn't expired? There are cases where removal cannot be tracked in the usual way")]
            public bool PresenceRestoring = true;
            
            [JsonProperty(PropertyName = "Is it worth revoking temporary permissions and temporary groups when unloading the plugin, without removing them from the data file?")]
            public bool RevokeOnUnload = true;
            
            [JsonProperty(PropertyName = "Is it worth revoking temporary permissions and temporary groups that haven't expired yet upon detecting a wipe?")]
            public bool RevokeAllOnWipe = false;
            
            [JsonProperty(PropertyName = "Custom wipe date(detected only during initialization). Only required if you're experiencing issues with the Wipe ID. Leave empty to use the Wipe ID. Example: 2025-06-25 13:00")]
            public string CustomWipe = string.Empty;
            
            [JsonProperty(PropertyName = "Wipe ID")]
            public string WipeID = string.Empty;
            
            public Oxide.Core.VersionNumber Version;
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try { _config = Config.ReadObject<Configuration>(); }
            catch (Exception ex) { PrintError($"{ex.Message}\n\n[{Title}] Your configuration file contains an error."); }
            if (_config == null || _config.Version == new VersionNumber())
            {
                PrintWarning("The configuration file is not found or contains errors. Creating a new one...");
                LoadDefaultConfig();
            }
            else if (_config.Version < Version)
            {
                PrintWarning($"Your configuration file version({_config.Version}) is outdated. Updating it to {Version}.");
                _config.Version = Version;
                PrintWarning($"The configuration file has been successfully updated to version {_config.Version}!");
            }
            
            if (string.IsNullOrWhiteSpace(_config.Command))
                _config.Command = "myperm";
            if (string.IsNullOrWhiteSpace(_config.AdminCommand))
                _config.AdminCommand = "tperm";
            if (_config.Command.Equals(_config.AdminCommand, StringComparison.OrdinalIgnoreCase))
                _config.Command += "_1";
            
            if (_config.LanguageKeys == null)
                _config.LanguageKeys = new List<string>();
            if (_config.LanguageKeys.Any())
            {
                for (int i = _config.LanguageKeys.Count - 1; i >= 0; i--)
                {
                    string langKey = ToLangKey(_config.LanguageKeys[i]);
                    if (langKey.Equals("en", StringComparison.OrdinalIgnoreCase) || langKey.Equals("ru", StringComparison.OrdinalIgnoreCase))
                        _config.LanguageKeys.RemoveAt(i);
                    else
                        _config.LanguageKeys[i] = langKey;
                }
            }
            _config.LanguageKeys.Add("en");
            
            _config.ExpirationInterval = Mathf.Max(_config.ExpirationInterval, 1f);
            _config.PresenceInterval = Mathf.Max(_config.PresenceInterval, 0f);
            
            SaveConfig();
        }
        
        protected override void SaveConfig() => Config.WriteObject(_config);
        protected override void LoadDefaultConfig() => _config = new Configuration() { Version = Version };
        #endregion
        
        #region ~DataFile~
        private static StoredData _storedData;

        private class StoredData
        {
            [JsonProperty(PropertyName = "Players list")]
            public Dictionary<string, PlayerData> PlayersList = new Dictionary<string, PlayerData>(StringComparer.OrdinalIgnoreCase);
            
            [JsonProperty(PropertyName = "Groups list")]
            public Dictionary<string, GroupData> GroupsList = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _storedData);
        #endregion
        
        #region ~Language~
        protected override void LoadDefaultMessages()
        {
            var enLang = new Dictionary<string, string>
            {
                ["CmdAdmin"] = string.Join("\n", new string[]
                {
                    "Available admin commands:\n",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *nameOrId* realpve.vip wipe</color> - Grants or extends the specified permission for the specified player until the end of the current wipe",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *nameOrId* realpve.vip *intValue* *boolValue*(optional)</color> - Grants or extends the specified permission for the specified player for the given number of seconds",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *nameOrId* realpve.vip *expirationDate* *assignmentDate*(optional)</color> - Grants or extends the specified permission for the specified player until the given date",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *groupName* realpve.vip wipe</color> - Grants or extends the specified permission for the specified group until the end of the current wipe",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *groupName* realpve.vip *intValue* *boolValue*(optional)</color> - Grants or extends the specified permission for the specified group for the given number of seconds",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *groupName* realpve.vip *expirationDate* *assignmentDate*(optional)</color> - Grants or extends the specified permission for the specified group until the given date",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>revoke user *nameOrId* realpve.vip</color> - Revokes the specified permission from the specified player",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>revoke group *groupName* realpve.vip</color> - Revokes the specified permission from the specified group",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *nameOrId* *groupName* wipe</color> - Adds or extends the specified player's membership in the specified group until the end of the current wipe",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *nameOrId* *groupName* *intValue* *boolValue*(optional)</color> - Adds or extends the specified player's membership in the specified group for the given number of seconds",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *nameOrId* *groupName* *expirationDate* *assignmentDate*(optional)</color> - Adds or extends the specified player's membership in the specified group until the given date",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>remove *nameOrId* *groupName*</color> - Removes the specified player from the specified group",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>wipe *wipeDate*</color> - Set a custom wipe date. Used in case of issues with the Wipe ID. Format: yyyy-MM-dd HH:mm",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>wipe reset</color> - Reset the custom wipe date",
                    "\n<color=#D1CBCB>Optional values:</color>",
                    "*boolValue* - If false(default) and an existing permission or group membership has not expired, the specified time will be added to the existing time. Otherwise, including when true, the specified time will be counted from the current time",
                    "*assignmentDate* - If the assignment date is not specified and there is no existing permission or group membership, the assignment date will be set to the current time. If the assignment date is specified, it will be applied regardless of existing permissions or group memberships",
                    "\n--------------------------------------------------"
                }),
                ["CmdPermissionNotFound"] = "Permission '{0}' not found!",
                ["CmdPlayerNotFound"] = "Player '{0}' not found! You must provide the player's name or ID.",
                ["CmdMultiplePlayers"] = "Multiple players found for '{0}': {1}",
                ["CmdGroupNotFound"] = "Group '{0}' not found!",
                ["CmdGrantWrongFormat"] = "Incorrect command format! Example: /tperm grant user/group *nameOrId* realpve.vip *secondsOrDateTime*",
                ["CmdRevokeWrongFormat"] = "Incorrect command format! Example: /tperm revoke user/group *nameOrId* realpve.vip",
                ["CmdUserGroupWrongFormat"] = "Incorrect command format! Example: /tperm group add/remove *nameOrId* *groupName*",
                ["CmdUserGranted"] = "Permission '{0}' granted to player '{1}'.",
                ["CmdGroupGranted"] = "Permission '{0}' granted to group '{1}'.",
                ["CmdUserGroupAdded"] = "Player '{0}' has been added to group '{1}'.",
                ["CmdUserRevoked"] = "Permission '{0}' has been revoked for player '{1}'.",
                ["CmdGroupRevoked"] = "Permission '{0}' has been revoked for group '{1}'.",
                ["CmdUserGroupRemoved"] = "Player '{0}' has been removed from group '{1}'.",
                ["CmdWipeNew"] = "New wipe date successfully set to '{0}'. The wipe will take effect only after the plugin is loaded following this date.",
                ["CmdWipeReset"] = "The custom wipe date has been reset. The wipe is now determined by the Wipe ID.",
                ["CmdWipeFailed"] = "The specified date '{0}' has an invalid format(yyyy-MM-dd HH:mm) or has already passed. Example: '{1}'.",
                ["CmdCheckNoActive"] = "You have no active temporary permissions or temporary groups!",
                ["CmdCheckTargetNoActive"] = "Player '{0}' has no active temporary permissions or temporary groups!",
                ["CmdCheckPermissions"] = string.Join("\n", new string[]
                {
                    "<color=#D1AB9A>You have {0} temporary permissions(time in UTC):</color>",
                    "{1}"
                }),
                ["CmdCheckGroups"] = string.Join("\n", new string[]
                {
                    "<color=#D1AB9A>You have {0} temporary groups(time in UTC):</color>",
                    "{1}"
                }),
                ["CmdCheckTargetPermissions"] = string.Join("\n", new string[]
                {
                    "<color=#D1AB9A>Player '{2}' has {0} temporary permissions(time in UTC):</color>",
                    "{1}"
                }),
                ["CmdCheckTargetGroups"] = string.Join("\n", new string[]
                {
                    "<color=#D1AB9A>Player '{2}' has {0} temporary groups(time in UTC):</color>",
                    "{1}"
                }),
                ["CmdCheckFormatPermissions"] = "'{0}' - {1}({2})",
                ["CmdCheckFormatGroups"] = "'{0}' - {1}({2})",
                ["CmdUntilWipe"] = "Until Wipe"
            };
            var ruLang = new Dictionary<string, string>
            {
                ["CmdAdmin"] = string.Join("\n", new string[]
                {
                    "Доступные админ команды:\n",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *имяИлиАйди* realpve.vip wipe</color> - Выдать или продлить указанный пермишен указанному игроку до конца текущего вайпа",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *имяИлиАйди* realpve.vip *числовоеЗначение* *булевоеЗначение*(опционально)</color> - Выдать или продлить указанный пермишен указанному игроку на указанное количество секунд",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant user *имяИлиАйди* realpve.vip *датаИстечения* *датаНазначения*(опционально)</color> - Выдать или продлить указанный пермишен указанному игроку до указанной даты",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *имяГруппы* realpve.vip wipe</color> - Выдать или продлить указанный пермишен указанной группе до конца текущего вайпа",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *имяГруппы* realpve.vip *числовоеЗначение* *булевоеЗначение*(опционально)</color> - Выдать или продлить указанный пермишен указанной группе на указанное количество секунд",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>grant group *имяГруппы* realpve.vip *датаИстечения* *датаНазначения*(опционально)</color> - Выдать или продлить указанный пермишен указанной группе до указанной даты",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>revoke user *имяИлиАйди* realpve.vip</color> - Снять указанный пермишен у указанного игрока",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>revoke group *имяГруппы* realpve.vip</color> - Снять указанный пермишен у указанной группы",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *имяИлиАйди* *имяГруппы* wipe</color> - Добавить или продлить пребывание в указанной группе указанному игроку до конца текущего вайпа",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *имяИлиАйди* *имяГруппы* *числовоеЗначение* *булевоеЗначение*(опционально)</color> - Добавить или продлить пребывание в указанной группе указанному игроку на указанное количество секунд",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>add *имяИлиАйди* *имяГруппы* *датаИстечения* *датаНазначения*(опционально)</color> - Добавить или продлить пребывание в указанной группе указанному игроку до указанной даты",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>remove *имяИлиАйди* *имяГруппы*</color> - Отменить пребывание в указанной группе указанному игроку",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>wipe *датаВайпа*</color> - Установка кастомной даты вайпа. Используется при проблемах с Wipe ID. Формат: yyyy-MM-dd HH:mm",
                    "<color=#D1CBCB>/tperm</color> <color=#D1AB9A>wipe reset</color> - Сброс кастомной даты вайпа",
                    "\n<color=#D1CBCB>Опциональные значения:</color>",
                    "*булевоеЗначение* - Если false(по умолчанию) и существующий пермишен или группа не истекли, указанное время будет добавлено к существующему времени. В противном случае, в т.ч. при true, указанное время будет отсчитываться от текущего времени",
                    "*датаНазначения* - Если дата назначения не указана и нет существующего пермишена или группы, дата назначения будет равна текущей. Если дата назначения указана, то вне зависимости от существования пермишенов или групп, присвоится указанная дата",
                    "\n--------------------------------------------------"
                }),
                ["CmdPermissionNotFound"] = "Пермишен '{0}' не найден!",
                ["CmdPlayerNotFound"] = "Игрок '{0}' не найден! Вы должны указать имя или ID игрока.",
                ["CmdMultiplePlayers"] = "По значению '{0}' найдено несколько игроков: {1}",
                ["CmdGroupNotFound"] = "Группа '{0}' не найдена!",
                ["CmdGrantWrongFormat"] = "Не верный формат команды! Пример: /tperm grant user/group *имяИлиАйди* realpve.vip *секундыИлиДата*",
                ["CmdRevokeWrongFormat"] = "Не верный формат команды! Пример: /tperm revoke user/group *имяИлиАйди* realpve.vip",
                ["CmdUserGroupWrongFormat"] = "Не верный формат команды! Пример: /tperm group add/remove *имяИлиАйди* *имяГруппы*",
                ["CmdUserGranted"] = "Пермишен '{0}' выдан игроку '{1}'.",
                ["CmdGroupGranted"] = "Пермишен '{0}' выдан группе '{1}'.",
                ["CmdUserGroupAdded"] = "Игрок '{0}' был добавлен в группу '{1}'.",
                ["CmdUserRevoked"] = "Пермишен '{0}' был удален для игрока '{1}'.",
                ["CmdGroupRevoked"] = "Пермишен '{0}' был удален для группы '{1}'.",
                ["CmdUserGroupRemoved"] = "Игрок '{0}' был удален из группы '{1}'.",
                ["CmdWipeNew"] = "Новая дата вайпа успешно установлена на '{0}'. Вайп вступит в силу только при загрузке плагина после этой даты.",
                ["CmdWipeReset"] = "Кастомная дата вайпа была сброшена. Вайп снова определяется по Wipe ID.",
                ["CmdWipeFailed"] = "Указанная дата '{0}' имеет неверный формат(yyyy-MM-dd HH:mm) или уже прошла. Пример: '{1}'.",
                ["CmdCheckFormatGroups"] = "'{0}' - {1}({2})",
                ["CmdCheckNoActive"] = "У вас нет активных временных пермишенов или временных групп!",
                ["CmdCheckTargetNoActive"] = "У игрока '{0}' нет активных временных пермишенов или временных групп!",
                ["CmdCheckPermissions"] = string.Join("\n", new string[]
                {
                    "<color=#D1AB9A>У вас есть {0} временных пермишенов(время по UTC):</color>",
                    "{1}"
                }),
                ["CmdCheckGroups"] = string.Join("\n", new string[]
                {
                    "<color=#D1AB9A>У вас есть {0} временных групп(время по UTC):</color>",
                    "{1}"
                }),
                ["CmdCheckTargetPermissions"] = string.Join("\n", new string[]
                {
                    "<color=#D1AB9A>У игрока '{2}' есть {0} временных пермишенов(время по UTC):</color>",
                    "{1}"
                }),
                ["CmdCheckTargetGroups"] = string.Join("\n", new string[]
                {
                    "<color=#D1AB9A>У игрока '{2}' есть {0} временных групп(время по UTC):</color>",
                    "{1}"
                }),
                ["CmdCheckFormatPermissions"] = "'{0}' - {1}({2})",
                ["CmdCheckFormatGroups"] = "'{0}' - {1}({2})",
                ["CmdUntilWipe"] = "До вайпа"
            };
            
            for (int i = 0; i < _config.LanguageKeys.Count; i++)
                lang.RegisterMessages(enLang, this, _config.LanguageKeys[i]);
            lang.RegisterMessages(ruLang, this, "ru");
        }
        #endregion
        
        #region ~Methods~
        private System.Collections.IEnumerator InitPlugin()
        {
            if (_storedData == null)
                _storedData = new StoredData();
            _existingPerms = new HashSet<string>(permission.GetPermissions(), StringComparer.OrdinalIgnoreCase);
            _existingGroups = new HashSet<string>(permission.GetGroups(), StringComparer.OrdinalIgnoreCase);
            
            string permName, groupName;
            TemporaryPermission tempPermission, tempGroup;
            
            var players = _storedData.PlayersList != null ? _storedData.PlayersList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, PlayerData>();
            _storedData.PlayersList = new Dictionary<string, PlayerData>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in players)
            {
                if (!kvp.Key.IsSteamId()) continue;
                var playerData = kvp.Value;
                playerData.UpdateUserID(kvp.Key);
                for (int i = 0; i < playerData.PermissionsList.Count; i++)
                {
                    tempPermission = playerData.PermissionsList[i];
                    if (PermissionExists_Helper(tempPermission.Name, out permName))
                        tempPermission.UpdateName(permName);
                    if (string.IsNullOrWhiteSpace(tempPermission.Name))
                        tempPermission.MarkAsRemoved(Log_UserPerms, $"The permission of player '{playerData.DisplayName}' ({playerData.UserID}), assigned by '{tempPermission.AssignedDate}' and valid until '{(tempPermission.UntilWipe ? "wipe" : tempPermission.ExpireDate)}', has been marked for revocation because the permission name is empty.");
                }
                for (int i = 0; i < playerData.GroupsList.Count; i++)
                {
                    tempGroup = playerData.GroupsList[i];
                    if (GroupExists_Helper(tempGroup.Name, out groupName))
                        tempGroup.UpdateName(groupName);
                    if (string.IsNullOrWhiteSpace(tempGroup.Name))
                        tempGroup.MarkAsRemoved(Log_UserGroups, $"The group of player '{playerData.DisplayName}' ({playerData.UserID}), assigned by '{tempGroup.AssignedDate}' and valid until '{(tempGroup.UntilWipe ? "wipe" : tempGroup.ExpireDate)}', has been marked for revocation because the group name is empty.");
                }
                _storedData.PlayersList[playerData.UserID] = playerData;
            }
            
            var groups = _storedData.GroupsList != null ? _storedData.GroupsList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : new Dictionary<string, GroupData>();
            _storedData.GroupsList = new Dictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in groups)
            {
                if (!GroupExists_Helper(kvp.Key, out groupName) || string.IsNullOrWhiteSpace(groupName)) continue;
                var groupData = kvp.Value;
                groupData.UpdateGroupName(groupName);
                for (int i = 0; i < groupData.PermissionsList.Count; i++)
                {
                    tempPermission = groupData.PermissionsList[i];
                    if (PermissionExists_Helper(tempPermission.Name, out permName))
                        tempPermission.UpdateName(permName);
                    if (string.IsNullOrWhiteSpace(tempPermission.Name))
                        tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission of group '{groupData.GroupName}', assigned by '{tempPermission.AssignedDate}' and valid until '{(tempPermission.UntilWipe ? "wipe" : tempPermission.ExpireDate)}', has been marked for revocation because the permission name is empty.");
                }
                _storedData.GroupsList[groupData.GroupName] = groupData;
            }

            bool isWipe;
            if (!string.IsNullOrWhiteSpace(_config.CustomWipe))
            {
                isWipe = DateTime.TryParseExact(_config.CustomWipe, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var wipeDate) && wipeDate <= DateTime.UtcNow;
                if (isWipe)
                    _config.CustomWipe = "WIPED";
            }
            else
                isWipe = string.IsNullOrWhiteSpace(_config.WipeID) || _config.WipeID != SaveRestore.WipeId;
            _config.WipeID = SaveRestore.WipeId;
            SaveConfig();
            if (isWipe)
            {
                int counter = 0;
                if (_config.RevokeAllOnWipe)
                {
                    foreach (var playerData in _storedData.PlayersList.Values)
                    {
                        counter += playerData.PermissionsList.Count;
                        for (int i = 0; i < playerData.PermissionsList.Count; i++)
                        {
                            tempPermission = playerData.PermissionsList[i];
                            tempPermission.MarkAsRemoved(Log_UserPerms, $"The permission '{tempPermission.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation due to a wipe that removes all temporary permissions.");
                        }
                        counter += playerData.GroupsList.Count;
                        for (int i = 0; i < playerData.GroupsList.Count; i++)
                        {
                            tempGroup = playerData.GroupsList[i];
                            tempGroup.MarkAsRemoved(Log_UserGroups, $"The group '{tempGroup.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation due to a wipe that removes all temporary groups.");
                        }
                    }
                    
                    foreach (var groupData in _storedData.GroupsList.Values)
                    {
                        counter += groupData.PermissionsList.Count;
                        for (int i = 0; i < groupData.PermissionsList.Count; i++)
                        {
                            tempPermission = groupData.PermissionsList[i];
                            tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation due to a wipe that removes all temporary permissions.");
                        }
                    }
                }
                else
                {
                    foreach (var playerData in _storedData.PlayersList.Values)
                    {
                        for (int i = 0; i < playerData.PermissionsList.Count; i++)
                        {
                            tempPermission = playerData.PermissionsList[i];
                            if (tempPermission.UntilWipe)
                            {
                                tempPermission.MarkAsRemoved(Log_UserPerms, $"The permission '{tempPermission.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation due to a wipe.");
                                counter++;
                            }
                        }
                        for (int i = 0; i < playerData.GroupsList.Count; i++)
                        {
                            tempGroup = playerData.GroupsList[i];
                            if (tempGroup.UntilWipe)
                            {
                                tempGroup.MarkAsRemoved(Log_UserGroups, $"The group '{tempGroup.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation due to a wipe.");
                                counter++;
                            }
                        }
                    }
                    
                    foreach (var groupData in _storedData.GroupsList.Values)
                    {
                        for (int i = 0; i < groupData.PermissionsList.Count; i++)
                        {
                            tempPermission = groupData.PermissionsList[i];
                            if (tempPermission.UntilWipe)
                            {
                                tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation due to a wipe.");
                                counter++;
                            }
                        }
                    }
                }
                PrintWarning($"Wipe detected! {counter} temporary permissions and temporary groups will be revoked.");
            }
            CheckForExpiration();
            yield return new WaitForSeconds(1);
            foreach (var playerData in _storedData.PlayersList.Values)
            {
                for (int i = 0; i < playerData.PermissionsList.Count; i++)
                    permission.GrantUserPermission(playerData.UserID, playerData.PermissionsList[i].Name, null);
                for (int i = 0; i < playerData.GroupsList.Count; i++)
                    permission.AddUserGroup(playerData.UserID, playerData.GroupsList[i].Name);
            }
            string[] groupNames = _existingGroups.ToArray();
            for (int i = 0; i < groupNames.Length; i++)
            {
                if (_storedData.GroupsList.TryGetValue(groupNames[i], out var groupData))
                {
                    for (int j = 0; j < groupData.PermissionsList.Count; j++)
                        permission.GrantGroupPermission(groupData.GroupName, groupData.PermissionsList[j].Name, null);
                }
            }
            _expirationTimer = timer.Every(_config.ExpirationInterval, CheckForExpiration);
            if (_config.PresenceInterval > 0f)
                _presenceTimer = timer.Every(_config.PresenceInterval, CheckForPresence);
            Subscribe(nameof(OnServerCommand));
            Subscribe(nameof(OnUserPermissionRevoked));
            Subscribe(nameof(OnUserGroupRemoved));
            Subscribe(nameof(OnGroupPermissionRevoked));
            Subscribe(nameof(OnPermissionRegistered));
            Subscribe(nameof(OnGroupCreated));
            Subscribe(nameof(OnGroupDeleted));
            Subscribe(nameof(OnUserNameUpdated));
            Subscribe(nameof(OnServerSave));
            _isReady = true;
            yield return new WaitForSeconds(1);
            Interface.CallHook(Hooks_OnLoaded, Version);
        }
        
        private void CheckForExpiration()
        {
            bool isExpired;
            var utcNow = DateTime.UtcNow;
            TemporaryPermission tempPermission, tempGroup;
            
            var playersList = _storedData.PlayersList;
            if (playersList.Any())
            {
                var usersToRemove = Pool.Get<List<string>>();
                foreach (var playerData in playersList.Values)
                {
                    if (!playerData.PermissionsList.Any() && !playerData.GroupsList.Any())
                    {
                        usersToRemove.Add(playerData.UserID);
                        continue;
                    }
                    
                    for (int i = playerData.PermissionsList.Count - 1; i >= 0; i--)
                    {
                        tempPermission = playerData.PermissionsList[i];
                        if (tempPermission.UntilWipe)
                        {
                            if (!tempPermission.AlreadyRemoved) continue;
                            isExpired = true;
                        }
                        else
                            isExpired = utcNow > tempPermission.ExpireDate;
                        if (tempPermission.AlreadyRemoved || isExpired)
                        {
                            permission.RevokeUserPermission(playerData.UserID, tempPermission.Name);
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"'{playerData.DisplayName}' ({playerData.UserID}) - Permission has been revoked: '{tempPermission.Name}' | Is expired: {isExpired}");
                            Interface.CallHook(Hooks_UserPermissionRevoked, playerData.UserID, tempPermission.Name, isExpired);
                            playerData.PermissionsList.RemoveAt(i);
                        }
                    }
                    for (int i = playerData.GroupsList.Count - 1; i >= 0; i--)
                    {
                        tempGroup = playerData.GroupsList[i];
                        if (tempGroup.UntilWipe)
                        {
                            if (!tempGroup.AlreadyRemoved) continue;
                            isExpired = true;
                        }
                        else
                            isExpired = utcNow > tempGroup.ExpireDate;
                        if (tempGroup.AlreadyRemoved || isExpired)
                        {
                            permission.RemoveUserGroup(playerData.UserID, tempGroup.Name);
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserGroups, $"'{playerData.DisplayName}' ({playerData.UserID}) - Has been removed from group: '{tempGroup.Name}' | Is expired: {isExpired}");
                            Interface.CallHook(Hooks_UserGroupRemoved, playerData.UserID, tempGroup.Name, isExpired);
                            playerData.GroupsList.RemoveAt(i);
                        }
                    }
                }
                foreach (string userID in usersToRemove)
                    playersList.Remove(userID);
                Pool.FreeUnmanaged(ref usersToRemove);
            }
            
            var groupsList = _storedData.GroupsList;
            if (groupsList.Any())
            {
                var groupsToRemove = Pool.Get<List<string>>();
                foreach (var groupData in groupsList.Values)
                {
                    if (groupData.PermissionsList.Any())
                    {
                        for (int i = groupData.PermissionsList.Count - 1; i >= 0; i--)
                        {
                            tempPermission = groupData.PermissionsList[i];
                            if (tempPermission.UntilWipe)
                            {
                                if (!tempPermission.AlreadyRemoved) continue;
                                isExpired = true;
                            }
                            else
                                isExpired = utcNow > tempPermission.ExpireDate;
                            if (tempPermission.AlreadyRemoved || isExpired)
                            {
                                permission.RevokeGroupPermission(groupData.GroupName, tempPermission.Name);
                                if (_config.FileLogs || _config.ConsoleLogs)
                                    SendLog(Log_GroupPerms, $"'{groupData.GroupName}' (group) - Permission has been revoked: '{tempPermission.Name}' | Is expired: {isExpired}");
                                Interface.CallHook(Hooks_GroupPermissionRevoked, groupData.GroupName, tempPermission.Name, isExpired);
                                groupData.PermissionsList.RemoveAt(i);
                            }
                        }
                    }
                    else
                        groupsToRemove.Add(groupData.GroupName);
                }
                foreach (string userID in groupsToRemove)
                    groupsList.Remove(userID);
                Pool.FreeUnmanaged(ref groupsToRemove);
            }
        }
        
        private void CheckForPresence()
        {
            TemporaryPermission tempPermission, tempGroup;
            HashSet<string> onlineUsers = _config.RevokeOnUnload ? covalence.Players.Connected.Select(player => player.Id).ToHashSet(StringComparer.OrdinalIgnoreCase) : null;
            foreach (var playerData in _storedData.PlayersList.Values)
            {
                if (onlineUsers != null && !onlineUsers.Contains(playerData.UserID))
                    continue;
                
                for (int i = 0; i < playerData.PermissionsList.Count; i++)
                {
                    tempPermission = playerData.PermissionsList[i];
                    if (!tempPermission.AlreadyRemoved && !UserHasPermission_Helper(playerData.UserID, tempPermission.Name))
                    {
                        if (_config.PresenceRestoring)
                            permission.GrantUserPermission(playerData.UserID, tempPermission.Name, null);
                        else
                            tempPermission.MarkAsRemoved(Log_UserPerms, $"The permission '{tempPermission.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation because someone outside the plugin revoked it.");
                    }
                }
                for (int i = 0; i < playerData.GroupsList.Count; i++)
                {
                    tempGroup = playerData.GroupsList[i];
                    if (!tempGroup.AlreadyRemoved && !UserHasGroup_Helper(playerData.UserID, tempGroup.Name))
                    {
                        if (_config.PresenceRestoring)
                            permission.AddUserGroup(playerData.UserID, tempGroup.Name);
                        else
                            tempGroup.MarkAsRemoved(Log_UserGroups, $"The group '{tempGroup.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation because someone outside the plugin revoked it.");
                    }
                }
            }
            
            foreach (var groupData in _storedData.GroupsList.Values)
            {
                if (permission.GetGroupData(groupData.GroupName) == null)
                {
                    for (int i = 0; i < groupData.PermissionsList.Count; i++)
                    {
                        tempPermission = groupData.PermissionsList[i];
                        tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation because the group was deleted.");
                    }
                    continue;
                }
                for (int i = 0; i < groupData.PermissionsList.Count; i++)
                {
                    tempPermission = groupData.PermissionsList[i];
                    if (!tempPermission.AlreadyRemoved && !GroupHasPermission_Helper(groupData.GroupName, tempPermission.Name))
                    {
                        if (_config.PresenceRestoring)
                            permission.GrantGroupPermission(groupData.GroupName, tempPermission.Name, null);
                        else
                            tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation because someone outside the plugin revoked it.");
                    }
                }
            }
        }
        
        private static string ToLangKey(string langKey) => string.IsNullOrWhiteSpace(langKey) || langKey.Length != 2 || !langKey.All(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') ? "en" : langKey.ToLower(System.Globalization.CultureInfo.InvariantCulture);
        
        private void SendMessage(IPlayer player, string text, bool isWarning = true)
        {
            if (_config.GameTips_Enabled && !player.IsServer)
                player.Command(Str_Showtoast, (int)(isWarning ? GameTip.Styles.Error : GameTip.Styles.Blue_Long), text, string.Empty);
            else
                player.Reply(text);
        }
        
        private void SendLog(string filename, string logMsg)
        {
            if (_config.FileLogs)
                LogToFile(filename, logMsg, this, true, true);
            if (_config.ConsoleLogs)
                Puts(logMsg);
        }
        
        private static PlayerData GetOrCreatePlayerData(string userID, string name)
        {
            if (!_storedData.PlayersList.TryGetValue(userID, out var result))
                _storedData.PlayersList[userID] = result = new PlayerData(userID, name);
            return result;
        }
        
        private GroupData GetOrCreateGroupData(string groupName)
        {
            if (GroupExists_Helper(groupName, out var actualName))
                groupName = actualName;
            
            if (!_storedData.GroupsList.TryGetValue(groupName, out var result))
                _storedData.GroupsList[groupName] = result = new GroupData(groupName);
            else if (!string.Equals(result.GroupName, groupName, StringComparison.Ordinal))
                result.UpdateGroupName(groupName);
            return result;
        }
        
        private bool TryGetPlayer(IPlayer initiator, string nameOrId, out IPlayer result, bool skipInitiator = true, bool all = true)
        {
            result = null;
            if (!TryGetPlayers(nameOrId, out var tPlayers, skipInitiator ? initiator : null))
                SendMessage(initiator, string.Format(lang.GetMessage("CmdPlayerNotFound", this, initiator.Id), nameOrId));
            else if (tPlayers.Count > 1)
                initiator.Reply(string.Format(lang.GetMessage("CmdMultiplePlayers", this, initiator.Id), nameOrId, string.Join(", ", tPlayers.Select(t => t.Name).ToArray())));
            else
                result = tPlayers[0];
            return result != null;
        }
        
        private bool TryGetPlayers(string nameOrId, out List<IPlayer> result, IPlayer initiator = null, bool all = true)
        {
            result = new List<IPlayer>();
            if (string.IsNullOrWhiteSpace(nameOrId))
                return false;

            bool onlyDigits = nameOrId.All(char.IsDigit);
            foreach (var player in all ? covalence.Players.All : covalence.Players.Connected)
            {
                if (!player.IsServer &&
                    ((onlyDigits && player.Id.Contains(nameOrId, StringComparison.OrdinalIgnoreCase)) || player.Name.Contains(nameOrId, StringComparison.OrdinalIgnoreCase)))
                    result.Add(player);
            }
            if (initiator != null)
                result.Remove(initiator);
            return result.Any();
        }
        
        private bool TryGetPlayerOxideCmd(string nameOrId, out IPlayer result)
        {
            result = null;
            if (nameOrId.IsSteamId())
            {
                foreach (var player in covalence.Players.All)
                {
                    if (!player.IsServer && player.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        result = player;
                        break;
                    }
                }
            }
            else 
            {
                foreach (var player in covalence.Players.All)
                {
                    if (!player.IsServer && player.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase))
                    {
                        result = player;
                        break;
                    }
                }
            }
            return result != null;
        }
        
        private bool UserHasPermission_Helper(string userID, string permName) => permission.GetUserData(userID).Perms.Contains<string>(permName, StringComparer.OrdinalIgnoreCase);
        private bool UserHasGroup_Helper(string userID, string groupName) => permission.GetUserData(userID).Groups.Contains<string>(groupName, StringComparer.OrdinalIgnoreCase);
        
        private bool GroupHasPermission_Helper(string groupName, string permName)
        {
            var group = permission.GetGroupData(groupName);
            return group != null && group.Perms.Contains<string>(permName, StringComparer.OrdinalIgnoreCase);
        }
        
        private bool PermissionExists_Helper(string input, out string permName) => _existingPerms.TryGetValue(input, out permName);
        private bool GroupExists_Helper(string input, out string groupName) => _existingGroups.TryGetValue(input, out groupName);
        #endregion

        #region ~Server Command~
        void OnServerCommand(ConsoleSystem.Arg arg)
        {
            string[] args = arg.Args;
            if (args == null || args.Length < 3 || !arg.IsAdmin)
                return;
            string fullName = arg.cmd.FullName;
            if (CommandsGrant.Contains(fullName))
                CommandGrant(args);
            else if (CommandsRevoke.Contains(fullName))
                CommandRevoke(args);
            else if (CommandsUserGroup.Contains(fullName))
                CommandUserGroup(args);
        }
        
        private void CommandGrant(string[] args)
        {
            if (args.Length < 4 || !PermissionExists_Helper(args[2], out var permName)) return;
            if (args[0] == CommandUser)
            {
                //          0     1        2                3                     4 - optional
                //o.grant user iiiaka realpve.vip wipe/seconds/DATETIME (true/false)/DATETIME
                if (!TryGetPlayerOxideCmd(args[1], out var player)) return;
                NextTick(() =>
                {
                    if (UserHasPermission_Helper(player.Id, permName))
                    {
                        var playerData = GetOrCreatePlayerData(player.Id, player.Name);
                        if (args[3].Equals(Str_Wipe, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'GrantTemporaryPermission(wipe)' to grant permission '{permName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the console Oxide/Carbon grant command.");
                            GrantTemporaryPermission(playerData, permName, false);
                        }
                        else if (int.TryParse(args[3], out var secondsToAdd))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'GrantTemporaryPermission(seconds)' to grant permission '{permName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the console Oxide/Carbon grant command.");
                            GrantTemporaryPermission(playerData, permName, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false, false);
                        }
                        else if (DateTime.TryParse(args[3], out var expireDate))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'GrantTemporaryPermission(DateTime)' to grant permission '{permName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the console Oxide/Carbon grant command.");
                            GrantTemporaryPermission(playerData, permName, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default, false);
                        }
                    }
                });
            }
            else if (args[0] == CommandGroup)
            {
                //          0     1        2                3                     4 - optional
                //o.grant group admin realpve.vip wipe/seconds/DATETIME (true/false)/DATETIME
                if (!GroupExists_Helper(args[1], out var groupName)) return;
                NextTick(() =>
                {
                    if (GroupHasPermission_Helper(groupName, permName))
                    {
                        var groupData = GetOrCreateGroupData(groupName);
                        if (args[3].Equals(Str_Wipe, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_GroupPerms, $"Method call 'GrantTemporaryPermission(wipe)' to grant permission '{permName}' to group '{groupData.GroupName}' as a result of using the console Oxide/Carbon grant command.");
                            GrantTemporaryPermission(groupData, permName, false);
                        }
                        else if (int.TryParse(args[3], out var secondsToAdd))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_GroupPerms, $"Method call 'GrantTemporaryPermission(seconds)' to grant permission '{permName}' to group '{groupData.GroupName}' as a result of using the console Oxide/Carbon grant command.");
                            GrantTemporaryPermission(groupData, permName, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false, false);
                        }
                        else if (DateTime.TryParse(args[3], out var expireDate))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_GroupPerms, $"Method call 'GrantTemporaryPermission(DateTime)' to grant permission '{permName}' to group '{groupData.GroupName}' as a result of using the console Oxide/Carbon grant command.");
                            GrantTemporaryPermission(groupData, permName, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default, false);
                        }
                    }
                });
            }
        }

        private void CommandRevoke(string[] args)
        {
            if (args.Length < 3 || !PermissionExists_Helper(args[2], out var permName)) return;
            if (args[0] == CommandUser)
            {
                //           0     1        2
                //o.revoke user iiiaka realpve.vip
                if (!_storedData.PlayersList.TryGetValue(args[1], out var playerData)) return;
                var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    NextTick(() =>
                    {
                        if (!UserHasPermission_Helper(playerData.UserID, tempPermission.Name))
                            tempPermission.MarkAsRemoved(Log_UserPerms, $"The permission '{tempPermission.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation as a result of using the console Oxide/Carbon revoke command.");
                    });
                }
            }
            else if (args[0] == CommandGroup)
            {
                //           0     1        2
                //o.revoke group admin realpve.vip
                if (!GroupExists_Helper(args[1], out var groupName) || !_storedData.GroupsList.TryGetValue(groupName, out var groupData)) return;
                var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    NextTick(() =>
                    {
                        if (!GroupHasPermission_Helper(groupData.GroupName, tempPermission.Name))
                            tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation as a result of using the console Oxide/Carbon revoke command.");
                    });
                }
            }
        }

        private void CommandUserGroup(string[] args)
        {
            if (args.Length < 4 || !TryGetPlayerOxideCmd(args[1], out var player) || !GroupExists_Helper(args[2], out var groupName)) return;
            if (args[0] == CommandAdd)
            {
                //             0     1     2             3                     4 - optional
                //o.usergroup add iiiaka admin wipe/seconds/DATETIME (true/false)/DATETIME
                NextTick(() =>
                {
                    if (UserHasGroup_Helper(player.Id, groupName))
                    {
                        var playerData = GetOrCreatePlayerData(player.Id, player.Name);
                        if (args[3].Equals(Str_Wipe, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'AddTemporaryGroup(wipe)' to grant group '{groupName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the console Oxide/Carbon add command.");
                            AddTemporaryGroup(playerData, groupName, false);
                        }
                        else if (int.TryParse(args[3], out var secondsToAdd))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'AddTemporaryGroup(seconds)' to grant group '{groupName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the console Oxide/Carbon add command.");
                            AddTemporaryGroup(playerData, groupName, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false, false);
                        }
                        else if (DateTime.TryParse(args[3], out var expireDate))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'AddTemporaryGroup(DateTime)' to grant group '{groupName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the console Oxide/Carbon add command.");
                            AddTemporaryGroup(playerData, groupName, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default, false);
                        }
                    }
                });
            }
            else if (args[0] == CommandRemove && _storedData.PlayersList.TryGetValue(player.Id, out var playerData))
            {
                //               0      1     2
                //o.usergroup remove iiiaka admin
                var tempGroup = playerData.GroupsList.FirstOrDefault(p => p.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                if (tempGroup != null && !tempGroup.AlreadyRemoved)
                {
                    NextTick(() =>
                    {
                        if (!UserHasGroup_Helper(playerData.UserID, tempGroup.Name))
                            tempGroup.MarkAsRemoved(Log_UserGroups, $"The group '{tempGroup.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation as a result of using the console Oxide/Carbon revoke command.");
                    });
                }
            }
        }
        #endregion

        #region ~Local~
        private void GrantTemporaryPermission(PlayerData playerData, string permName, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantUserPermission(playerData.UserID, permName, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < playerData.PermissionsList.Count; i++)
            {
                tempPermission = playerData.PermissionsList[i];
                if (tempPermission.Name.Equals(permName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(tempPermission.Name, permName, StringComparison.Ordinal))
                        tempPermission.UpdateName(permName);
                    tempPermission.ExpireDate = default;
                    tempPermission.UntilWipe = true;
                    tempPermission.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_UserPerms, $"'{playerData.DisplayName}' ({playerData.UserID}) - Permission has been extended: '{tempPermission.Name}' until the wipe");
                    Interface.CallHook(Hooks_UserPermissionUpdated, playerData.UserID, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    return;
                }
            }
            tempPermission = new TemporaryPermission(permName);
            playerData.PermissionsList.Add(tempPermission);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_UserPerms, $"'{playerData.DisplayName}' ({playerData.UserID}) - Permission has been granted: '{tempPermission.Name}' until the wipe");
            Interface.CallHook(Hooks_UserPermissionGranted, playerData.UserID, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void GrantTemporaryPermission(PlayerData playerData, string permName, int secondsToAdd, bool fromNow, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantUserPermission(playerData.UserID, permName, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < playerData.PermissionsList.Count; i++)
            {
                tempPermission = playerData.PermissionsList[i];
                if (tempPermission.Name.Equals(permName, StringComparison.OrdinalIgnoreCase))
                {
                    tempPermission.ExpireDate = fromNow || tempPermission.ExpireDate == default ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempPermission.ExpireDate.AddSeconds(secondsToAdd);
                    tempPermission.UntilWipe = false;
                    tempPermission.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_UserPerms, $"'{playerData.DisplayName}' ({playerData.UserID}) - Permission has been extended: '{tempPermission.Name}' until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
                    Interface.CallHook(Hooks_UserPermissionUpdated, playerData.UserID, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    return;
                }
            }
            tempPermission = new TemporaryPermission(permName, secondsToAdd);
            playerData.PermissionsList.Add(tempPermission);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_UserPerms, $"'{playerData.DisplayName}' ({playerData.UserID}) - Permission has been granted: '{tempPermission.Name}' until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
            Interface.CallHook(Hooks_UserPermissionGranted, playerData.UserID, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void GrantTemporaryPermission(PlayerData playerData, string permName, DateTime expireDate, DateTime assignedDate, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantUserPermission(playerData.UserID, permName, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < playerData.PermissionsList.Count; i++)
            {
                tempPermission = playerData.PermissionsList[i];
                if (tempPermission.Name.Equals(permName, StringComparison.OrdinalIgnoreCase))
                {
                    if (assignedDate != default)
                        tempPermission.AssignedDate = assignedDate;
                    tempPermission.ExpireDate = expireDate;
                    tempPermission.UntilWipe = false;
                    tempPermission.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_UserPerms, $"'{playerData.DisplayName}' ({playerData.UserID}) - Permission has been extended: '{tempPermission.Name}' until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
                    Interface.CallHook(Hooks_UserPermissionUpdated, playerData.UserID, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    return;
                }
            }
            tempPermission = new TemporaryPermission(permName, expireDate, assignedDate);
            playerData.PermissionsList.Add(tempPermission);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_UserPerms, $"'{playerData.DisplayName}' ({playerData.UserID}) - Permission has been granted: '{tempPermission.Name}' until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
            Interface.CallHook(Hooks_UserPermissionGranted, playerData.UserID, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void AddTemporaryGroup(PlayerData playerData, string groupName, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.AddUserGroup(playerData.UserID, groupName);
            TemporaryPermission tempGroup;
            for (int i = 0; i < playerData.GroupsList.Count; i++)
            {
                tempGroup = playerData.GroupsList[i];
                if (tempGroup.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                {
                    tempGroup.ExpireDate = default;
                    tempGroup.UntilWipe = true;
                    tempGroup.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_UserGroups, $"'{playerData.DisplayName}' ({playerData.UserID}) - Extended time for participating in group: '{tempGroup.Name}' until the wipe");
                    Interface.CallHook(Hooks_UserGroupUpdated, playerData.UserID, tempGroup.Name, tempGroup.ExpireDate, tempGroup.AssignedDate);
                    return;
                }
            }
            tempGroup = new TemporaryPermission(groupName);
            playerData.GroupsList.Add(tempGroup);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_UserGroups, $"'{playerData.DisplayName}' ({playerData.UserID}) - Added to group: '{tempGroup.Name}' until the wipe");
            Interface.CallHook(Hooks_UserGroupAdded, playerData.UserID, tempGroup.Name, tempGroup.ExpireDate, tempGroup.AssignedDate);
        }
        
        private void AddTemporaryGroup(PlayerData playerData, string groupName, int secondsToAdd, bool fromNow, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.AddUserGroup(playerData.UserID, groupName);
            TemporaryPermission tempGroup;
            for (int i = 0; i < playerData.GroupsList.Count; i++)
            {
                tempGroup = playerData.GroupsList[i];
                if (tempGroup.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                {
                    tempGroup.ExpireDate = fromNow || tempGroup.ExpireDate == default ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempGroup.ExpireDate.AddSeconds(secondsToAdd);
                    tempGroup.UntilWipe = false;
                    tempGroup.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_UserGroups, $"'{playerData.DisplayName}' ({playerData.UserID}) - Extended time for participating in group: '{tempGroup.Name}' until {tempGroup.ExpireDate.ToString(TimeFormat)}(UTC)");
                    Interface.CallHook(Hooks_UserGroupUpdated, playerData.UserID, tempGroup.Name, tempGroup.ExpireDate, tempGroup.AssignedDate);
                    return;
                }
            }
            tempGroup = new TemporaryPermission(groupName, secondsToAdd);
            playerData.GroupsList.Add(tempGroup);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_UserGroups, $"'{playerData.DisplayName}' ({playerData.UserID}) - Added to group: '{tempGroup.Name}' until {tempGroup.ExpireDate.ToString(TimeFormat)}(UTC)");
            Interface.CallHook(Hooks_UserGroupAdded, playerData.UserID, tempGroup.Name, tempGroup.ExpireDate, tempGroup.AssignedDate);
        }
        
        private void AddTemporaryGroup(PlayerData playerData, string groupName, DateTime expireDate, DateTime assignedDate, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.AddUserGroup(playerData.UserID, groupName);
            TemporaryPermission tempGroup;
            for (int i = 0; i < playerData.GroupsList.Count; i++)
            {
                tempGroup = playerData.GroupsList[i];
                if (tempGroup.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                {
                    if (assignedDate != default)
                        tempGroup.AssignedDate = assignedDate;
                    tempGroup.ExpireDate = expireDate;
                    tempGroup.UntilWipe = false;
                    tempGroup.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_UserGroups, $"'{playerData.DisplayName}' ({playerData.UserID}) - Extended time for participating in group: '{tempGroup.Name}' until {tempGroup.ExpireDate.ToString(TimeFormat)}(UTC)");
                    Interface.CallHook(Hooks_UserGroupUpdated, playerData.UserID, tempGroup.Name, tempGroup.ExpireDate, tempGroup.AssignedDate);
                    return;
                }
            }
            tempGroup = new TemporaryPermission(groupName, expireDate, assignedDate);
            playerData.GroupsList.Add(tempGroup);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_UserGroups, $"'{playerData.DisplayName}' ({playerData.UserID}) - Added to group: '{tempGroup.Name}' until {tempGroup.ExpireDate.ToString(TimeFormat)}(UTC)");
            Interface.CallHook(Hooks_UserGroupAdded, playerData.UserID, tempGroup.Name, tempGroup.ExpireDate, tempGroup.AssignedDate);
        }
        
        private void GrantTemporaryPermission(GroupData groupData, string permName, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantGroupPermission(groupData.GroupName, permName, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < groupData.PermissionsList.Count; i++)
            {
                tempPermission = groupData.PermissionsList[i];
                if (tempPermission.Name.Equals(permName, StringComparison.OrdinalIgnoreCase))
                {
                    tempPermission.ExpireDate = default;
                    tempPermission.UntilWipe = true;
                    tempPermission.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_GroupPerms, $"'{groupData.GroupName}' (group) - Permission has been extended: '{tempPermission.Name}' until the wipe");
                    Interface.CallHook(Hooks_GroupPermissionUpdated, groupData.GroupName, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    return;
                }
            }
            tempPermission = new TemporaryPermission(permName);
            groupData.PermissionsList.Add(tempPermission);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_GroupPerms, $"'{groupData.GroupName}' (group) - Permission has been granted: '{tempPermission.Name}' until the wipe");
            Interface.CallHook(Hooks_GroupPermissionGranted, groupData.GroupName, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void GrantTemporaryPermission(GroupData groupData, string permName, int secondsToAdd, bool fromNow, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantGroupPermission(groupData.GroupName, permName, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < groupData.PermissionsList.Count; i++)
            {
                tempPermission = groupData.PermissionsList[i];
                if (tempPermission.Name.Equals(permName, StringComparison.OrdinalIgnoreCase))
                {
                    tempPermission.ExpireDate = fromNow || tempPermission.ExpireDate == default ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempPermission.ExpireDate.AddSeconds(secondsToAdd);
                    tempPermission.UntilWipe = false;
                    tempPermission.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_GroupPerms, $"'{groupData.GroupName}' (group) - Permission has been extended: '{tempPermission.Name}' until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
                    Interface.CallHook(Hooks_GroupPermissionUpdated, groupData.GroupName, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    return;
                }
            }
            tempPermission = new TemporaryPermission(permName, secondsToAdd);
            groupData.PermissionsList.Add(tempPermission);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_GroupPerms, $"'{groupData.GroupName}' (group) - Permission has been granted: '{tempPermission.Name}' until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
            Interface.CallHook(Hooks_GroupPermissionGranted, groupData.GroupName, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void GrantTemporaryPermission(GroupData groupData, string permName, DateTime expireDate, DateTime assignedDate, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantGroupPermission(groupData.GroupName, permName, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < groupData.PermissionsList.Count; i++)
            {
                tempPermission = groupData.PermissionsList[i];
                if (tempPermission.Name.Equals(permName, StringComparison.OrdinalIgnoreCase))
                {
                    if (assignedDate != default)
                        tempPermission.AssignedDate = assignedDate;
                    tempPermission.ExpireDate = expireDate;
                    tempPermission.UntilWipe = false;
                    tempPermission.AlreadyRemoved = false;
                    if (_config.FileLogs || _config.ConsoleLogs)
                        SendLog(Log_GroupPerms, $"'{groupData.GroupName}' (group) - Permission has been extended: '{tempPermission.Name}' until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
                    Interface.CallHook(Hooks_GroupPermissionUpdated, groupData.GroupName, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    return;
                }
            }
            tempPermission = new TemporaryPermission(permName, expireDate, assignedDate);
            groupData.PermissionsList.Add(tempPermission);
            if (_config.FileLogs || _config.ConsoleLogs)
                SendLog(Log_GroupPerms, $"'{groupData.GroupName}' (group) - Permission has been granted: '{tempPermission.Name}' until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
            Interface.CallHook(Hooks_GroupPermissionGranted, groupData.GroupName, tempPermission.Name, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        #endregion

        #region ~API~
        private object IsReady() => _isReady ? true : null;
        #endregion

        #region ~API - User's Permissions~
        private bool GrantUserPermission(string userID, string permName, int secondsToAdd, bool fromNow = false, bool checkExistence = true) => GrantUserPermission(covalence.Players.FindPlayerById(userID), permName, secondsToAdd, fromNow, checkExistence);
        private bool GrantUserPermission(BasePlayer player, string permName, int secondsToAdd, bool fromNow = false, bool checkExistence = true) => GrantUserPermission(player.IPlayer, permName, secondsToAdd, fromNow, checkExistence);
        private bool GrantUserPermission(IPlayer player, string permName, int secondsToAdd, bool fromNow = false, bool checkExistence = true)
        {
            if (player != null && (!checkExistence || PermissionExists_Helper(permName, out permName)))
            {
                var playerData = GetOrCreatePlayerData(player.Id, player.Name);
                if (_config.FileLogs || _config.ConsoleLogs)
                    SendLog(Log_UserPerms, $"Method call 'GrantTemporaryPermission({(secondsToAdd < 1 ? "wipe" : "seconds")})' to grant permission '{permName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the API method 'GrantUserPermission'.");
                
                if (secondsToAdd < 1)
                    GrantTemporaryPermission(playerData, permName);
                else
                    GrantTemporaryPermission(playerData, permName, secondsToAdd, fromNow);
                return true;
            }
            return false;
        }
        
        private bool GrantUserPermission(string userID, string permName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true) => GrantUserPermission(covalence.Players.FindPlayerById(userID), permName, expireDate, assignedDate, checkExistence);
        private bool GrantUserPermission(BasePlayer player, string permName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true) => GrantUserPermission(player.IPlayer, permName, expireDate, assignedDate, checkExistence);
        private bool GrantUserPermission(IPlayer player, string permName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true)
        {
            if (player != null && (!checkExistence || PermissionExists_Helper(permName, out permName)))
            {
                var playerData = GetOrCreatePlayerData(player.Id, player.Name);
                if (_config.FileLogs || _config.ConsoleLogs)
                    SendLog(Log_UserPerms, $"Method call 'GrantTemporaryPermission(DateTime)' to grant permission '{permName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the API method 'GrantUserPermission'.");
                
                GrantTemporaryPermission(playerData, permName, expireDate, assignedDate);
                return true;
            }
            return false;
        }
        
        private bool RevokeUserPermission(IPlayer player, string permName) => RevokeUserPermission(player.Id, permName);
        private bool RevokeUserPermission(BasePlayer player, string permName) => RevokeUserPermission(player.UserIDString, permName);
        private bool RevokeUserPermission(string userID, string permName)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null)
                {
                    tempPermission.MarkAsRemoved(Log_UserPerms, $"The permission '{tempPermission.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation as a result of using the API method 'RevokeUserPermission'.");
                    return true;
                }
            }
            return false;
        }
        
        private bool UserHasPermission(IPlayer player, string permName) => UserHasPermission(player.Id, permName);
        private bool UserHasPermission(BasePlayer player, string permName) => UserHasPermission(player.UserIDString, permName);
        private bool UserHasPermission(string userID, string permName)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    if (!UserHasPermission_Helper(userID, permName))
                        permission.GrantUserPermission(playerData.UserID, permName, null);
                    return true;
                }
            }
            return false;
        }
        
        private int GrantActiveUsersPermission(string permName, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (PermissionExists_Helper(permName, out permName))
            {
                foreach (var player in covalence.Players.Connected)
                {
                    if (GrantUserPermission(player, permName, secondsToAdd, fromNow, false))
                        result++;
                }
            }
            return result;
        }

        private int GrantActiveUsersPermission(string permName, DateTime expireDate, DateTime assignedDate = default)
        {
            int result = 0;
            if (PermissionExists_Helper(permName, out permName))
            {
                foreach (var player in covalence.Players.Connected)
                {
                    if (GrantUserPermission(player, permName, expireDate, assignedDate, false))
                        result++;
                }
            }
            return result;
        }
        
        private int GrantAllUsersPermission(string permName, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (PermissionExists_Helper(permName, out permName))
            {
                foreach (var player in covalence.Players.All)
                {
                    if (GrantUserPermission(player, permName, secondsToAdd, fromNow, false))
                        result++;
                }
            }
            return result;
        }

        private int GrantAllUsersPermission(string permName, DateTime expireDate, DateTime assignedDate = default)
        {
            int result = 0;
            if (PermissionExists_Helper(permName, out permName))
            {
                foreach (var player in covalence.Players.All)
                {
                    if (GrantUserPermission(player, permName, expireDate, assignedDate, false))
                        result++;
                }
            }
            return result;
        }

        private int RevokeActiveUsersPermission(string permName)
        {
            int result = 0;
            foreach (var player in covalence.Players.Connected)
            {
                if (RevokeUserPermission(player.Id, permName))
                    result++;
            }
            return result;
        }

        private int RevokeAllUsersPermission(string permName)
        {
            int result = 0;
            foreach (var player in covalence.Players.All)
            {
                if (RevokeUserPermission(player.Id, permName))
                    result++;
            }
            return result;
        }
        
        private Dictionary<string, DateTime[]> UserGetAllPermissions(IPlayer player) => UserGetAllPermissions(player.Id);
        private Dictionary<string, DateTime[]> UserGetAllPermissions(BasePlayer player) => UserGetAllPermissions(player.UserIDString);
        private Dictionary<string, DateTime[]> UserGetAllPermissions(string userID)
        {
            var result = new Dictionary<string, DateTime[]>();
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                TemporaryPermission tempPermission;
                for (int i = 0; i < playerData.PermissionsList.Count; i++)
                {
                    tempPermission = playerData.PermissionsList[i];
                    if (tempPermission != null && !tempPermission.AlreadyRemoved)
                        result[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
                }
            }
            return result;
        }
        
        private DateTime[] UserGetPermissionByExpiry(IPlayer player, string permName) => UserGetPermissionByExpiry(player.Id, permName);
        private DateTime[] UserGetPermissionByExpiry(BasePlayer player, string permName) => UserGetPermissionByExpiry(player.UserIDString, permName);
        private DateTime[] UserGetPermissionByExpiry(string userID, string permName)
        {
            var result = new DateTime[2];
            TemporaryPermission tempPermission;
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    result[0] = tempPermission.AssignedDate;
                    result[1] = tempPermission.ExpireDate;
                    if (tempPermission.ExpireDate == default)
                        return result;
                }
            }

            var groups = permission.GetUserGroups(userID);
            for (int i = 0; i < groups.Length; i++)
            {
                if (_storedData.GroupsList.TryGetValue(groups[i], out var groupData))
                {
                    tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                    if (tempPermission != null && !tempPermission.AlreadyRemoved)
                    {
                        result[0] = tempPermission.AssignedDate;
                        result[1] = tempPermission.ExpireDate;
                        if (tempPermission.ExpireDate == default)
                            return result;
                    }
                }
            }

            return result[0] == default ? Array.Empty<DateTime>() : result;
        }
        
        private Dictionary<string, DateTime[]> UserGetAllPermissionsByExpiry(IPlayer player) => UserGetAllPermissionsByExpiry(player.Id);
        private Dictionary<string, DateTime[]> UserGetAllPermissionsByExpiry(BasePlayer player) => UserGetAllPermissionsByExpiry(player.UserIDString);
        private Dictionary<string, DateTime[]> UserGetAllPermissionsByExpiry(string userID)
        {
            var result = UserGetAllPermissions(userID);
            
            var groups = permission.GetUserGroups(userID);
            TemporaryPermission tempPermission;
            for (int i = 0; i < groups.Length; i++)
            {
                if (_storedData.GroupsList.TryGetValue(groups[i], out var groupData))
                {
                    for (int j = 0; j < groupData.PermissionsList.Count; j++)
                    {
                        tempPermission = groupData.PermissionsList[j];
                        if (tempPermission == null || tempPermission.AlreadyRemoved) continue;
                        if (!result.ContainsKey(tempPermission.Name))
                            result[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
                        else
                        {
                            var existedPermission = result[tempPermission.Name];
                            if (existedPermission[1] != default && tempPermission.ExpireDate > existedPermission[1])
                            {
                                existedPermission[0] = tempPermission.AssignedDate;
                                existedPermission[1] = tempPermission.ExpireDate;
                            }
                        }
                    }
                }
            }
            
            return result;
        }
        
        private Dictionary<string, Dictionary<string, DateTime[]>> ActiveUsersGetAllPermissions()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            foreach (var player in covalence.Players.Connected)
            {
                var perms = UserGetAllPermissions(player.Id);
                if (perms.Any())
                    result[player.Id] = perms;
            }
            return result;
        }
        
        private Dictionary<string, Dictionary<string, DateTime[]>> AllUsersGetAllPermissions()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            TemporaryPermission tempPermission;
            foreach (var playerData in _storedData.PlayersList.Values)
            {
                var perms = new Dictionary<string, DateTime[]>();
                for (int i = 0; i < playerData.PermissionsList.Count; i++)
                {
                    tempPermission = playerData.PermissionsList[i];
                    if (tempPermission != null && !tempPermission.AlreadyRemoved)
                        perms[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
                }
                if (perms.Any())
                    result[playerData.UserID] = perms;
            }
            return result;
        }
        #endregion

        #region ~API - User's Groups~
        private bool AddUserGroup(string userID, string groupName, int secondsToAdd, bool fromNow = false, bool checkExistence = true) => AddUserGroup(covalence.Players.FindPlayerById(userID), groupName, secondsToAdd, fromNow, checkExistence);
        private bool AddUserGroup(BasePlayer player, string groupName, int secondsToAdd, bool fromNow = false, bool checkExistence = true) => AddUserGroup(player.IPlayer, groupName, secondsToAdd, fromNow, checkExistence);
        private bool AddUserGroup(IPlayer player, string groupName, int secondsToAdd, bool fromNow = false, bool checkExistence = true)
        {
            if (player != null && (!checkExistence || GroupExists_Helper(groupName, out groupName)))
            {
                var playerData = GetOrCreatePlayerData(player.Id, player.Name);
                if (_config.FileLogs || _config.ConsoleLogs)
                    SendLog(Log_UserGroups, $"Method call 'AddTemporaryGroup({(secondsToAdd < 1 ? "wipe" : "seconds")})' to grant group '{groupName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the API method 'AddUserGroup'.");
                
                if (secondsToAdd < 1)
                    AddTemporaryGroup(playerData, groupName);
                else
                    AddTemporaryGroup(playerData, groupName, secondsToAdd, fromNow);
                return true;
            }
            return false;
        }

        private bool AddUserGroup(string userID, string groupName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true) => AddUserGroup(covalence.Players.FindPlayerById(userID), groupName, expireDate, assignedDate, checkExistence);
        private bool AddUserGroup(BasePlayer player, string groupName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true) => AddUserGroup(player.IPlayer, groupName, expireDate, assignedDate, checkExistence);
        private bool AddUserGroup(IPlayer player, string groupName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true)
        {
            if (player != null && (!checkExistence || GroupExists_Helper(groupName, out groupName)))
            {
                var playerData = GetOrCreatePlayerData(player.Id, player.Name);
                if (_config.FileLogs || _config.ConsoleLogs)
                    SendLog(Log_UserGroups, $"Method call 'AddTemporaryGroup(DateTime)' to grant group '{groupName}' to player '{playerData.DisplayName}' ({playerData.UserID}) as a result of using the API method 'AddUserGroup'.");
                
                AddTemporaryGroup(playerData, groupName, expireDate, assignedDate);
                return true;
            }
            return false;
        }
        
        private bool RemoveUserGroup(IPlayer player, string groupName) => RemoveUserGroup(player.Id, groupName);
        private bool RemoveUserGroup(BasePlayer player, string groupName) => RemoveUserGroup(player.UserIDString, groupName);
        private bool RemoveUserGroup(string userID, string groupName)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                var tempGroup = playerData.GroupsList.FirstOrDefault(p => p.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                if (tempGroup != null)
                {
                    tempGroup.MarkAsRemoved(Log_UserGroups, $"The group '{tempGroup.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation as a result of using the API method 'RemoveUserGroup'.");
                    return true;
                }
            }
            return false;
        }
        
        private bool UserHasGroup(IPlayer player, string groupName) => UserHasGroup(player.Id, groupName);
        private bool UserHasGroup(BasePlayer player, string groupName) => UserHasGroup(player.UserIDString, groupName);
        private bool UserHasGroup(string userID, string groupName)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                var tempGroup = playerData.GroupsList.FirstOrDefault(p => p.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                if (tempGroup != null && !tempGroup.AlreadyRemoved)
                {
                    if (!UserHasGroup_Helper(userID, groupName))
                        permission.AddUserGroup(playerData.UserID, groupName);
                    return true;
                }
            }
            return false;
        }
        
        private int AddActiveUsersGroup(string groupName, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (GroupExists_Helper(groupName, out groupName))
            {
                foreach (var player in covalence.Players.Connected)
                {
                    if (AddUserGroup(player, groupName, secondsToAdd, fromNow, false))
                        result++;
                }
            }
            return result;
        }

        private int AddActiveUsersGroup(string groupName, DateTime expireDate, DateTime assignedDate = default)
        {
            int result = 0;
            if (GroupExists_Helper(groupName, out groupName))
            {
                foreach (var player in covalence.Players.Connected)
                {
                    if (AddUserGroup(player, groupName, expireDate, assignedDate, false))
                        result++;
                }
            }
            return result;
        }
        
        private int AddAllUsersGroup(string groupName, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (GroupExists_Helper(groupName, out groupName))
            {
                foreach (var player in covalence.Players.All)
                {
                    if (AddUserGroup(player, groupName, secondsToAdd, fromNow, false))
                        result++;
                }
            }
            return result;
        }

        private int AddAllUsersGroup(string groupName, DateTime expireDate, DateTime assignedDate = default)
        {
            int result = 0;
            if (GroupExists_Helper(groupName, out groupName))
            {
                foreach (var player in covalence.Players.All)
                {
                    if (AddUserGroup(player, groupName, expireDate, assignedDate, false))
                        result++;
                }
            }
            return result;
        }

        private int RemoveActiveUsersGroup(string groupName)
        {
            int result = 0;
            if (GroupExists_Helper(groupName, out groupName))
            {
                foreach (var player in covalence.Players.Connected)
                {
                    if (RemoveUserGroup(player.Id, groupName))
                        result++;
                }
            }
            return result;
        }

        private int RemoveAllUsersGroup(string groupName)
        {
            int result = 0;
            if (GroupExists_Helper(groupName, out groupName))
            {
                foreach (var player in covalence.Players.All)
                {
                    if (RemoveUserGroup(player.Id, groupName))
                        result++;
                }
            }
            return result;
        }
        
        private Dictionary<string, DateTime[]> UserGetAllGroups(IPlayer player) => UserGetAllGroups(player.Id);
        private Dictionary<string, DateTime[]> UserGetAllGroups(BasePlayer player) => UserGetAllGroups(player.UserIDString);
        private Dictionary<string, DateTime[]> UserGetAllGroups(string userID)
        {
            var result = new Dictionary<string, DateTime[]>();
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                TemporaryPermission tempGroup;
                for (int i = 0; i < playerData.GroupsList.Count; i++)
                {
                    tempGroup = playerData.GroupsList[i];
                    if (tempGroup != null && !tempGroup.AlreadyRemoved)
                        result[tempGroup.Name] = new DateTime[2] { tempGroup.AssignedDate, tempGroup.ExpireDate };
                }
            }
            return result;
        }
        
        private Dictionary<string, Dictionary<string, DateTime[]>> ActiveUsersGetAllGroups()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            foreach (var player in covalence.Players.Connected)
            {
                var groups = UserGetAllGroups(player.Id);
                if (groups.Any())
                    result[player.Id] = groups;
            }
            return result;
        }
        
        private Dictionary<string, Dictionary<string, DateTime[]>> AllUsersGetAllGroups()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            TemporaryPermission tempGroup;
            foreach (var playerData in _storedData.PlayersList.Values)
            {
                var groups = new Dictionary<string, DateTime[]>();
                for (int i = 0; i < playerData.GroupsList.Count; i++)
                {
                    tempGroup = playerData.GroupsList[i];
                    if (tempGroup != null && !tempGroup.AlreadyRemoved)
                        groups[tempGroup.Name] = new DateTime[2] { tempGroup.AssignedDate, tempGroup.ExpireDate };
                }
                if (groups.Any())
                    result[playerData.UserID] = groups;
            }
            return result;
        }
        #endregion

        #region ~API - Group's Permissions~
        private bool GrantGroupPermission(string groupName, string permName, int secondsToAdd, bool fromNow = false, bool checkExistence = true)
        {
            if (!checkExistence || (GroupExists_Helper(groupName, out groupName) && PermissionExists_Helper(permName, out permName)))
            {
                var groupData = GetOrCreateGroupData(groupName);
                if (_config.FileLogs || _config.ConsoleLogs)
                    SendLog(Log_GroupPerms, $"Method call 'GrantTemporaryPermission({(secondsToAdd < 1 ? "wipe" : "seconds")})' to grant permission '{permName}' to group '{groupData.GroupName}' as a result of using the API method 'GrantGroupPermission'.");
                
                if (secondsToAdd < 1)
                    GrantTemporaryPermission(groupData, permName);
                else
                    GrantTemporaryPermission(groupData, permName, secondsToAdd, fromNow);
                return true;
            }
            return false;
        }
        
        private bool GrantGroupPermission(string groupName, string permName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true)
        {
            if (!checkExistence || (GroupExists_Helper(groupName, out groupName) && PermissionExists_Helper(permName, out permName)))
            {
                var groupData = GetOrCreateGroupData(groupName);
                if (_config.FileLogs || _config.ConsoleLogs)
                    SendLog(Log_GroupPerms, $"Method call 'GrantTemporaryPermission(DateTime)' to grant permission '{permName}' to group '{groupData.GroupName}' as a result of using the API method 'GrantGroupPermission'.");
                
                GrantTemporaryPermission(groupData, permName, expireDate, assignedDate);
                return true;
            }
            return false;
        }
        
        private bool RevokeGroupPermission(string groupName, string permName, bool checkExistence = true)
        {
            if ((!checkExistence || GroupExists_Helper(groupName, out groupName)) && _storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null)
                {
                    tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation as a result of using the API method 'RevokeGroupPermission'.");
                    return true;
                }
            }
            return false;
        }
        
        private bool GroupHasPermission(string groupName, string permName)
        {
            if (GroupExists_Helper(groupName, out groupName) && _storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    if (!GroupHasPermission_Helper(groupName, permName))
                        permission.GrantGroupPermission(groupData.GroupName, permName, null);
                    return true;
                }
            }
            return false;
        }
        
        private int GrantAllGroupsPermission(string permName, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (PermissionExists_Helper(permName, out permName))
            {
                var groups = permission.GetGroups();
                for (int i = 0; i < groups.Length; i++)
                {
                    if (GrantGroupPermission(groups[i], permName, secondsToAdd, fromNow, false))
                        result++;
                }
            }
            return result;
        }
        
        private int GrantAllGroupsPermission(string permName, DateTime expireDate, DateTime assignedDate = default)
        {
            int result = 0;
            if (PermissionExists_Helper(permName, out permName))
            {
                var groups = permission.GetGroups();
                for (int i = 0; i < groups.Length; i++)
                {
                    if (GrantGroupPermission(groups[i], permName, expireDate, assignedDate, false))
                        result++;
                }
            }
            return result;
        }
        
        private int RevokeAllGroupsPermission(string permName)
        {
            int result = 0;
            if (PermissionExists_Helper(permName, out permName))
            {
                var groups = permission.GetGroups();
                for (int i = 0; i < groups.Length; i++)
                {
                    if (RevokeGroupPermission(groups[i], permName))
                        result++;
                }
            }
            return result;
        }
        
        private Dictionary<string, DateTime[]> GroupGetAllPermissions(string groupName)
        {
            var result = new Dictionary<string, DateTime[]>();
            if (_storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                TemporaryPermission tempPermission;
                for (int i = 0; i < groupData.PermissionsList.Count; i++)
                {
                    tempPermission = groupData.PermissionsList[i];
                    if (tempPermission != null && !tempPermission.AlreadyRemoved)
                        result[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
                }
            }
            return result;
        }
        
        private Dictionary<string, Dictionary<string, DateTime[]>> AllGroupsGetAllPermissions()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            TemporaryPermission tempPermission;
            foreach (var groupData in _storedData.GroupsList.Values)
            {
                var perms = new Dictionary<string, DateTime[]>();
                for (int i = 0; i < groupData.PermissionsList.Count; i++)
                {
                    tempPermission = groupData.PermissionsList[i];
                    if (tempPermission != null && !tempPermission.AlreadyRemoved)
                        perms[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
                }
                if (perms.Any())
                    result[groupData.GroupName] = perms;
            }
            return result;
        }
        #endregion

        #region ~Oxide Hooks~
        void OnUserPermissionRevoked(string userID, string permName)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null)
                    tempPermission.MarkAsRemoved(Log_UserPerms, $"The permission '{tempPermission.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation as a result of calling hook 'OnUserPermissionRevoked'.");
            }
        }
        
        void OnUserGroupRemoved(string userID, string groupName)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                var tempGroup = playerData.GroupsList.FirstOrDefault(p => p.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                if (tempGroup != null)
                    tempGroup.MarkAsRemoved(Log_UserGroups, $"The group '{tempGroup.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation as a result of calling hook 'OnUserGroupRemoved'.");
            }
        }
        
        void OnGroupPermissionRevoked(string groupName, string permName)
        {
            if (_storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                if (tempPermission != null)
                    tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation as a result of calling hook 'OnGroupPermissionRevoked'.");
            }
        }
        
        void OnPermissionRegistered(string permName)
        {
            if (!PermissionExists_Helper(permName, out var existingPerm))
                _existingPerms.Add(permName);
            else if (!string.Equals(existingPerm, permName, StringComparison.Ordinal))
            {
                _existingPerms.Remove(existingPerm);
                _existingPerms.Add(permName);
            }
        }

        void OnGroupCreated(string groupName)
        {
            if (!GroupExists_Helper(groupName, out var existingGroup))
                _existingGroups.Add(groupName);
            else if (!string.Equals(existingGroup, groupName, StringComparison.Ordinal))
            {
                _existingGroups.Remove(existingGroup);
                _existingGroups.Add(groupName);
            }
        }
        
        void OnGroupDeleted(string groupName)
        {
            TemporaryPermission tempPermission, tempGroup;
            foreach (var playerData in _storedData.PlayersList.Values)
            {
                for (int i = 0; i < playerData.GroupsList.Count; i++)
                {
                    tempGroup = playerData.GroupsList[i];
                    if (tempGroup.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                    {
                        tempGroup.MarkAsRemoved(Log_UserGroups, $"The group '{tempGroup.Name}' of player '{playerData.DisplayName}' ({playerData.UserID}) has been marked for revocation as a result of the group being deleted.");
                        break;
                    }
                }
            }
            
            if (_storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                for (int i = 0; i < groupData.PermissionsList.Count; i++)
                {
                    tempPermission = groupData.PermissionsList[i];
                    tempPermission.MarkAsRemoved(Log_UserGroups, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation as a result of the group being deleted.");
                }
            }
            _existingGroups.Remove(groupName);
        }
        
        void OnUserNameUpdated(string userID, string oldName, string newName)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
                playerData.DisplayName = newName;
        }
        
        void Init()
        {
            Unsubscribe(nameof(OnServerCommand));
            Unsubscribe(nameof(OnUserPermissionRevoked));
            Unsubscribe(nameof(OnUserGroupRemoved));
            Unsubscribe(nameof(OnGroupPermissionRevoked));
            Unsubscribe(nameof(OnPermissionRegistered));
            Unsubscribe(nameof(OnGroupCreated));
            Unsubscribe(nameof(OnGroupDeleted));
            Unsubscribe(nameof(OnUserNameUpdated));
            Unsubscribe(nameof(OnServerSave));
            Instance = this;
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            AddCovalenceCommand(_config.AdminCommand, nameof(Command_Admin));
            AddCovalenceCommand(_config.Command, nameof(Command_TemporaryPermissions));
            try { _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            if (_storedData == null)
                _storedData = new StoredData();
        }
        
        void OnServerInitialized(bool initial)
        {
            if (initial)
            {
                Interface.Oxide.ReloadPlugin(Name);
                return;
            }
            ServerMgr.Instance.StartCoroutine(InitPlugin());
        }
        
        void OnServerSave() => SaveData();
        #endregion
        
        #region ~Commands~
        private void Command_TemporaryPermissions(IPlayer player, string command, string[] args)
        {
            bool hasTarget = false;
            var targetPlayer = player;
            if (args != null && args.Length > 0 && (player.IsAdmin || permission.UserHasPermission(player.Id, PERMISSION_ADMIN)))
            {
                if (!TryGetPlayer(player, args[0], out targetPlayer))
                    return;
                hasTarget = true;
            }
            
            int totalPerms = 0, totalGroups = 0;
            HashSet<string> perms = new HashSet<string>(), groups = new HashSet<string>();
            if (_storedData.PlayersList.TryGetValue(targetPlayer.Id, out var playerData))
            {
                TemporaryPermission tempPermission;
                for (int i = 0; i < playerData.PermissionsList.Count; i++)
                {
                    tempPermission = playerData.PermissionsList[i];
                    if (tempPermission != null && !tempPermission.AlreadyRemoved)
                        perms.Add(string.Format(lang.GetMessage("CmdCheckFormatPermissions", this, player.Id), tempPermission.Name, tempPermission.UntilWipe ? lang.GetMessage("CmdUntilWipe", this, player.Id) : tempPermission.ExpireDate, tempPermission.AssignedDate));
                }
                totalPerms = perms.Count;
                if (totalPerms > 0)
                    player.Reply(string.Format(lang.GetMessage(hasTarget ? "CmdCheckTargetPermissions" : "CmdCheckPermissions", this, player.Id), totalPerms, string.Join(",\n", perms), targetPlayer.Name));
                
                TemporaryPermission tempGroup;
                for (int i = 0; i < playerData.GroupsList.Count; i++)
                {
                    tempGroup = playerData.GroupsList[i];
                    if (tempGroup != null && !tempGroup.AlreadyRemoved)
                        groups.Add(string.Format(lang.GetMessage("CmdCheckFormatGroups", this, player.Id), tempGroup.Name, tempGroup.UntilWipe ? lang.GetMessage("CmdUntilWipe", this, player.Id) : tempGroup.ExpireDate, tempGroup.AssignedDate));
                }
                totalGroups = groups.Count;
                if (totalGroups > 0)
                    player.Reply(string.Format(lang.GetMessage(hasTarget ? "CmdCheckTargetGroups" : "CmdCheckGroups", this, player.Id), totalGroups, string.Join(",\n", groups), targetPlayer.Name));
            }
            
            if (totalPerms < 1 && totalGroups < 1)
                SendMessage(player, string.Format(lang.GetMessage(hasTarget ? "CmdCheckTargetNoActive" : "CmdCheckNoActive", this, player.Id), targetPlayer.Name));
        }
        #endregion
        
        #region ~Commands - Admin~
        private readonly string[] _cmdKeysAdmin = { "grant", "revoke", "add", "remove", "wipe" }, _cmdKeysAdminSub = { "user", "group" };
        private void Command_Admin(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.Id, PERMISSION_ADMIN)) return;
            int index = args != null && args.Length > 0 ? Array.FindIndex(_cmdKeysAdmin, key => key.Equals(args[0], StringComparison.OrdinalIgnoreCase)) : -1;
            if (index < 0)
            {
                player.Reply(lang.GetMessage("CmdAdmin", this, player.Id));
                return;
            }
            
            string groupName = string.Empty;
            IPlayer targetPlayer = null;
            args = args.Length >= 6 ? args : args.Concat(Enumerable.Repeat(string.Empty, 6 - args.Length)).ToArray();
            if (index == 0 || index == 1)
            {
                //grant, revoke
                int subIndex = Array.FindIndex(_cmdKeysAdminSub, key => key.Equals(args[1], StringComparison.OrdinalIgnoreCase));
                if (subIndex < 0 || string.IsNullOrWhiteSpace(args[2]) || string.IsNullOrWhiteSpace(args[3]))
                {
                    player.Reply(lang.GetMessage("CmdAdmin", this, player.Id));
                    return;
                }

                if (!PermissionExists_Helper(args[3], out var permName))
                    SendMessage(player, string.Format(lang.GetMessage("CmdPermissionNotFound", this, player.Id), args[3]));
                else if (subIndex == 0 && !TryGetPlayer(player, args[2], out targetPlayer, false))
                    return;
                else if (subIndex == 1 && !GroupExists_Helper(args[2], out groupName))
                    SendMessage(player, string.Format(lang.GetMessage("CmdGroupNotFound", this, player.Id), args[2]));
                else if (index == 0)
                {
                    //grant
                    if (subIndex == 0)
                    {
                        //        0     1     2        3                4                     5 - optional
                        //tperm grant user iiiaka realpve.vip wipe/seconds/DATETIME (true/false)/DATETIME
                        var targetData = GetOrCreatePlayerData(targetPlayer.Id, targetPlayer.Name);
                        if (args[4].Equals(Str_Wipe, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'GrantTemporaryPermission(wipe)' to grant permission '{permName}' to player '{targetData.DisplayName}' ({targetData.UserID}) as a result of using the internal grant command by player '{player.Name}'.");
                            GrantTemporaryPermission(targetData, permName);
                        }
                        else if (int.TryParse(args[4], out var secondsToAdd))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'GrantTemporaryPermission(seconds)' to grant permission '{permName}' to player '{targetData.DisplayName}' ({targetData.UserID}) as a result of using the internal grant command by player '{player.Name}'.");
                            GrantTemporaryPermission(targetData, permName, secondsToAdd, bool.TryParse(args[5], out var fromNow) ? fromNow : false);
                        }
                        else if (DateTime.TryParse(args[4], out var expireDate))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_UserPerms, $"Method call 'GrantTemporaryPermission(DateTime)' to grant permission '{permName}' to player '{targetData.DisplayName}' ({targetData.UserID}) as a result of using the internal grant command by player '{player.Name}'.");
                            GrantTemporaryPermission(targetData, permName, expireDate, DateTime.TryParse(args[5], out var assignedDate) ? assignedDate : default);
                        }
                        else
                        {
                            SendMessage(player, lang.GetMessage("CmdGrantWrongFormat", this, player.Id));
                            return;
                        }
                        SendMessage(player, string.Format(lang.GetMessage("CmdUserGranted", this, player.Id), permName, targetPlayer.Name), false);
                    }
                    else if (subIndex == 1)
                    {
                        //        0     1    2       3                4                     5 - optional
                        //tperm grant group vip realpve.vip wipe/seconds/DATETIME (true/false)/DATETIME
                        var groupData = GetOrCreateGroupData(groupName);
                        if (args[4].Equals(Str_Wipe, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_GroupPerms, $"Method call 'GrantTemporaryPermission(wipe)' to grant permission '{permName}' to group '{groupData.GroupName}' as a result of using the internal grant command by player '{player.Name}'.");
                            GrantTemporaryPermission(groupData, permName);
                        }
                        else if (int.TryParse(args[4], out var secondsToAdd))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_GroupPerms, $"Method call 'GrantTemporaryPermission(seconds)' to grant permission '{permName}' to group '{groupData.GroupName}' as a result of using the internal grant command by player '{player.Name}'.");
                            GrantTemporaryPermission(groupData, permName, secondsToAdd, bool.TryParse(args[5], out var fromNow) ? fromNow : false);
                        }
                        else if (DateTime.TryParse(args[4], out var expireDate))
                        {
                            if (_config.FileLogs || _config.ConsoleLogs)
                                SendLog(Log_GroupPerms, $"Method call 'GrantTemporaryPermission(DateTime)' to grant permission '{permName}' to group '{groupData.GroupName}' as a result of using the internal grant command by player '{player.Name}'.");
                            GrantTemporaryPermission(groupData, permName, expireDate, DateTime.TryParse(args[5], out var assignedDate) ? assignedDate : default);
                        }
                        else
                        {
                            SendMessage(player, lang.GetMessage("CmdGrantWrongFormat", this, player.Id));
                            return;
                        }
                        SendMessage(player, string.Format(lang.GetMessage("CmdGroupGranted", this, player.Id), permName, groupData.GroupName), false);
                    }
                    else
                        SendMessage(player, lang.GetMessage("CmdGrantWrongFormat", this, player.Id));
                }
                else if (index == 1)
                {
                    //revoke
                    if (subIndex == 0)
                    {
                        //         0     1     2        3
                        //tperm revoke user iiiaka realpve.vip
                        permission.RevokeUserPermission(targetPlayer.Id, permName);
                        if (_storedData.PlayersList.TryGetValue(targetPlayer.Id, out var targetData))
                        {
                            var tempPermission = targetData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                            if (tempPermission != null)
                                tempPermission.MarkAsRemoved(Log_UserPerms, $"The permission '{tempPermission.Name}' of player '{targetData.DisplayName}' ({targetData.UserID}) has been marked for revocation as a result of using the internal revoke command by player '{player.Name}'.");
                        }
                        SendMessage(player, string.Format(lang.GetMessage("CmdUserRevoked", this, player.Id), permName, targetPlayer.Name), false);
                    }
                    else if (subIndex == 1)
                    {
                        //         0     1    2       3
                        //tperm revoke group vip realpve.vip
                        permission.RevokeGroupPermission(groupName, permName);
                        if (_storedData.GroupsList.TryGetValue(groupName, out var groupData))
                        {
                            var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name.Equals(permName, StringComparison.OrdinalIgnoreCase));
                            if (tempPermission != null)
                                tempPermission.MarkAsRemoved(Log_GroupPerms, $"The permission '{tempPermission.Name}' of group '{groupData.GroupName}' has been marked for revocation as a result of using the internal revoke command by player '{player.Name}'.");
                        }
                        SendMessage(player, string.Format(lang.GetMessage("CmdGroupRevoked", this, player.Id), permName, groupName), false);
                    }
                    else
                        SendMessage(player, lang.GetMessage("CmdRevokeWrongFormat", this, player.Id));
                }
            }
            else if (index == 2 || index == 3)
            {
                //add, remove
                if (string.IsNullOrWhiteSpace(args[1]) || string.IsNullOrWhiteSpace(args[2]))
                {
                    player.Reply(lang.GetMessage("CmdAdmin", this, player.Id));
                    return;
                }

                if (!GroupExists_Helper(args[2], out groupName))
                    SendMessage(player, string.Format(lang.GetMessage("CmdGroupNotFound", this, player.Id), args[2]));
                else if (!TryGetPlayer(player, args[1], out targetPlayer, false))
                    return;
                else if (index == 2)
                {
                    //       0     1    2            3                     4 - optional
                    //tperm add iiiaka vip wipe/seconds/DATETIME (true/false)/DATETIME
                    var targetData = GetOrCreatePlayerData(targetPlayer.Id, targetPlayer.Name);
                    if (args[3].Equals(Str_Wipe, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_config.FileLogs || _config.ConsoleLogs)
                            SendLog(Log_UserGroups, $"Method call 'AddTemporaryGroup(wipe)' to grant group '{groupName}' to player '{targetData.DisplayName}' ({targetData.UserID}) as a result of using the internal add command by player '{player.Name}'.");
                        AddTemporaryGroup(targetData, groupName);
                    }
                    else if (int.TryParse(args[3], out var secondsToAdd))
                    {
                        if (_config.FileLogs || _config.ConsoleLogs)
                            SendLog(Log_UserGroups, $"Method call 'AddTemporaryGroup(seconds)' to grant group '{groupName}' to player '{targetData.DisplayName}' ({targetData.UserID}) as a result of using the internal add command by player '{player.Name}'.");
                        AddTemporaryGroup(targetData, groupName, secondsToAdd, bool.TryParse(args[4], out var fromNow) ? fromNow : false);
                    }
                    else if (DateTime.TryParse(args[3], out var expireDate))
                    {
                        if (_config.FileLogs || _config.ConsoleLogs)
                            SendLog(Log_UserGroups, $"Method call 'AddTemporaryGroup(DateTime)' to grant group '{groupName}' to player '{targetData.DisplayName}' ({targetData.UserID}) as a result of using the internal add command by player '{player.Name}'.");
                        AddTemporaryGroup(targetData, groupName, expireDate, DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default);
                    }
                    else
                    {
                        SendMessage(player, lang.GetMessage("CmdUserGroupWrongFormat", this, player.Id));
                        return;
                    }
                    SendMessage(player, string.Format(lang.GetMessage("CmdUserGroupAdded", this, player.Id), targetPlayer.Name, groupName), false);
                }
                else if (index == 3)
                {
                    //        0       1    2
                    //tperm remove iiiaka vip
                    permission.RemoveUserGroup(targetPlayer.Id, groupName);
                    if (_storedData.PlayersList.TryGetValue(targetPlayer.Id, out var targetData))
                    {
                        var tempGroup = targetData.GroupsList.FirstOrDefault(p => p.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
                        if (tempGroup != null)
                            tempGroup.MarkAsRemoved(Log_UserGroups, $"The group '{tempGroup.Name}' of player '{targetData.DisplayName}' ({targetData.UserID}) has been marked for revocation as a result of using the internal remove command by player '{player.Name}'.");
                    }
                    SendMessage(player, string.Format(lang.GetMessage("CmdUserGroupRemoved", this, player.Id), targetPlayer.Name, groupName), false);
                }
            }
            else if (index == 4)
            {
                //        0        1
                //tperm wipe reset/DATETIME
                var utcNow = DateTime.UtcNow;
                if (args[1].Equals("reset"))
                {
                    _config.CustomWipe = string.Empty;
                    SaveConfig();
                    SendMessage(player, lang.GetMessage("CmdWipeReset", this, player.Id), false);
                }
                else if (DateTime.TryParse(args[1], out var wipeDate) && wipeDate > utcNow)
                {
                    _config.CustomWipe = wipeDate.ToString(TimeFormat);
                    SaveConfig();
                    SendMessage(player, string.Format(lang.GetMessage("CmdWipeNew", this, player.Id), _config.CustomWipe), false);
                }
                else
                    SendMessage(player, string.Format(lang.GetMessage("CmdWipeFailed", this, player.Id), args[1], utcNow.AddDays(1).ToString(TimeFormat)));
            }
        }
        #endregion

        #region ~Unload~
        void Unload()
        {
            Unsubscribe(nameof(OnServerSave));
            if (_expirationTimer != null)
                _expirationTimer.Destroy();
            if (_presenceTimer != null)
                _presenceTimer.Destroy();
            if (_config.RevokeOnUnload)
            {
                foreach (var playerData in _storedData.PlayersList.Values)
                {
                    for (int i = 0; i < playerData.PermissionsList.Count; i++)
                        permission.RevokeUserPermission(playerData.UserID, playerData.PermissionsList[i].Name);
                    for (int i = 0; i < playerData.GroupsList.Count; i++)
                        permission.RemoveUserGroup(playerData.UserID, playerData.GroupsList[i].Name);
                }
                foreach (var groupData in _storedData.GroupsList.Values)
                {
                    for (int i = 0; i < groupData.PermissionsList.Count; i++)
                        permission.RevokeGroupPermission(groupData.GroupName, groupData.PermissionsList[i].Name);
                }
            }
            OnServerSave();
            Instance = null;
            _storedData = null;
            _config = null;
        }
        #endregion
        
        #region ~Classes~
        public class PlayerData
        {
            [JsonIgnore] public string UserID { get; private set; } = string.Empty;
            
            [JsonProperty(PropertyName = "User Display Name(last)")]
            public string DisplayName { get; set; } = string.Empty;
            
            [JsonProperty(PropertyName = "Temporary Permissions")]
            public List<TemporaryPermission> PermissionsList { get; private set; } = new List<TemporaryPermission>();
            
            [JsonProperty(PropertyName = "Temporary Groups")]
            public List<TemporaryPermission> GroupsList { get; private set; } = new List<TemporaryPermission>();
            
            public PlayerData() {}
            public PlayerData(string userId, string displayName = "")
            {
                UserID = userId;
                DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : userId;
            }
            
            public void UpdateUserID(string userId) => UserID = userId;
        }
        
        public class GroupData
        {
            [JsonIgnore] public string GroupName { get; private set; } = string.Empty;
            
            [JsonProperty(PropertyName = "Temporary Permissions")]
            public List<TemporaryPermission> PermissionsList { get; private set; } = new List<TemporaryPermission>();
            
            public GroupData() {}
            public GroupData(string name)
            {
                GroupName = name;
            }
            
            public void UpdateGroupName(string groupName) => GroupName = groupName;
        }

        public class TemporaryPermission
        {
            [JsonProperty] public string Name { get; private set; } = string.Empty;
            public DateTime AssignedDate { get; set; }
            public DateTime ExpireDate { get; set; }
            public bool UntilWipe { get; set; }
            [JsonIgnore] public bool AlreadyRemoved { get; set; } = false;
            
            public TemporaryPermission() {}
            public TemporaryPermission(string name)
            {
                Name = name;
                AssignedDate = DateTime.UtcNow;
                UntilWipe = true;
            }
            public TemporaryPermission(string name, DateTime expireDate, DateTime assignedDate = default)
            {
                Name = name;
                AssignedDate = assignedDate == default ? DateTime.UtcNow : assignedDate;
                ExpireDate = expireDate;
            }
            public TemporaryPermission(string name, int secondsToAdd, DateTime assignedDate = default)
            {
                var utcNow = DateTime.UtcNow;
                Name = name;
                AssignedDate = assignedDate == default ? utcNow : assignedDate;
                ExpireDate = utcNow.AddSeconds(secondsToAdd);
            }
            
            public void UpdateName(string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                    Name = newName;
            }
            
            public void MarkAsRemoved(string filename, string logMsg)
            {
                if (!AlreadyRemoved)
                {
                    AlreadyRemoved = true;
                    Instance.SendLog(filename, logMsg);
                }
            }
            
            public override int GetHashCode() => Name.GetHashCode();
        }
        #endregion
    }
}