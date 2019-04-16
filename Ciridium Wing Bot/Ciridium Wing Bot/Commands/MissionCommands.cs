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
    class MissionCommands
    {
        public MissionCommands()
        {
            // createmission
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_CREATEMISSION, 3, 1000), HandleCreateRoomCommand, AccessLevel.Pilot, CMDSUMMARY_CREATEMISSION, CMDSYNTAX_CREATEMISSION, CMDARGS_CREATEMISSION, useTyping: true));
            // enlistmission
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_ENLISTMISSION, 2, 2), HandleEnlistMissionCommand, AccessLevel.Director, CMDSUMMARY_ENLISTMISSION, CMDSYNTAX_ENLISTMISSION, CMDARGS_ENLISTMISSION));
            // completemission
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_COMPLETEMISSION), HandleCompleteMissionCommand, AccessLevel.Pilot, CMDSUMMARY_COMPLETEMISSION, CMDSYNTAX_COMPLETEMISSION, Command.NO_ARGUMENTS, SpecialChannelType.Mission));
            // closemission
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_CLOSEMISSION, 2, 2), HandleCloseMissionCommand, AccessLevel.Dispatch, CMDSUMMARY_CLOSEMISSION, CMDSYNTAX_CLOSEMISSION, CMDARGS_CLOSEMISSION));
            // unlistmission
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_UNLISTMISSION, 2, 2), HandleUnlistMissionCommand, AccessLevel.Director, CMDSUMMARY_UNLISTMISSION, CMDSYNTAX_UNLISTMISSION, CMDARGS_UNLISTMISSION));
            // listmissions
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_LISTMISSIONS), HandleListMissionsCommand, AccessLevel.Director, CMDSUMMARY_LISTMISSIONS, CMDSYNTAX_LISTMISSIONS, Command.NO_ARGUMENTS));
            // gettopic
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_GETTOPIC), HandleGetTopicCommand, AccessLevel.Dispatch, CMDSUMMARY_GETTOPIC, CMDSYNTAX_GETTOPIC, Command.NO_ARGUMENTS, SpecialChannelType.Mission));
            // settopic
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_SETTOPIC, 3, 1000), HandleSetTopicCommand, AccessLevel.Dispatch, CMDSUMMARY_SETTOPIC, CMDSYNTAX_SETTOPIC, CMDARGS_SETTOPIC));
            // missionpoll
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_MISSIONPOLL), HandleMissionPollCommand, AccessLevel.Dispatch, CMDSUMMARY_MISSIONPOLL, CMDSYNTAX_MISSIONPOLL, Command.NO_ARGUMENTS, SpecialChannelType.Mission));
            // voicemoveuser
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_VOICEMOVEUSER, 2, 2), HandleVoiceMoveUserCommand, AccessLevel.Dispatch, CMDSUMMARY_VOICEMOVEUSER, CMDSYNTAX_VOICEMOVEUSER, CMDARGS_VOICEMOVEUSER));
        }

        #region /createmission

        private const string CMDKEYS_CREATEMISSION = "createmission";
        private const string CMDSYNTAX_CREATEMISSION = "createmission <NameSuffixes> {<@Explorers>}";
        private const string CMDSUMMARY_CREATEMISSION = "Creates a new mission room";
        private const string CMDARGS_CREATEMISSION =
                "    <NameSuffixes>\n" +
                "Whatever the mission channel name should list right from number\n" +
                "    {<@Explorers>}\n" +
                "Mention all explorers here that are a part of that mission";

        public async Task HandleCreateRoomCommand(CommandContext context)
        {
            if (context.Message.MentionedUsers.Count > 0)
            {
                RestTextChannel NewMissionChannel =
                await MissionModel.CreateMission(context.Args[1], context.Message.MentionedUsers, context.Guild, context.User);
                await context.Channel.SendEmbedAsync("Successfully created new Mission. Check it out: " + NewMissionChannel.Mention);
            }
            else
            {
                await context.Channel.SendEmbedAsync("You need to mention explorers (Example: @Explorer#0001)!");
            }
        }

        #endregion
        #region /enlistmission

        private const string CMDKEYS_ENLISTMISSION = "enlistmission";
        private const string CMDSYNTAX_ENLISTMISSION = "enlistmission <ChannelId>";
        private const string CMDSUMMARY_ENLISTMISSION = "Adds an unlisted channel to the list of mission rooms";
        private const string CMDARGS_ENLISTMISSION =
                "    <ChannelId>\n" +
                "Either a uInt64 channel Id, a channel mention or 'this' (for current channel) that marks the mission channel to be added to the missions list";

        public async Task HandleEnlistMissionCommand(CommandContext context)
        {
            bool isError = true;
            string message;

            ulong? channelId = null;
            if (context.Args[1].Equals("this"))
            {
                channelId = context.Channel.Id;
            }
            else if (context.Message.MentionedChannels.Count > 0)
            {
                channelId = new List<SocketGuildChannel>(context.Message.MentionedChannels)[0].Id;
            }
            else if (ulong.TryParse(context.Args[1], out ulong parsedChannelId))
            {
                channelId = parsedChannelId;
            }
            if (channelId != null)
            {
                if (!MissionModel.missionList.Contains((ulong)channelId))
                {
                    SocketTextChannel channel = context.Guild.GetTextChannel((ulong)channelId);
                    if (channel != null)
                    {
                        if (channel.CategoryId == MissionSettingsModel.MissionCategoryId)
                        {
                            if (channel.Name.StartsWith("mission_"))
                            {
                                isError = false;
                                MissionModel.missionList.Add((ulong)channelId);
                                message = string.Format("Added channel {0} (ID: `{1}`) to the mission list.", channel.Mention, (ulong)channelId);
                            }
                            else
                            {
                                message = "The mission channels name must start with `mission_`!";
                            }
                        }
                        else
                        {
                            message = "The mission channel must be under the mission category!";
                        }
                    }
                    else
                    {
                        message = string.Format("Could not find a channel with ID `{0}`!", (ulong)channelId);
                    }
                }
                else
                {
                    message = "The mission list already contains this mission!";
                }
            }
            else
            {
                message = "Second argument must specify a channel!";
            }
            await context.Channel.SendEmbedAsync(message, isError);
        }

        #endregion
        #region /completemission

        private const string CMDKEYS_COMPLETEMISSION = "completemission";
        private const string CMDSYNTAX_COMPLETEMISSION = "completemission";
        private const string CMDSUMMARY_COMPLETEMISSION = "Notifies the explorer to leave a testimonial and the dispatch to file a mission report";

        public async Task HandleCompleteMissionCommand(CommandContext context)
        {
            ITextChannel channel = context.Channel as ITextChannel;
            if (channel != null)
            {
                await context.Channel.SendEmbedAsync(channel.Topic);
            }
            await context.Channel.SendEmbedAsync(MissionSettingsModel.TestimonialPrompt);
            await context.Channel.SendEmbedAsync(MissionSettingsModel.FileReportPrompt);
        }

        #endregion
        #region /closemission

        private const string CMDKEYS_CLOSEMISSION = "closemission";
        private const string CMDSYNTAX_CLOSEMISSION = "closemission <ChannelId>";
        private const string CMDSUMMARY_CLOSEMISSION = "Closes a mission room";
        private const string CMDARGS_CLOSEMISSION =
                "    <ChannelId>\n" +
                "Either a uInt64 channel Id, a channel mention or 'this' (for current channel) that marks the mission channel to be closed";

        public async Task HandleCloseMissionCommand(CommandContext context)
        {
            string channelname = context.Channel.Name;
            ulong? channelId = null;
            if (context.Args[1].Equals("this"))
            {
                channelId = context.Channel.Id;
            }
            else if (context.Message.MentionedChannels.Count > 0)
            {
                channelId = new List<SocketGuildChannel>(context.Message.MentionedChannels)[0].Id;
            }
            else if (ulong.TryParse(context.Args[1], out ulong parsedChannelId))
            {
                channelId = parsedChannelId;
            }

            if (channelId != null)
            {
                if (MissionModel.IsMissionChannel((ulong)channelId, context.Guild.Id))
                {
                    await MissionModel.DeleteMission((ulong)channelId, context.Guild.Id);
                    if ((ulong)channelId != context.Channel.Id)
                    {
                        await context.Channel.SendEmbedAsync("Successfully deleted mission channel!", false);
                    }
                    await SettingsModel.SendDebugMessage(string.Format("Closed mission {0}", channelname), DebugCategories.missions);
                }
                else
                {
                    await context.Channel.SendEmbedAsync("Could not verify this channel as a mission channel! Ask an admin to delete it.", true);
                }
            }
            else
            {
                await context.Channel.SendEmbedAsync("Second argument must specify a channel!", true);
            }
        }

        #endregion
        #region /unlistmission

        private const string CMDKEYS_UNLISTMISSION = "unlistmission";
        private const string CMDSYNTAX_UNLISTMISSION = "unlistmission <MissionId>";
        private const string CMDSUMMARY_UNLISTMISSION = "Removes a (most likely lost) mission room from the mission list";
        private const string CMDARGS_UNLISTMISSION =
                "    <ChannelId>\n" +
                "A uInt64 channel Id that marks the mission Id to be removed";

        public async Task HandleUnlistMissionCommand(CommandContext context)
        {
            string channelname = context.Channel.Name;
            string message = string.Empty;
            bool error = true;
            if (ulong.TryParse(context.Args[1], out ulong parsedChannelId))
            {
                if (MissionModel.missionList.Contains(parsedChannelId))
                {
                    MissionModel.missionList.Remove(parsedChannelId);
                    await MissionModel.SaveMissions();
                    message = string.Format("Successfully removed mission `{0}` from missionlist!", context.Args[1]);
                    error = false;
                }
                else
                {
                    message = string.Format("Could not find mission Id `{0}` in missionlist!", context.Args[1]);
                }
            }
            else
            {
                message = "Could not parse second argument as an uInt64!";
            }
            await context.Channel.SendEmbedAsync(message, error);
        }

        #endregion
        #region /listmissions

        private const string CMDKEYS_LISTMISSIONS = "listmissions";
        private const string CMDSYNTAX_LISTMISSIONS = "listmissions";
        private const string CMDSUMMARY_LISTMISSIONS = "Lists all stored missions";

        public async Task HandleListMissionsCommand(CommandContext context)
        {
            if (MissionModel.missionList.Count == 0)
            {
                await context.Channel.SendEmbedAsync("No missions active!");
            }
            else
            {
                List<EmbedField> embed = new List<EmbedField>();
                foreach (ulong missionchannel in MissionModel.missionList)
                {
                    SocketTextChannel channel = context.Guild.GetTextChannel(missionchannel);
                    if (channel != null)
                    {
                        embed.Add(new EmbedField(Macros.InlineCodeBlock(missionchannel), channel.Mention));
                    }
                    else
                    {
                        embed.Add(new EmbedField(Macros.InlineCodeBlock(missionchannel), "Could not find mission channel!"));
                    }
                }
                await context.Channel.SendSafeEmbedList("**__Currently active mission channels__**", embed);
            }
        }

        #endregion
        #region /gettopic

        private const string CMDKEYS_GETTOPIC = "gettopic";
        private const string CMDSYNTAX_GETTOPIC = "gettopic";
        private const string CMDSUMMARY_GETTOPIC = "PMs the user an easy to edit version of the mission topic";

        public async Task HandleGetTopicCommand(CommandContext context)
        {
            ITextChannel channel = context.Channel as ITextChannel;
            if (channel != null)
            {
                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Var.BOTCOLOR,
                    Title = string.Format("Channel topic of channel #{0}", channel.Name),
                    Description = Macros.MultiLineCodeBlock(channel.Topic)
                };
                embed.AddField("How to apply changes", string.Format("Back in the original mission channel, use the command `{0}settopic <New Topic>` with the updated topic", CommandService.Prefix));
                await context.User.SendMessageAsync(string.Empty, false, embed.Build());
                await context.Channel.SendEmbedAsync(string.Format("{0}, I have sent you a direct message!", context.User.Mention));
            }
            else
            {
                await context.Channel.SendEmbedAsync("Internal problem converting this channel into the required channel type!", true);
            }
        }

        #endregion
        #region /settopic

        private const string CMDKEYS_SETTOPIC = "settopic";
        private const string CMDSYNTAX_SETTOPIC = "settopic <MissionChannel> {<NewTopic>}";
        private const string CMDSUMMARY_SETTOPIC = "Sets the channel topic. (!) Usually mentions/pings the explorer (!)";
        private const string CMDARGS_SETTOPIC =
                "    <MissionChannel>\n" +
                "Either a uInt64 channel Id, a channel mention or 'this' (for current channel) that marks the mission topic to update" +
                "    {<NewTopic>}\n" +
                "All arguments following the initial command identifier are copied as the new channel topic";

        public async Task HandleSetTopicCommand(CommandContext context)
        {
            if (Macros.TryParseChannelId(context.Args[1], out ulong channelId, context.Channel.Id))
            {
                if (MissionModel.IsMissionChannel(channelId, context.Guild.Id))
                {
                    string newTopic = context.Message.Content.Substring(CMDKEYS_SETTOPIC.Length + context.Args[1].Length + 3);
                    ITextChannel channel = Var.Guild.GetTextChannel(channelId);
                    if (channel != null)
                    {
                        await channel.ModifyAsync(TextChannelProperties =>
                        {
                            TextChannelProperties.Topic = newTopic;
                        });
                        await context.Channel.SendEmbedAsync("Done");
                    }
                    else
                    {
                        await context.Channel.SendEmbedAsync("Internal problem converting this channel into the required channel type!", true);
                    }
                }
                else
                {
                    await context.Channel.SendEmbedAsync("Could not verify this channel as a mission channel!", true);
                }
            }
        }

        #endregion
        #region /missionpoll

        private const string CMDKEYS_MISSIONPOLL = "missionpoll";
        private const string CMDSYNTAX_MISSIONPOLL = "missionpoll";
        private const string CMDSUMMARY_MISSIONPOLL = "Creates a new mission availability poll";

        private readonly string MISSIONPOLL_FORMAT = string.Format("{0} Available\n{1} Not available\n{2} Unsure, deciding later", UnicodeEmoteService.GetEmote(Emotes.checkmark), UnicodeEmoteService.GetEmote(Emotes.cross), UnicodeEmoteService.GetEmote(Emotes.question));

        public async Task HandleMissionPollCommand(CommandContext context)
        {
            ITextChannel channel = context.Channel as ITextChannel;
            if (channel != null)
            {
                SocketRole PilotRole = context.Guild.GetRole(SettingsModel.EscortPilotRole);
                EmbedBuilder embed = new EmbedBuilder();
                string message = string.Format("{0} Availability Poll for {1}", PilotRole.Mention, channel.Mention);
                embed.Color = Var.BOTCOLOR;
                embed.AddField("Mission Information", channel.Topic);
                embed.AddField("Poll Format", MISSIONPOLL_FORMAT);
                IUserMessage pollMessage = await channel.SendMessageAsync(message, embed: embed.Build());
                await pollMessage.AddReactionAsync(new Emote(Emotes.checkmark));
                await pollMessage.AddReactionAsync(new Emote(Emotes.cross));
                await pollMessage.AddReactionAsync(new Emote(Emotes.question));
            }
        }

        #endregion
        #region /voicemoveuser

        private const string CMDKEYS_VOICEMOVEUSER = "voicemovetome";
        private const string CMDSYNTAX_VOICEMOVEUSER = "voicemovetome <@User>";
        private const string CMDSUMMARY_VOICEMOVEUSER = "Moves a user into the channel the command issuer is currently in";
        private const string CMDARGS_VOICEMOVEUSER =
                "    <@User>\n" +
                "Either a uInt64 user Id or a user mention that marks the user to be moved into the same channel as the command issuer";

        public async Task HandleVoiceMoveUserCommand(CommandContext context)
        {
            string message = string.Empty;
            bool error = true;

            if (Macros.TryParseUserId(context.Args[1], out ulong userId, ulong.MaxValue))
            {
                SocketGuildUser targetUser = Var.Guild.GetUser(userId);
                if (targetUser != null)
                {
                    if (targetUser.Id == context.User.Id)
                    {
                        message = "Target user cannot be the command issuer!";
                    }
                    else
                    {
                        List<SocketVoiceChannel> voicechannels = new List<SocketVoiceChannel>(Var.Guild.VoiceChannels);
                        SocketVoiceChannel currentUserChannel = null;
                        SocketVoiceChannel targetChannel = null;
                        foreach (SocketVoiceChannel channel in voicechannels)
                        {
                            foreach (SocketGuildUser user in channel.Users)
                            {
                                if (user.Id == targetUser.Id)
                                {
                                    currentUserChannel = channel;
                                }
                                if (user.Id == context.User.Id)
                                {
                                    targetChannel = channel;
                                }
                            }
                            if (targetChannel != null && currentUserChannel != null)
                            {
                                break;
                            }
                        }
                        if (targetChannel != null && currentUserChannel != null)
                        {
                            if (targetChannel == currentUserChannel)
                            {
                                message = string.Format("{0} is already in voice channel {1} ;)", targetUser.Mention, targetChannel.Name);
                                error = false;
                            }
                            else
                            {
                                await targetUser.ModifyAsync(GuildUserProperties =>
                                {
                                    GuildUserProperties.Channel = targetChannel;
                                });
                                message = string.Format("Successfully moved {0} to voice channel {1}", targetUser.Mention, targetChannel.Name);
                                error = false;
                            }
                        }
                        else if (targetChannel != null && currentUserChannel == null)
                        {
                            message = string.Format("Cannot move {0} because {0} is not connected to a voice channel!", targetUser.Mention);
                        }
                        else if (targetChannel == null && currentUserChannel != null)
                        {
                            message = string.Format("You need to be in a voice channel to use this command!", targetUser.Mention);
                        }
                        else
                        {
                            message = string.Format("Both the target user and the command issuer have to be in a voice channel!", targetUser.Mention);
                        }
                    }
                }
                else
                {
                    message = "Could not parse argument#1 as a user!";
                }
            }
            else
            {
                message = "Could not parse argument#1 as a user!";
            }
            await context.Channel.SendEmbedAsync(message, error);
        }

        #endregion
    }
}
