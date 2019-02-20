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
            await context.Channel.SendEmbedAsync(string.Format("Hi {0}", context.User.Mention));
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
                await context.Channel.SendEmbedAsync(channel.Topic);
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
            await context.Channel.SendEmbedAsync(message);
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
            EmbedBuilder categoryembed = new EmbedBuilder();
            categoryembed.Color = Var.BOTCOLOR;
            categoryembed.Title = "**__Categories on this server__**";
            EmbedBuilder channelembed = new EmbedBuilder();
            channelembed.Color = Var.BOTCOLOR;
            channelembed.Title = "**__Channels on this server__**";

            List<SocketGuildChannel> channels = new List<SocketGuildChannel>(context.Guild.Channels);
            List<SocketCategoryChannel> categories = new List<SocketCategoryChannel>(context.Guild.CategoryChannels);
            List<ulong> categoryIds = new List<ulong>();

            foreach (var category in categories)
            {
                if (category != null)
                {
                    categoryIds.Add(category.Id);
                    categoryembed.AddField(category.Name, string.Format("ID: `{0}`", category.Id));
                }
            }

            foreach (var channel in channels)
            {
                if (channel != null)
                {
                    if (!categoryIds.Contains(channel.Id))
                    {
                        channelembed.AddField(channel.Name, string.Format("ID: `{0}`", channel.Id));
                    }
                }
            }

            await context.Channel.SendEmbedAsync(categoryembed);
            await context.Channel.SendEmbedAsync(channelembed);
        }

        public async Task HandleListRolesCommand(SocketCommandContext context)
        {
            EmbedBuilder roleembed = new EmbedBuilder();
            roleembed.Color = Var.BOTCOLOR;
            roleembed.Title = "**__Roles on this server__**";

            var roles = context.Guild.Roles;

            foreach (var role in roles)
            {
                if (role != null)
                {
                    roleembed.AddField(role.Name, string.Format("ID: `{0}`", role.Id));
                }
            }


            await context.Channel.SendEmbedAsync(roleembed);
        }

        public async Task HandleUserInfoCommand(SocketCommandContext context)
        {
            var users = context.Message.MentionedUsers;

            foreach (SocketUser user in users)
            {
                EmbedBuilder userembed = new EmbedBuilder();
                userembed.Color = Var.BOTCOLOR;
                userembed.Title = string.Format("**__User {0}__**", user.Username);

                userembed.AddField("Discriminator", Macros.MultiLineCodeBlock(string.Format("{0}#{1}", user.Username, user.Discriminator)));
                userembed.AddField("Mention", Macros.MultiLineCodeBlock(user.Mention));
                if (user.IsBot || user.IsWebhook)
                {
                    userembed.AddField("Add. Info", string.Format("```Bot: {0} Webhook: {1}```", user.IsBot, user.IsWebhook));
                }
                await context.Channel.SendEmbedAsync(userembed);
            }
        }

    }

    class ShutdownCommand
    {
        public ShutdownCommand(CommandService service)
        {
            string summary = "Shuts down the bot";
            Var.cmdService.AddCommand(new CommandKeys("kys"), HandleShutdownCommand, AccessLevel.Moderator, summary, "/kys", Command.NO_ARGUMENTS);
            Var.cmdService.AddCommand(new CommandKeys("shutdown"), HandleShutdownCommand, AccessLevel.Moderator, summary, "/shutdown", Command.NO_ARGUMENTS);
            summary = "Restarts the bot.";
            Var.cmdService.AddCommand(new CommandKeys("restart"), HandleRestartCommand, AccessLevel.Moderator, summary, "/restart", Command.NO_ARGUMENTS);
        }

        public async Task HandleShutdownCommand(SocketCommandContext context)
        {
            await context.Channel.SendEmbedAsync("Shutting down ...");
            Var.running = false;
        }

        public async Task HandleRestartCommand(SocketCommandContext context)
        {
            if (SettingsModel.UserIsBotAdmin(context.User.Id))
            {
                await context.Channel.SendEmbedAsync("Restarting ... " + Environment.CurrentDirectory);
                System.Diagnostics.Process.Start(Environment.CurrentDirectory);
                Var.running = false;
            }
        }
    }

    class HelpCommand
    {
        public async Task HandleHelpCommand(CommandContext context)
        {
            AccessLevel userLevel = SettingsModel.GetUserAccessLevel(context.Guild.GetUser(context.User.Id));

            EmbedBuilder embedmessage = new EmbedBuilder();
            embedmessage.Color = Var.BOTCOLOR;
            embedmessage.Title = "You have access to the following commands";
            foreach (Command cmd in Var.cmdService.commands)
            {
                if (CommandService.HasPermission(userLevel, cmd.AccessLevel))
                {
                    embedmessage.AddField(cmd.Syntax, cmd.Summary);
                }
            }
            embedmessage.Description = "Use `/help <cmdname>` to see syntax.";
            await context.Channel.SendEmbedAsync(embedmessage);
        }

        public async Task HandleHelpCommandSpecific(CommandContext context)
        {
            AccessLevel userLevel = SettingsModel.GetUserAccessLevel(context.Guild.GetUser(context.User.Id));

            string[] keys = new string[context.ArgCnt - 1];
            for (int i = 1; i < context.ArgCnt; i++)
            {
                keys[i - 1] = context.Args[i];
            }
            if (Var.cmdService.TryGetCommands(keys, out List<Command> cmds))
            {
                foreach (Command cmd in cmds)
                {
                    if (CommandService.HasPermission(userLevel, cmd.AccessLevel))
                    {
                        EmbedBuilder embedmessage = new EmbedBuilder();
                        embedmessage.Color = Var.BOTCOLOR;
                        embedmessage.Title = string.Format("Help for command `/{0}`", cmd.Key.KeyList);
                        embedmessage.AddField("Description", Macros.MultiLineCodeBlock(cmd.Summary));
                        embedmessage.AddField("Syntax", Macros.MultiLineCodeBlock(cmd.Syntax));
                        if (!cmd.ArgumentHelp.Equals(Command.NO_ARGUMENTS))
                        {
                            embedmessage.AddField("Arguments", Macros.MultiLineCodeBlock(cmd.ArgumentHelp));
                        }
                        //string.Format(
                        //"Help for command **/{0}**:\n" +
                        //"**Description**:```{1}```" +
                        //"**Syntax:**```{2}```" +
                        //"**Arguments:**```{3}```",
                        //    cmd.Key.KeyList, cmd.Summary, cmd.Syntax, cmd.ArgumentHelp)
                        await context.Channel.SendEmbedAsync(embedmessage);
                    }
                    else
                    {
                        await context.Channel.SendEmbedAsync(string.Format("Unsufficient permissions to access the command summary for `/{0}`!", cmd.Key.KeyList));
                    }
                }
            }
            else
            {
                await context.Channel.SendEmbedAsync("Could not find that command!");
            }
        }

        public HelpCommand(CommandService service)
        {
            string summary = "Lists a summary for all commands the user has access to.";
            service.AddCommand(new CommandKeys("help"), HandleHelpCommand, AccessLevel.Basic, summary, "/help", Command.NO_ARGUMENTS);
            summary = "Provides help for a specific command.";
            string arguments =
                "    {<CommandKeys>}\n" +
                "List all command keys here that make up the command.";
            service.AddCommand(new CommandKeys("help", 2, 6), HandleHelpCommandSpecific, AccessLevel.Basic, summary, "/help {<CommandKeys>}", arguments);
        }
    }

}
