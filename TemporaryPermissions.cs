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
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("Temporary Permissions", "IIIaKa", "0.1.1")]
    [Description("Useful plugin for managing temporary permissions, temporary groups and temporary permissions for groups. This is done through chat commands, built-in Oxide commands, and API methods.")]
    class TemporaryPermissions : RustPlugin
    {
        #region ~Variables~
        private bool _isReady = false;
        private const string PERMISSION_ADMIN = "temporarypermissions.admin", TimeFormat = "yyyy-MM-dd HH:mm:ss", CommandAdd = "add", CommandRemove = "remove", CommandUser = "user", CommandGroup = "group", Str_Wipe = "wipe", Hooks_OnLoaded = "OnTemporaryPermissionsLoaded",
            Hooks_UserPermissionGranted = "OnTemporaryPermissionGranted", Hooks_UserPermissionUpdated = "OnTemporaryPermissionUpdated", Hooks_UserPermissionRevoked = "OnTemporaryPermissionRevoked",
            Hooks_UserGroupAdded = "OnTemporaryGroupAdded", Hooks_UserGroupUpdated = "OnTemporaryGroupUpdated", Hooks_UserGroupRemoved = "OnTemporaryGroupRemoved",
            Hooks_GroupPermissionGranted = "OnGroupTemporaryPermissionGranted", Hooks_GroupPermissionUpdated = "OnGroupTemporaryPermissionUpdated", Hooks_GroupPermissionRevoked = "OnGroupTemporaryPermissionRevoked";
        private readonly string[] CommandsGrant = new string[3] { "oxide.grant", "o.grant", "umod.grant" },
            CommandsRevoke = new string[3] { "oxide.revoke", "o.revoke", "umod.revoke" },
            CommandsUserGroup = new string[3] { "oxide.usergroup", "o.usergroup", "umod.usergroup" };
        private Timer _expirationTimer, _presenceTimer;
        #endregion

        #region ~Configuration~
        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Chat command")]
            public string Command = string.Empty;
            
            [JsonProperty(PropertyName = "Interval in seconds for expiration check")]
            public float ExpirationInterval = 1f;
            
            [JsonProperty(PropertyName = "Interval in seconds for checking the presence of temporary permissions and temporary groups. There are cases where removal cannot be tracked in the usual way. A value of 0 disables the check")]
            public float PresenceInterval = 600f;
            
            [JsonProperty(PropertyName = "Is it worth restoring removed temporary permissions and temporary groups if the timer hasn't expired?")]
            public bool PresenceRestoring = true;
            
            [JsonProperty(PropertyName = "Is it worth removing temporary permissions and temporary groups when unloading the plugin?")]
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
                _config.Version = Version;
                PrintWarning($"The configuration file has been successfully updated to version {_config.Version}!");
            }
            
            if (string.IsNullOrWhiteSpace(_config.Command))
                _config.Command = "tperm";
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
            if (_storedData == null)
                _storedData = new StoredData();
            if (_storedData.PlayersList == null)
                _storedData.PlayersList = new Dictionary<string, PlayerData>();
            if (_storedData.GroupsList == null)
                _storedData.GroupsList = new Dictionary<string, GroupData>();
            
            if (string.IsNullOrWhiteSpace(_config.WipeID) || _config.WipeID != SaveRestore.WipeId)
            {
                int counter = 0;
                _config.WipeID = SaveRestore.WipeId;
                SaveConfig();
                if (_config.ClearOnWipe)
                {
                    foreach (var playerData in _storedData.PlayersList.Values)
                    {
                        counter += playerData.PermissionsList.Count;
                        foreach (var tempPermission in playerData.PermissionsList)
                            tempPermission.AlreadyRemoved = true;
                        counter += playerData.GroupsList.Count;
                        foreach (var tempGroup in playerData.GroupsList)
                            tempGroup.AlreadyRemoved = true;
                    }
                    
                    foreach (var groupData in _storedData.GroupsList.Values)
                    {
                        counter += groupData.PermissionsList.Count;
                        foreach (var tempPermission in groupData.PermissionsList)
                            tempPermission.AlreadyRemoved = true;
                    }
                }
                else
                {
                    foreach (var playerData in _storedData.PlayersList.Values)
                    {
                        foreach (var tempPermission in playerData.PermissionsList)
                        {
                            if (tempPermission.UntilWipe)
                            {
                                tempPermission.UntilWipe = false;
                                tempPermission.AlreadyRemoved = true;
                                counter++;
                            }
                        }
                        foreach (var tempGroup in playerData.GroupsList)
                        {
                            if (tempGroup.UntilWipe)
                            {
                                tempGroup.UntilWipe = false;
                                tempGroup.AlreadyRemoved = true;
                                counter++;
                            }
                        }
                    }
                    
                    foreach (var groupData in _storedData.GroupsList.Values)
                    {
                        foreach (var tempPermission in groupData.PermissionsList)
                        {
                            if (tempPermission.UntilWipe)
                            {
                                tempPermission.UntilWipe = false;
                                tempPermission.AlreadyRemoved = true;
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
                foreach (var tempPerm in playerData.PermissionsList)
                    permission.GrantUserPermission(playerData.UserID, tempPerm.Name, null);
                foreach (var tempGroup in playerData.GroupsList)
                    permission.AddUserGroup(playerData.UserID, tempGroup.Name);
            }
            string[] groupNames = permission.GetGroups();
            string groupName;
            for (int i = 0; i < groupNames.Length; i++)
            {
                groupName = groupNames[i];
                if (!_storedData.GroupsList.TryGetValue(groupName, out var groupData)) continue;
                foreach (var tempPerm in groupData.PermissionsList)
                    permission.GrantGroupPermission(groupName, tempPerm.Name, null);
            }
            _expirationTimer = timer.Every(_config.ExpirationInterval, CheckForExpiration);
            if (_config.PresenceInterval > 0f)
                _presenceTimer = timer.Every(_config.PresenceInterval, CheckForPresence);
            Subscribe(nameof(OnServerCommand));
            Subscribe(nameof(OnUserPermissionRevoked));
            Subscribe(nameof(OnUserGroupRemoved));
            Subscribe(nameof(OnGroupPermissionRevoked));
            Subscribe(nameof(OnGroupDeleted));
            Subscribe(nameof(OnServerSave));
            _isReady = true;
            yield return new WaitForSeconds(1);
            Interface.CallHook(Hooks_OnLoaded, Version);
        }
        
        private void CheckForExpiration()
        {
            var utcNow = DateTime.UtcNow;
            bool isExpired;
            
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
                    
                    foreach (var tempPermission in playerData.PermissionsList)
                    {
                        if (tempPermission.UntilWipe) continue;
                        isExpired = utcNow > tempPermission.ExpireDate;
                        if (tempPermission.AlreadyRemoved || isExpired)
                        {
                            tempPermission.AlreadyRemoved = true;
                            permission.RevokeUserPermission(playerData.UserID, tempPermission.Name);
                            if (_config.ConsoleLog)
                                Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission has been removed: {tempPermission.Name} | Is expired: {isExpired}");
                            Interface.CallHook(Hooks_UserPermissionRevoked, playerData.UserID, tempPermission.Name, isExpired);
                        }
                    }
                    playerData.PermissionsList.RemoveAll(x => x.AlreadyRemoved);
                    
                    foreach (var tempGroup in playerData.GroupsList)
                    {
                        if (tempGroup.UntilWipe) continue;
                        isExpired = utcNow > tempGroup.ExpireDate;
                        if (tempGroup.AlreadyRemoved || isExpired)
                        {
                            tempGroup.AlreadyRemoved = true;
                            permission.RemoveUserGroup(playerData.UserID, tempGroup.Name);
                            if (_config.ConsoleLog)
                                Puts($"{playerData.DisplayName} ({playerData.UserID}) - Has been removed from group: {tempGroup.Name} | Is expired: {isExpired}");
                            Interface.CallHook(Hooks_UserGroupRemoved, playerData.UserID, tempGroup.Name, isExpired);
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
                foreach (var groupData in groupsList.Values)
                {
                    if (groupData.PermissionsList.Any())
                    {
                        foreach (var tempPermission in groupData.PermissionsList)
                        {
                            if (tempPermission.UntilWipe) continue;
                            isExpired = utcNow > tempPermission.ExpireDate;
                            if (tempPermission.AlreadyRemoved || isExpired)
                            {
                                tempPermission.AlreadyRemoved = true;
                                permission.RevokeGroupPermission(groupData.GroupName, tempPermission.Name);
                                if (_config.ConsoleLog)
                                    Puts($"{groupData.GroupName} (group) - Permission has been removed: {tempPermission.Name} | Is expired: {isExpired}");
                                Interface.CallHook(Hooks_GroupPermissionRevoked, groupData.GroupName, tempPermission.Name, isExpired);
                            }
                        }
                        groupData.PermissionsList.RemoveAll(x => x.AlreadyRemoved);
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
            HashSet<string> users = _config.RemoveOnUnload ? covalence.Players.Connected.Select(player => player.Id).ToHashSet() : null;
            foreach (var playerData in _storedData.PlayersList.Values)
            {
                if (users != null && !users.Contains(playerData.UserID))
                    continue;
                
                foreach (var tempPermission in playerData.PermissionsList)
                {
                    if (!tempPermission.AlreadyRemoved && !UserHasPermission_Helper(playerData.UserID, tempPermission.Name))
                    {
                        if (_config.PresenceRestoring)
                            permission.GrantUserPermission(playerData.UserID, tempPermission.Name, null);
                        else
                            tempPermission.AlreadyRemoved = true;
                    }
                }
                
                foreach (var tempGroup in playerData.GroupsList)
                {
                    if (!tempGroup.AlreadyRemoved && !UserHasGroup_Helper(playerData.UserID, tempGroup.Name))
                    {
                        if (_config.PresenceRestoring)
                            permission.AddUserGroup(playerData.UserID, tempGroup.Name);
                        else
                            tempGroup.AlreadyRemoved = true;
                    }
                }
            }
            
            foreach (var groupData in _storedData.GroupsList.Values)
            {
                if (!GroupExists_Helper(groupData.GroupName))
                {
                    foreach (var tempPermission in groupData.PermissionsList)
                        tempPermission.AlreadyRemoved = true;
                    continue;
                }
                foreach (var tempPermission in groupData.PermissionsList)
                {
                    if (!tempPermission.AlreadyRemoved && !GroupHasPermission_Helper(groupData.GroupName, tempPermission.Name))
                    {
                        if (_config.PresenceRestoring)
                            permission.GrantGroupPermission(groupData.GroupName, tempPermission.Name, null);
                        else
                            tempPermission.AlreadyRemoved = true;
                    }
                }
            }
        }
        
        private void CommandGrant(string[] args)
        {
            //            0            1           2                3                     4 - optional
            //o.grant user/group iiiaka/admin realpve.vip wipe/seconds/DATETIME (true/false)/DATETIME
            if (args.Length < 4 || !permission.PermissionExists(args[2])) return;
            string perm = args[2];
            if (args[0] == CommandUser)
            {
                if (!TryGetPlayer(args[1], out var player)) return;
                NextTick(() =>
                {
                    if (UserHasPermission_Helper(player.Id, perm))
                    {
                        var playerData = CreateOrGetPlayerData(player.Id, player.Name);
                        if (args[3] == Str_Wipe)
                            GrantTemporaryPermission(playerData, perm, false);
                        else if (int.TryParse(args[3], out var secondsToAdd))
                            GrantTemporaryPermission(playerData, perm, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false, false);
                        else if (DateTime.TryParse(args[3], out var expireDate))
                            GrantTemporaryPermission(playerData, perm, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default, false);
                    }
                });
            }
            else if (args[0] == CommandGroup)
            {
                string groupName = args[1];
                if (!GroupExists_Helper(groupName)) return;
                NextTick(() =>
                {
                    if (GroupHasPermission_Helper(groupName, perm))
                    {
                        var groupData = CreateOrGetGroupData(groupName);
                        if (args[3] == Str_Wipe)
                            GrantTemporaryPermission(groupData, perm, false);
                        else if (int.TryParse(args[3], out var secondsToAdd))
                            GrantTemporaryPermission(groupData, perm, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false, false);
                        else if (DateTime.TryParse(args[3], out var expireDate))
                            GrantTemporaryPermission(groupData, perm, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default, false);
                    }
                });
            }
        }
        
        private void CommandRevoke(string[] args)
        {
            //             0            1           2
            //o.revoke user/group iiiaka/admin realpve.vip
            if (args.Length < 3) return;
            string perm = args[2];
            if (args[0] == CommandUser)
            {
                if (!_storedData.PlayersList.TryGetValue(args[1], out var playerData)) return;
                var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    NextTick(() =>
                    {
                        if (!UserHasPermission_Helper(args[1], perm))
                            tempPermission.AlreadyRemoved = true;
                    });
                }
            }
            else if (args[0] == CommandGroup)
            {
                string groupName = args[1];
                if (!GroupExists_Helper(groupName) || !_storedData.GroupsList.TryGetValue(groupName, out var groupData)) return;
                var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    NextTick(() =>
                    {
                        if (!GroupHasPermission_Helper(groupName, perm))
                            tempPermission.AlreadyRemoved = true;
                    });
                }
            }
        }
        
        private void CommandUserGroup(string[] args)
        {
            if (args.Length < 4 || !TryGetPlayer(args[1], out var player) || !GroupExists_Helper(args[2])) return;
            string groupName = args[2];
            if (args[0] == CommandAdd)
            {
                //             0     1     2             3                     4 - optional
                //o.usergroup add iiiaka admin wipe/seconds/DATETIME (true/false)/DATETIME
                NextTick(() =>
                {
                    if (player.BelongsToGroup(groupName))
                    {
                        var playerData = CreateOrGetPlayerData(player.Id, player.Name);
                        if (args[3] == Str_Wipe)
                            AddTemporaryGroup(playerData, groupName, false);
                        else if (int.TryParse(args[3], out var secondsToAdd))
                            AddTemporaryGroup(playerData, groupName, secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false, false);
                        else if (DateTime.TryParse(args[3], out var expireDate))
                            AddTemporaryGroup(playerData, groupName, expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default, false);
                    }
                });
            }
            else if (args[0] == CommandRemove && _storedData.PlayersList.TryGetValue(player.Id, out var playerData))
            {
                //               0      1     2
                //o.usergroup remove iiiaka admin
                var tempPermission = playerData.GroupsList.FirstOrDefault(p => p.Name == groupName);
                if (tempPermission != null && !tempPermission.AlreadyRemoved)
                {
                    NextTick(() =>
                    {
                        if (!player.BelongsToGroup(groupName))
                            tempPermission.AlreadyRemoved = true;
                    });
                }
            }
        }
        
        private static PlayerData CreateOrGetPlayerData(string userID, string name)
        {
            if (!_storedData.PlayersList.TryGetValue(userID, out var playerData))
                _storedData.PlayersList[userID] = playerData = new PlayerData(userID, name);
            return playerData;
        }
        
        private static GroupData CreateOrGetGroupData(string groupName)
        {
            if (!_storedData.GroupsList.TryGetValue(groupName, out var groupData))
                _storedData.GroupsList[groupName] = groupData = new GroupData(groupName);
            return groupData;
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
        
        private bool UserHasPermission_Helper(string userID, string perm) => permission.GetUserData(userID).Perms.Contains<string>(perm, StringComparer.OrdinalIgnoreCase);
        private bool UserHasGroup_Helper(string userID, string groupName) => permission.GetUserData(userID).Groups.Contains<string>(groupName, StringComparer.OrdinalIgnoreCase);
        
        private bool GroupHasPermission_Helper(string groupName, string perm)
        {
            var group = permission.GetGroupData(groupName);
            return group != null && group.Perms.Contains<string>(perm, StringComparer.OrdinalIgnoreCase);
        }
        
        private bool GroupExists_Helper(string groupName) => permission.GetGroupData(groupName) != null;
        #endregion

        #region ~Local~
        private void GrantTemporaryPermission(PlayerData playerData, string perm, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantUserPermission(playerData.UserID, perm, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < playerData.PermissionsList.Count; i++)
            {
                tempPermission = playerData.PermissionsList[i];
                if (tempPermission.Name == perm)
                {
                    tempPermission.ExpireDate = default;
                    tempPermission.UntilWipe = true;
                    tempPermission.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_UserPermissionUpdated, playerData.UserID, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission has been extended: {tempPermission.Name} until the wipe");
                    return;
                }
            }
            tempPermission = new TemporaryPermission(perm);
            playerData.PermissionsList.Add(tempPermission);
            Interface.CallHook(Hooks_UserPermissionGranted, playerData.UserID, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission has been granted: {tempPermission.Name} until the wipe");
        }
        
        private void GrantTemporaryPermission(PlayerData playerData, string perm, int secondsToAdd, bool fromNow, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantUserPermission(playerData.UserID, perm, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < playerData.PermissionsList.Count; i++)
            {
                tempPermission = playerData.PermissionsList[i];
                if (tempPermission.Name == perm)
                {
                    tempPermission.ExpireDate = fromNow || tempPermission.ExpireDate == default ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempPermission.ExpireDate.AddSeconds(secondsToAdd);
                    tempPermission.UntilWipe = false;
                    tempPermission.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_UserPermissionUpdated, playerData.UserID, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission has been extended: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
                    return;
                }
            }
            tempPermission = new TemporaryPermission(perm, secondsToAdd);
            playerData.PermissionsList.Add(tempPermission);
            Interface.CallHook(Hooks_UserPermissionGranted, playerData.UserID, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission has been granted: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
        }
        
        private void GrantTemporaryPermission(PlayerData playerData, string perm, DateTime expireDate, DateTime assignedDate, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantUserPermission(playerData.UserID, perm, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < playerData.PermissionsList.Count; i++)
            {
                tempPermission = playerData.PermissionsList[i];
                if (tempPermission.Name == perm)
                {
                    if (assignedDate != default)
                        tempPermission.AssignedDate = assignedDate;
                    tempPermission.ExpireDate = expireDate;
                    tempPermission.UntilWipe = false;
                    tempPermission.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_UserPermissionUpdated, playerData.UserID, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission has been extended: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
                    return;
                }
            }
            tempPermission = new TemporaryPermission(perm, expireDate, assignedDate);
            playerData.PermissionsList.Add(tempPermission);
            Interface.CallHook(Hooks_UserPermissionGranted, playerData.UserID, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{playerData.DisplayName} ({playerData.UserID}) - Permission has been granted: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
        }
        
        private void AddTemporaryGroup(PlayerData playerData, string groupName, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.AddUserGroup(playerData.UserID, groupName);
            TemporaryPermission tempGroup;
            for (int i = 0; i < playerData.GroupsList.Count; i++)
            {
                tempGroup = playerData.GroupsList[i];
                if (tempGroup.Name == groupName)
                {
                    tempGroup.ExpireDate = default;
                    tempGroup.UntilWipe = true;
                    tempGroup.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_UserGroupUpdated, playerData.UserID, groupName, tempGroup.ExpireDate, tempGroup.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{playerData.DisplayName} ({playerData.UserID}) - Extended time for participating in group: {tempGroup.Name} until the wipe");
                    return;
                }
            }
            tempGroup = new TemporaryPermission(groupName);
            playerData.GroupsList.Add(tempGroup);
            Interface.CallHook(Hooks_UserGroupAdded, playerData.UserID, groupName, tempGroup.ExpireDate, tempGroup.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{playerData.DisplayName} ({playerData.UserID}) - Added to group: {tempGroup.Name} until the wipe");
        }
        
        private void AddTemporaryGroup(PlayerData playerData, string groupName, int secondsToAdd, bool fromNow, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.AddUserGroup(playerData.UserID, groupName);
            TemporaryPermission tempGroup;
            for (int i = 0; i < playerData.GroupsList.Count; i++)
            {
                tempGroup = playerData.GroupsList[i];
                if (tempGroup.Name == groupName)
                {
                    tempGroup.ExpireDate = fromNow || tempGroup.ExpireDate == default ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempGroup.ExpireDate.AddSeconds(secondsToAdd);
                    tempGroup.UntilWipe = false;
                    tempGroup.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_UserGroupUpdated, playerData.UserID, groupName, tempGroup.ExpireDate, tempGroup.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{playerData.DisplayName} ({playerData.UserID}) - Extended time for participating in group: {tempGroup.Name} until {tempGroup.ExpireDate.ToString(TimeFormat)}(UTC)");
                    return;
                }
            }
            tempGroup = new TemporaryPermission(groupName, secondsToAdd);
            playerData.GroupsList.Add(tempGroup);
            Interface.CallHook(Hooks_UserGroupAdded, playerData.UserID, groupName, tempGroup.ExpireDate, tempGroup.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{playerData.DisplayName} ({playerData.UserID}) - Added to group: {tempGroup.Name} until {tempGroup.ExpireDate.ToString(TimeFormat)}(UTC)");
        }
        
        private void AddTemporaryGroup(PlayerData playerData, string groupName, DateTime expireDate, DateTime assignedDate, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.AddUserGroup(playerData.UserID, groupName);
            TemporaryPermission tempGroup;
            for (int i = 0; i < playerData.GroupsList.Count; i++)
            {
                tempGroup = playerData.GroupsList[i];
                if (tempGroup.Name == groupName)
                {
                    if (assignedDate != default)
                        tempGroup.AssignedDate = assignedDate;
                    tempGroup.ExpireDate = expireDate;
                    tempGroup.UntilWipe = false;
                    tempGroup.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_UserGroupUpdated, playerData.UserID, groupName, tempGroup.ExpireDate, tempGroup.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{playerData.DisplayName} ({playerData.UserID}) - Extended time for participating in group: {tempGroup.Name} until {tempGroup.ExpireDate.ToString(TimeFormat)}(UTC)");
                    return;
                }
            }
            tempGroup = new TemporaryPermission(groupName, expireDate, assignedDate);
            playerData.GroupsList.Add(tempGroup);
            Interface.CallHook(Hooks_UserGroupAdded, playerData.UserID, groupName, tempGroup.ExpireDate, tempGroup.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{playerData.DisplayName} ({playerData.UserID}) - Added to group: {tempGroup.Name} until {tempGroup.ExpireDate.ToString(TimeFormat)}(UTC)");
        }
        
        private void GrantTemporaryPermission(GroupData groupData, string perm, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantGroupPermission(groupData.GroupName, perm, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < groupData.PermissionsList.Count; i++)
            {
                tempPermission = groupData.PermissionsList[i];
                if (tempPermission.Name == perm)
                {
                    tempPermission.ExpireDate = default;
                    tempPermission.UntilWipe = true;
                    tempPermission.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_GroupPermissionUpdated, groupData.GroupName, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{groupData.GroupName} (group) - Permission has been extended: {tempPermission.Name} until the wipe");
                    return;
                }
            }
            tempPermission = new TemporaryPermission(perm);
            groupData.PermissionsList.Add(tempPermission);
            Interface.CallHook(Hooks_GroupPermissionGranted, groupData.GroupName, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{groupData.GroupName} (group) - Permission has been granted: {tempPermission.Name} until the wipe");
        }
        
        private void GrantTemporaryPermission(GroupData groupData, string perm, int secondsToAdd, bool fromNow, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantGroupPermission(groupData.GroupName, perm, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < groupData.PermissionsList.Count; i++)
            {
                tempPermission = groupData.PermissionsList[i];
                if (tempPermission.Name == perm)
                {
                    tempPermission.ExpireDate = fromNow || tempPermission.ExpireDate == default ? DateTime.UtcNow.AddSeconds(secondsToAdd) : tempPermission.ExpireDate.AddSeconds(secondsToAdd);
                    tempPermission.UntilWipe = false;
                    tempPermission.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_GroupPermissionUpdated, groupData.GroupName, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{groupData.GroupName} (group) - Permission has been extended: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
                    return;
                }
            }
            tempPermission = new TemporaryPermission(perm, secondsToAdd);
            groupData.PermissionsList.Add(tempPermission);
            Interface.CallHook(Hooks_GroupPermissionGranted, groupData.GroupName, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{groupData.GroupName} (group) - Permission has been granted: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
        }
        
        private void GrantTemporaryPermission(GroupData groupData, string perm, DateTime expireDate, DateTime assignedDate, bool shouldGrant = true)
        {
            if (shouldGrant)
                permission.GrantGroupPermission(groupData.GroupName, perm, null);
            TemporaryPermission tempPermission;
            for (int i = 0; i < groupData.PermissionsList.Count; i++)
            {
                tempPermission = groupData.PermissionsList[i];
                if (tempPermission.Name == perm)
                {
                    if (assignedDate != default)
                        tempPermission.AssignedDate = assignedDate;
                    tempPermission.ExpireDate = expireDate;
                    tempPermission.UntilWipe = false;
                    tempPermission.AlreadyRemoved = false;
                    Interface.CallHook(Hooks_GroupPermissionUpdated, groupData.GroupName, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
                    if (_config.ConsoleLog)
                        Puts($"{groupData.GroupName} (group) - Permission has been extended: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
                    return;
                }
            }
            tempPermission = new TemporaryPermission(perm, expireDate, assignedDate);
            groupData.PermissionsList.Add(tempPermission);
            Interface.CallHook(Hooks_GroupPermissionGranted, groupData.GroupName, perm, tempPermission.ExpireDate, tempPermission.AssignedDate);
            if (_config.ConsoleLog)
                Puts($"{groupData.GroupName} (group) - Permission has been granted: {tempPermission.Name} until {tempPermission.ExpireDate.ToString(TimeFormat)}(UTC)");
        }
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
                if (secondsToAdd < 1)
                    GrantTemporaryPermission(CreateOrGetPlayerData(player.Id, player.Name), perm);
                else
                    GrantTemporaryPermission(CreateOrGetPlayerData(player.Id, player.Name), perm, secondsToAdd, fromNow);
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
                GrantTemporaryPermission(CreateOrGetPlayerData(player.Id, player.Name), perm, expireDate, assignedDate);
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
                    if (!UserHasPermission_Helper(player.Id, perm))
                        permission.GrantUserPermission(playerData.UserID, perm, null);
                    return true;
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
        
        private Dictionary<string, Dictionary<string, DateTime[]>> ActiveUsersGetAllPermissions()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            foreach (var player in covalence.Players.Connected)
            {
                if (_storedData.PlayersList.TryGetValue(player.Id, out var playerData))
                {
                    var perms = new Dictionary<string, DateTime[]>();
                    foreach (var tempPermission in playerData.PermissionsList)
                        perms[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
                    if (perms.Any())
                        result[playerData.UserID] = perms;
                }
            }
            return result;
        }
        
        private Dictionary<string, Dictionary<string, DateTime[]>> AllUsersGetAllPermissions()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            foreach (var playerData in _storedData.PlayersList.Values)
            {
                var perms = new Dictionary<string, DateTime[]>();
                foreach (var tempPermission in playerData.PermissionsList)
                    perms[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
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
            if (player != null && (!checkExistence || permission.GroupExists(groupName)))
            {
                if (secondsToAdd < 1)
                    AddTemporaryGroup(CreateOrGetPlayerData(player.Id, player.Name), groupName);
                else
                    AddTemporaryGroup(CreateOrGetPlayerData(player.Id, player.Name), groupName, secondsToAdd, fromNow);
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
                AddTemporaryGroup(CreateOrGetPlayerData(player.Id, player.Name), groupName, expireDate, assignedDate);
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
                    if (!UserHasGroup_Helper(player.Id, groupName))
                        permission.AddUserGroup(playerData.UserID, groupName);
                    return true;
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
        
        private Dictionary<string, Dictionary<string, DateTime[]>> ActiveUsersGetAllGroups()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            foreach (var player in covalence.Players.Connected)
            {
                if (_storedData.PlayersList.TryGetValue(player.Id, out var playerData))
                {
                    var groups = new Dictionary<string, DateTime[]>();
                    foreach (var tempGroup in playerData.GroupsList)
                        groups[tempGroup.Name] = new DateTime[2] { tempGroup.AssignedDate, tempGroup.ExpireDate };
                    if (groups.Any())
                        result[playerData.UserID] = groups;
                }
            }
            return result;
        }
        
        private Dictionary<string, Dictionary<string, DateTime[]>> AllUsersGetAllGroups()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            foreach (var playerData in _storedData.PlayersList.Values)
            {
                var groups = new Dictionary<string, DateTime[]>();
                foreach (var tempGroup in playerData.GroupsList)
                    groups[tempGroup.Name] = new DateTime[2] { tempGroup.AssignedDate, tempGroup.ExpireDate };
                if (groups.Any())
                    result[playerData.UserID] = groups;
            }
            return result;
        }
        #endregion

        #region ~API - Group's Permissions~
        private bool GrantGroupPermission(string groupName, string perm, int secondsToAdd, bool fromNow = false, bool checkExistence = true)
        {
            if (!checkExistence || (permission.GroupExists(groupName) && permission.PermissionExists(perm)))
            {
                if (secondsToAdd < 1)
                    GrantTemporaryPermission(CreateOrGetGroupData(groupName), perm);
                else
                    GrantTemporaryPermission(CreateOrGetGroupData(groupName), perm, secondsToAdd, fromNow);
                return true;
            }
            return false;
        }
        
        private bool GrantGroupPermission(string groupName, string perm, DateTime expireDate, DateTime assignedDate = default, bool checkExistence = true)
        {
            if (!checkExistence || (permission.GroupExists(groupName) && permission.PermissionExists(perm)))
            {
                GrantTemporaryPermission(CreateOrGetGroupData(groupName), perm, expireDate, assignedDate);
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
                    if (!GroupHasPermission_Helper(groupName, perm))
                        permission.GrantGroupPermission(groupData.GroupName, perm, null);
                    return true;
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
        
        private Dictionary<string, Dictionary<string, DateTime[]>> AllGroupsGetAllPermissions()
        {
            var result = new Dictionary<string, Dictionary<string, DateTime[]>>();
            foreach (var groupName in _storedData.GroupsList.Values)
            {
                var perms = new Dictionary<string, DateTime[]>();
                foreach (var tempPermission in groupName.PermissionsList)
                    perms[tempPermission.Name] = new DateTime[2] { tempPermission.AssignedDate, tempPermission.ExpireDate };
                if (perms.Any())
                    result[groupName.GroupName] = perms;
            }
            return result;
        }
        #endregion

        #region ~Oxide Hooks~
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
        
        void OnUserPermissionRevoked(string userID, string perm)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                var tempPermission = playerData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null)
                    tempPermission.AlreadyRemoved = true;
            }
        }
        
        void OnUserGroupRemoved(string userID, string groupName)
        {
            if (_storedData.PlayersList.TryGetValue(userID, out var playerData))
            {
                var tempPermission = playerData.GroupsList.FirstOrDefault(p => p.Name == groupName);
                if (tempPermission != null)
                    tempPermission.AlreadyRemoved = true;
            }
        }
        
        void OnGroupPermissionRevoked(string groupName, string perm)
        {
            if (_storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                var tempPermission = groupData.PermissionsList.FirstOrDefault(p => p.Name == perm);
                if (tempPermission != null)
                    tempPermission.AlreadyRemoved = true;
            }
        }
        
        void OnGroupDeleted(string groupName)
        {
            if (_storedData.GroupsList.TryGetValue(groupName, out var groupData))
            {
                foreach (var tempPermission in groupData.PermissionsList)
                    tempPermission.AlreadyRemoved = true;
            }
        }
        
        void Init()
        {
            Unsubscribe(nameof(OnServerCommand));
            Unsubscribe(nameof(OnUserPermissionRevoked));
            Unsubscribe(nameof(OnUserGroupRemoved));
            Unsubscribe(nameof(OnGroupPermissionRevoked));
            Unsubscribe(nameof(OnGroupDeleted));
            Unsubscribe(nameof(OnServerSave));
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
                        //        0     1     2        3                4                     5 - optional
                        //tperm grant user iiiaka realpve.vip wipe/seconds/DATETIME (true/false)/DATETIME
                        var targetData = CreateOrGetPlayerData(target.Id, target.Name);
                        if (args[4] == Str_Wipe)
                            GrantTemporaryPermission(targetData, args[3]);
                        else if (int.TryParse(args[4], out var secondsToAdd))
                            GrantTemporaryPermission(targetData, args[3], secondsToAdd, args.Length > 5 && bool.TryParse(args[5], out var fromNow) ? fromNow : false);
                        else if (DateTime.TryParse(args[4], out var expireDate))
                            GrantTemporaryPermission(targetData, args[3], expireDate, args.Length > 5 && DateTime.TryParse(args[5], out var assignedDate) ? assignedDate : default);
                        else
                        {
                            replyKey = "MsgGrantWrongFormat";
                            goto exit;
                        }
                        replyKey = "MsgUserGranted";
                        replyArgs[0] = args[3];
                        replyArgs[1] = target.Name;
                    }
                }
                else if (args[1] == "group")
                {
                    if (!GroupExists_Helper(args[2]))
                        replyKey = "MsgGroupNotFound";
                    else
                    {
                        //        0     1    2       3                4                     5 - optional
                        //tperm grant group vip realpve.vip wipe/seconds/DATETIME (true/false)/DATETIME
                        var groupData = CreateOrGetGroupData(args[2]);
                        if (args[4] == Str_Wipe)
                            GrantTemporaryPermission(groupData, args[3]);
                        else if (int.TryParse(args[4], out var secondsToAdd))
                            GrantTemporaryPermission(groupData, args[3], secondsToAdd, args.Length > 5 && bool.TryParse(args[5], out var fromNow) ? fromNow : false);
                        else if (DateTime.TryParse(args[4], out var expireDate))
                            GrantTemporaryPermission(groupData, args[3], expireDate, args.Length > 5 && DateTime.TryParse(args[5], out var assignedDate) ? assignedDate : default);
                        else
                        {
                            replyKey = "MsgGrantWrongFormat";
                            goto exit;
                        }
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
                    if (!GroupExists_Helper(args[2]))
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
                else if (!GroupExists_Helper(args[2]))
                    replyKey = "MsgGroupNotFound";
                else
                {
                    //       0     1    2            3                     4 - optional
                    //tperm add iiiaka vip wipe/seconds/DATETIME (true/false)/DATETIME
                    var targetData = CreateOrGetPlayerData(target.Id, target.Name);
                    if (args[3] == Str_Wipe)
                        AddTemporaryGroup(targetData, args[2]);
                    else if (int.TryParse(args[3], out var secondsToAdd))
                        AddTemporaryGroup(targetData, args[2], secondsToAdd, args.Length > 4 && bool.TryParse(args[4], out var fromNow) ? fromNow : false);
                    else if (DateTime.TryParse(args[3], out var expireDate))
                        AddTemporaryGroup(targetData, args[2], expireDate, args.Length > 4 && DateTime.TryParse(args[4], out var assignedDate) ? assignedDate : default);
                    else
                    {
                        replyKey = "MsgUserGroupWrongFormat";
                        goto exit;
                    }
                    replyKey = "MsgUserGroupAdded";
                    replyArgs[0] = target.Name;
                    replyArgs[1] = args[2];
                }
            }
            else if (args[0] == "remove")
            {
                if (!TryGetPlayer(args[1], out var target))
                    replyKey = "MsgPlayerNotFound";
                else if (!GroupExists_Helper(args[2]))
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
            Unsubscribe(nameof(OnServerSave));
            if (_expirationTimer != null)
                _expirationTimer.Destroy();
            if (_presenceTimer != null)
                _presenceTimer.Destroy();
            if (_config.RemoveOnUnload)
            {
                foreach (var playerData in _storedData.PlayersList.Values)
                {
                    foreach (var tempPerm in playerData.PermissionsList)
                        permission.RevokeUserPermission(playerData.UserID, tempPerm.Name);
                    foreach (var tempGroup in playerData.GroupsList)
                        permission.RemoveUserGroup(playerData.UserID, tempGroup.Name);
                }
                foreach (var groupData in _storedData.GroupsList.Values)
                {
                    foreach (var tempPerm in groupData.PermissionsList)
                        permission.RevokeGroupPermission(groupData.GroupName, tempPerm.Name);
                }
            }
            OnServerSave();
            _storedData = null;
            _config = null;
        }
        #endregion
        
        #region ~Classes~
        public class PlayerData
        {
            [JsonProperty(PropertyName = "User Id. Do not modify this property")]
            public string UserID { get; private set; } = string.Empty;
            
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
            [JsonProperty(PropertyName = "Group Name. Do not modify this property")]
            public string GroupName { get; private set; } = string.Empty;
            
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
            
            public override int GetHashCode() => Name.GetHashCode();
        }
        #endregion
    }
}