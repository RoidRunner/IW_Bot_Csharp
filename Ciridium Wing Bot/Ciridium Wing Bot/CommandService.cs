using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium
{
    class CommandService
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="prefix">The command prefix that marks messages as commands</param>
        public CommandService(char prefix)
        {
            this.prefix = prefix;
            commands = new List<Command>();
        }

        /// <summary>
        /// The command prefix that marks messages as commands
        /// </summary>
        private char prefix;

        /// <summary>
        /// The dictionary storing commands by their first argument key
        /// </summary>
        public List<Command> commands { get; private set; }

        /// <summary>
        /// Add a new command by key
        /// </summary>
        /// <param name="key">The key to identify the command</param>
        /// <param name="command">The command object defining the commands behaviour</param>
        public void AddCommand(CommandKeys keys, HandleCommand commandHandler, AccessLevel accessLevel, string summary, string syntax)
        {
            Command cmd = new Command(keys, accessLevel, commandHandler, summary, syntax);
            commands.Add(cmd);
        }

        /// <summary>
        /// Command handling
        /// </summary>
        /// <param name="context">The context the command runs in</param>
        /// <returns></returns>
        public async Task HandleCommand(SocketUserMessage msg)
        {
            if (IsCommand(msg.Content))
            {
                Command cmd;
                CommandContext context = new CommandContext(Var.client, msg);
                if (TryGetCommand(context, out cmd))
                {
                    SocketGuildUser user = context.Guild.GetUser(context.User.Id);
                    AccessLevel userLevel = SettingsModel.GetUserAccessLevel(user);
                    if (HasPermission(userLevel, cmd.AccessLevel))
                    {
                        try
                        {
                            await cmd.HandleCommand(context);
                        } catch (Exception e)
                        {
                            await context.Channel.SendMessageAsync(string.Format("Exception Occured while trying to execute command `{0}`\n```{1}```\n```{2}```", cmd.Key.KeyList, e.Message, e.StackTrace));
                        }
                    }
                    else
                    {
                        await context.Channel.SendMessageAsync(
                            string.Format("Insufficient Permissions. `/{0}` requires {1} access, you have {2} access",
                            cmd.Key.KeyList, cmd.AccessLevel.ToString(), userLevel.ToString()));
                    }
                }
                else
                {
                    await SettingsModel.SendDebugMessage(string.Format("A potential command `{0}` could not be identified", msg.Content), DebugCategories.misc);
                }
            }
        }

        public bool TryGetCommand(CommandContext context, out Command result)
        {
            result = new Command();
            int argCntMatched = -2;
            foreach (Command command in commands)
            {
                if (command.Key.Matches(context.Args) && command.Key.MinArgCnt > argCntMatched)
                {
                    result = command;
                    argCntMatched = command.Key.MinArgCnt;
                }
            }
            return argCntMatched != -2;
        }

        public bool TryGetCommand(string[] keys, out Command result)
        {
            result = new Command();
            int argCntMatched = -2;
            foreach (Command command in commands)
            {
                if (command.Key.Matches(keys) && command.Key.MinArgCnt > argCntMatched)
                {
                    result = command;
                    argCntMatched = command.Key.MinArgCnt;
                }
            }
            return argCntMatched != -2;
        }

        private bool IsCommand(string content)
        {
            return content.StartsWith(prefix);
        }

        public static bool HasPermission(AccessLevel userLevel, AccessLevel cmdLevel)
        {
            return userLevel.CompareTo(cmdLevel) >= 0;
        }
    }

    public enum AccessLevel
    {
        Basic,
        Pilot,
        Moderator,
        BotAdmin
    }

    class HelpCommand
    {
        public async Task HandleHelpCommand(CommandContext context)
        {
            AccessLevel userLevel = SettingsModel.GetUserAccessLevel(context.Guild.GetUser(context.User.Id));

            StringBuilder message = new StringBuilder();
            message.Append("You have access to the following commands:```");
            foreach (Command cmd in Var.cmdService.commands)
            {
                if (CommandService.HasPermission(userLevel, cmd.AccessLevel))
                {
                    message.AppendLine(string.Format("{0} : {1}", cmd.Syntax.PadRight(40), cmd.Summary));
                }
            }
            message.Append("```Use `/help <cmdname>` to see syntax.");
            await context.Channel.SendMessageAsync(message.ToString());
        }

        public async Task HandleHelpCommandSpecific(CommandContext context)
        {
            AccessLevel userLevel = SettingsModel.GetUserAccessLevel(context.Guild.GetUser(context.User.Id));

            string[] keys = new string[context.ArgCnt - 1];
            for (int i = 1; i < context.ArgCnt; i++)
            {
                keys[i - 1] = context.Args[i];
            }
            if (Var.cmdService.TryGetCommand(keys, out Command cmd))
            {
                if (CommandService.HasPermission(userLevel, cmd.AccessLevel))
                {
                    await context.Channel.SendMessageAsync(string.Format(
                        "Syntax for command `/{0}`:\n```" +
                        "Description : {1}\n" +
                        "Syntax      : {2}" +
                        "```",
                        cmd.Key.KeyList, cmd.Summary, cmd.Syntax));
                } else
                {
                    await context.Channel.SendMessageAsync("Unsufficient permissions to access this commands summary!");
                }
            }
            else
            {
                await context.Channel.SendMessageAsync("Could not find that command!");
            }
        }

        public void RegisterCommand(CommandService service)
        {
            service.AddCommand(new CommandKeys("help"), HandleHelpCommand, AccessLevel.Basic, "Lists a summary for all commands the user has access to.", "/help");
            service.AddCommand(new CommandKeys("help", 2), HandleHelpCommandSpecific, AccessLevel.Basic, "Provides help for a specific command.", "/help [{<CommandKeys>}]");
        }
    }
}
