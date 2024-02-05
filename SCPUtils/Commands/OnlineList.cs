﻿using CommandSystem;
using Exiled.API.Features.Roles;
using Exiled.Permissions.Extensions;
using System;
using System.Text;

namespace SCPUtils.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    internal class OnlineList : ICommand
    {
        public string Command { get; } = ScpUtils.StaticInstance.Translation.OnlinelistCommand;

        public string[] Aliases { get; } = ScpUtils.StaticInstance.Translation.OnlinelistAliases;

        public string Description { get; } = ScpUtils.StaticInstance.Translation.OnlinelistDescription;


        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (ScpUtils.StaticInstance.Functions.CheckCommandCooldown(sender) == true)
            {
                response = ScpUtils.StaticInstance.Config.CooldownMessage;
                return false;
            }

            if (!sender.CheckPermission("scputils.onlinelist.basic"))
            {
                response = ScpUtils.StaticInstance.Translation.NoPermissions;
                return false;
            }
            StringBuilder message = new StringBuilder($"Online Players ({Exiled.API.Features.Player.Dictionary.Count})");

            foreach (Exiled.API.Features.Player player in Exiled.API.Features.Player.List)
            {
                message.AppendLine();
                message.Append($"({player.Id}) {player.Nickname}");

                if (sender.CheckPermission("scputils.onlinelist.userid"))
                {
                    message.Append($" ({player.UserId})");
                }

                if (sender.CheckPermission("scputils.onlinelist.badge") && player.Group?.BadgeText != null)
                {
                    message.Append($" [{player.Group.BadgeText}]");
                }

                if (sender.CheckPermission("scputils.onlinelist.role"))
                {
                    message.Append($" [{player.Role.Type}]");
                }

                if (sender.CheckPermission("scputils.onlinelist.health"))
                {
                    message.Append($" [HP {player.Health} / {player.MaxHealth}]");
                }

                if (sender.CheckPermission("scputils.onlinelist.flags"))
                {
                    if (player.IsOverwatchEnabled)
                    {
                        message.Append(" [OVERWATCH]");
                    }

                    if (player.Role.Is(out FpcRole role))
                    {
                        if (role.IsNoclipEnabled)
                        {
                            message.Append(" [NOCLIP]");
                        }
                    }
                    else
                    {
                        message.Append(" [NOT-FPCROLE]");
                    }

                    if (player.IsGodModeEnabled)
                    {
                        message.Append(" [GODMODE]");
                    }

                    if (player.IsStaffBypassEnabled)
                    {
                        message.Append(" [BYPASS MODE]");
                    }

                    if (player.IsIntercomMuted)
                    {
                        message.Append(" [INTERCOM MUTED]");
                    }

                    if (player.IsMuted)
                    {
                        message.Append(" [SERVER MUTED]");
                    }

                    if (player.DoNotTrack)
                    {
                        message.Append(" [DO NOT TRACK]");
                    }

                    if (player.RemoteAdminAccess)
                    {
                        message.Append(" [RA]");
                    }
                }
            }
            if (Exiled.API.Features.Player.Dictionary.Count == 0)
            {
                response = ScpUtils.StaticInstance.Translation.NoPlayers;
                return true;
            }
            response = message.ToString();
            return true;
        }

    }
}
