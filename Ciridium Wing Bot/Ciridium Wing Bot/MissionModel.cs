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
        static MissionModel()
        {
            missionList = new List<ulong>();
        }

        public static List<ulong> missionList;

        public static async Task<RestTextChannel> CreateMission(string platform, IReadOnlyCollection<SocketUser> explorers, SocketGuild guild, SocketUser source)
        {
            int failcode = 0;
            try
            {
                // [00] retrieving mission number and other variables
                int missionnumber = MissionSettingsModel.NextMissionNumber;
                string channelname = string.Format("mission_{0}_{1}", missionnumber, platform);

                SocketGuildChannel missioncategory = guild.GetChannel(MissionSettingsModel.MissionCategoryId);

                failcode++;
                // [01] Creating a temporary ulong list of explorerIds to generate a ping string
                List<ulong> explorerIDs = new List<ulong>();

                foreach (IUser user in explorers)
                {
                    explorerIDs.Add(user.Id);
                }
                string pingstring = ResourcesModel.GetMentionsFromUserIdList(explorerIDs);

                failcode++;

                // [02] Create new channel
                RestTextChannel NewMissionChannel = await guild.CreateTextChannelAsync(channelname);

                failcode++;

                // [03] Sync permissions with mission category
                foreach (Overwrite perm in missioncategory.PermissionOverwrites)
                {
                    if (perm.TargetType == PermissionTarget.Role)
                    {
                        IRole role = guild.GetRole(perm.TargetId);
                        await NewMissionChannel.AddPermissionOverwriteAsync(role, perm.Permissions);
                    }
                    else
                    {
                        IUser user = guild.GetUser(perm.TargetId);
                        await NewMissionChannel.AddPermissionOverwriteAsync(user, perm.Permissions);
                    }
                }

                failcode++;

                // [04] Add explorers  to mission room
                foreach (IUser user in explorers)
                {
                    await NewMissionChannel.AddPermissionOverwriteAsync(user, MissionSettingsModel.ExplorerPerms);
                }

                failcode++;

                // [05] Move to mission category and add channel topic
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

                failcode++;

                // [06] Sending explorer questions
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
                await NewMissionChannel.SendMessageAsync(pingstring, embed: embed.Build());

                failcode++;

                // [07] Sending debug message, adding to mission list and returning
                await SettingsModel.SendDebugMessage(string.Format("Created new mission room {0} on behalf of {1} for explorer {2}", NewMissionChannel.Mention, source.Mention, pingstring), DebugCategories.missions);
                missionList.Add(NewMissionChannel.Id);
                await SaveMissions();
                await MissionSettingsModel.SaveMissionSettings();
                return NewMissionChannel;
            }
            catch (Exception e)
            {
                await SettingsModel.SendDebugMessage(string.Format("Creation of new mission channel failed. Failcode: {0}", failcode.ToString("X")), DebugCategories.missions);
                throw e;
            }
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
            LoadFileOperation operation = await ResourcesModel.LoadToJSONObject(ResourcesModel.MissionsFilePath);
            if (operation.Success)
            {
                JSONObject json = operation.Result;
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
        }

        public static async Task SaveMissions()
        {
            JSONObject json = new JSONObject();
            foreach (ulong mission in missionList)
            {
                json.Add(mission.ToString());
            }
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.MissionsFilePath, json);
        }
        #endregion
    }
}
