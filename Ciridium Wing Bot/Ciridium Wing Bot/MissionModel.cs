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
        public static void Init ()
        {
            missionList = new List<Mission>();
        }

        public static List<Mission> missionList;

        public static async Task<RestTextChannel> CreateMission(Platform platform, IReadOnlyCollection<SocketUser> explorers, SocketGuild guild)
        {
            int missionnumber = MissionSettingsModel.NextMissionNumber;
            string channelname = string.Format("mission_{0}_{1}", missionnumber, platform.ToString());
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

            
            Mission newMission = new Mission() {
                ChannelId = NewMissionChannel.Id,
                Number = missionnumber,
                Platform = platform,
                ExplorerIds = explorerIDs
            };
            missionList.Add(newMission);
            await NewMissionChannel.SendMessageAsync(string.Format(MissionSettingsModel.ExplorerQuestions, ResourcesModel.GetMentionsFromUserIdList(explorerIDs)));

            return NewMissionChannel;
        }

        public static Mission GetMission(ulong channelID)
        {
            foreach (Mission mission in missionList)
            {
                if (mission.ChannelId == channelID)
                {
                    return mission;
                }
            }
            return null;
        }

        public static bool IsMissionChannel(ulong Id)
        {
            bool result = false;
            foreach (Mission mission in missionList)
            {
                if (mission.ChannelId == Id)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        #region JSON Save/Load

        private const string JSON_MISSIONS = "Missions";

        public static async Task loadMissions()
        {
            JSONObject json = await ResourcesModel.LoadToJSONObject(ResourcesModel.Path + @"Missions.json");
            if (json.HasField(JSON_MISSIONS))
            {
                JSONObject jMissions = json[JSON_MISSIONS];
                if (jMissions.IsArray)
                {
                    foreach (var jMission in jMissions)
                    {
                        missionList.Add(new Mission(jMission));
                    }
                }
            }
        }

        public static async Task saveMissions()
        {
            JSONObject json = new JSONObject();
            JSONObject jMissions = new JSONObject();
            foreach (Mission mission in missionList)
            {
                jMissions.Add(mission.ToJSON());
            }
            json.AddField(JSON_MISSIONS, jMissions);
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.Path + @"Missions.json", json);
        }
        #endregion
    }

    class Mission
    {
        public ulong ChannelId;
        public int Number;
        public Platform Platform;
        public List<ulong> ExplorerIds;
        public bool IsIntact { get { return ChannelId != 0 && Number != -1; } }

        public string GetChannelName()
        {
            return string.Format("mission_{0}_{1}", Number, Platform.ToString());
        }

        public Mission(JSONObject json)
        {
            string channelID = "";
            int jPlatform = 0;
            if (json.GetField(ref channelID, JSON_CHANNELID) &&
                json.GetField(ref Number, JSON_NUMBER) && 
                json.GetField(ref jPlatform, JSON_PLATFORM) && json.HasField(JSON_EXPLORERS))
            {
                Platform = (Platform)jPlatform;
                if (!ulong.TryParse(channelID, out ChannelId))
                {
                    ChannelId = 0;
                }
                JSONObject explorerList = json[JSON_EXPLORERS];
                if (explorerList.IsArray && explorerList.list != null)
                {
                    foreach (var explorer in explorerList.list)
                    {
                        ulong nID;
                        if (ulong.TryParse(explorer.str, out nID))
                        {
                            ExplorerIds.Add(nID);
                        }
                    }
                }
            }
            else
            {
                ChannelId = 0;
                Platform = Platform.NULL;
                Number = -1;
            }
        }

        public Mission()
        {
        }

        private const string JSON_CHANNELID = "ChannelID";
        private const string JSON_NUMBER = "Number";
        private const string JSON_PLATFORM = "Platform";
        private const string JSON_EXPLORERS = "Explorers";

        public JSONObject ToJSON()
        {
            JSONObject json = new JSONObject();
            json.AddField(JSON_CHANNELID, ChannelId.ToString());
            json.AddField(JSON_NUMBER, Number);
            json.AddField(JSON_PLATFORM, (int)Platform);
            JSONObject explorerList = new JSONObject();
            foreach (var explorerId in ExplorerIds)
            {
                explorerList.Add(explorerId.ToString());
            }
            return json;
        }
    }

    public enum Platform
    {
        NULL,
        pc,
        xbox,
        ps4
    }
}
