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

        public static async Task<RestTextChannel> CreateMission(string platform, IReadOnlyCollection<SocketUser> explorers, SocketGuild guild)
        {
            int missionnumber = MissionSettingsModel.NextMissionNumber;
            string channelname = string.Format("mission_{0}_{1}", missionnumber, platform);
            RestTextChannel NewMissionChannel = await guild.CreateTextChannelAsync(channelname);

            await NewMissionChannel.ModifyAsync(TextChannelProperties => {
                TextChannelProperties.CategoryId = MissionSettingsModel.MissionCategoryId;
                TextChannelProperties.Topic = MissionSettingsModel.DefaultTopic;
            });

            List<ulong> explorerIDs = new List<ulong>();

            await NewMissionChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, MissionSettingsModel.EveryonePerms);
            foreach (IUser user in explorers)
            {
                await NewMissionChannel.AddPermissionOverwriteAsync(user, MissionSettingsModel.ExplorerPerms);
                explorerIDs.Add(user.Id);
            }

            
            missionList.Add(NewMissionChannel.Id);
            await NewMissionChannel.SendMessageAsync(string.Format(MissionSettingsModel.ExplorerQuestions, ResourcesModel.GetMentionsFromUserIdList(explorerIDs)));

            await SaveMissions();
            await MissionSettingsModel.SaveMissionSettings();
            return NewMissionChannel;
        }

        public static async Task DeleteMission(ulong channelId, ulong guildId)
        {
            if (IsMissionChannel(channelId, guildId))
            {
                missionList.Remove(channelId);
                await SaveMissions();
                await Var.client.GetGuild(guildId).GetTextChannel(channelId).DeleteAsync();
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
