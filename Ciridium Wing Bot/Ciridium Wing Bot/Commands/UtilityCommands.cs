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
            // ping
            service.AddCommand(new CommandKeys(CMDKEYS_PING), HandlePingCommand, AccessLevel.Basic, CMDSUMMARY_PING, CMDSYNTAX_PING, Command.NO_ARGUMENTS);
            // topic
            service.AddCommand(new CommandKeys(CMDKEYS_TOPIC), HandleTopicCommand, AccessLevel.Pilot, CMDSUMMARY_TOPIC, CMDSYNTAX_TOPIC, Command.NO_ARGUMENTS);
        }

        #region /ping

        private const string CMDKEYS_PING = "ping";
        private const string CMDSYNTAX_PING = "/ping";
        private const string CMDSUMMARY_PING = "Pings the user back. Use if bot seems unresponsive";

        public async Task HandlePingCommand(CommandContext context)
        {
            await context.Channel.SendEmbedAsync(context.User.Mention, "Hi!");
        }

        #endregion
        #region /topic

        private const string CMDKEYS_TOPIC = "topic";
        private const string CMDSYNTAX_TOPIC = "/topic";
        private const string CMDSUMMARY_TOPIC = "Prints out the channels topic";

        public async Task HandleTopicCommand(CommandContext context)
        {
            ITextChannel channel = context.Channel as ITextChannel;
            if (channel != null)
            {
                await context.Channel.SendEmbedAsync(channel.Topic);
            }
        }

        #endregion
    }

    class DebugCommands
    {
        public DebugCommands(CommandService service)
        {
            // debug channels
            service.AddCommand(new CommandKeys(CMDKEYS_DEBUG_CHANNELS), HandleListChannelsCommand, AccessLevel.Moderator, CMDSUMMARY_DEBUG_CHANNELS, CMDSYNTAX_DEBUG_CHANNELS, Command.NO_ARGUMENTS);
            // debug roles
            service.AddCommand(new CommandKeys(CMDKEYS_DEBUG_ROLES), HandleListRolesCommand, AccessLevel.Moderator, CMDSUMMARY_DEBUG_ROLES, CMDSYNTAX_DEBUG_ROLES, Command.NO_ARGUMENTS);
            // debug userinfo
            service.AddCommand(new CommandKeys(CMDKEYS_DEBUG_USERINFO, 3, 1000), HandleUserInfoCommand, AccessLevel.Moderator, CMDSUMMARY_DEBUG_USERINFO, CMDSYNTAX_DEBUG_USERINFO, CMDARGS_DEBUG_USERINFO);
        }

        #region /debug channels

        private const string CMDKEYS_DEBUG_CHANNELS = "debug channels";
        private const string CMDSYNTAX_DEBUG_CHANNELS = "/debug channels";
        private const string CMDSUMMARY_DEBUG_CHANNELS = "Lists all channels & categories on current server";

        public async Task HandleListChannelsCommand(SocketCommandContext context)
        {
            List<EmbedField> categoryembed = new List<EmbedField>();
            List<EmbedField> channelembed = new List<EmbedField>();

            List<SocketGuildChannel> channels = new List<SocketGuildChannel>(context.Guild.Channels);
            List<SocketCategoryChannel> categories = new List<SocketCategoryChannel>(context.Guild.CategoryChannels);
            List<ulong> categoryIds = new List<ulong>();

            foreach (var category in categories)
            {
                if (category != null)
                {
                    categoryIds.Add(category.Id);
                    categoryembed.Add(new EmbedField(category.Name, string.Format("ID: `{0}`", category.Id)));
                }
            }

            foreach (var channel in channels)
            {
                if (channel != null)
                {
                    if (!categoryIds.Contains(channel.Id))
                    {
                        channelembed.Add(new EmbedField(channel.Name, string.Format("ID: `{0}`", channel.Id)));
                    }
                }
            }

            await context.Channel.SendSafeEmbedList("**__Categories on this server__**", categoryembed);
            await context.Channel.SendSafeEmbedList("**__Channels on this server__**", channelembed);
        }

        #endregion
        #region /debug roles

        private const string CMDKEYS_DEBUG_ROLES = "debug roles";
        private const string CMDSYNTAX_DEBUG_ROLES = "/debug roles";
        private const string CMDSUMMARY_DEBUG_ROLES = "Lists all roles on current server";

        public async Task HandleListRolesCommand(SocketCommandContext context)
        {
            List<EmbedField> roleembed = new List<EmbedField>();

            var roles = context.Guild.Roles;

            foreach (var role in roles)
            {
                if (role != null)
                {
                    roleembed.Add(new EmbedField(role.Name, string.Format("ID: `{0}`", role.Id)));
                }
            }


            await context.Channel.SendSafeEmbedList("**__Roles on this server__**", roleembed);
        }

        #endregion
        #region /debug userinfo

        private const string CMDKEYS_DEBUG_USERINFO = "debug userinfo";
        private const string CMDSYNTAX_DEBUG_USERINFO = "/debug userinfo {<@user>}";
        private const string CMDSUMMARY_DEBUG_USERINFO = "Prints out some debug info on all users mentioned";
        private const string CMDARGS_DEBUG_USERINFO =
                "    {<@user>}\n" +
                "Mention all users you want debug info about here";

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
                userembed.AddField("Profile Picture URL", user.GetAvatarUrl());
                if (user.IsBot || user.IsWebhook)
                {
                    userembed.AddField("Add. Info", string.Format("```Bot: {0} Webhook: {1}```", user.IsBot, user.IsWebhook));
                }
                await context.Channel.SendEmbedAsync(userembed);
            }
        }

        #endregion
    }

    class ShutdownCommands
    {
        public ShutdownCommands(CommandService service)
        {
            // shutdown
            Var.cmdService.AddSynchronousCommand(new CommandKeys(CMDKEYS_SHUTDOWN), HandleShutdownCommand, AccessLevel.Moderator, CMDSUMMARY_SHUTDOWN, CMDSYNTAX_SHUTDOWN, Command.NO_ARGUMENTS);
            // kys
            Var.cmdService.AddSynchronousCommand(new CommandKeys(CMDKEYS_SHUTDOWN_ALT), HandleShutdownCommand, AccessLevel.Moderator, CMDSUMMARY_SHUTDOWN, CMDSYNTAX_SHUTDOWN_ALT, Command.NO_ARGUMENTS);
            // restart
            Var.cmdService.AddSynchronousCommand(new CommandKeys(CMDKEYS_RESTART), HandleRestartCommand, AccessLevel.Moderator, CMDSUMMARY_RESTART, CMDSYNTAX_RESTART, Command.NO_ARGUMENTS);
        }

        #region /shutdown

        private const string CMDKEYS_SHUTDOWN = "shutdown";
        private const string CMDKEYS_SHUTDOWN_ALT = "kys";
        private const string CMDSYNTAX_SHUTDOWN = "/shutdown";
        private const string CMDSYNTAX_SHUTDOWN_ALT = "/kys";
        private const string CMDSUMMARY_SHUTDOWN = "Shuts down the bot";

        public void HandleShutdownCommand(SocketCommandContext context)
        {
            Var.running = false;
        }

        #endregion
        #region /restart

        private const string CMDKEYS_RESTART = "restart";
        private const string CMDSYNTAX_RESTART = "/restart";
        private const string CMDSUMMARY_RESTART = "Restarts the bot";

        public void HandleRestartCommand(SocketCommandContext context)
        {
            //await context.Channel.SendEmbedAsync("Restarting ..."
            //    //+ "```\n" + Environment.CurrentDirectory + "\n\n" + System.Reflection.Assembly.GetEntryAssembly().Location + "\n\n" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "```"
            //    );
            Var.RestartPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            Var.running = false;
        }

        #endregion
    }

    class HelpCommands
    {
        public HelpCommands(CommandService service)
        {
            // help (list)
            service.AddCommand(new CommandKeys(CMDKEYS_HELP_LIST), HandleHelpCommand, AccessLevel.Basic, CMDSUMMARY_HELP_LIST, CMDSYNTAX_HELP_LIST, Command.NO_ARGUMENTS);
            // help (specific)
            service.AddCommand(new CommandKeys(CMDKEYS_HELP_SPECIFIC, 2, 6), HandleHelpCommandSpecific, AccessLevel.Basic, CMDSUMMARY_HELP_SPECIFIC, CMDSYNTAX_HELP_SPECIFIC, CMDARGS_HELP_SPECIFIC);
        }

        #region /help (list)

        private const string CMDKEYS_HELP_LIST = "help";
        private const string CMDSYNTAX_HELP_LIST = "/help";
        private const string CMDSUMMARY_HELP_LIST = "Lists a summary for all commands you have access to";

        public async Task HandleHelpCommand(CommandContext context)
        {
            AccessLevel userLevel = SettingsModel.GetUserAccessLevel(context.Guild.GetUser(context.User.Id));

            EmbedBuilder embedmessage = new EmbedBuilder();
            embedmessage.Color = Var.BOTCOLOR;
            embedmessage.Title = "You have access to the following commands";
            foreach (Command cmd in Var.cmdService.commands)
            {
                if (cmd.HasPermission(userLevel))
                {
                    embedmessage.AddField(cmd.Syntax, cmd.Summary);
                }
            }
            embedmessage.Description = "Use `/help <cmdname>` to see syntax.";
            await context.Channel.SendEmbedAsync(embedmessage);
        }

        #endregion
        #region /help (specific)

        private const string CMDKEYS_HELP_SPECIFIC = "help";
        private const string CMDSYNTAX_HELP_SPECIFIC = "/help {<CommandKeys>}";
        private const string CMDSUMMARY_HELP_SPECIFIC = "Provides summary, syntax and argument information for a specific command";
        private const string CMDARGS_HELP_SPECIFIC =
                "    {<CommandKeys>}\n" +
                "List all command keys here that make up the command";

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
                    if (cmd.HasPermission(userLevel))
                    {
                        EmbedBuilder embedmessage = new EmbedBuilder();
                        embedmessage.Color = Var.BOTCOLOR;
                        embedmessage.Title = string.Format("Help for command `/{0}`", cmd.Key.KeyList);
                        embedmessage.AddField("Description", cmd.Summary);
                        embedmessage.AddField("Required Access Level", cmd.AccessLevel.ToString());
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

        #endregion


    }
}
