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
            if (context.ArgCnt >= 3)
            {
                RestTextChannel NewMissionChannel =
                await MissionModel.CreateMission(context.Args[1], context.Message.MentionedUsers, context.Guild);
                await context.Channel.SendMessageAsync("Successfully created new Mission. Check it out: " + NewMissionChannel.Mention);
            }
            else
            {
                await context.Channel.SendMessageAsync("Please specify both user and platform. '/help createmission' for more info.");
            }
        }

        public async Task HandleCloseMissionCommand(CommandContext context)
        {
            await MissionModel.DeleteMission(context.Channel.Id, context.Guild.Id);
        }

        public MissionCommands(CommandService service)
        {
            AccessLevel pilot = AccessLevel.Pilot;
            string summary = "Creates a new mission room.";
            string arguments =
                "    <NameSuffixes>\n" +
                "Whatever the mission channel name should list right from number\n" +
                "    {<@Explorers>}\n" +
                "Mention all explorers here that are a part of that mission";
            service.AddCommand(new CommandKeys("createmission", 3, 1000), HandleCreateRoomCommand, pilot, summary, "/createmission <NameSuffixes> {<@Explorers>}", arguments);
            summary = "Closes a mission room.";
            service.AddCommand(new CommandKeys("closemission"), HandleCloseMissionCommand, pilot, summary, "/closemission", Command.NO_ARGUMENTS);
        }
    }
}
