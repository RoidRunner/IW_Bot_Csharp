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
    class PingCommand
    {
        public void RegisterCommand(CommandService service)
        {
            string syntax = "/ping";
            string summary = "Pings the user back. Use if bot seems unresponsive.";
            Var.cmdService.AddCommand(new CommandKeys("ping"), HandleCommand, AccessLevel.Basic, summary, syntax);
        }

        public async Task HandleCommand(CommandContext context)
        {
            await context.Channel.SendMessageAsync(string.Format("Hi {0}", context.User.Mention));
        }
    }

    class ListChannelsCommand
    {
        public async Task HandleCommand(SocketCommandContext context)
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

        public void RegisterCommand(CommandService service)
        {
            string summary = "Lists all Channels & Categorychannels on current server.";
            Var.cmdService.AddCommand(new CommandKeys("listchannels"), HandleCommand, AccessLevel.Moderator, summary, "/listchannels");
        }
    }

    class PrintTopicCommand
    {
        public async Task HandleCommand(SocketCommandContext context)
        {
            ITextChannel channel = context.Channel as ITextChannel;
            if (channel != null)
            {
                await context.Channel.SendMessageAsync(channel.Topic);
            }
        }

        public void RegisterCommand(CommandService service)
        {
            string summary = "Prints out the channels topic";
            Var.cmdService.AddCommand(new CommandKeys("topic"), HandleCommand, AccessLevel.Pilot, summary, "/topic");
        }
    }

    class GetUserInfoCommand
    {
        public async Task HandleCommand(SocketCommandContext context)
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

        public void RegisterCommand(CommandService service)
        {
            string summary = "Prints out some debug info on all users mentioned.";
            string syntax = "/userinfo {<@user>}";
            Var.cmdService.AddCommand(new CommandKeys("userinfo", 2, 1000), HandleCommand, AccessLevel.Moderator, summary, syntax);
        }
    }

    class ShutdownCommand
    {
        public async Task HandleCommand(SocketCommandContext context)
        {
            await context.Channel.SendMessageAsync("Shutting down ...");
            Var.running = false;
        }

        public void RegisterCommand(CommandService service)
        {
            string summary = "Shuts down the bot";
            Var.cmdService.AddCommand(new CommandKeys("kys"), HandleCommand, AccessLevel.Moderator, summary, "/kys");
            Var.cmdService.AddCommand(new CommandKeys("shutdown"), HandleCommand, AccessLevel.Moderator, summary, "/shutdown");
        }
    }

    class RestartCommand
    {
        public async Task HandleCommand(SocketCommandContext context)
        {
            if (SettingsModel.UserIsBotAdmin(context.User.Id))
            {
                await context.Channel.SendMessageAsync("Restarting ...");
                Var.running = false;
                System.Diagnostics.Process.Start(Environment.CurrentDirectory);
            }
        }

        public void RegisterCommand(CommandService service)
        {
            string summary = "Restarts the bot.";
            Var.cmdService.AddCommand(new CommandKeys("restart"), HandleCommand, AccessLevel.Moderator, summary, "/restart");
        }
    }
}
