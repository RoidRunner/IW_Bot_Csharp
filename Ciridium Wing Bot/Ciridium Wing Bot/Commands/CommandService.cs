#define PMSTACKTRACE

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
        #region Variables

        /// <summary>
        /// The command prefix that marks messages as commands
        /// </summary>
        private char prefix;

        /// <summary>
        /// The dictionary storing commands by their first argument key
        /// </summary>
        public List<Command> commands { get; private set; }

        #endregion
        #region Init

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="prefix">The command prefix that marks messages as commands</param>
        public CommandService(char prefix)
        {
            this.prefix = prefix;
            commands = new List<Command>();
        }

        #endregion
        #region Command Adding

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

        public void AddSynchronousCommand(CommandKeys keys, HandleSynchronousCommand commandHandler, AccessLevel accessLevel, string summary, string syntax, string argumentHelp)
        {
            Command cmd = new Command(keys, accessLevel, commandHandler, summary, syntax, argumentHelp);
            commands.Add(cmd);
        }

        #endregion
        #region Command Handling

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
                    if (cmd.HasPermission(userLevel))
                    {
                        if (context.ArgCnt >= cmd.Key.MinArgCnt)
                        {
                            try
                            {
                                if (cmd.async)
                                {
                                    await cmd.HandleCommand(context);
                                }
                                else
                                {
                                    cmd.HandleSynchronousCommand(context);
                                }
                            }
                            catch (Exception e)
                            {
                                SendCommandExecutionExceptionMessage(e, context, cmd);
                            }
                        }
                        else
                        {
                            await context.Channel.SendEmbedAsync(string.Format("The command `/{0}` expects {1} arguments, that is {2} more than you supplied! Try `/help {0}` for more info",
                                cmd.Key.KeyList, cmd.Key.MinArgCnt, cmd.Key.MinArgCnt - context.ArgCnt
                                ), true);
                        }
                    }
                    else
                    {
                        await context.Channel.SendEmbedAsync(
                            string.Format("Insufficient Permissions. `/{0}` requires {1} access, you have {2} access",
                            cmd.Key.KeyList, cmd.AccessLevel.ToString(), userLevel.ToString()), true);
                    }
                }
                else
                {
                    await SettingsModel.SendDebugMessage(string.Format("A potential command `{0}` could not be identified", msg.Content), DebugCategories.misc);
                }
            }
        }

        /// <summary>
        /// Tries to match a context to a command
        /// </summary>
        /// <param name="context">The command context the command would execute in</param>
        /// <param name="result">The command match</param>
        /// <returns>Wether the command matching attempt was successful</returns>
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

        /// <summary>
        /// Tries to match a key input to a command
        /// </summary>
        /// <param name="keys">The command keys identifying the command</param>
        /// <param name="result">The command match</param>
        /// <returns>Wether the command matching attempt was successful</returns>
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

        /// <summary>
        /// Tries to match a context to all matching commands
        /// </summary>
        /// <param name="keys">The command keys identifying the command</param>
        /// <param name="results">The command matches</param>
        /// <returns>Wether the command matching attempt was successful (as in yielding >= 1 results)</returns>
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

        /// <summary>
        /// Sends an exception message to the debugmessage channel pinging the botdevs about it
        /// </summary>
        /// <param name="e">Exception</param>
        /// <param name="context">The context the command failing executed in</param>
        /// <param name="cmd">The command matched to the context</param>
        public async static void SendCommandExecutionExceptionMessage(Exception e, CommandContext context, Command cmd)
        {
            await context.Channel.SendEmbedAsync("Something went horribly wrong trying to execute your command! I have contacted my creators to help fix this issue!", true);
            ISocketMessageChannel channel = Var.client.GetChannel(SettingsModel.DebugMessageChannelId) as ISocketMessageChannel;
            if (channel != null)
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = Var.ERRORCOLOR;
                embed.Title = "**__Exception__**";
                embed.AddField("Command", cmd.Key.KeyList);
                embed.AddField("Location", context.Guild.GetTextChannel(context.Channel.Id).Mention);
                embed.AddField("Message", Macros.MultiLineCodeBlock(e.Message));
                string stacktrace;
                if (e.StackTrace.Length <= 500)
                {
                    stacktrace = e.StackTrace;
                }
                else
                {
                    stacktrace = e.StackTrace.Substring(0, 500);
                }
                embed.AddField("StackTrace", Macros.MultiLineCodeBlock(stacktrace));
                string message = string.Empty;
                SocketRole botDevRole = context.Guild.GetRole(SettingsModel.BotDevRole);
                if (botDevRole != null)
                {
                    message = botDevRole.Mention;
                }
                await channel.SendMessageAsync(message, embed:embed.Build());
            }
            await BotCore.Logger(new LogMessage(LogSeverity.Error, "CMDSERVICE", string.Format("An Exception occured while trying to execute command `/{0}`.Message: '{1}'\nStackTrace {2}", cmd.Key.KeyList, e.Message, e.StackTrace)));
        }

        #endregion
    }
}
