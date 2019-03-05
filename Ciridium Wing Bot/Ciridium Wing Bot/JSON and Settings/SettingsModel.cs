using Discord.WebSocket;
using Discord;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ciridium
{
    /// <summary>
    /// Handles Saving/Loading and storing of settings, aswell as some utility methods
    /// </summary>
    static class SettingsModel
    {
        #region Variables
        /// <summary>
        /// The bot token used to log into discord
        /// </summary>
        internal static string token;
        /// <summary>
        /// A list containing all Bot Admin IDs
        /// </summary>
        public static List<ulong> botAdminIDs;
        /// <summary>
        /// The ID of the bots Welcoming Message Channel
        /// </summary>
        public static ulong WelcomeMessageChannelId = 0;
        /// <summary>
        /// The ID of the bots Debug Message Channel
        /// </summary>
        public static ulong DebugMessageChannelId = 0;
        /// <summary>
        /// The ID of the moderator role
        /// </summary>
        public static ulong ModeratorRole = 0;
        /// <summary>
        /// The ID of the dispatch role
        /// </summary>
        public static ulong DispatchRole = 0;
        /// <summary>
        /// The ID of the pilot role
        /// </summary>
        public static ulong EscortPilotRole = 0;
        /// <summary>
        /// The ID of the bot dev role (pinging on error messages)
        /// </summary>
        public static ulong BotDevRole = 0;
        /// <summary>
        /// The Formatting string for the Welcoming Message. {0} is replaced with the new users mention.
        /// </summary>
        public static string welcomingMessage = "Hi {0}";

        #endregion
        #region Initialization

        static SettingsModel()
        {
            botAdminIDs = new List<ulong>();
        }

        /// <summary>
        /// Initializes variables, loads settings and checks if loading was successful
        /// </summary>
        /// <param name="nclient"></param>
        /// <returns>False if loading of the critical variables (bottoken, botadminIDs) fails</returns>
        public static async Task<bool> LoadSettingsAndCheckToken(DiscordSocketClient nclient)
        {
            await loadSettings();
            return token != null && botAdminIDs.Count > 0;
        }

        #endregion
        #region JSON, Save/Load

        private const string JSON_BOTTOKEN = "BotToken";
        private const string JSON_ADMINIDS = "BotAdminIDs";
        private const string JSON_ENABLEDEBUG = "DebugEnabled";
        private const string JSON_DEBUGCHANNEL = "DebugChannelID";
        private const string JSON_WELCOMINGCHANNEL = "WelcomingChannelID";
        private const string JSON_WELCOMINGMESSAGE = "WelcomingMessage";
        private const string JSON_PILOTROLE = "PilotRole";
        private const string JSON_MODERATORROLE = "ModeratorRole";
        private const string JSON_BOTDEVROLE = "BotDevRole";
        private const string JSON_DISPATCHROLE = "DispatchRole";

        /// <summary>
        /// Loads and applies Settings from appdata/locallow/Ciridium Wing Bot/Settings.json
        /// </summary>
        /// <returns></returns>
        private static async Task loadSettings()
        {
            LoadFileOperation operation = await ResourcesModel.LoadToJSONObject(ResourcesModel.SettingsFilePath);
            if (operation.Success)
            {
                JSONObject json = operation.Result;
                if (json.GetField(ref token, JSON_BOTTOKEN) && json.HasField(JSON_ADMINIDS))
                {
                    JSONObject botadmins = json[JSON_ADMINIDS];
                    if (botadmins.IsArray && botadmins.list != null)
                    {
                        foreach (var admin in botadmins.list)
                        {
                            ulong nID;
                            if (ulong.TryParse(admin.str, out nID))
                            {
                                botAdminIDs.Add(nID);
                            }
                        }
                    }
                    if (json.HasField(JSON_ENABLEDEBUG))
                    {
                        JSONObject debugSettings = json[JSON_ENABLEDEBUG];
                        if (debugSettings.IsArray)
                        {
                            for (int i = 0; i < debugSettings.list.Count; i++)
                            {
                                debugLogging[i] = debugSettings.list[i].b;
                            }
                        }
                    }
                    string id = "";
                    if (json.GetField(ref id, JSON_DEBUGCHANNEL))
                    {
                        ulong.TryParse(id, out DebugMessageChannelId);
                    }
                    if (json.GetField(ref id, JSON_WELCOMINGCHANNEL))
                    {
                        ulong.TryParse(id, out WelcomeMessageChannelId);
                    }
                    json.GetField(ref welcomingMessage, JSON_WELCOMINGMESSAGE);
                    if (json.GetField(ref id, JSON_MODERATORROLE))
                    {
                        ulong.TryParse(id, out ModeratorRole);
                    }
                    if (json.GetField(ref id, JSON_PILOTROLE))
                    {
                        ulong.TryParse(id, out EscortPilotRole);
                    }
                    if (json.GetField(ref id, JSON_BOTDEVROLE))
                    {
                        ulong.TryParse(id, out BotDevRole);
                    }
                    if (json.GetField(ref id, JSON_DISPATCHROLE))
                    {
                        ulong.TryParse(id, out DispatchRole);
                    }
                }
            }
        }

        /// <summary>
        /// Saves all settings to appdata/locallow/Ciridium Wing Bot/Settings.json
        /// </summary>
        internal static async Task SaveSettings()
        {
            JSONObject json = new JSONObject();

            json.AddField(JSON_BOTTOKEN, token);
            JSONObject adminIDs = new JSONObject();
            foreach (var adminID in botAdminIDs)
            {
                adminIDs.Add(adminID.ToString());
            }
            json.AddField(JSON_ADMINIDS, adminIDs);
            JSONObject debugSettings = new JSONObject();
            foreach (bool b in debugLogging)
            {
                debugSettings.Add(b);
            }
            json.AddField(JSON_ENABLEDEBUG, debugSettings);
            json.AddField(JSON_DEBUGCHANNEL, DebugMessageChannelId.ToString());
            json.AddField(JSON_WELCOMINGCHANNEL, WelcomeMessageChannelId.ToString());
            json.AddField(JSON_WELCOMINGMESSAGE, welcomingMessage);
            json.AddField(JSON_MODERATORROLE, ModeratorRole.ToString());
            json.AddField(JSON_PILOTROLE, EscortPilotRole.ToString());
            json.AddField(JSON_BOTDEVROLE, BotDevRole.ToString());
            json.AddField(JSON_DISPATCHROLE, DispatchRole.ToString());


            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.SettingsFilePath, json);
        }

        #endregion
        #region Welcoming

        internal static async Task WelcomeNewUser(SocketUser user)
        {
            if (WelcomeMessageChannelId != 0)
            {
                ISocketMessageChannel channel = Var.client.GetChannel(WelcomeMessageChannelId) as ISocketMessageChannel;
                if (channel != null)
                {
                    //await channel.SendMessageAsync(string.Format(welcomingMessage, user.Mention));
                    await channel.SendEmbedAsync(user.Mention, string.Format(welcomingMessage, user.Mention));
                }
            }
        }

        #endregion
        #region Debug

        public static bool[] debugLogging = new bool[Enum.GetValues(typeof(DebugCategories)).Length];

        public delegate Task Logger(LogMessage log);
        public static event Logger DebugMessage;

        /// <summary>
        /// Puts together a debug message for the "/settings" command
        /// </summary>
        public static EmbedBuilder DebugSettingsMessage
        {
            get
            {
                EmbedBuilder result = new EmbedBuilder();
                result.Color = Var.BOTCOLOR;
                result.Title = "**__Current Settings__**";
                result.AddField("Bot Version", "Ciridium Wing Bot " + Var.VERSION_CODE);
                StringBuilder debugSettings = new StringBuilder();
                debugSettings.Append("```");
                for (int i = 0; i < debugLogging.Length; i++)
                {
                    bool catEnabled = debugLogging[i];
                    debugSettings.Append("Debug ");
                    string padded = ((DebugCategories)i).ToString().PadRight(12);
                    debugSettings.Append(padded);
                    debugSettings.Append(": ");
                    debugSettings.AppendLine(catEnabled ? "Enabled" : "Disabled");
                }
                debugSettings.Append("```");
                result.AddField("Debug Outputs", debugSettings.ToString());
                result.AddField("Channel Settings", string.Format("```" +
                    "Debug Channel:     {0}\n" +
#if WELCOMING_MESSAGES
                    "Welcoming Channel: {1}\n" +
#endif
                    "Mission Category:  {2}```", DebugMessageChannelId, WelcomeMessageChannelId, MissionSettingsModel.MissionCategoryId));
                result.AddField("Role Settings", string.Format("```" +
                    "Moderator:         {0}\n" +
                    "Escort Pilot:      {1}\n" +
                    "Dispatch:          {2}\n" +
                    "Bot Dev:           {3}```", ModeratorRole, EscortPilotRole, DispatchRole, BotDevRole));
            return result;
            }
        }

        /// <summary>
        /// Sends a message into the Debug Message Channel if it is defined and Debug is true
        /// </summary>
        /// <param name="message">Message to send</param>
        public static async Task SendDebugMessage(string message, DebugCategories category)
        {
            if (DebugMessage != null)
            {
                await DebugMessage(new LogMessage(LogSeverity.Debug, category.ToString(), message));
            }
            if (debugLogging[(int)category] && DebugMessageChannelId != 0)
            {
                ISocketMessageChannel channel = Var.client.GetChannel(DebugMessageChannelId) as ISocketMessageChannel;
                if (channel != null)
                {
                    EmbedBuilder debugembed = new EmbedBuilder();
                    debugembed.Color = Var.BOTCOLOR;
                    debugembed.Title = string.Format("**__Debug: {0}__**", category.ToString().ToUpper());
                    debugembed.Description = message;
                    await channel.SendEmbedAsync(debugembed);
                }
            }
        }

#endregion
#region Access Levels

        /// <summary>
        /// Checks if the user is listed as bot admin
        /// </summary>
        /// <param name="userID">User ID to check</param>
        /// <returns>true if the user is a bot admin</returns>
        public static bool UserIsBotAdmin(ulong userID)
        {
            return botAdminIDs.Contains(userID);
        }

        /// <summary>
        /// Checks for the specified user having a role on a guild
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="roleID">Role ID to check</param>
        /// <returns>true if the user has the role</returns>
        public static bool UserHasRole(SocketGuildUser user, ulong roleID)
        {
            foreach (var role in user.Roles)
            {
                if (role.Id == roleID)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks a users access level
        /// </summary>
        /// <param name="user">User to check</param>
        /// <returns>the users access level</returns>
        public static AccessLevel GetUserAccessLevel(SocketGuildUser user)
        {
            if (UserIsBotAdmin(user.Id))
            {
                return AccessLevel.BotAdmin;
            }
            AccessLevel result = AccessLevel.Basic;
            foreach (var role in user.Roles)
            {
                if (role.Id == ModeratorRole && result < AccessLevel.Moderator)
                {
                    result = AccessLevel.Moderator;
                }
                else if (role.Id == DispatchRole && result < AccessLevel.Dispatch)
                {
                    result = AccessLevel.Dispatch;
                }
                else if (role.Id == EscortPilotRole && result < AccessLevel.Pilot)
                {
                    result = AccessLevel.Pilot;
                }
            }
            return result;
        }
#endregion

    }

    public enum DebugCategories
    {
        misc,
        timing,
        joinleave,
        missions
    }
}
