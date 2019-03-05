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

        /// <summary>
        /// Creates a new mission channel
        /// </summary>
        /// <param name="channel_name_suffix">The suffix added to the channel name</param>
        /// <param name="explorers">All explorers to be part of this new mission channel</param>
        /// <param name="guild">The guild containing the mission channel category</param>
        /// <param name="source">The user that issued the createmission command</param>
        /// <returns>The RestTextChannel created by the command</returns>
        public static async Task<RestTextChannel> CreateMission(string channel_name_suffix, IReadOnlyCollection<SocketUser> explorers, SocketGuild guild, SocketUser source)
        {
            int failcode = 0;
            try
            {
                // [00] retrieving mission number and other variables
                int missionnumber = MissionSettingsModel.NextMissionNumber;
                string channelname = string.Format("mission_{0}_{1}", missionnumber, channel_name_suffix);

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
                string channeltopic;
                if (MissionSettingsModel.DefaultTopic.Contains("{0}"))
                {
                    channeltopic = string.Format(MissionSettingsModel.DefaultTopic, pingstring);
                }
                else
                {
                    channeltopic = MissionSettingsModel.DefaultTopic;
                }
                await NewMissionChannel.ModifyAsync(TextChannelProperties =>
                {
                    TextChannelProperties.CategoryId = MissionSettingsModel.MissionCategoryId;
                    TextChannelProperties.Topic = channeltopic;
                });

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

        /// <summary>
        /// Deletes a mission channel
        /// </summary>
        /// <param name="channelId">ID of the channel</param>
        /// <param name="guildId">ID of the guild containing the channel</param>
        public static async Task DeleteMission(ulong channelId, ulong guildId)
        {
            if (IsMissionChannel(channelId, guildId))
            {
                missionList.Remove(channelId);
                await Var.client.GetGuild(guildId).GetTextChannel(channelId).DeleteAsync();
                await SaveMissions();
            }
        }

        /// <summary>
        /// Check for a channel to be a mission channel
        /// </summary>
        /// <param name="channelId">ID of the channel</param>
        /// <param name="guildId">ID of the guild containing the channel</param>
        /// <returns>true if channel is confirmed a mission channel</returns>
        public static bool IsMissionChannel(ulong channelId, ulong guildId)
        {
            bool result = false;
            SocketGuild guild = Var.client.GetGuild(guildId);
            if (guild != null)
            {
                SocketGuildChannel channel = guild.GetChannel(channelId);
                if (channel != null)
                {
                    result = missionList.Contains(channelId) && channel.Name.StartsWith("mission");
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
