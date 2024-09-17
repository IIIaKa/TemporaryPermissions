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
*      https://www.patreon.com/iiiaka
*      https://boosty.to/iiiaka
*  GitHub repository page: https://github.com/IIIaKa/TemporaryPermissions
*  Codefling plugin page: https://codefling.com/plugins/temporary-permissions
*  Codefling license: https://codefling.com/plugins/temporary-permissions?tab=downloads_field_4
*
*  Copyright © 2024 IIIaKa
*/

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Temporary Permissions", "IIIaKa", "0.1.0")]
    [Description("Useful plugin for managing temporary permissions, temporary groups and temporary permissions for groups. This is done through chat commands, built-in Oxide commands, and API methods.")]
    class TemporaryPermissions : RustPlugin
    {
        #region ~Variables~
        private bool _isReady = false;
        private const string PERMISSION_ADMIN = "temporarypermissions.admin", TimeFormat = "yyyy-MM-dd HH:mm:ss", CommandAdd = "add", CommandRemove = "remove", CommandUser = "user", CommandGroup = "group",
            Str_UserPermissionGranted = "OnTemporaryPermissionGranted", Str_UserPermissionUpdated = "OnTemporaryPermissionUpdated", Str_UserPermissionRevoked = "OnTemporaryPermissionRevoked",
            Str_UserGroupAdded = "OnTemporaryGroupAdded", Str_UserGroupUpdated = "OnTemporaryGroupUpdated", Str_UserGroupRemoved = "OnTemporaryGroupRemoved",
            Str_GroupPermissionGranted = "OnGroupTemporaryPermissionGranted", Str_GroupPermissionUpdated = "OnGroupTemporaryPermissionUpdated", Str_GroupPermissionRevoked = "OnGroupTemporaryPermissionRevoked";
        private readonly string[] CommandsGrant = new string[3] { "oxide.grant", "o.grant", "umod.grant" };
        private readonly string[] CommandsRevoke = new string[3] { "oxide.revoke", "o.revoke", "umod.revoke" };
        private readonly string[] CommandsUserGroup = new string[3] { "oxide.usergroup", "o.usergroup", "umod.usergroup" };
        private Timer _updatesTimer;
        #endregion

        #region ~Configuration~
        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Chat command")]
            public string Command = "tperm";
            
            [JsonProperty(PropertyName = "Expiration Check Interval")]
            public float CheckInterval = 1f;
            
            [JsonProperty(PropertyName = "Is it worth removing temporary permissions and groups when unloading the plugin or when a player disconnects?")]
            public bool RemoveOnUnload = true;
            
            [JsonProperty(PropertyName = "Is it worth using console logging?")]
            public bool ConsoleLog = true;
            
            [JsonProperty(PropertyName = "Is it worth clearing all temporary permissions upon detecting a wipe?")]
            public bool ClearOnWipe = false;
            
            [JsonProperty(PropertyName = "Wipe ID")]
            public string WipeID = "";
            
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
                LoadDefaultConfig();
                PrintWarning($"The configuration file has been successfully updated to version {_config.Version}!");
            }
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
            public Dictionary<string, PlayerData> PlayersList = new Dictionary<string, PlayerData>();
            
            [JsonProperty(PropertyName = "Groups list")]
            public Dictionary<string, GroupData> GroupsList = new Dictionary<string, GroupData>();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _storedData);
        #endregion
        
        #region ~Language~
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MsgPermissionNotFound"] = "Permission not found!",
                ["MsgPlayerNotFound"] = "Player not found!",
                ["MsgGroupNotFound"] = "Group not found!",
                ["MsgGrantWrongFormat"] = "Invalid command format! Example: /tperm grant user/group *NameOrId* realpve.vip *secondsOrDateTime*",
                ["MsgRevokeWrongFormat"] = "Invalid command format! Example: /tperm revoke user/group *NameOrId* realpve.vip",
                ["MsgUserGroupWrongFormat"] = "Invalid command format! Example: /tperm group add/remove *NameOrId* *groupName*",
                ["MsgUserGranted"] = "Permission {0} granted to player {1}",
                ["MsgGroupGranted"] = "Permission {0} granted to group {1}",
                ["MsgUserGroupAdded"] = "Player {0} has been added to group {1}",
                ["MsgUserRevoked"] = "Permission {0} has been removed for player {1}",
                ["MsgGroupRevoked"] = "Permission {0} has been removed for group {1}",
                ["MsgUserGroupRemoved"] = "Player {0} has been removed from group {1}"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MsgPermissionNotFound"] = "Пермишен не найден!",
                ["MsgPlayerNotFound"] = "Игрок не найден!",
                ["MsgGroupNotFound"] = "Группа не найдена!",
                ["MsgGrantWrongFormat"] = "Не верный формат команды! Пример: /tperm grant user/group *NameOrId* realpve.vip *secondsOrDateTime*",
                ["MsgRevokeWrongFormat"] = "Не верный формат команды! Пример: /tperm revoke user/group *NameOrId* realpve.vip",
                ["MsgUserGroupWrongFormat"] = "Не верный формат команды! Пример: /tperm group add/remove *NameOrId* *groupName*",
                ["MsgUserGranted"] = "Пермишен {0} выдан игроку {1}",
                ["MsgGroupGranted"] = "Пермишен {0} выдан группе {1}",
                ["MsgUserGroupAdded"] = "Игрок {0} был добавлен в группу {1}",
                ["MsgUserRevoked"] = "Пермишен {0} был удален для игрока {1}",
                ["MsgGroupRevoked"] = "Пермишен {0} был удален для группы {1}",
                ["MsgUserGroupRemoved"] = "Игрок {0} был удален из группы {1}"
            }, this, "ru");
        }
        #endregion

        #region ~Methods~
        private System.Collections.IEnumerator InitPlugin()
        {
            PlayerData playerData;
            if (string.IsNullOrWhiteSpace(_config.WipeID) || _config.WipeID != SaveRestore.WipeId)
            {
                _config.WipeID = SaveRestore.WipeId;
                SaveConfig();
                if (_config.ClearOnWipe)
                {
                    var playersList = _storedData.PlayersList;
                    if (playersList.Any())
                    {
                        foreach (var kvp in playersList)
                        {
                            playerData = kvp.Value;
                            foreach (var tempPermission in playerData.PermissionsList)
                                tempPermission.AlreadyRemoved = true;
                            foreach (var tempGroup in playerData.GroupsList)
                                tempGroup.AlreadyRemoved = true;
                        }
                    }

                    var groupsList = _storedData.GroupsList;
                    if (groupsList.Any())
                    {
                        foreach (var kvp in groupsList)
                        {
                            var groupData = kvp.Value;
                            if (groupData.PermissionsList.Any())
                            {
                                foreach (var tempPermission in groupData.PermissionsList)
                                    tempPermission.AlreadyRemoved = true;
                            }
                        }
                    }

                    PrintWarning("Wipe detected! Resetting all temporary permissions and groups.");
                }
            }
            CheckForUpdates();
            yield return new WaitForSeconds(1);
            var utcNow = DateTime.UtcNow;
            if (_config.RemoveOnUnload)
            {
                foreach (var player in BasePlayer.activePlayerList)
                    GrantOnConnect(player.UserIDString, utcNow);
            }
            else
            {
                foreach (var kvp in _storedData.PlayersList)
                    GrantOnConnect(kvp.Key, utcNow);
            }
            foreach (var groupName in permission.GetGroups())
            {
                if (!_storedData.GroupsList.TryGetValue(groupName, out var groupData)) continue;
                foreach (var tempPerm in groupData.PermissionsList)
                {
                    if (!tempPerm.AlreadyRemoved && tempPerm.ExpireDate > utcNow)
                        permission.GrantGroupPermission(groupName, tempPerm.Name, null);
                }
            }
            _updatesTimer = timer.Every(_config.CheckInterval, CheckForUpdates);
            if (_config.RemoveOnUnload)
            {
                Subscribe(nameof(CanBypassQueue));
                Subscribe(nameof(OnPlayerDisconnected));
            }
            Subscribe(nameof(OnServerCommand));
            _isReady = true;
            yield return new WaitForSeconds(1);
            Interface.CallHook("OnTemporaryPermissionsLoaded");
        }
        
        private void GrantOnConnect(string userID, DateTime utcNow)
        {
            if (!_storedData.PlayersList.TryGetValue(userID, out var playerData)) return;
            foreach (var tempPerm in playerData.PermissionsList)
            {
                if (!tempPerm.AlreadyRemoved && tempPerm.ExpireDate > utcNow)
                    permission.GrantUserPermission(userID, tempPerm.Name, null);
            }
            foreach (var tempGroup in playerData.GroupsList)
            {
                if (!tempGroup.AlreadyRemoved && tempGroup.ExpireDate > utcNow)
                    permission.AddUserGroup(userID, tempGroup.Name);
            }
        }
        
        private void CheckForUpdates()
        {
            var utcNow = DateTime.UtcNow;
            bool isExpired = false;
            
            var playersList = _storedData.PlayersList;
            if (playersList.Any())
            {
                var usersToRemove = Pool.Get<List<string>>();
                foreach (var kvp in playersList)
                {
                    var userID = kvp.Key;
                    var playerData = kvp.Value;
                    if (!playerData.PermissionsList.Any() && !playerData.GroupsList.Any())
                    {
                        usersToRemove.Add(userID);
                        continue;
                    }
                    foreach (var tempPermission in playerData.PermissionsList)
                    {
                        isExpired = utcNow > tempPermission.ExpireDate;
                        if (tempPermission.AlreadyRemoved || isExpired)
                        {
                            tempPermission.AlreadyRemoved = true;
                            permission.RevokeUserPermission(userID, tempPermission.Name);
                            if (_config.ConsoleLog)
                                Puts($"{playerData.DisplayName} ({userID}) - Permission removed: {tempPermission.Name} | Is expired: {isExpired}");
                            Interface.CallHook(Str_UserPermissionRevoked, userID, tempPermission.Name, isExpired);
                        }
                    }
                    playerData.PermissionsList.RemoveAll(x => x.AlreadyRemoved);
                    
                    foreach (var tempGroup in playerData.GroupsList)
                    {
                        isExpired = utcNow > tempGroup.ExpireDate;
                        if (tempGroup.AlreadyRemoved || isExpired)
                        {
                            tempGroup.AlreadyRemoved = true;
                            permission.RemoveUserGroup(userID, tempGroup.Name);
                            if (_config.ConsoleLog)
                                Puts($"{playerData.DisplayName} ({userID}) - Removed from group: {tempGroup.Name} | Is expired: {isExpired}");
                            Interface.CallHook(Str_UserGroupRemoved, userID, tempGroup.Name, isExpired);
                        }
                    }
                    playerData.GroupsList.RemoveAll(x => x.AlreadyRemoved);
                }
                foreach (string userID in usersToRemove)
                    playersList.Remove(userID);
                Pool.FreeUnmanaged(ref usersToRemove);
            }
            
            var groupsList = _storedData.GroupsList;
            if (groupsList.Any())
            {
                var groupsToRemove = Pool.Get<List<string>>();
                foreach (var kvp in groupsList)
                {
                    var groupID = kvp.Key;
                    var groupData = kvp.Value;
                    if (groupData.PermissionsList.Any())
                    {
                        foreach (var tempPermission in groupData.PermissionsList)
                        {
                            isExpired = utcNow > tempPermission.ExpireDate;
                            if (tempPermission.AlreadyRemoved || isExpired)
                            {
                                tempPermission.AlreadyRemoved = true;
                                permission.RevokeGroupPermission(groupID, tempPermission.Name);
                                if (_config.ConsoleLog)
                                    Puts($"{groupData.GroupName} (group) - Permission removed: {tempPermission.Name} | Is expired: {isExpired}");
                                Interface.CallHook(Str_GroupPermissionRevoked, groupID, tempPermission.Name, isExpired);
                            }
                        }
                        groupData.PermissionsList.RemoveAll(x => x.AlreadyRemoved);
                    }
                    else
                        groupsToRemove.Add(groupID);
                }
                foreach (string userID in groupsToRemove)
                    groupsList.Remove(userID);
                Pool.FreeUnmanaged(ref groupsToRemove);
            }
        }
        
        private void GrantTemporaryPermission(PlayerData playerData, string perm, int secondsToAdd, bool fromNow = false)
        {
            string hookName = Str_UserPermissionGranted;
            var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name == perm);
            if (tempPermission == null)
            {
                tempPermission = new TemporaryPermission(perm, secondsToAdd);
                playerData.PermissionsList.Add(tempPermission);
                if (_config.ConsoleLog)
                    Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission granted: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}");
            }
            else
            {
                tempPermission.ExpireDate = fromNow ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempPermission.ExpireDate.AddSeconds(secondsToAdd);
                tempPermission.AlreadyRemoved = false;
                hookName = Str_UserPermissionUpdated;
                if (_config.ConsoleLog)
                    Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission extended: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}");
            }
            Interface.CallHook(hookName, playerData.UserID, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void GrantTemporaryPermission(PlayerData playerData, string perm, DateTime expireDate, DateTime assignedDate = default)
        {
            string hookName = Str_UserPermissionGranted;
            var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name == perm);
            if (tempPermission == null)
            {
                tempPermission = new TemporaryPermission(perm, expireDate, assignedDate);
                playerData.PermissionsList.Add(tempPermission);
                if (_config.ConsoleLog)
                    Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission granted: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}");
            }
            else
            {
                if (assignedDate != default)
                    tempPermission.AssignedDate = assignedDate;
                tempPermission.ExpireDate = expireDate;
                tempPermission.AlreadyRemoved = false;
                hookName = Str_UserPermissionUpdated;
                if (_config.ConsoleLog)
                    Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission extended: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}");
            }
            Interface.CallHook(hookName, playerData.UserID, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void AddTemporaryGroup(PlayerData playerData, string groupName, int secondsToAdd, bool fromNow = false)
        {
            string hookName = Str_UserGroupAdded;
            var tempGroup = playerData.GroupsList.FirstOrDefault(p => p.Name == groupName);
            if (tempGroup == null)
            {
                tempGroup = new TemporaryPermission(groupName, secondsToAdd);
                playerData.GroupsList.Add(tempGroup);
                if (_config.ConsoleLog)
                    Puts($"{playerData.DisplayName} ({playerData.UserID}) - Added to group: {tempGroup.Name} until {tempGroup.ExpireDate.ToString(TimeFormat)}");
            }
            else
            {
                tempGroup.ExpireDate = fromNow ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempGroup.ExpireDate.AddSeconds(secondsToAdd);
                tempGroup.AlreadyRemoved = false;
                hookName = Str_UserGroupUpdated;
                if (_config.ConsoleLog)
                    Puts($"{playerData.DisplayName} ({playerData.UserID}) - Extended time for participating in group: {tempGroup.Name} until {tempGroup.ExpireDate.ToString(TimeFormat)}");
            }
            Interface.CallHook(hookName, playerData.UserID, groupName, tempGroup.ExpireDate, tempGroup.AssignedDate);
        }
        
        private void AddTemporaryGroup(PlayerData playerData, string groupName, DateTime expireDate, DateTime assignedDate = default)
        {
            string hookName = Str_UserGroupAdded;
            var tempGroup = playerData.GroupsList.FirstOrDefault(p => p.Name == groupName);
            if (tempGroup == null)
            {
                tempGroup = new TemporaryPermission(groupName, expireDate, assignedDate);
                playerData.GroupsList.Add(tempGroup);
                if (_config.ConsoleLog)
                    Puts($"{playerData.DisplayName} ({playerData.UserID}) - Added to group: {tempGroup.Name} until {tempGroup.ExpireDate.ToString(TimeFormat)}");
            }
            else
            {
                if (assignedDate != default)
                    tempGroup.AssignedDate = assignedDate;
                tempGroup.ExpireDate = expireDate;
                tempGroup.AlreadyRemoved = false;
                hookName = Str_UserGroupUpdated;
                if (_config.ConsoleLog)
                    Puts($"{playerData.DisplayName} ({playerData.UserID}) - Extended time for participating in group: {tempGroup.Name} until {tempGroup.ExpireDate.ToString(TimeFormat)}");
            }
            Interface.CallHook(hookName, playerData.UserID, groupName, tempGroup.ExpireDate, tempGroup.AssignedDate);
        }
        
        private void GrantTemporaryPermission(GroupData groupData, string perm, int secondsToAdd, bool fromNow = false)
        {
            string hookName = Str_GroupPermissionGranted;
            var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name == perm);
            if (tempPermission == null)
            {
                tempPermission = new TemporaryPermission(perm, secondsToAdd);
                groupData.PermissionsList.Add(tempPermission);
                if (_config.ConsoleLog)
                    Puts($"{groupData.GroupName} (group) - Permission granted: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}");
            }
            else
            {
                tempPermission.ExpireDate = fromNow ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempPermission.ExpireDate.AddSeconds(secondsToAdd);
                tempPermission.AlreadyRemoved = false;
                hookName = Str_GroupPermissionUpdated;
                if (_config.ConsoleLog)
                    Puts($"{groupData.GroupName} (group) - Permission extended: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}");
            }
            Interface.CallHook(hookName, groupData.GroupName, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void GrantTemporaryPermission(GroupData groupData, string perm, DateTime expireDate, DateTime assignedDate = default)
        {
            string hookName = Str_GroupPermissionGranted;
            var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name == perm);
            if (tempPermission == null)
            {
                tempPermission = new TemporaryPermission(perm, expireDate, assignedDate);
                groupData.PermissionsList.Add(tempPermission);
                if (_config.ConsoleLog)
                    Puts($"{groupData.GroupName} (group) - Permission granted: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}");
            }
            else
            {
                if (assignedDate != default)
                    tempPermission.AssignedDate = assignedDate;
                tempPermission.ExpireDate = expireDate;
                tempPermission.AlreadyRemoved = false;
                hookName = Str_GroupPermissionUpdated;
                if (_config.ConsoleLog)
                    Puts($"{groupData.GroupName} (group) - Permission extended: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}");
            }
            Interface.CallHook(hookName, groupData.GroupName, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
        }
        
        private void CommandGrant(string[] args)
        {
            //            0            1           2            3          4 - optional
            //o.grant user/group iiiaka/admin realpve.vip 123/DATETIME true/false
            if (args.Length < 4 || !permission.PermissionExists(args[2])) return;
            string perm = args[2];
            if (args[0] == CommandUser && TryGetPlayer(args[1], out var player))
            {
                if (!_storedData.PlayersList.TryGetValue(player.Id, out var playerData))
                    _storedData.PlayersList[player.Id] = playerData = new PlayerData(player.Id, player.Name);
                NextTick(() =>
                {
                    if (player.HasPermission(perm))
                    {
                        if (int.TryParse(args[3], out var secondsToAdd))
                            GrantTemporaryPermission(playerData, perm, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false);
                        else if (DateTime.TryParse(args[3], out var expireDate))
                            GrantTemporaryPermission(playerData, perm, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default);
                    }
                });
            }
            else if (args[0] == CommandGroup && GroupExists(args[1]))
            {
                string groupName = args[1];
                if (!_storedData.GroupsList.TryGetValue(groupName, out var groupData))
                    _storedData.GroupsList[groupName] = groupData = new GroupData(groupName);
                NextTick(() =>
                {
                    if (permission.GroupHasPermission(groupName, perm))
                    {
                        if (int.TryParse(args[3], out var secondsToAdd))
                            GrantTemporaryPermission(groupData, perm, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false);
                        else if (DateTime.TryParse(args[3], out var expireDate))
                            GrantTemporaryPermission(groupData, perm, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default);
                    }
                });
            }
        }
        
        private void CommandRevoke(string[] args)
        {
            //             0            1           2
            //o.revoke user/group iiiaka/admin realpve.vip
            if (args.Length < 3) return;
            string userGroupId = args[1], perm = args[2];
            TemporaryPermission tempPermission;
            if (args[0] == CommandUser && TryGetPlayer(userGroupId, out var player) && _storedData.PlayersList.TryGetValue(player.Id, out var playerData))
            {
                tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null && player.HasPermission(perm))
                {
                    NextTick(() =>
                    {
                        if (!player.HasPermission(perm))
                            tempPermission.AlreadyRemoved = true;
                    });
                }
            }
            else if (args[0] == CommandGroup && GroupExists(userGroupId) && _storedData.GroupsList.TryGetValue(userGroupId, out var groupData))
            {
                tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null && permission.GroupHasPermission(userGroupId, perm))
                {
                    NextTick(() =>
                    {
                        if (!permission.GroupHasPermission(userGroupId, perm))
                            tempPermission.AlreadyRemoved = true;
                    });
                }
            }
        }
        
        private void CommandUserGroup(string[] args)
        {
            if (args.Length < 3 || !TryGetPlayer(args[1], out var player) || !GroupExists(args[2])) return;
            _storedData.PlayersList.TryGetValue(player.Id, out var playerData);
            string groupName = args[2];
            if (args[0] == CommandAdd && args.Length >= 4 && int.TryParse(args[3], out var secondsToAdd))
            {
                //             0     1     2         3          4 - optional
                //o.usergroup add iiiaka admin 123/DATETIME true/false
                if (playerData == null)
                    _storedData.PlayersList[player.Id] = playerData = new PlayerData(player.Id, player.Name);
                NextTick(() =>
                {
                    if (player.BelongsToGroup(groupName))
                    {
                        if (int.TryParse(args[3], out var secondsToAdd))
                            AddTemporaryGroup(playerData, groupName, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false);
                        else if (DateTime.TryParse(args[3], out var expireDate))
                            AddTemporaryGroup(playerData, groupName, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default);
                    }
                });
            }
            else if (args[0] == CommandRemove)
            {
                //               0      1     2
                //o.usergroup remove iiiaka admin
                if (playerData != null)
                {
                    var tempPermission = playerData.GroupsList.FirstOrDefault(p => p.Name == groupName);
                    if (tempPermission != null && player.BelongsToGroup(groupName))
                    {
                        NextTick(() =>
                        {
                            if (!player.BelongsToGroup(groupName))
                                tempPermission.AlreadyRemoved = true;
                        });
                    }
                }
            }
        }
        
        private bool TryGetPlayer(string nameOrId, out IPlayer result)
        {
            result = null;
            if (nameOrId.IsSteamId())
            {
                foreach (var player in covalence.Players.All)
                {
                    if (player.Id == nameOrId)
                    {
                        result = player;
                        break;
                    }
                }
            }
            else 
            {
                nameOrId = nameOrId.ToLower();
                foreach (var player in covalence.Players.All)
                {
                    if (player.Name.ToLower() == nameOrId)
                    {
                        result = player;
                        break;
                    }
                }
            }
            return result != null;
        }
        
        private bool GroupExists(string groupName) => !groupName.Equals("*") && permission.GroupExists(groupName);
        #endregion
        
        #region ~API~
        private object IsReady() => _isReady ? true : null;
        #endregion
        
        #region ~API - User's Permissions~
        private bool GrantUserPermission(string userID, string perm, int secondsToAdd, bool fromNow = false, bool checkExistence = true) => GrantUserPermission(covalence.Players.FindPlayerById(userID), perm, secondsToAdd, fromNow, checkExistence);
        private bool GrantUserPermission(BasePlayer player, string perm, int secondsToAdd, bool fromNow = false, bool checkExistence = true) => GrantUserPermission(player.IPlayer, perm, secondsToAdd, fromNow, checkExistence);
        private bool GrantUserPermission(IPlayer player, string perm, int secondsToAdd, bool fromNow = false, bool checkExistence = true)
        {
            if (player != null && (!checkExistence || permission.PermissionExists(perm)))
            {
                if (!_storedData.PlayersList.TryGetValue(player.Id, out var playerData))
                    _storedData.PlayersList[player.Id] = playerData = new PlayerData(player.Id, player.Name);
                permission.GrantUserPermission(player.Id, perm, null);
                GrantTemporaryPermission(playerData, perm, secondsToAdd, fromNow);
                return true;
            }
            return false;
        }

        private bool GrantUserPermission(string userID, string perm, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true) => GrantUserPermission(covalence.Players.FindPlayerById(userID), perm, expireDate, assignedDate, checkExistence);
        private bool GrantUserPermission(BasePlayer player, string perm, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true) => GrantUserPermission(player.IPlayer, perm, expireDate, assignedDate, checkExistence);
        private bool GrantUserPermission(IPlayer player, string perm, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true)
        {
            if (player != null && (!checkExistence || permission.PermissionExists(perm)))
            {
                if (!_storedData.PlayersList.TryGetValue(player.Id, out var playerData))
                    _storedData.PlayersList[player.Id] = playerData = new PlayerData(player.Id, player.Name);
                permission.GrantUserPermission(player.Id, perm, null);
                GrantTemporaryPermission(playerData, perm, expireDate, assignedDate);
                return true;
            }
            return false;
        }

        private bool RevokeUserPermission(string userID, string perm) => RevokeUserPermission(covalence.Players.FindPlayerById(userID), perm);
        private bool RevokeUserPermission(BasePlayer player, string perm) => RevokeUserPermission(player.IPlayer, perm);
        private bool RevokeUserPermission(IPlayer player, string perm)
        {
            if (player != null && _storedData.PlayersList.TryGetValue(player.Id, out var playerData))
            {
                var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null)
                {
                    tempPermission.AlreadyRemoved = true;
                    return true;
                }
            }
            return false;
        }

        private bool UserHasPermission(string userID, string perm) => UserHasPermission(covalence.Players.FindPlayerById(userID), perm);
        private bool UserHasPermission(BasePlayer player, string perm) => UserHasPermission(player.IPlayer, perm);
        private bool UserHasPermission(IPlayer player, string perm)
        {
            if (player != null && _storedData.PlayersList.TryGetValue(player.Id, out var playerData))
            {
                var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    if (player.HasPermission(perm))
                        return true;
                    tempPermission.AlreadyRemoved = true;
                }
            }
            return false;
        }
        
        private int GrantActiveUsersPermission(string perm, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (permission.PermissionExists(perm))
            {
                foreach (var player in covalence.Players.Connected)
                {
                    if (GrantUserPermission(player, perm, secondsToAdd, fromNow, false))
                        result++;
                }
            }
            return result;
        }

        private int GrantActiveUsersPermission(string perm, DateTime expireDate, DateTime assignedDate = default)
        {
            int result = 0;
            if (permission.PermissionExists(perm))
            {
                foreach (var player in covalence.Players.Connected)
                {
                    if (GrantUserPermission(player, perm, expireDate, assignedDate, false))
                        result++;
                }
            }
            return result;
        }

        private int GrantAllUsersPermission(string perm, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (permission.PermissionExists(perm))
            {
                foreach (var player in covalence.Players.All)
                {
                    if (GrantUserPermission(player, perm, secondsToAdd, fromNow, false))
                        result++;
                }
            }
            return result;
        }

        private int GrantAllUsersPermission(string perm, DateTime expireDate, DateTime assignedDate = default)
        {
            int result = 0;
            if (permission.PermissionExists(perm))
            {
                foreach (var player in covalence.Players.All)
                {
                    if (GrantUserPermission(player, perm, expireDate, assignedDate, false))
                        result++;
                }
            }
            return result;
        }

        private int RevokeActiveUsersPermission(string perm)
        {
            int result = 0;
            foreach (var player in covalence.Players.Connected)
            {
                if (RevokeUserPermission(player, perm))
                    result++;
            }
            return result;
        }

        private int RevokeAllUsersPermission(string perm)
        {
            int result = 0;
            foreach (var player in covalence.Players.All)
            {
                if (RevokeUserPermission(player, perm))
                    result++;
            }
            return result;
        }
        
        private Dictionary<string, DateTime[]> UserGetAllPermissions(string userID)
        {
            var result = new Dictionary<string, DateTime[]>();
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                foreach (var tempPermission in playerData.PermissionsList)
                    result[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
            }
            return result;
        }
        #endregion
        
        #region ~API - User's Groups~
        private bool AddUserGroup(string userID, string groupName, int secondsToAdd, bool fromNow = false, bool checkExistence = true) => AddUserGroup(covalence.Players.FindPlayerById(userID), groupName, secondsToAdd, fromNow, checkExistence);
        private bool AddUserGroup(BasePlayer player, string groupName, int secondsToAdd, bool fromNow = false, bool checkExistence = true) => AddUserGroup(player.IPlayer, groupName, secondsToAdd, fromNow, checkExistence);
        private bool AddUserGroup(IPlayer player, string groupName, int secondsToAdd, bool fromNow = false, bool checkExistence = true)
        {
            if (player != null && (!checkExistence || permission.GroupExists(groupName)))
            {
                if (!_storedData.PlayersList.TryGetValue(player.Id, out var playerData))
                    _storedData.PlayersList[player.Id] = playerData = new PlayerData(player.Id, player.Name);
                permission.AddUserGroup(player.Id, groupName);
                AddTemporaryGroup(playerData, groupName, secondsToAdd, fromNow);
                return true;
            }
            return false;
        }

        private bool AddUserGroup(string userID, string groupName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true) => AddUserGroup(covalence.Players.FindPlayerById(userID), groupName, expireDate, assignedDate, checkExistence);
        private bool AddUserGroup(BasePlayer player, string groupName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true) => AddUserGroup(player.IPlayer, groupName, expireDate, assignedDate, checkExistence);
        private bool AddUserGroup(IPlayer player, string groupName, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true)
        {
            if (player != null && (!checkExistence || permission.GroupExists(groupName)))
            {
                if (!_storedData.PlayersList.TryGetValue(player.Id, out var playerData))
                    _storedData.PlayersList[player.Id] = playerData = new PlayerData(player.Id, player.Name);
                permission.AddUserGroup(player.Id, groupName);
                AddTemporaryGroup(playerData, groupName, expireDate, assignedDate);
                return true;
            }
            return false;
        }

        private bool RemoveUserGroup(string userID, string groupName) => RemoveUserGroup(covalence.Players.FindPlayerById(userID), groupName);
        private bool RemoveUserGroup(BasePlayer player, string groupName) => RemoveUserGroup(player.IPlayer, groupName);
        private bool RemoveUserGroup(IPlayer player, string groupName)
        {
            if (player != null && _storedData.PlayersList.TryGetValue(player.Id, out var playerData))
            {
                var tempGroup = playerData.GroupsList.FirstOrDefault(p => p.Name == groupName);
                if (tempGroup != null)
                {
                    tempGroup.AlreadyRemoved = true;
                    return true;
                }
            }
            return false;
        }

        private bool UserHasGroup(string userID, string groupName) => UserHasGroup(covalence.Players.FindPlayerById(userID), groupName);
        private bool UserHasGroup(BasePlayer player, string groupName) => UserHasGroup(player.IPlayer, groupName);
        private bool UserHasGroup(IPlayer player, string groupName)
        {
            if (player != null && _storedData.PlayersList.TryGetValue(player.Id, out var playerData))
            {
                var tempGroup = playerData.GroupsList.FirstOrDefault(p => p.Name == groupName);
                if (tempGroup != null && !tempGroup.AlreadyRemoved)
                {
                    if (permission.UserHasGroup(player.Id, groupName))
                        return true;
                    tempGroup.AlreadyRemoved = true;
                }
            }
            return false;
        }
        
        private int AddActiveUsersGroup(string groupName, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (permission.GroupExists(groupName))
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
            if (permission.GroupExists(groupName))
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
            if (permission.GroupExists(groupName))
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
            if (permission.GroupExists(groupName))
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
            if (permission.GroupExists(groupName))
            {
                foreach (var player in covalence.Players.Connected)
                {
                    if (RemoveUserGroup(player, groupName))
                        result++;
                }
            }
            return result;
        }

        private int RemoveAllUsersGroup(string groupName)
        {
            int result = 0;
            if (permission.GroupExists(groupName))
            {
                foreach (var player in covalence.Players.All)
                {
                    if (RemoveUserGroup(player, groupName))
                        result++;
                }
            }
            return result;
        }
        
        private Dictionary<string, DateTime[]> UserGetAllGroups(string userID)
        {
            var result = new Dictionary<string, DateTime[]>();
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                foreach (var tempGroup in playerData.GroupsList)
                    result[tempGroup.Name] = new DateTime[2] { tempGroup.AssignedDate, tempGroup.ExpireDate };
            }
            return result;
        }
        #endregion

        #region ~API - Group's Permissions~
        private bool GrantGroupPermission(string groupName, string perm, int secondsToAdd, bool fromNow = false, bool checkExistence = true)
        {
            if (!checkExistence || (permission.GroupExists(groupName) && permission.PermissionExists(perm)))
            {
                if (!_storedData.GroupsList.TryGetValue(groupName, out var groupData))
                    _storedData.GroupsList[groupName] = groupData = new GroupData(groupName);
                permission.GrantGroupPermission(groupName, perm, null);
                GrantTemporaryPermission(groupData, perm, secondsToAdd, fromNow);
                return true;
            }
            return false;
        }
        
        private bool GrantGroupPermission(string groupName, string perm, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true)
        {
            if (!checkExistence || (permission.GroupExists(groupName) && permission.PermissionExists(perm)))
            {
                if (!_storedData.GroupsList.TryGetValue(groupName, out var groupData))
                    _storedData.GroupsList[groupName] = groupData = new GroupData(groupName);
                permission.GrantGroupPermission(groupName, perm, null);
                GrantTemporaryPermission(groupData, perm, expireDate, assignedDate);
                return true;
            }
            return false;
        }
        
        private bool RevokeGroupPermission(string groupName, string perm, bool checkExistence = true)
        {
            if ((!checkExistence || permission.GroupExists(groupName)) && _storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null)
                {
                    tempPermission.AlreadyRemoved = true;
                    return true;
                }
            }
            return false;
        }
        
        private bool GroupHasPermission(string groupName, string perm)
        {
            if (permission.GroupExists(groupName) && _storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    if (permission.GroupHasPermission(groupName, perm))
                        return true;
                    tempPermission.AlreadyRemoved = true;
                }
            }
            return false;
        }
        
        private int GrantAllGroupsPermission(string perm, int secondsToAdd, bool fromNow = false)
        {
            int result = 0;
            if (permission.PermissionExists(perm))
            {
                foreach (var groupName in permission.GetGroups())
                {
                    if (GrantGroupPermission(groupName, perm, secondsToAdd, fromNow, false))
                        result++;
                }
            }
            return result;
        }
        
        private int GrantAllGroupsPermission(string perm, DateTime expireDate, DateTime assignedDate = default)
        {
            int result = 0;
            if (permission.PermissionExists(perm))
            {
                foreach (var groupName in permission.GetGroups())
                {
                    if (GrantGroupPermission(groupName, perm, expireDate, assignedDate, false))
                        result++;
                }
            }
            return result;
        }
        
        private int RevokeAllGroupsPermission(string perm)
        {
            int result = 0;
            if (permission.PermissionExists(perm))
            {
                foreach (var groupName in permission.GetGroups())
                {
                    if (RevokeGroupPermission(groupName, perm))
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
                foreach (var tempPermission in groupData.PermissionsList)
                    result[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
            }
            return result;
        }
        #endregion

        #region ~Oxide Hooks~
        void CanBypassQueue(Network.Connection connection) => GrantOnConnect(connection.userid.ToString(), DateTime.UtcNow);
        
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!_storedData.PlayersList.TryGetValue(player.UserIDString, out var playerData)) return;
            foreach (var tempPerm in playerData.PermissionsList)
                permission.RevokeUserPermission(player.UserIDString, tempPerm.Name);
            foreach (var tempGroup in playerData.GroupsList)
                permission.RemoveUserGroup(player.UserIDString, tempGroup.Name);
        }
        
        void OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (CommandsGrant.Contains(arg.cmd.FullName))
                CommandGrant(arg.Args);
            else if (CommandsRevoke.Contains(arg.cmd.FullName))
                CommandRevoke(arg.Args);
            else if (CommandsUserGroup.Contains(arg.cmd.FullName))
                CommandUserGroup(arg.Args);
        }
        
        void Init()
        {
            Unsubscribe(nameof(CanBypassQueue));
            Unsubscribe(nameof(OnPlayerDisconnected));
            Unsubscribe(nameof(OnServerCommand));
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            AddCovalenceCommand(_config.Command, nameof(TemporaryPermissions_Command));
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
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
        private void TemporaryPermissions_Command(IPlayer player, string command, string[] args)
        {
            if (args == null || args.Length < 3 || (!player.IsServer && !permission.UserHasPermission(player.Id, PERMISSION_ADMIN))) return;
            string replyKey = string.Empty;
            string[] replyArgs = new string[5];
            
            if (args[0] == "grant")
            {
                if (args.Length < 5)
                    replyKey = "MsgGrantWrongFormat";
                else if (!permission.PermissionExists(args[3]))
                    replyKey = "MsgPermissionNotFound";
                else if (args[1] == "user")
                {
                    if (!TryGetPlayer(args[2], out var target))
                        replyKey = "MsgPlayerNotFound";
                    else
                    {
                        //        0     1     2       3            4           5 - optional
                        //tperm grant user iiiaka realpve.vip 123/DATETIME true/false
                        if (!_storedData.PlayersList.TryGetValue(target.Id, out var targetData))
                            _storedData.PlayersList[target.Id] = targetData = new PlayerData(target.Id, target.Name);
                        if (int.TryParse(args[4], out var secondsToAdd))
                            GrantTemporaryPermission(targetData, args[3], secondsToAdd, args.Length > 5 && bool.TryParse(args[5], out var fromNow) ? fromNow : false);
                        else if (DateTime.TryParse(args[4], out var expireDate))
                            GrantTemporaryPermission(targetData, args[3], expireDate, args.Length > 5 && DateTime.TryParse(args[5], out var assignedDate) ? assignedDate : default);
                        else
                        {
                            replyKey = "MsgGrantWrongFormat";
                            goto exit;
                        }
                        permission.GrantUserPermission(target.Id, args[3], null);
                        replyKey = "MsgUserGranted";
                        replyArgs[0] = args[3];
                        replyArgs[1] = target.Name;
                    }
                }
                else if (args[1] == "group")
                {
                    if (!GroupExists(args[2]))
                        replyKey = "MsgGroupNotFound";
                    else
                    {
                        //        0     1    2       3           4            5 - optional
                        //tperm grant group vip realpve.vip 123/DATETIME true/false
                        if (!_storedData.GroupsList.TryGetValue(args[2], out var groupData))
                            _storedData.GroupsList[args[2]] = groupData = new GroupData(args[2]);
                        if (int.TryParse(args[4], out var secondsToAdd))
                            GrantTemporaryPermission(groupData, args[3], secondsToAdd, args.Length > 5 && bool.TryParse(args[5], out var fromNow) ? fromNow : false);
                        else if (DateTime.TryParse(args[4], out var expireDate))
                            GrantTemporaryPermission(groupData, args[3], expireDate, args.Length > 5 && DateTime.TryParse(args[5], out var assignedDate) ? assignedDate : default);
                        else
                        {
                            replyKey = "MsgGrantWrongFormat";
                            goto exit;
                        }
                        permission.GrantGroupPermission(args[2], args[3], null);
                        replyKey = "MsgGroupGranted";
                        replyArgs[0] = args[3];
                        replyArgs[1] = args[2];
                    }
                }
            }
            else if (args[0] == "revoke")
            {
                if (args.Length < 4)
                    replyKey = "MsgRevokeWrongFormat";
                else if (!permission.PermissionExists(args[3]))
                    replyKey = "MsgPermissionNotFound";
                else if (args[1] == "user")
                {
                    if (!TryGetPlayer(args[2], out var target))
                        replyKey = "MsgPlayerNotFound";
                    else
                    {
                        //         0     1     2        3
                        //tperm revoke user iiiaka realpve.vip
                        permission.RevokeUserPermission(target.Id, args[3]);
                        if (_storedData.PlayersList.TryGetValue(target.Id, out var targetData))
                        {
                            var tempPermission = targetData.PermissionsList.FirstOrDefault(p => p.Name == args[3]);
                            if (tempPermission != null)
                                tempPermission.AlreadyRemoved = true;
                        }
                        replyKey = "MsgUserRevoked";
                        replyArgs[0] = args[3];
                        replyArgs[1] = target.Name;
                    }
                }
                else if (args[1] == "group")
                {
                    if (!GroupExists(args[2]))
                        replyKey = "MsgGroupNotFound";
                    else
                    {
                        //         0     1    2       3
                        //tperm revoke group vip realpve.vip
                        permission.RevokeGroupPermission(args[2], args[3]);
                        if (_storedData.GroupsList.TryGetValue(args[2], out var groupData))
                        {
                            var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name == args[3]);
                            if (tempPermission != null)
                                tempPermission.AlreadyRemoved = true;
                        }
                        replyKey = "MsgGroupRevoked";
                        replyArgs[0] = args[3];
                        replyArgs[1] = args[2];
                    }
                }
            }
            else if (args[0] == "add")
            {
                if (args.Length < 4)
                    replyKey = "MsgUserGroupWrongFormat";
                else if (!TryGetPlayer(args[1], out var target))
                    replyKey = "MsgPlayerNotFound";
                else if (!GroupExists(args[2]))
                    replyKey = "MsgGroupNotFound";
                else
                {
                    //       0     1    2         3         4 - optional
                    //tperm add iiiaka vip 123/DATETIME true/false
                    if (!_storedData.PlayersList.TryGetValue(target.Id, out var targetData))
                        _storedData.PlayersList[target.Id] = targetData = new PlayerData(target.Id, target.Name);
                    if (int.TryParse(args[3], out var secondsToAdd))
                        AddTemporaryGroup(targetData, args[2], secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false);
                    else if (DateTime.TryParse(args[3], out var expireDate))
                        AddTemporaryGroup(targetData, args[2], expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default);
                    else
                    {
                        replyKey = "MsgUserGroupWrongFormat";
                        goto exit;
                    }
                    permission.AddUserGroup(target.Id, args[2]);
                    replyKey = "MsgUserGroupAdded";
                    replyArgs[0] = target.Name;
                    replyArgs[1] = args[2];
                }
            }
            else if (args[0] == "remove")
            {
                if (!TryGetPlayer(args[1], out var target))
                    replyKey = "MsgPlayerNotFound";
                else if (!GroupExists(args[2]))
                    replyKey = "MsgGroupNotFound";
                else
                {
                    //        0       1    2
                    //tperm remove iiiaka vip
                    permission.RemoveUserGroup(target.Id, args[2]);
                    if (_storedData.PlayersList.TryGetValue(target.Id, out var targetData))
                    {
                        var tempPermission = targetData.GroupsList.FirstOrDefault(p => p.Name == args[2]);
                        if (tempPermission != null)
                            tempPermission.AlreadyRemoved = true;
                    }
                    replyKey = "MsgUserGroupRemoved";
                    replyArgs[0] = target.Name;
                    replyArgs[1] = args[2];
                }
            }
        
        exit:
            if (!string.IsNullOrWhiteSpace(replyKey))
                player.Reply(string.Format(lang.GetMessage(replyKey, this, player.Id), replyArgs));
        }
        #endregion

        #region ~Unload~
        void Unload()
        {
            if (_updatesTimer != null)
                _updatesTimer.Destroy();
            if (_config.RemoveOnUnload)
            {
                string userID;
                PlayerData playerData;
                foreach (var kvp in _storedData.PlayersList)
                {
                    userID = kvp.Key;
                    playerData = kvp.Value;
                    foreach (var tempPerm in playerData.PermissionsList)
                        permission.RevokeUserPermission(userID, tempPerm.Name);
                    foreach (var tempGroup in playerData.GroupsList)
                        permission.RemoveUserGroup(userID, tempGroup.Name);
                }
                string groupName;
                foreach (var kvp in _storedData.GroupsList)
                {
                    groupName = kvp.Key;
                    foreach (var tempPerm in kvp.Value.PermissionsList)
                        permission.RevokeGroupPermission(groupName, tempPerm.Name);
                }
            }
            SaveData();
            _storedData = null;
            _config = null;
        }
        #endregion
        
        #region ~Classes~
        public class PlayerData
        {
            [JsonProperty(PropertyName = "User Id")]
            public string UserID { get; set; } = string.Empty;
            
            [JsonProperty(PropertyName = "User Display Name(last)")]
            public string DisplayName { get; set; } = string.Empty;
            
            [JsonProperty(PropertyName = "Temporary Permissions")]
            public List<TemporaryPermission> PermissionsList { get; private set; } = new List<TemporaryPermission>();
            
            [JsonProperty(PropertyName = "Temporary Groups")]
            public List<TemporaryPermission> GroupsList { get; private set; } = new List<TemporaryPermission>();
            
            public PlayerData() {}
            public PlayerData(string id, string displayName = "")
            {
                UserID = id;
                DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : id;
            }
        }
        
        public class GroupData
        {
            [JsonProperty(PropertyName = "Group Name")]
            public string GroupName { get; set; } = string.Empty;
            
            [JsonProperty(PropertyName = "Temporary Permissions")]
            public List<TemporaryPermission> PermissionsList { get; private set; } = new List<TemporaryPermission>();
            
            public GroupData() {}
            public GroupData(string name)
            {
                GroupName = name;
            }
        }

        public class TemporaryPermission
        {
            public string Name { get; private set; } = string.Empty;
            public DateTime AssignedDate { get; set; }
            public DateTime ExpireDate { get; set; }
            [JsonIgnore] public bool AlreadyRemoved { get; set; } = false;
            
            public TemporaryPermission() {}
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
            
            public override int GetHashCode() => Name.GetHashCode();
        }
        #endregion
    }
}