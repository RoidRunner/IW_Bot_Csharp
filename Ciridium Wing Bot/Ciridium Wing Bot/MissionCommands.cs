using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;

namespace Ciridium
{
    class MissionCommands
    {
        public async Task HandleCreateRoomCommand(CommandContext context)
        {
            RestTextChannel NewMissionChannel =
            await MissionModel.CreateMission(context.Args[1], context.Message.MentionedUsers, context.Guild);
            await context.Channel.SendEmbedAsync("Successfully created new Mission. Check it out: " + NewMissionChannel.Mention);
        }

        public async Task HandleCloseMissionCommand(CommandContext context)
        {
            await MissionModel.DeleteMission(context.Channel.Id, context.Guild.Id);
        }

        public MissionCommands(CommandService service)
        {
            string summary = "Creates a new mission room.";
            string arguments =
                "    <NameSuffixes>\n" +
                "Whatever the mission channel name should list right from number\n" +
                "    {<@Explorers>}\n" +
                "Mention all explorers here that are a part of that mission";
            service.AddCommand(new CommandKeys("createmission", 3, 1000), HandleCreateRoomCommand, AccessLevel.Pilot, summary, "/createmission <NameSuffixes> {<@Explorers>}", arguments);
            summary = "Closes a mission room.";
            service.AddCommand(new CommandKeys("closemission"), HandleCloseMissionCommand, AccessLevel.Moderator, summary, "/closemission", Command.NO_ARGUMENTS);
        }
    }
}
