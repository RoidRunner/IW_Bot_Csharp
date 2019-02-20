using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ciridium
{
    /// <summary>
    /// Handles all commands that change settings, such as default channels, welcoming messages.
    /// </summary>
    class SettingsCommand
    {
        public SettingsCommand(CommandService service)
        {
            string summary = "Lists current settings.";
            CommandService s = Var.cmdService;
            AccessLevel mod = AccessLevel.Moderator;
            AccessLevel bAdmin = AccessLevel.BotAdmin;
            s.AddCommand(new CommandKeys("settings"), HandleCommand, mod, summary, "/settings", Command.NO_ARGUMENTS);
            summary = "Enables/Disables debug messages for a debug category";
            string arguments =
                "    <Category>\n" +
                "Available debug categories are: 'misc', 'timing' & 'joinleave'\n" +
                "    <Enable>\n" +
                "Enable the category by setting 'true', disable by 'false'";
            s.AddCommand(new CommandKeys("settings debug", 4, 4), HandleDebugLoggingCommand, mod, summary, "/settings debug <Category> <Enable>", arguments);
            summary = "Sets the channel used for debug, welcoming and the mission channel category";
            arguments =
                "    <Channel>\n" +
                "Which default channel setting you wish to override. Available are 'debug', 'welcoming' & 'missioncategory'\n" +
                "    <ChannelId>\n" +
                "The uInt64 Id of the subject channel. Get Ids by using '/debug channels'";
            s.AddCommand(new CommandKeys("settings channel", 4, 4), HandleDefaultChannelCommand, mod, summary, "/settings channel <Channel> <ChannelId>", arguments);
            summary = "Sets the pilot/moderator role used to handle access to bot commands";
            arguments =
                "    <AccessLevel>\n" +
                "Which of the access levels you want to assign a role to. Available are 'pilot' & 'moderator'\n" +
                "    <@Role>\n" +
                "Ping the role here that you want to give the access level";
            s.AddCommand(new CommandKeys("settings role", 4, 4), HandleSetRoleCommand, bAdmin, summary, "/settings role <AccessLevel> <@Role>", arguments);
            summary = "Sets the welcoming message.";
            arguments =
                "    {<Words>}\n" +
                "All words following the initial arguments will be the new join message. Insert '{0}' wherever you want the new user pinged!";
            s.AddCommand(new CommandKeys("settings setjoinmsg", 3, 1000), HandleWelcomingMessageCommand, mod, summary, "/settings setjoinmsg {<Words>}", arguments);
            summary = "Sets the number for the next created mission";
            arguments =
                "    <Number>\n" +
                "Specify the number for the next created mission";
            s.AddCommand(new CommandKeys("settings setmissionnumber", 3, 3), HandleMissionNumberCommand, AccessLevel.Pilot, summary, "/settings setmissionnumber <Number>", arguments);
            summary = "Sets the default mission channel topic";
            arguments =
                "    {<Words>}\n" +
                "All words following the initial arguments will be the new default mission channel topic. Insert '{0}' wherever you want the explorers mentioned (no notifications)!";
            s.AddCommand(new CommandKeys("settings setmissionchanneltopic", 3, 1000), HandleMissionTopicCommand, mod, summary, "/settings setmissionchanneltopic {<Words>}", arguments);
            summary = "Sets the mission channel explorer questions";
            arguments =
                "    {<Words>}\n" +
                "All words following the initial arguments will be the mission channel explorer questions. Insert '{0}' wherever you want the explorers mentioned!";
            s.AddCommand(new CommandKeys("settings setexplorerquestions", 3, 1000), HandleMissionExplorerQuestionsCommand, mod, summary, "/settings setexplorerquestions {<Words>}", arguments);
        }

        public async Task HandleCommand(SocketCommandContext context)
        {
            await context.Channel.SendEmbedAsync(SettingsModel.DebugSettingsMessage);
        }

        private static async Task HandleDebugLoggingCommand(CommandContext context)
        {
            string message;
            bool error = false;
            DebugCategories debugcategory;
            if (Enum.TryParse(context.Args[2], out debugcategory))
            {
                bool oldsetting = SettingsModel.debugLogging[(int)debugcategory];
                if (bool.TryParse(context.Args[3], out SettingsModel.debugLogging[(int)debugcategory]))
                {
                    message = string.Format("{0} debug logging setting {1}", SettingsModel.debugLogging[(int)debugcategory] ? "Enabled" : "Disabled", context.Args[2]);
                }
                else
                {
                    error = true;
                    SettingsModel.debugLogging[(int)debugcategory] = oldsetting;
                    message = "Do you want it turned on or off? I am confused";
                }
            }
            else
            {
                error = true;
                message = "I don't know that debug logging category";
            }
            if (!error)
            {
                await SettingsModel.SaveSettings();
            }
            await context.Channel.SendEmbedAsync(message, error);
        }

        /// <summary>
        /// Handles the "/settings role" command
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task HandleSetRoleCommand(CommandContext context)
        {
            List<SocketRole> roles = new List<SocketRole>();
            roles.AddRange(context.Message.MentionedRoles);
            string message = "";
            bool error = false;
            switch (context.Args[2])
            {
                case "pilot":
                    SettingsModel.pilotRole = roles[0].Id;
                    message = "Pilot Role updated!";
                    break;
                case "moderator":
                    SettingsModel.moderatorRole = roles[0].Id;
                    message = "Moderator Role updated!";
                    break;
                default:
                    error = true;
                    message = "Unknown Role Identifier";
                    break;
            }
            if (!error)
            {
                await SettingsModel.SaveSettings();
            }
            await context.Channel.SendEmbedAsync(message, error);
        }

        /// <summary>
        /// Handles the "/settings setwelcomingmessage" command
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static async Task HandleWelcomingMessageCommand(CommandContext context)
        {
            string nwelcomingMessage = context.Message.Content.Substring(21);

            if (!nwelcomingMessage.Contains("{0}"))
            {
                await context.Channel.SendEmbedAsync("You need to specify locations for:```" +
                    "{0} : User that joined\n" +
                    "```", true);
            }
            else
            {
                SettingsModel.welcomingMessage = nwelcomingMessage;
                SocketTextChannel channel = context.Guild.GetTextChannel(SettingsModel.WelcomeMessageChannelId);
                await SettingsModel.SaveSettings();
                await context.Channel.SendEmbedAsync(string.Format("Welcoming Message updated successfully. I welcomed you in the welcoming channel: {0}", channel.Mention));
                await SettingsModel.WelcomeNewUser(context.User);
            }
        }

        /// <summary>
        /// handles the "/settings channel" command
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static async Task HandleDefaultChannelCommand(CommandContext context)
        {
            string message = "";
            bool error = false;
            ulong Id = 0;
            if (ulong.TryParse(context.Args[3], out Id))
            {
                switch (context.Args[2])
                {
                    case "debug":
                        SettingsModel.DebugMessageChannelId = Id;
                        await SettingsModel.SaveSettings();
                        message = "Debug channel successfully set to " + Var.client.GetChannel(Id).ToString();
                        break;
                    case "welcoming":
                        SettingsModel.WelcomeMessageChannelId = Id;
                        await SettingsModel.SaveSettings();
                        message = "Welcoming channel successfully set to " + Var.client.GetChannel(Id).ToString();
                        break;
                    case "missioncategory":
                        MissionSettingsModel.MissionCategoryId = Id;
                        await MissionSettingsModel.SaveMissionSettings();
                        message = "Mission category successfully set to " + Var.client.GetChannel(Id).ToString();
                        break;
                    default:
                        error = true;
                        message = "I don't know that default channel!";
                        break;
                }
            }
            else
            {
                error = true;
                message = "Cannot Parse the supplied Id as an uInt64 Value!";
            }
            if (!error)
            {
                await SettingsModel.SaveSettings();
            }
            await context.Channel.SendEmbedAsync(message, error);
        }

        private static async Task HandleMissionNumberCommand(CommandContext context)
        {
            string message = "";
            bool error = false;
            int missionNr;
            if (int.TryParse(context.Args[2], out missionNr))
            {
                MissionSettingsModel.NextMissionNumber = missionNr;
                message = "Next missions number successfuly set to " + missionNr;
            }
            else
            {
                error = true;
                message = "Could not parse supplied argument to a int32 value!";
            }
            if (!error)
            {
                await MissionSettingsModel.SaveMissionSettings();
            }

            await context.Channel.SendEmbedAsync(message, error);
        }

        private static async Task HandleMissionTopicCommand(CommandContext context)
        {
            string nDefaultTopic = context.Message.Content.Substring(32);

            MissionSettingsModel.DefaultTopic = nDefaultTopic;
            await MissionSettingsModel.SaveMissionSettings();
            await context.Channel.SendEmbedAsync("Default mission channel topic successfully updated!");
        }

        private static async Task HandleMissionExplorerQuestionsCommand(CommandContext context)
        {
            string nExplorerQuestions = context.Message.Content.Substring(30);

            MissionSettingsModel.ExplorerQuestions = nExplorerQuestions;
            await MissionSettingsModel.SaveMissionSettings();
            await context.Channel.SendEmbedAsync("Default mission channel explorer questions successfully updated!");
        }
    }

    public enum DebugCategories
    {
        misc,
        timing,
        joinleave
    }
}
