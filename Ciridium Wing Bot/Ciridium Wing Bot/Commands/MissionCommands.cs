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
        public MissionCommands(CommandService service)
        {
            // createmission
            service.AddCommand(new CommandKeys(CMDKEYS_CREATEMISSION, 3, 1000), HandleCreateRoomCommand, AccessLevel.Pilot, CMDSUMMARY_CREATEMISSION, CMDSYNTAX_CREATEMISSION, CMDARGS_CREATEMISSION);
            // enlistmission
            service.AddCommand(new CommandKeys(CMDKEYS_ENLISTMISSION, 2, 2), HandleEnlistMissionCommand, AccessLevel.Director, CMDSUMMARY_ENLISTMISSION, CMDSYNTAX_ENLISTMISSION, CMDARGS_ENLISTMISSION);
            // completemission
            service.AddCommand(new CommandKeys(CMDKEYS_COMPLETEMISSION), HandleCompleteMissionCommand, AccessLevel.Pilot, CMDSUMMARY_COMPLETEMISSION, CMDSYNTAX_COMPLETEMISSION, Command.NO_ARGUMENTS);
            // closemission
            service.AddCommand(new CommandKeys(CMDKEYS_CLOSEMISSION, 2, 2), HandleCloseMissionCommand, AccessLevel.Dispatch, CMDSUMMARY_CLOSEMISSION, CMDSYNTAX_CLOSEMISSION, CMDARGS_CLOSEMISSION);
            // unlistmission
            service.AddCommand(new CommandKeys(CMDKEYS_UNLISTMISSION, 2, 2), HandleUnlistMissionCommand, AccessLevel.Director, CMDSUMMARY_UNLISTMISSION, CMDSYNTAX_UNLISTMISSION, CMDARGS_UNLISTMISSION);
            // listmissions
            service.AddCommand(new CommandKeys(CMDKEYS_LISTMISSIONS), HandleListMissionsCommand, AccessLevel.Director, CMDSUMMARY_LISTMISSIONS, CMDSYNTAX_LISTMISSIONS, Command.NO_ARGUMENTS);
        }

        #region /createmission

        private const string CMDKEYS_CREATEMISSION = "createmission";
        private const string CMDSYNTAX_CREATEMISSION = "/createmission <NameSuffixes> {<@Explorers>}";
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
        private const string CMDSYNTAX_ENLISTMISSION = "/enlistmission <ChannelId>";
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
        private const string CMDSYNTAX_COMPLETEMISSION = "/completemission";
        private const string CMDSUMMARY_COMPLETEMISSION = "Notifies the explorer to leave a testimonial and the dispatch to file a mission report";

        public async Task HandleCompleteMissionCommand(CommandContext context)
        {
            if (MissionModel.IsMissionChannel(context.Channel.Id, context.Guild.Id))
            {
                ITextChannel channel = context.Channel as ITextChannel;
                if (channel != null)
                {
                    await context.Channel.SendEmbedAsync(channel.Topic);
                }
                await context.Channel.SendEmbedAsync(MissionSettingsModel.TestimonialPrompt);
                await context.Channel.SendEmbedAsync(MissionSettingsModel.FileReportPrompt);
            }
            else
            {
                await context.Channel.SendEmbedAsync("Could not verify this channel as a mission channel!", true);
            }
        }

        #endregion
        #region /closemission

        private const string CMDKEYS_CLOSEMISSION = "closemission";
        private const string CMDSYNTAX_CLOSEMISSION = "/closemission <ChannelId>";
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
        private const string CMDSYNTAX_UNLISTMISSION = "/unlistmission <MissionId>";
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
        private const string CMDSYNTAX_LISTMISSIONS = "/listmissions";
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
    }
}
