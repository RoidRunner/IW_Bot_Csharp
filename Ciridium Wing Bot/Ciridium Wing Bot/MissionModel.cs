using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Ciridium
{
    static class MissionModel
    {
        public static async Task Init ()
        {
            missionList = new List<ulong>();
            await LoadMissions();
        }

        public static List<ulong> missionList;

        public static async Task<RestTextChannel> CreateMission(string platform, IReadOnlyCollection<SocketUser> explorers, SocketGuild guild, SocketUser source)
        {
            int missionnumber = MissionSettingsModel.NextMissionNumber;
            string channelname = string.Format("mission_{0}_{1}", missionnumber, platform);


            List<ulong> explorerIDs = new List<ulong>();

            foreach (IUser user in explorers)
            {
                explorerIDs.Add(user.Id);
            }

            string pingstring = ResourcesModel.GetMentionsFromUserIdList(explorerIDs);
            RestTextChannel NewMissionChannel = await guild.CreateTextChannelAsync(channelname);

            if (MissionSettingsModel.DefaultTopic.Contains("{0}"))
            {
                await NewMissionChannel.ModifyAsync(TextChannelProperties =>
                {
                    TextChannelProperties.CategoryId = MissionSettingsModel.MissionCategoryId;
                    TextChannelProperties.Topic = string.Format(MissionSettingsModel.DefaultTopic, pingstring);
                });
            }
            else
            {
                await NewMissionChannel.ModifyAsync(TextChannelProperties =>
                {
                    TextChannelProperties.CategoryId = MissionSettingsModel.MissionCategoryId;
                    TextChannelProperties.Topic = MissionSettingsModel.DefaultTopic;
                });
            }

            await NewMissionChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, MissionSettingsModel.EveryonePerms);
            foreach (IUser user in explorers)
            {
                await NewMissionChannel.AddPermissionOverwriteAsync(user, MissionSettingsModel.ExplorerPerms);
            }

            
            missionList.Add(NewMissionChannel.Id);
            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Var.BOTCOLOR;
            if (MissionSettingsModel.ExplorerQuestions.Contains("{0}"))
            {
                embed.Description = string.Format(MissionSettingsModel.ExplorerQuestions, pingstring);
            }
            else
            {
                embed.Description = MissionSettingsModel.ExplorerQuestions;
            }
            await NewMissionChannel.SendMessageAsync(pingstring, embed:embed.Build());

            await SettingsModel.SendDebugMessage(string.Format("Created new mission room {0} on behalf of {1} for explorer {2}", NewMissionChannel.Mention, source.Mention, pingstring), DebugCategories.missions);
            await SaveMissions();
            await MissionSettingsModel.SaveMissionSettings();
            return NewMissionChannel;
        }

        public static async Task DeleteMission(ulong channelId, ulong guildId)
        {
            if (IsMissionChannel(channelId, guildId))
            {
                missionList.Remove(channelId);
                await Var.client.GetGuild(guildId).GetTextChannel(channelId).DeleteAsync();
                await SaveMissions();
            }
        }

        public static bool IsMissionChannel(ulong channelId, ulong guildId)
        {
            bool result = false;
            SocketGuild guild = Var.client.GetGuild(guildId);
            if (guild != null)
            {
                SocketGuildChannel channel = guild.GetChannel(channelId);
                if (channel != null)
                {
                    foreach (ulong missionChannelId in missionList)
                    {
                        if (missionChannelId == channelId && channel.Name.StartsWith("mission"))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            return result;

        }

        #region JSON Save/Load

        private const string JSON_MISSIONS = "Missions";

        public static async Task LoadMissions()
        {
            JSONObject json = await ResourcesModel.LoadToJSONObject(ResourcesModel.Path + @"Missions.json");
                if (json.IsArray)
                {
                    foreach (JSONObject jMission in json.list)
                    {
                        ulong nId;
                        if (ulong.TryParse(jMission.str, out nId))
                        {
                            missionList.Add(nId);
                        }
                    }
            }
        }

        public static async Task SaveMissions()
        {
            JSONObject json = new JSONObject();
            foreach (ulong mission in missionList)
            {
                json.Add(mission.ToString());
            }
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.Path + @"Missions.json", json);
        }
        #endregion
    }
}
