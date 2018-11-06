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
                Platform platform;
                if (Enum.TryParse<Platform>(context.Args[1], out platform)) {
                    RestTextChannel NewMissionChannel =
                    await MissionModel.CreateMission(platform, context.Message.MentionedUsers, context.Guild);
                    await context.Channel.SendMessageAsync("Successfully created new Mission. Check it out: " + NewMissionChannel.Mention);
                } else
                {
                    await context.Channel.SendMessageAsync("Please specify a valid platform. Valid are: 'pc', 'ps4', 'xbox'");
                }
            } else
            {
                await context.Channel.SendMessageAsync("Please specify both user and platform. '/help createmission' for more info."); 
            }
        }

        public async Task HandleSetMissionNumberCommand(CommandContext context)
        {
            if (context.ArgCnt == 2)
            {
                if (MissionModel.IsMissionChannel(context.Channel.Id))
                {
                    Mission mission = MissionModel.GetMission(context.Channel.Id);
                    int missionNumber = 0;
                    if (int.TryParse(context.Args[1], out missionNumber))
                    {
                        mission.Number = missionNumber;
                        RestTextChannel channel = context.Channel as RestTextChannel;
                        await channel.ModifyAsync(TextChannelProperties => {
                            TextChannelProperties.Name = mission.GetChannelName();
                        });
                    }
                }
            } else
            {

            }
        }

        public async Task HandleCloseMissionCommand(CommandContext context)
        {
            if (MissionModel.IsMissionChannel(context.Channel.Id))
            {
                MissionModel.missionList.Remove(MissionModel.GetMission(context.Channel.Id));
                await MissionModel.SaveMissions();
                await context.Guild.GetTextChannel(context.Channel.Id).DeleteAsync(new RequestOptions() { AuditLogReason = "Deleted channel upon request by " + context.User.Discriminator });
            }
        }

        public void RegisterCommand(CommandService service)
        {
            AccessLevel pilot = AccessLevel.Pilot;
            string summary = "Creates a new mission room. Specify platform and as many explorers as there are to add.";
            service.AddCommand(new CommandKeys("createmission", 3, 1000), HandleCreateRoomCommand, pilot, summary, "/createmission pc/xbox/ps4 {<@Explorers>}");
            summary = "If issued in a mission channel will update its number. Elsewhere will set the next missions number.";
            service.AddCommand(new CommandKeys("setmissionnumber", 2, 2), HandleSetMissionNumberCommand, AccessLevel.Moderator, summary, "/setmissionnumber <Number>");
            summary = "Closes a mission room.";
            service.AddCommand(new CommandKeys("closemission"), HandleCloseMissionCommand, pilot, summary, "/closemission");
        }
    }
}
