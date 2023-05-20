﻿namespace SCPUtils.Commands.RemoteAdmin
{
    using CommandSystem;
    using System;

    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SCPUtilsCommand : ParentCommand, IUsageProvider
    {
        public SCPUtilsCommand() => LoadGeneratedCommands();

        public override string Command { get; } = "scputils";
        public override string[] Aliases { get; } = new[]
        {
            "scpu", "su"
        };
        public override string Description { get; } = "The most famous plugin that offers many additions to the servers.";

        public string[] Usage { get; } = new[]
        {
            "command"
        };

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new Announce.AnnounceCommand());
            RegisterCommand(new ASN.AsnCommand());
            RegisterCommand(new Badge.BadgeCommand());
            RegisterCommand(new Ip.IpCommand());
            RegisterCommand(new PlayTime.PlayTimeCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = ScpUtils.StaticInstance.commandTranslation.ParentCommands;
            foreach (ICommand command in AllCommands)
            {
                response = string.Concat(new string[]
                {
                    response,
                    "\n\n",
                    ScpUtils.StaticInstance.commandTranslation.CommandName+command.Command,
                    "\n",
                    ScpUtils.StaticInstance.commandTranslation.CommandDescription+command.Description,
                });
                if (command.Aliases != null && command.Aliases.Length != 0)
                {
                    response = response + "\n" + ScpUtils.StaticInstance.commandTranslation.CommandAliases + string.Join(", ", command.Aliases);
                }
            }
            return false;
        }
    }
}