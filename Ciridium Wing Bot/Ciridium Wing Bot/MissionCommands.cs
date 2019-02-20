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
            // closemission
            service.AddCommand(new CommandKeys(CMDKEYS_CLOSEMISSION), HandleCloseMissionCommand, AccessLevel.Moderator, CMDSUMMARY_CLOSEMISSION, CMDSYNTAX_CLOSEMISSION, Command.NO_ARGUMENTS);
            // listmissions
            service.AddCommand(new CommandKeys(CMDKEYS_LISTMISSIONS), HandleListMissionsCommand, AccessLevel.Moderator, CMDSUMMARY_LISTMISSIONS, CMDSYNTAX_LISTMISSIONS, Command.NO_ARGUMENTS);
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
            RestTextChannel NewMissionChannel =
            await MissionModel.CreateMission(context.Args[1], context.Message.MentionedUsers, context.Guild, context.User);
            await context.Channel.SendEmbedAsync("Successfully created new Mission. Check it out: " + NewMissionChannel.Mention);
        }

        #endregion
        #region /closemission

        private const string CMDKEYS_CLOSEMISSION = "closemission";
        private const string CMDSYNTAX_CLOSEMISSION = "/closemission";
        private const string CMDSUMMARY_CLOSEMISSION = "Closes a mission room";

        public async Task HandleCloseMissionCommand(CommandContext context)
        {
            string channelname = context.Channel.Name;
            if (MissionModel.IsMissionChannel(context.Channel.Id, context.Guild.Id))
            {
                await MissionModel.DeleteMission(context.Channel.Id, context.Guild.Id);
                await SettingsModel.SendDebugMessage(string.Format("Closed mission {0}", channelname), DebugCategories.missions);
            }
            else
            {
                await context.Channel.SendEmbedAsync("Could not verify this channel as a mission channel! Ask an admin to delete it.", true);
            }
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
            } else
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = Var.BOTCOLOR;
                embed.Title = "**__Currently active mission channels__**";
                foreach (ulong missionchannel in MissionModel.missionList)
                {
                    SocketTextChannel channel = context.Guild.GetTextChannel(missionchannel);
                    embed.AddField(" "+channel.Mention+" ", string.Format("```ID: {0}```", missionchannel));
                }
                await context.Channel.SendEmbedAsync(embed);
            }
        }

        #endregion
    }
}
