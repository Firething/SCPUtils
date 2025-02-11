﻿using CommandSystem;
using Exiled.Permissions.Extensions;
using System;
using System.Text;

namespace SCPUtils.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    public class SearchIp : ICommand
    {
        public string Command { get; } = ScpUtils.StaticInstance.Translation.SearchipCommand;

        public string[] Aliases { get; } = ScpUtils.StaticInstance.Translation.SearchipAliases;

        public string Description { get; } = ScpUtils.StaticInstance.Translation.SearchipDescription;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (ScpUtils.StaticInstance.Functions.CheckCommandCooldown(sender) == true)
            {
                response = ScpUtils.StaticInstance.Config.CooldownMessage;
                return false;
            }

            if (!sender.CheckPermission("scputils.dupeip"))
            {
                response = ScpUtils.StaticInstance.Translation.NoPermissions;
                return false;
            }
            if (arguments.Count != 1)
            {
                response = $"<color=yellow>{ScpUtils.StaticInstance.Translation.Usage} {Command} {ScpUtils.StaticInstance.Translation.ArgPlayer}</color>";
                return false;
            }

            string ip = arguments.Array[1];
            var databaseIp = GetIp.GetIpAddress(ip);
            if (databaseIp == null)
            {
                response = ScpUtils.StaticInstance.Translation.InvalidIp;
                return false;
            }

            StringBuilder message = new StringBuilder($"<color=green>[Accounts associated with the same IP: {ip}]</color>").AppendLine();
            foreach (var userId in databaseIp.UserIds)
            {
                //Log.Info(userId);
                var databasePlayer2 = DatabasePlayer.GetDatabasePlayer(userId);

                if (databasePlayer2 != null)
                {

                    message.AppendLine();
                    message.Append(
                            $"Player: <color=yellow>{databasePlayer2.Name} ({databasePlayer2.Id}@{databasePlayer2.Authentication})</color>\nFirst Join: <color=yellow>{databasePlayer2.FirstJoin}</color>\nLast seen: <color=yellow>{databasePlayer2.LastSeen}</color>\nIsRestricted: <color=yellow>{databasePlayer2.IsRestricted()}</color>\nIsBanned: <color=yellow>{databasePlayer2.IsBanned()}</color>\nMuted: <color=yellow>{VoiceChat.VoiceChatMutes.QueryLocalMute(userId)}</color>\nTotal played as SCP: <color=yellow>{databasePlayer2.TotalScpGamesPlayed}</color>\nTotal suicide: <color=yellow>{databasePlayer2.ScpSuicideCount}</color>\nRound(s) ban left: <color=yellow>{databasePlayer2.RoundBanLeft}</color>")
                        .AppendLine();

                }
            }
            response = message.ToString();
            return true;
        }
    }
}