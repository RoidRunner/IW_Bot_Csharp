﻿using System;
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
        public UtilityCommands()
        {
            // ping
            CommandService.AddCommand(new CommandKeys(CMDKEYS_PING), HandlePingCommand, AccessLevel.Basic, CMDSUMMARY_PING, CMDSYNTAX_PING, Command.NO_ARGUMENTS);
            // topic
            CommandService.AddCommand(new CommandKeys(CMDKEYS_TOPIC), HandleTopicCommand, AccessLevel.Pilot, CMDSUMMARY_TOPIC, CMDSYNTAX_TOPIC, Command.NO_ARGUMENTS);
            // about
            CommandService.AddCommand(new CommandKeys(CMDKEYS_ABOUT), HandleAboutCommand, AccessLevel.Basic, CMDSUMMARY_ABOUT, CMDSYNTAX_ABOUT, Command.NO_ARGUMENTS);
        }

        #region /ping

        private const string CMDKEYS_PING = "ping";
        private const string CMDSYNTAX_PING = "ping";
        private const string CMDSUMMARY_PING = "Pings the user back. Use if bot seems unresponsive";

        public async Task HandlePingCommand(CommandContext context)
        {
            await context.Channel.SendEmbedAsync(context.User.Mention, "Hi!");
        }

        #endregion
        #region /topic

        private const string CMDKEYS_TOPIC = "topic";
        private const string CMDSYNTAX_TOPIC = "topic";
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
        #region /about

        private const string CMDKEYS_ABOUT = "about";
        private const string CMDSYNTAX_ABOUT = "about";
        private const string CMDSUMMARY_ABOUT = "Provides basic info about me";

        public async Task HandleAboutCommand(CommandContext context)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Var.BOTCOLOR;
            embed.Title = "Ciridium Wing Bot";
            embed.ThumbnailUrl = Var.client.CurrentUser.GetAvatarUrl();
            embed.AddField("Version", "v" + Var.VERSION.ToString());
            embed.AddField("Credits", "Programming: <@!117260771200598019>\nSupport: <@!181013221661081600>");
            embed.AddField("Data Sources", "[EDSM](https://www.edsm.net/), [Inara](https://inara.cz/), [EDAssets](https://edassets.org/#/)");
            await context.Channel.SendEmbedAsync(embed);
        }

        #endregion
    }

    class DebugCommands
    {
        public DebugCommands()
        {
            // debug channels
            CommandService.AddCommand(new CommandKeys(CMDKEYS_DEBUG_CHANNELS), HandleListChannelsCommand, AccessLevel.Director, CMDSUMMARY_DEBUG_CHANNELS, CMDSYNTAX_DEBUG_CHANNELS, Command.NO_ARGUMENTS);
            // debug roles
            CommandService.AddCommand(new CommandKeys(CMDKEYS_DEBUG_ROLES), HandleListRolesCommand, AccessLevel.Director, CMDSUMMARY_DEBUG_ROLES, CMDSYNTAX_DEBUG_ROLES, Command.NO_ARGUMENTS);
            // debug userinfo
            CommandService.AddCommand(new CommandKeys(CMDKEYS_DEBUG_USERINFO, 3, 1000), HandleUserInfoCommand, AccessLevel.Director, CMDSUMMARY_DEBUG_USERINFO, CMDSYNTAX_DEBUG_USERINFO, CMDARGS_DEBUG_USERINFO);
            // debug guilds
            CommandService.AddCommand(new CommandKeys(CMDKEYS_DEBUG_GUILDS), HandleListGuildsCommand, AccessLevel.BotAdmin, CMDSUMMARY_DEBUG_GUILDS, CMDSYNTAX_DEBUG_GUILDS, Command.NO_ARGUMENTS);
        }

        #region /debug channels

        private const string CMDKEYS_DEBUG_CHANNELS = "debug channels";
        private const string CMDSYNTAX_DEBUG_CHANNELS = "debug channels";
        private const string CMDSUMMARY_DEBUG_CHANNELS = "Lists all channels & categories on current server";

        public async Task HandleListChannelsCommand(CommandContext context)
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
        private const string CMDSYNTAX_DEBUG_ROLES = "debug roles";
        private const string CMDSUMMARY_DEBUG_ROLES = "Lists all roles on current server";

        public async Task HandleListRolesCommand(CommandContext context)
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
        #region /debug guilds

        private const string CMDKEYS_DEBUG_GUILDS = "debug guilds";
        private const string CMDSYNTAX_DEBUG_GUILDS = "debug guilds";
        private const string CMDSUMMARY_DEBUG_GUILDS = "Lists all guilds this bot is on";

        public async Task HandleListGuildsCommand(CommandContext context)
        {
            List<EmbedField> guildembed = new List<EmbedField>();

            var guilds = Var.client.Guilds;

            foreach (var guild in guilds)
            {
                if (guild != null)
                {
                    guildembed.Add(new EmbedField(guild.Name, string.Format("ID: `{0}`", guild.Id)));
                }
            }


            await context.Channel.SendSafeEmbedList("**__Roles on this server__**", guildembed);
        }

        #endregion
        #region /debug userinfo

        private const string CMDKEYS_DEBUG_USERINFO = "debug userinfo";
        private const string CMDSYNTAX_DEBUG_USERINFO = "debug userinfo {<@user>}";
        private const string CMDSUMMARY_DEBUG_USERINFO = "Prints out some debug info on all users mentioned";
        private const string CMDARGS_DEBUG_USERINFO =
                "    {<@user>}\n" +
                "Mention all users you want debug info about here";

        public async Task HandleUserInfoCommand(CommandContext context)
        {
            var users = context.Message.MentionedUsers;

            foreach (SocketUser user in users)
            {
                EmbedBuilder userembed = new EmbedBuilder();
                userembed.Color = Var.BOTCOLOR;
                userembed.Title = string.Format("**__User {0}__**", user.Username);

                userembed.AddField("Discriminator", Macros.MultiLineCodeBlock(string.Format("{0}#{1}", user.Username, user.Discriminator)));
                userembed.AddField("Mention", Macros.MultiLineCodeBlock(user.Mention));
                userembed.AddField("uInt64 Id", Macros.MultiLineCodeBlock(user.Id));
                userembed.ThumbnailUrl = user.GetAvatarUrl();
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
        public ShutdownCommands()
        {
            // shutdown
            CommandService.AddSynchronousCommand(new CommandKeys(CMDKEYS_SHUTDOWN), HandleShutdownCommand, AccessLevel.Director, CMDSUMMARY_SHUTDOWN, CMDSYNTAX_SHUTDOWN, Command.NO_ARGUMENTS);
            // kys
            CommandService.AddSynchronousCommand(new CommandKeys(CMDKEYS_SHUTDOWN_ALT), HandleShutdownCommand, AccessLevel.Director, CMDSUMMARY_SHUTDOWN, CMDSYNTAX_SHUTDOWN_ALT, Command.NO_ARGUMENTS);
            // restart
            CommandService.AddSynchronousCommand(new CommandKeys(CMDKEYS_RESTART), HandleRestartCommand, AccessLevel.Director, CMDSUMMARY_RESTART, CMDSYNTAX_RESTART, Command.NO_ARGUMENTS);
        }

        #region /shutdown

        private const string CMDKEYS_SHUTDOWN = "shutdown";
        private const string CMDKEYS_SHUTDOWN_ALT = "kys";
        private const string CMDSYNTAX_SHUTDOWN = "shutdown";
        private const string CMDSYNTAX_SHUTDOWN_ALT = "/kys";
        private const string CMDSUMMARY_SHUTDOWN = "Shuts down the bot";

        public void HandleShutdownCommand(CommandContext context)
        {
            Var.running = false;
        }

        #endregion
        #region /restart

        private const string CMDKEYS_RESTART = "restart";
        private const string CMDSYNTAX_RESTART = "restart";
        private const string CMDSUMMARY_RESTART = "Restarts the bot";

        public void HandleRestartCommand(CommandContext context)
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
        public HelpCommands()
        {
            // help (list)
            CommandService.AddCommand(new CommandKeys(CMDKEYS_HELP_LIST), HandleHelpCommand, AccessLevel.Basic, CMDSUMMARY_HELP_LIST, CMDSYNTAX_HELP_LIST, Command.NO_ARGUMENTS);
            // help (specific)
            CommandService.AddCommand(new CommandKeys(CMDKEYS_HELP_SPECIFIC, 2, 6), HandleHelpCommandSpecific, AccessLevel.Basic, CMDSUMMARY_HELP_SPECIFIC, CMDSYNTAX_HELP_SPECIFIC, CMDARGS_HELP_SPECIFIC);
        }

        #region /help (list)

        private const string CMDKEYS_HELP_LIST = "help";
        private const string CMDSYNTAX_HELP_LIST = "help";
        private const string CMDSUMMARY_HELP_LIST = "Lists a summary for all commands you have access to";

        public async Task HandleHelpCommand(CommandContext context)
        {
            AccessLevel userLevel = SettingsModel.GetUserAccessLevel(context.Guild.GetUser(context.User.Id));

            List<EmbedField> embeds = new List<EmbedField>();

            foreach (Command cmd in CommandService.commands)
            {
                if (cmd.HasPermission(userLevel))
                {
                    embeds.Add(new EmbedField(CommandService.Prefix + cmd.Syntax, cmd.Summary));
                }
            }
            await context.Channel.SendSafeEmbedList(string.Format("Your access level is `{0}`. Available commands:", userLevel.ToString()), embeds, string.Format("Use `{0}help <cmdname>` to see syntax.", CommandService.Prefix));
        }

        #endregion
        #region /help (specific)

        private const string CMDKEYS_HELP_SPECIFIC = "help";
        private const string CMDSYNTAX_HELP_SPECIFIC = "help {<CommandKeys>}";
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
            if (CommandService.TryGetCommands(keys, out List<Command> cmds))
            {
                foreach (Command cmd in cmds)
                {
                    if (cmd.HasPermission(userLevel))
                    {
                        EmbedBuilder embedmessage = new EmbedBuilder();
                        embedmessage.Color = Var.BOTCOLOR;
                        embedmessage.Title = string.Format("Help for command `{0}{1}`", CommandService.Prefix, cmd.Key.KeyList);
                        embedmessage.AddField("Description", cmd.Summary);
                        embedmessage.AddField("Required Access Level", cmd.AccessLevel.ToString());
                        embedmessage.AddField("Syntax", Macros.MultiLineCodeBlock(CommandService.Prefix + cmd.Syntax));
                        if (!cmd.ArgumentHelp.Equals(Command.NO_ARGUMENTS))
                        {
                            embedmessage.AddField("Arguments", Macros.MultiLineCodeBlock(cmd.ArgumentHelp));
                        }
                        await context.Channel.SendEmbedAsync(embedmessage);
                    }
                    else
                    {
                        await context.Channel.SendEmbedAsync(string.Format("Unsufficient permissions to access the command summary for `{0}{1}`!", CommandService.Prefix, cmd.Key.KeyList));
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
