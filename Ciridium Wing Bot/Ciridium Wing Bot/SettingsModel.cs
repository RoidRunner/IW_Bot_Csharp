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
        private static DiscordSocketClient client;

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
        public static ulong moderatorRole = 0;
        /// <summary>
        /// The ID of the pilot role
        /// </summary>
        public static ulong pilotRole = 0;
        /// <summary>
        /// The Formatting string for the Welcoming Message. {0} is replaced with the new users mention.
        /// </summary>
        public static string welcomingMessage = "Hi {0}";

        /// <summary>
        /// Initializes variables, loads settings and checks if loading was successful
        /// </summary>
        /// <param name="nclient"></param>
        /// <returns>False if loading of the critical variables (bottoken, botadminIDs) fails</returns>
        public static async Task<bool> Init(DiscordSocketClient nclient)
        {
            client = nclient;
            botAdminIDs = new List<ulong>();
            await loadSettings();
            return token != null && botAdminIDs.Count > 0;
        }

        #region JSON, Save/Load

        private const string JSON_BOTTOKEN = "BotToken";
        private const string JSON_ADMINIDS = "BotAdminIDs";
        private const string JSON_ENABLEDEBUG = "DebugEnabled";
        private const string JSON_DEBUGCHANNEL = "DebugChannelID";
        private const string JSON_WELCOMINGCHANNEL = "WelcomingChannelID";
        private const string JSON_WELCOMINGMESSAGE = "WelcomingMessage";
        private const string JSON_PILOTROLE = "PilotRole";
        private const string JSON_MODERATORROLE = "ModeratorRole";

        /// <summary>
        /// Loads and applies Settings from appdata/locallow/Ciridium Wing Bot/Settings.json
        /// </summary>
        /// <returns></returns>
        private static async Task loadSettings()
        {
            JSONObject json = await ResourcesModel.LoadToJSONObject(ResourcesModel.Path + @"Settings.json");
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
                    ulong.TryParse(id, out moderatorRole);
                }
                if (json.GetField(ref id, JSON_PILOTROLE))
                {
                    ulong.TryParse(id, out pilotRole);
                }
            }
        }

        internal static async Task WelcomeNewUser(SocketUser user)
        {
            if (WelcomeMessageChannelId != 0)
            {
                ISocketMessageChannel channel = client.GetChannel(WelcomeMessageChannelId) as ISocketMessageChannel;
                if (channel != null)
                {
                    EmbedBuilder embed = new EmbedBuilder();
                    embed.Color = Var.BOTCOLOR;
                    embed.Description = string.Format(welcomingMessage, user.Mention);
                    await channel.SendMessageAsync(string.Empty, embed:embed.Build());
                    //await channel.SendMessageAsync(string.Format(welcomingMessage, user.Mention));
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
            json.AddField(JSON_MODERATORROLE, moderatorRole.ToString());
            json.AddField(JSON_PILOTROLE, pilotRole.ToString());



            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.Path + "Settings.json", json);
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
                for (int i = 0; i < debugLogging.Length; i++)
                {
                    bool catEnabled = debugLogging[i];
                    result.AddField(string.Format("Debug {0}", ((DebugCategories)i).ToString().PadRight(12)), catEnabled ? "```Enabled```" : "```Disabled```");
                }
                result.AddField("Debug Channel", Macros.MultiLineCodeBlock(DebugMessageChannelId));
                result.AddField("Welcoming Channel", Macros.MultiLineCodeBlock(WelcomeMessageChannelId));
                result.AddField("Moderator Role", Macros.MultiLineCodeBlock(moderatorRole));
                result.AddField("Escort Pilot Role", Macros.MultiLineCodeBlock(pilotRole));
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
                ISocketMessageChannel channel = client.GetChannel(DebugMessageChannelId) as ISocketMessageChannel;
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
            bool hasPilotRole = false;
            foreach (var role in user.Roles)
            {
                if (role.Id == moderatorRole)
                {
                    return AccessLevel.Moderator;
                }
                else if (role.Id == pilotRole)
                {
                    hasPilotRole = true;
                }
            }
            if (hasPilotRole)
            {
                return AccessLevel.Pilot;
            }
            else
            {
                return AccessLevel.Basic;
            }
        }
        #endregion

    }
}
