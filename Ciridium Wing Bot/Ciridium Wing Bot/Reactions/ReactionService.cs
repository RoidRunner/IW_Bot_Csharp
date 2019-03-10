using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium.Reactions
{
    static class ReactionService
    {
        internal static Dictionary<string, ReactionCommand> ReactionCommands;

        static ReactionService()
        {
            ReactionCommands = new Dictionary<string, ReactionCommand>();
        }

        internal static void AddReactionCommand(ReactionCommand command)
        {
            ReactionCommands.TryAdd(command.Emote, command);
        }

        internal static async Task HandleReactionAdded(ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (ReactionCommands.TryGetValue(reaction.Emote.Name, out ReactionCommand reactionCommand))
            {
                SocketGuildUser user = Var.Guild.GetUser(reaction.UserId);

                if (user != null)
                {
                    AccessLevel userLevel = SettingsModel.GetUserAccessLevel(user);
                    if (reactionCommand.HasPermission(userLevel))
                    {
                        IMessage message = await channel.GetMessageAsync(reaction.MessageId);
                        ReactionContext context = new ReactionContext(message, user, channel, reaction);
                        try
                        {
                            await reactionCommand.HandleReaction(context);
                        }
                        catch (Exception e)
                        {
                            await SendCommandExecutionExceptionMessage(e, context, reactionCommand);
                        }

                    }
                }
            }
        }

        private static async Task SendCommandExecutionExceptionMessage(Exception e, ReactionContext context, ReactionCommand command)
        {
            await context.Channel.SendEmbedAsync("Something went horribly wrong trying to execute your emojicommand! I have contacted my creators to help fix this issue!", true);
            ISocketMessageChannel channel = Var.client.GetChannel(SettingsModel.DebugMessageChannelId) as ISocketMessageChannel;
            if (channel != null)
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = Var.ERRORCOLOR;
                embed.Title = "**__Exception__**";
                embed.AddField("Command", command.Emote);
                embed.AddField("Location", Var.Guild.GetTextChannel(context.Channel.Id).Mention);
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
                SocketRole botDevRole = Var.Guild.GetRole(SettingsModel.BotDevRole);
                if (botDevRole != null)
                {
                    message = botDevRole.Mention;
                }
                await channel.SendMessageAsync(message, embed: embed.Build());
            }
            await BotCore.Logger(new LogMessage(LogSeverity.Error, "CMDSERVICE", string.Format("An Exception occured while trying to execute command `/{0}`.Message: '{1}'\nStackTrace {2}", command.Emote, e.Message, e.StackTrace)));
        }
    }

    internal delegate Task HandleReaction(ReactionContext context);

}
