﻿using CommandSystem;
using Exiled.Permissions.Extensions;
using System;

namespace SCPUtils.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    internal class SetRoundBan : ICommand
    {
        public string Command { get; } = "scputils_set_round_ban";

        public string[] Aliases { get; } = new[] { "srb", "roundban", "su_srb", "su_roundban" };

        public string Description { get; } = "Sets the number of round ban to one player";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (ScpUtils.StaticInstance.Functions.CheckCommandCooldown(sender) == true)
            {
                response = ScpUtils.StaticInstance.Config.CooldownMessage;
                return false;
            }

            string target;

            if (!sender.CheckPermission("scputils.roundban"))
            {
                response = "<color=red> You need a higher administration level to use this command!</color>";
                return false;


            }

            if (arguments.Count < 2)
            {
                response = $"<color=yellow>Usage: {Command} <player name/id> <Round bans to set> </color>";
                return false;
            }
            else target = arguments.Array[1].ToString();


            Player databasePlayer = target.GetDatabasePlayer();

            if (databasePlayer == null)
            {
                response = "Player not found on Database or Player is loading data!";
                return false;
            }

            if (int.TryParse(arguments.Array[2].ToString(), out int rounds))
            {
                databasePlayer.RoundBanLeft = rounds;
                databasePlayer.SaveData();
                response = "Success!";
                return true;
            }

            else
            {
                response = "Number of rounds must be an integer!";
                return false;
            }
        }
    }
}
