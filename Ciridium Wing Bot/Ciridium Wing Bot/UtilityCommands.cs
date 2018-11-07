using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Ciridium
{
    class UtilityCommands
    {
        public UtilityCommands(CommandService service)
        {
            string syntax = "/ping";
            string summary = "Pings the user back. Use if bot seems unresponsive.";
            service.AddCommand(new CommandKeys("ping"), HandlePingCommand, AccessLevel.Basic, summary, syntax, Command.NO_ARGUMENTS);
            summary = "Prints out the channels topic";
            service.AddCommand(new CommandKeys("topic"), HandleTopicCommand, AccessLevel.Pilot, summary, "/topic", Command.NO_ARGUMENTS);
            summary = "Try it";
            service.AddCommand(new CommandKeys("time"), HandleTimeCommand, AccessLevel.Basic, summary, "/time", Command.NO_ARGUMENTS);
        }

        /// <summary>
        /// Handles "/ping" command
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandlePingCommand(CommandContext context)
        {
            await context.Channel.SendMessageAsync(string.Format("Hi {0}", context.User.Mention));
        }

        /// <summary>
        /// Handles /topic command
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleTopicCommand(CommandContext context)
        {
            ITextChannel channel = context.Channel as ITextChannel;
            if (channel != null)
            {
                await context.Channel.SendMessageAsync(channel.Topic);
            }
        }

        /// <summary>
        /// Handles /time "command"
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleTimeCommand(CommandContext context)
        {
            string message = string.Format("You see UTC date & time in the top right so what else would you ever want from me?");
            await context.Channel.SendMessageAsync(message);
        }
    }

    class DebugCommands
    {
        public DebugCommands(CommandService service)
        {
            string summary = "Lists all Channels & Categorychannels on current server.";
            Var.cmdService.AddCommand(new CommandKeys("debug channels"), HandleListChannelsCommand, AccessLevel.Moderator, summary, "/debug channels", Command.NO_ARGUMENTS);
            summary = "Lists all Roles on current server.";
            Var.cmdService.AddCommand(new CommandKeys("debug roles"), HandleListRolesCommand, AccessLevel.Moderator, summary, "/debug roles", Command.NO_ARGUMENTS);
            summary = "Prints out some debug info on all users mentioned.";
            string arguments =
                "    {<@user>}\n" +
                "Mention all users you want debug info about here";
            Var.cmdService.AddCommand(new CommandKeys("debug userinfo", 3, 1000), HandleUserInfoCommand, AccessLevel.Moderator, summary, "/debug userinfo {<@user>}", arguments);
        }

        /// <summary>
        /// Handles /debug channels command
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task HandleListChannelsCommand(SocketCommandContext context)
        {
            StringBuilder message = new StringBuilder();
            message.Append("Channels on this server:```");

            var channels = context.Guild.Channels;

            foreach (var channel in channels)
            {
                if (channel == null)
                {
                    message.AppendLine("[WARNING] One channel is NULL");
                }
                else
                {
                    message.AppendLine(string.Format("{0} : {1}", channel.Id, channel.Name));
                }
            }

            message.Append("```Categories on this server:```");

            var categories = context.Guild.CategoryChannels;

            foreach (var category in categories)
            {
                if (category == null)
                {
                    message.AppendLine("[WARNING] This channel is NULL");
                }
                else
                {
                    message.AppendLine(string.Format("{0} : {1}", category.Id, category.Name));
                }
            }

            message.Append("```");

            await context.Channel.SendMessageAsync(message.ToString());
        }

        public async Task HandleListRolesCommand(SocketCommandContext context)
        {
            StringBuilder message = new StringBuilder();
            message.Append("Roles on this server:```");

            var roles = context.Guild.Roles;

            foreach (var role in roles)
            {
                if (role == null)
                {
                    message.AppendLine("[WARNING] One channel is NULL");
                }
                else
                {
                    message.AppendLine(string.Format("{0} : {1}", role.Id, role.Name));
                }
            }

            message.Append("```");

            await context.Channel.SendMessageAsync(message.ToString());
        }

        public async Task HandleUserInfoCommand(SocketCommandContext context)
        {
            var users = context.Message.MentionedUsers;
            StringBuilder message = new StringBuilder();

            try
            {
                foreach (SocketUser user in users)
                {
                    message.Append(string.Format("User {0}:```" +
                        "Name + Tag    : {0}#{1}\n" +
                        "Mention       : {2}\n",
                        user.Username, user.Discriminator, user.Mention));
                    if (user.IsBot)
                    {
                        message.Append("User is Bot\n");
                    }
                    if (user.IsWebhook)
                    {
                        message.Append("User is Webhook\n");
                    }
                    message.Append("```");
                }
            }
            catch (Exception e)
            {
                await context.Channel.SendMessageAsync("[Exception]```" + e.Message + "\n" + e.StackTrace + "```");
            }
            if (string.IsNullOrWhiteSpace(message.ToString()))
            {
                message.Append("Please tag atleast one user when using this command!");
            }
            await context.Channel.SendMessageAsync(message.ToString());
        }

    }

    class ShutdownCommand
    {
        public ShutdownCommand(CommandService service)
        {
            string summary = "Shuts down the bot";
            Var.cmdService.AddCommand(new CommandKeys("kys"), HandleShutdownCommand, AccessLevel.Moderator, summary, "/kys", Command.NO_ARGUMENTS);
            Var.cmdService.AddCommand(new CommandKeys("shutdown"), HandleShutdownCommand, AccessLevel.Moderator, summary, "/shutdown", Command.NO_ARGUMENTS);
            //summary = "Restarts the bot.";
            //Var.cmdService.AddCommand(new CommandKeys("restart"), HandleRestartCommand, AccessLevel.Moderator, summary, "/restart");
        }

        public async Task HandleShutdownCommand(SocketCommandContext context)
        {
            await context.Channel.SendMessageAsync("Shutting down ...");
            Var.running = false;
        }

        public async Task HandleRestartCommand(SocketCommandContext context)
        {
            if (SettingsModel.UserIsBotAdmin(context.User.Id))
            {
                await context.Channel.SendMessageAsync("Restarting ...");
                Var.running = false;
                System.Diagnostics.Process.Start(Environment.CurrentDirectory);
            }
        }
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
                        "Help for command **/{0}**:\n" +
                        "**Description**:```{1}```" +
                        "**Syntax:**```{2}```" +
                        "**Arguments:**```{3}```",
                        cmd.Key.KeyList, cmd.Summary, cmd.Syntax, cmd.ArgumentHelp));
                }
                else
                {
                    await context.Channel.SendMessageAsync("Unsufficient permissions to access this commands summary!");
                }
            }
            else
            {
                await context.Channel.SendMessageAsync("Could not find that command!");
            }
        }

        public HelpCommand(CommandService service)
        {
            string summary = "Lists a summary for all commands the user has access to.";
            service.AddCommand(new CommandKeys("help"), HandleHelpCommand, AccessLevel.Basic, summary, "/help", Command.NO_ARGUMENTS);
            summary = "Provides help for a specific command.";
            string arguments =
                "    [{<CommandKeys>}]\n" +
                "List all command keys here that make up the command.";
            service.AddCommand(new CommandKeys("help", 2, 6), HandleHelpCommandSpecific, AccessLevel.Basic, summary, "/help [{<CommandKeys>}]", arguments);
        }
    }

}
