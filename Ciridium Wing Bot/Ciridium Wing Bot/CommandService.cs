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
        public void AddCommand(CommandKeys keys, HandleCommand commandHandler, AccessLevel accessLevel, string summary, string syntax, string argumentHelp)
        {
            Command cmd = new Command(keys, accessLevel, commandHandler, summary, syntax, argumentHelp);
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
                        if (cmd.Key.HasMinArgCnt(context.ArgCnt))
                        {
                            try
                            {
                                await cmd.HandleCommand(context);
                            }
                            catch (Exception e)
                            {
                                SendExceptionMessage(e, context, cmd);
                            }
                        }
                        else
                        {
                            await context.Channel.SendEmbedAsync(string.Format("The command `/{0}` expects {1} arguments, that is {2} more than you supplied! Try `/help {0}` for more info",
                                cmd.Key.KeyList, cmd.Key.MinArgCnt - cmd.Key.FixedArgCnt, cmd.Key.MinArgCnt - context.ArgCnt
                                ));
                        }
                    }
                    else
                    {
                        await context.Channel.SendEmbedAsync(
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
                if (command.Key.Matches(context.Args) && command.Key.FixedArgCnt > argCntMatched)
                {
                    result = command;
                    argCntMatched = command.Key.FixedArgCnt;
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
                if (command.Key.Matches(keys) && command.Key.FixedArgCnt >= argCntMatched)
                {
                    result = command;
                    argCntMatched = command.Key.FixedArgCnt;
                }
            }
            return argCntMatched != -2;
        }

        public bool TryGetCommands(string[] keys, out List<Command> results)
        {
            results = new List<Command>();
            int argCntMatched = -2;
            foreach (Command command in commands)
            {
                if (command.Key.Matches(keys) && command.Key.FixedArgCnt >= argCntMatched)
                {
                    results.Add(command);
                    argCntMatched = command.Key.FixedArgCnt;
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

        public async static void SendExceptionMessage(Exception e, CommandContext context, Command cmd)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Var.ERRORCOLOR;
            embed.Title = string.Format("**__An Exception occured while trying to execute command __**`/{0}`", cmd.Key.KeyList);
            embed.AddField("Message", Macros.MultiLineCodeBlock(e.Message));
            embed.AddField("StackTrace", Macros.MultiLineCodeBlock(e.StackTrace));
            await context.Channel.SendEmbedAsync(embed);
        }
    }

    public enum AccessLevel
    {
        Basic,
        Pilot,
        Moderator,
        BotAdmin
    }
}
