﻿namespace SCPUtils
{
    using CommandSystem;
    using MEC;
    using SCPUtils.Events;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using PluginAPI.Core;

    public class Function : EventArgs
    {
        public CoroutineHandle RS;
        public int i = 0;
        private readonly ScpUtils pluginInstance;

        public Function(ScpUtils pluginInstance)
        {
            this.pluginInstance = pluginInstance;
        }

        public void CoroutineRestart()
        {
            TimeSpan timeParts = TimeSpan.Parse(pluginInstance.configs.AutoRestartTimeTask);
            double timeCalc;
            timeCalc = (timeParts - DateTime.Now.TimeOfDay).TotalSeconds;
            if (timeCalc <= 0)
            {
                timeCalc += 86400;
            }

            RS = Timing.RunCoroutine(Restarter((float)timeCalc), Segment.FixedUpdate);
        }

        private IEnumerator<float> Restarter(float second)
        {
            yield return Timing.WaitForSeconds(second);
            Log.Info("Warning: Server is auto-restarting");
            Server.Restart();
        }

        public Dictionary<string, DateTime> LastWarn { get; private set; } = new Dictionary<string, DateTime>();

        public void AutoRoundBanPlayer(PluginAPI.Core.Player player)
        {
            int rounds;
            Player databasePlayer = player.GetDatabasePlayer();
            databasePlayer.TotalScpSuicideBans++;
            databasePlayer.SuicidePunishment[databasePlayer.SuicidePunishment.Count() - 1] = "Round-Ban";
            if (pluginInstance.configs.MultiplyBanDurationEachBan == true)
            {
                rounds = databasePlayer.TotalScpSuicideBans * pluginInstance.configs.AutoBanRoundsCount;
            }
            else
            {
                rounds = pluginInstance.configs.AutoBanDuration;

            }
            if (pluginInstance.configs.BroadcastSanctions)
            {
                BroadcastSuicideQuitAction($"<color=blue><SCPUtils> {player.Nickname} ({player.Role}) has been <color=red>BANNED</color> from playing SCP for exceeding Quits / Suicides (as SCP) limit for {rounds} rounds.</color>");
                if (databasePlayer.RoundBanLeft >= 1) BroadcastSuicideQuitAction($"<color=blue><SCPUtils> {player.Nickname} has suicided while having an active ban!</color>");
            }
            databasePlayer.RoundsBan[databasePlayer.RoundsBan.Count() - 1] = rounds;
            databasePlayer.RoundBanLeft += rounds;
            if (pluginInstance.configs.RoundBanNotification.Show)
            {
                player.ClearBroadcasts();
                var message = pluginInstance.configs.RoundBanNotification.Content;
                message = message.Replace("%roundnumber%", databasePlayer.RoundBanLeft.ToString());
                player.SendBroadcast(message, pluginInstance.configs.WelcomeMessage.Duration, pluginInstance.configs.WelcomeMessage.Type, false);
            }

        }

        public void AutoBanPlayer(PluginAPI.Core.Player player)
        {
            int duration;
            Player databasePlayer = player.GetDatabasePlayer();
            databasePlayer.TotalScpSuicideBans++;
            databasePlayer.SuicidePunishment[databasePlayer.SuicidePunishment.Count() - 1] = "Ban";

            if (pluginInstance.configs.MultiplyBanDurationEachBan == true)
            {
                duration = databasePlayer.TotalScpSuicideBans * pluginInstance.configs.AutoBanDuration * 60;
            }
            else
            {
                duration = pluginInstance.configs.AutoBanDuration * 60;
            }

            if (pluginInstance.configs.BroadcastSanctions) BroadcastSuicideQuitAction(pluginInstance.configs.AutoBanPlayerMessage.Content.Replace("%player.Nickname%", player.Nickname).Replace("%player.Role%", player.Role.ToString()).Replace("%duration%", (duration / 60).ToString()));

            if (pluginInstance.configs.MultiplyBanDurationEachBan == true) databasePlayer.Expire[databasePlayer.Expire.Count() - 1] = DateTime.Now.AddMinutes((duration / 60) * databasePlayer.TotalScpSuicideBans);
            else databasePlayer.Expire[databasePlayer.Expire.Count() - 1] = DateTime.Now.AddMinutes(duration / 60);           
            player.Ban($"Auto-Ban: {string.Format(pluginInstance.configs.AutoBanMessage)}", duration);
        }

        public void AutoKickPlayer(PluginAPI.Core.Player player)
        {
            if (pluginInstance.configs.BroadcastSanctions)
            {
                BroadcastSuicideQuitAction($"<color=blue><SCPUtils> {player.Nickname} ({player.Role}) has been <color=red>KICKED</color> from the server for exceeding Quits / Suicides (as SCP) limit</color>");
            }

            Player databasePlayer = player.GetDatabasePlayer();
            databasePlayer.TotalScpSuicideKicks++;
            databasePlayer.SuicidePunishment[databasePlayer.SuicidePunishment.Count() - 1] = "Kick";
            player.Kick($"Auto-Kick: {pluginInstance.configs.SuicideKickMessage}");
        }

        public void AutoWarnPlayer(PluginAPI.Core.Player player)
        {
            if (pluginInstance.configs.BroadcastWarns)
            {
                BroadcastSuicideQuitAction($"<color=blue><SCPUtils> {player.Nickname} ({player.Role}) has been <color=red>WARNED</color> for Quitting or Suiciding as SCP</color>");
            }

            player.GetDatabasePlayer().ScpSuicideCount++;
            player.ClearBroadcasts();
            player.SendBroadcast(pluginInstance.configs.SuicideWarnMessage.Content, pluginInstance.configs.SuicideWarnMessage.Duration, pluginInstance.configs.SuicideWarnMessage.Type);
        }

        public void OnQuitOrSuicide(PluginAPI.Core.Player player)
        {
            if (!pluginInstance.configs.EnableSCPSuicideAutoWarn || pluginInstance.EventHandlers.KickedList.Contains(player) || EventHandlers.TemporarilyDisabledWarns)
            {
                return;
            }
            if (!LastWarn.ContainsKey(player.UserId))
            {
                LastWarn.Add(player.UserId, DateTime.MinValue);
            }
            else if (LastWarn[player.UserId] >= DateTime.Now)
            {
                return;
            }
            Player databasePlayer = player.GetDatabasePlayer();
            float suicidePercentage = databasePlayer.SuicidePercentage;
            databasePlayer.SuicidePunishment[databasePlayer.SuicidePunishment.Count() - 1] = "Warn";
            AutoWarnPlayer(player);
            if (pluginInstance.configs.EnableSCPSuicideAutoBan && suicidePercentage >= pluginInstance.configs.AutoBanThreshold && player.GetDatabasePlayer().TotalScpGamesPlayed > pluginInstance.configs.ScpSuicideTollerance)
            {
                AutoBanPlayer(player);
            }
            else if (pluginInstance.configs.EnableSCPSuicideSoftBan && suicidePercentage >= pluginInstance.configs.AutoBanThreshold && player.GetDatabasePlayer().TotalScpGamesPlayed > pluginInstance.configs.ScpSuicideTollerance)
            {
                AutoRoundBanPlayer(player);
            }
            else if (pluginInstance.configs.AutoKickOnSCPSuicide && suicidePercentage >= pluginInstance.configs.AutoKickThreshold && suicidePercentage < pluginInstance.configs.AutoBanThreshold && player.GetDatabasePlayer().TotalScpGamesPlayed > pluginInstance.configs.ScpSuicideTollerance)
            {
                AutoKickPlayer(player);
            }

            LastWarn[player.UserId] = DateTime.Now.AddSeconds(5);
        }

        public void PostLoadPlayer(PluginAPI.Core.Player player)
        {

            Player databasePlayer = player.GetDatabasePlayer();

            if (!string.IsNullOrEmpty(databasePlayer.BadgeName))
            {
                UserGroup group = ServerStatic.GetPermissionsHandler()._groups[databasePlayer.BadgeName];


                if (databasePlayer.BadgeExpire >= DateTime.Now)
                {
                    player.ReferenceHub.serverRoles.SetGroup(group, false, true, true);
                    if (ServerStatic.PermissionsHandler._members.ContainsKey(player.UserId))
                    {
                        ServerStatic.PermissionsHandler._members.Remove(player.UserId);
                    }

                    ServerStatic.PermissionsHandler._members.Add(player.UserId, databasePlayer.BadgeName);
                    BadgeSetEvent args = new BadgeSetEvent();
                    args.Player = player;
                    args.NewBadgeName = databasePlayer.BadgeName;
                    pluginInstance.Events.OnBadgeSet(args);

                }
                else
                {

                    BadgeRemovedEvent args = new BadgeRemovedEvent();
                    args.Player = player;
                    args.BadgeName = databasePlayer.BadgeName;
                    databasePlayer.BadgeName = "";

                    if (ServerStatic.PermissionsHandler._members.ContainsKey(player.UserId))
                    {
                        ServerStatic.PermissionsHandler._members.Remove(player.UserId);
                    }
                    if (ServerStatic.RolesConfig.GetStringDictionary("Members").ContainsKey(player.UserId))                        
                    {                        
                        UserGroup previous = ServerStatic.GetPermissionsHandler()._groups[ServerStatic.RolesConfig.GetStringDictionary("Members")[player.UserId]];
                        ServerStatic.PermissionsHandler._members.Add(player.UserId, ServerStatic.RolesConfig.GetStringDictionary("Members")[player.UserId]);
                        player.ReferenceHub.serverRoles.SetGroup(previous, false, true, true);
                    }
                    pluginInstance.Events.OnBadgeRemoved(args);
                }
            }

            Timing.CallDelayed(1.5f, () =>
            {

                if (!string.IsNullOrEmpty(databasePlayer.ColorPreference) && databasePlayer.ColorPreference != "None")
                {
                    if (/*player.CheckPermission("scputils.changecolor") || player.CheckPermission("scputils.playersetcolor") ||*/ databasePlayer.KeepPreferences || pluginInstance.configs.KeepColorWithoutPermission)
                    {
                        player.ReferenceHub.serverRoles.SetColor(databasePlayer.ColorPreference);
                    }
                    else
                    {
                        databasePlayer.ColorPreference = "";
                    }
                }

                if (databasePlayer.HideBadge == true)
                {
                    if (/*player.CheckPermission("scputils.badgevisibility") ||*/ databasePlayer.KeepPreferences || pluginInstance.configs.KeepBadgeVisibilityWithoutPermission)
                    {
                        player.ReferenceHub.characterClassManager.UserCode_CmdRequestHideTag();
                    }
                    else
                    {
                        databasePlayer.HideBadge = false;
                    }
                }


                if (!string.IsNullOrEmpty(databasePlayer.CustomNickName) && databasePlayer.CustomNickName != "None")
                {
                    if (/*player.CheckPermission("scputils.changenickname") || player.CheckPermission("scputils.playersetname") ||*/ databasePlayer.KeepPreferences || pluginInstance.configs.KeepNameWithoutPermission)
                    {
                        player.DisplayNickname = databasePlayer.CustomNickName;
                    }
                    else
                    {
                        databasePlayer.CustomNickName = "";
                    }
                }

                if (pluginInstance.configs.AutoKickBannedNames && pluginInstance.Functions.CheckNickname(player.Nickname) /*&& !player.CheckPermission("scputils.bypassnickrestriction")*/)
                {
                    Timing.CallDelayed(2f, () =>
                    {
                        player.Kick("Auto-Kick: " + pluginInstance.configs.AutoKickBannedNameMessage);
                    });
                }

            });

            if (databasePlayer.UserNotified.Count() <= 0)
            {
                return;
            }

            if (databasePlayer.UserNotified[databasePlayer.UserNotified.Count() - 1] == false)
            {
                if (databasePlayer.SuicidePunishment[databasePlayer.UserNotified.Count() - 1] == "None")
                {
                    databasePlayer.UserNotified[databasePlayer.UserNotified.Count() - 1] = true;
                }
                else
                {
                    player.ClearBroadcasts();
                    player.SendBroadcast(pluginInstance.configs.OfflineWarnNotification.Content, pluginInstance.configs.OfflineWarnNotification.Duration);
                    databasePlayer.UserNotified[databasePlayer.UserNotified.Count() - 1] = true;
                }
            }

            SetCommandBan(player);

        }

        public bool CheckNickname(string name)
        {
            if (pluginInstance.configs.BannedNickNames == null)
            {
                return false;
            }

            foreach (string nickname in pluginInstance.configs.BannedNickNames)
            {
                if (Regex.Match(name.ToLower(), nickname.ToLower()).Success)
                {
                    return true;
                }
            }
            return false;
        }
        public void LogWarn(PluginAPI.Core.Player player, string suicidetype)
        {
            if (!Round.IsRoundStarted) return;
            Player databasePlayer = player.GetDatabasePlayer();
            FixBanTime(databasePlayer);
            databasePlayer.SuicideDate.Add(DateTime.Now);
            databasePlayer.SuicideType.Add(suicidetype);
            databasePlayer.SuicideScp.Add(player.Role.ToString());
            databasePlayer.Expire.Add(DateTime.Now);
            databasePlayer.RoundsBan.Add(0);
            databasePlayer.SuicidePunishment.Add("None");
            databasePlayer.LogStaffer.Add("SCPUtils");
            if (suicidetype == "Disconnect")
            {
                databasePlayer.UserNotified.Add(false);
            }
            else
            {
                databasePlayer.UserNotified.Add(true);
            }

        }
        public void SaveData(PluginAPI.Core.Player player)
        {
            if (player.Nickname != "Dedicated Server" && player != null && Database.PlayerData.ContainsKey(player))
            {
                if ((player.Team == PlayerRoles.Team.SCPs || (pluginInstance.configs.AreTutorialsSCP && player.Role == PlayerRoles.RoleTypeId.Tutorial)) && pluginInstance.configs.QuitEqualsSuicide && PluginAPI.Core.Round.IsRoundStarted)
                {
                    if (pluginInstance.configs.EnableSCPSuicideAutoWarn && pluginInstance.configs.QuitEqualsSuicide && !pluginInstance.EventHandlers.KickedList.Contains(player))
                    {
                        pluginInstance.Functions.OnQuitOrSuicide(player);
                    }
                }
                Player databasePlayer = player.GetDatabasePlayer();


                if (player.DoNotTrack && !pluginInstance.configs.IgnoreDntRequests && !pluginInstance.configs.DntIgnoreList.Contains(player.GetGroupName()) && !databasePlayer.IgnoreDNT)
                {
                    databasePlayer.PlayTimeRecords.Clear();
                    //   databasePlayer.PlaytimeSessionsLog.Clear();
                    databasePlayer.ResetPreferences();
                    databasePlayer.FirstJoin = DateTime.MinValue;
                    databasePlayer.LastSeen = DateTime.MinValue;
                }
                else if (!player.DoNotTrack)
                {
                    databasePlayer.SetCurrentDayPlayTime();
                }
                else
                {
                    databasePlayer.SetCurrentDayPlayTime();
                }

                if (!string.IsNullOrEmpty(databasePlayer.BadgeName))
                {
                    if (ServerStatic.PermissionsHandler._members.ContainsKey(player.UserId))
                    {
                        ServerStatic.PermissionsHandler._members.Remove(player.UserId);
                    }
                }

                databasePlayer.Ip = player.IpAddress;
                databasePlayer.SaveData();
                Database.PlayerData.Remove(player);
            }
            if (pluginInstance.EventHandlers.KickedList.Contains(player)) pluginInstance.EventHandlers.KickedList.Remove(player);
        }

        public void SavePlaytime(PluginAPI.Core.Player player)
        {
            if (player.Nickname != "Dedicated Server" && player != null && Database.PlayerData.ContainsKey(player))
            {
                Player databasePlayer = player.GetDatabasePlayer();
                databasePlayer.SetCurrentDayPlayTime();
                databasePlayer.LastSeen = DateTime.Now;
                databasePlayer.SaveData();
            }
        }


        private void BroadcastSuicideQuitAction(string text)
        {
            foreach (PluginAPI.Core.Player admin in PluginAPI.Core.Player.GetPlayers())
            {
                if (pluginInstance.configs.BroadcastSanctions)
                {
                    if (admin.ReferenceHub.serverRoles.RemoteAdmin)
                        if (admin.RemoteAdminAccess)
                        {
                            admin.SendBroadcast(text, 12, Broadcast.BroadcastFlags.AdminChat, false);
                        }
                }
            }
        }

        public void AdminMessage(string text)
        {
            foreach (PluginAPI.Core.Player admin in PluginAPI.Core.Player.GetPlayers())
            {
                if (admin.ReferenceHub.serverRoles.RemoteAdmin)
                    if (admin.RemoteAdminAccess)
                    {
                        admin.SendBroadcast(text, 15, Broadcast.BroadcastFlags.AdminChat, false);
                    }
            }
        }

        public bool IsTeamImmune(PluginAPI.Core.Player player, PluginAPI.Core.Player attacker)
        {
            if (pluginInstance.configs.CuffedImmunityPlayers[player.Team]?.Any() == true)
            {

                if (pluginInstance.configs.CuffedImmunityPlayers[player.Team].Contains(attacker.Team))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Log.Error($"Detected invalid setting on cuffed_immunity_players! Key: {player.Team}, List cannot be null!");
                return false;
            }

        }

        public bool CuffedCheck(PluginAPI.Core.Player player)
        {
            if (pluginInstance.configs.CuffedProtectedTeams?.Any() == true)
            {
                if (pluginInstance.configs.CuffedProtectedTeams.Contains(player.Team) && player.IsDisarmed)
                {
                    return true;
                }
                else if (!pluginInstance.configs.CuffedProtectedTeams.Contains(player.Team))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }


        public bool CheckSafeZones(PluginAPI.Core.Player player)
        {
            if (pluginInstance.configs.CuffedSafeZones == null)
            {
                Log.Error($"Detected invalid setting on cuffed_safe_zones! Key cannot be null!");
                return false;
            }

            else if (pluginInstance.configs.CuffedSafeZones[player.Team]?.Any() == true)
            {
                if (pluginInstance.configs.CuffedSafeZones[player.Team].Contains(player.Room.Zone))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            else
            {
                Log.Error($"Detected invalid setting on cuffed_safe_zones! Key: {player.Team}, List cannot be null!");
                return false;
            }

        }

        public bool CheckAsnPlayer(PluginAPI.Core.Player player)
        {
            Player databasePlayer = player.GetDatabasePlayer();
            if (pluginInstance.configs.ASNBlacklist.IsEmpty()) return false;

            if (pluginInstance.configs.ASNBlacklist.Contains(player.ReferenceHub.characterClassManager.Asn) && !databasePlayer.ASNWhitelisted) return true;
            else return false;
        }


        public void FixBanTime(Player databasePlayer)
        {
            if (databasePlayer.SuicideDate.Count() != databasePlayer.Expire.Count())
            {
                databasePlayer.Expire.Clear();
                for (var i = 0; i < databasePlayer.SuicideDate.Count(); i++)
                {
                    databasePlayer.Expire.Add(DateTime.MinValue);
                }
                databasePlayer.SaveData();
            }
        }

        public void FixBanRounds(SCPUtils.Player databasePlayer)
        {

            if (databasePlayer.SuicideDate.Count() != databasePlayer.SuicideDate.Count())
            {
                databasePlayer.RoundsBan.Clear();
                for (var i = 0; i < databasePlayer.SuicideDate.Count(); i++)
                {
                    databasePlayer.RoundsBan.Add(0);
                }
                databasePlayer.SaveData();
            }
        }

        public void ReplacePlayer(PluginAPI.Core.Player player)
        {
            Player databasePlayer = player.GetDatabasePlayer();


            var list = PluginAPI.Core.Player.GetPlayers();
            list.Remove(player);
            list.RemoveAll(x => x.IsSCP);
            list.RemoveAll(x => x.Role == PlayerRoles.RoleTypeId.Tutorial);
            if (list.Count() == 0)
            {
                Log.Info("[SCPUtils] Couldnt find a player to replace the banned one!");
                return;
            }
            var id = UnityEngine.Random.Range(0, list.Count - 1);
            var role = player.Role;
            ReplacePlayerEvent args = new ReplacePlayerEvent();
            args.BannedPlayer = player;
            args.ReplacedPlayer = list[id];
            args.ScpRole = player.Role;
            args.NormalRole = list[id].Role;
            player.SetRole(list[id].Role);            
            list[id].SetRole(role);
            pluginInstance.Events.OnReplacePlayerEvent(args);


            databasePlayer.RoundBanLeft--;
            if (pluginInstance.configs.RoundBanNotification.Show)
            {
                player.ClearBroadcasts();
                var message = pluginInstance.configs.RoundBanSpawnNotification.Content;
                message = message.Replace("%roundnumber%", databasePlayer.RoundBanLeft.ToString());
                player.SendBroadcast(message, pluginInstance.configs.RoundBanSpawnNotification.Duration, pluginInstance.configs.RoundBanSpawnNotification.Type, false);
            }

        }

        public void RandomScp(PluginAPI.Core.Player player, PlayerRoles.RoleTypeId role)
        {
            if (role == PlayerRoles.RoleTypeId.None) return;
            Player databasePlayer = player.GetDatabasePlayer();
            var list = PluginAPI.Core.Player.GetPlayers();
            list.RemoveAll(x => x.Role != PlayerRoles.RoleTypeId.ClassD);
            if (list.Count == 0)
            {
                RandomScp2(player, role);
                return;
            }

            var id = UnityEngine.Random.Range(0, list.Count - 1);

            Timing.CallDelayed(2f, () =>
            {
                if (list[id] != null)
                {
                    list[id].SetRole(role);
                }
                else RandomScp2(player, role);
            });
        }

        public void RandomScp2(PluginAPI.Core.Player player, PlayerRoles.RoleTypeId role)
        {
            Player databasePlayer = player.GetDatabasePlayer();
            var list = PluginAPI.Core.Player.GetPlayers();
            list.Remove(player);
            list.RemoveAll(x => x.IsSCP);
            list.RemoveAll(x => x.Role == PlayerRoles.RoleTypeId.Tutorial);
            if (list.Count == 0) return;
            var id = UnityEngine.Random.Range(0, list.Count - 1);

            if (list[id] != null)
            {
                list[id].SetRole(role);
            }

        }

        public void IpCheck(PluginAPI.Core.Player player)
        {
            var databaseIp = GetIp.GetIpAddress(player.IpAddress);
            if (databaseIp == null)
            {
                player.AddIp();
                databaseIp = GetIp.GetIpAddress(player.IpAddress);
            }

            if (!databaseIp.UserIds.Contains(player.UserId))
            {
                databaseIp.UserIds.Add(player.UserId);
                databaseIp.SaveIp();

            }
            if (pluginInstance.configs.ASNWhiteslistMultiAccount?.Any() ?? true)
            {
                if (player.GetDatabasePlayer().MultiAccountWhiteList) return;
                CheckIp(player);
                return;
            }
            if (!pluginInstance.configs.ASNWhiteslistMultiAccount.Contains(player.ReferenceHub.characterClassManager.Asn) && !player.GetDatabasePlayer().MultiAccountWhiteList) CheckIp(player);
        }


        public void CheckIp(PluginAPI.Core.Player player)
        {
            var databaseIp = GetIp.GetIpAddress(player.IpAddress);
            if (databaseIp.UserIds.Count() > 1)
            {
                MultiAccountEvent args = new MultiAccountEvent();
                args.Player = player;
                args.UserIds = databaseIp.UserIds;
                pluginInstance.Events.OnMultiAccountEvent(args);


                if (pluginInstance.configs.MultiAccountBroadcast)
                {

                    AdminMessage($"Multi-Account detected on {player.Nickname} - ID: {player.PlayerId} Number of accounts: {databaseIp.UserIds.Count()}");
                }


                foreach (var userId in databaseIp.UserIds)
                {
                    if (player.IsMuted) return;
                    if (VoiceChat.VoiceChatMutes.QueryLocalMute(userId))
                    {
                        //if (!string.Equals(ScpUtils.StaticInstance.configs.WebhookUrl, "None")) DiscordWebHook.Message(userId, player);
                        AdminMessage($"<color=red><size=25>Mute evasion detected on {player.Nickname} ID: {player.PlayerId} Userid of muted user: {userId}</size></color>");
                        if (pluginInstance.configs.AutoMute) player.Mute(false);
                    }

                }
            }
        }

        public bool CheckCommandCooldown(ICommandSender sender)
        {
            if (((CommandSender)sender).Nickname.Equals("SERVER CONSOLE") /*&& sender.CheckPermission("scputils.bypass")*/)
            {
                return false;
            }
            var player = PluginAPI.Core.Player.Get(((CommandSender)sender).SenderId);
            if (!pluginInstance.EventHandlers.LastCommand.ContainsKey(player))
            {
                pluginInstance.EventHandlers.LastCommand.Add(player, DateTime.Now.AddSeconds(pluginInstance.configs.CommandCooldownSeconds));
                return false;
            }
            else if (DateTime.Now <= pluginInstance.EventHandlers.LastCommand[player])
            {
                if (pluginInstance.configs.CommandAbuseReport)
                {
                    Log.Info($"[ABUSE-REPORT] {player.Nickname} - {player.UserId} tried to spam commands!");
                }
                return true;
            }
            else
            {
                pluginInstance.EventHandlers.LastCommand[player] = DateTime.Now.AddSeconds(pluginInstance.configs.CommandCooldownSeconds);
                return false;
            }
        }

        public void SetCommandBan(PluginAPI.Core.Player player)
        {
            var databasePlayer = player.GetDatabasePlayer();

            foreach (KeyValuePair<DateTime, string> a in databasePlayer.Restricted)
            {
                if (a.Key >= DateTime.Now)
                {
                    player.SendConsoleMessage($"You are banned from using commands until {a.Key} for the following reason {a.Value}", "red");
                    if (!pluginInstance.EventHandlers.LastCommand.ContainsKey(player))
                    {
                        pluginInstance.EventHandlers.LastCommand.Add(player, a.Key);
                        return;
                    }
                    else
                    {
                        pluginInstance.EventHandlers.LastCommand[player] = a.Key;
                        return;
                    }
                }
            }
        }

        public bool IsSuicide(PluginAPI.Core.Player player, PluginAPI.Core.Player attacker)
        {
            return (player.UserId == attacker.UserId);
        }

        /*public bool IsAuthorized(string badge, string permission)
        {
            if (pluginInstance.perms.PermissionsList[badge]?.Any() == true)
            {
                if (pluginInstance.perms.PermissionsList[badge].Contains(permission) || pluginInstance.perms.PermissionsList[badge].Contains("scputils.*") || badge == "SERVER CONSOLE")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Log.Error($"SCPUtils permissions error! Badge {badge} is not present in configs!");
                return false;
            }
        }*/

        public string GetGroupName(PluginAPI.Core.Player player)
        {
            if (ServerStatic.PermissionsHandler._members.TryGetValue(player.UserId, out string name))
            {
                return name;
            }
            else
            {
                return "none";
            }

        }

        /*     public void CheckPtStatus()
             {
                 if ((Features.Player.Dictionary.Count >= pluginInstance.configs.MinPlayersPtCount) && (!pluginInstance.EventHandlers.ptEnabled))
                 {
                     foreach (var player in Features.Player.List)
                     {
                         var databasePlayer = player.GetDatabasePlayer();
                         databasePlayer.LastSeen = DateTime.Now;
                     }
                     pluginInstance.EventHandlers.ptEnabled = true;
                 }
                 else if ((Features.Player.Dictionary.Count >= pluginInstance.configs.MinPlayersPtCount) && (pluginInstance.EventHandlers.ptEnabled))
                 {
                     foreach (var player in Features.Player.List)
                     {
                         pluginInstance.Functions.SavePlaytime(player);
                     }
                     pluginInstance.EventHandlers.ptEnabled = false;
                 }
             } */

    }
}
