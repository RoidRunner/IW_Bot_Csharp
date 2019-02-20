﻿using Discord.Commands;
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
            CommandService s = Var.cmdService;
            // settings
            s.AddCommand(new CommandKeys(CMDKEYS_SETTINGS), HandleCommand, AccessLevel.Moderator, CMDSUMMARY_SETTINGS, CMDSYNTAX_SETTINGS, Command.NO_ARGUMENTS);
            // settings debug
            s.AddCommand(new CommandKeys(CMDKEYS_SETTINGS_DEBUG, 4, 4), HandleDebugLoggingCommand, AccessLevel.Moderator, CMDSUMMARY_SETTINGS_DEBUG, CMDSYNTAX_SETTINGS_DEBUG, CMDARGS_SETTINGS_DEBUG);
            // settings channel
            s.AddCommand(new CommandKeys(CMDKEYS_SETTINGS_CHANNEL, 4, 4), HandleDefaultChannelCommand, AccessLevel.Moderator, CMDSUMMARY_SETTINGS_CHANNEL, CMDSYNTAX_SETTINGS_CHANNEL, CMDARGS_SETTINGS_CHANNEL);
            // settings role
            s.AddCommand(new CommandKeys(CMDKEYS_SETTINGS_ROLE, 4, 4), HandleSetRoleCommand, AccessLevel.BotAdmin, CMDSUMMARY_SETTINGS_ROLE, CMDSYNTAX_SETTINGS_ROLE, CMDARGS_SETTINGS_ROLE);
            // settings setjoinmsg
            s.AddCommand(new CommandKeys(CMDKEYS_SETTINGS_SETJOINMSG, 3, 1000), HandleWelcomingMessageCommand, AccessLevel.Moderator, CMDSUMMARY_SETTINGS_SETJOINMSG, CMDSYNTAX_SETTINGS_SETJOINMSG, CMDARGS_SETTINGS_SETJOINMSG);
            // settings setmissionnumber
            s.AddCommand(new CommandKeys(CMDKEYS_SETTINGS_SETMISSIONNUMBER, 3, 3), HandleMissionNumberCommand, AccessLevel.Pilot, CMDSUMMARY_SETTINGS_SETMISSIONNUMBER, CMDSYNTAX_SETTINGS_SETMISSIONNUMBER, CMDARGS_SETTINGS_SETMISSIONNUMBER);
            // settings setmissionchanneltopic
            s.AddCommand(new CommandKeys(CMDKEYS_SETTINGS_SETMISSIONCHANNELTOPIC, 3, 1000), HandleMissionTopicCommand, AccessLevel.Moderator, CMDSUMMARY_SETTINGS_SETMISSIONCHANNELTOPIC, CMDSYNTAX_SETTINGS_SETMISSIONCHANNELTOPIC, CMDARGS_SETTINGS_SETMISSIONCHANNELTOPIC);
            // settings setexplorerquestions
            s.AddCommand(new CommandKeys(CMDKEYS_SETTINGS_SETEXPLORERQUESTIONS, 3, 1000), HandleMissionExplorerQuestionsCommand, AccessLevel.Moderator, CMDSUMMARY_SETTINGS_SETEXPLORERQUESTIONS, CMDSYNTAX_SETTINGS_SETEXPLORERQUESTIONS, CMDARGS_SETTINGS_SETEXPLORERQUESTIONS);
        }

        #region /settings

        private const string CMDKEYS_SETTINGS = "settings";
        private const string CMDSYNTAX_SETTINGS = "/settings";
        private const string CMDSUMMARY_SETTINGS = "Lists current settings";

        public async Task HandleCommand(SocketCommandContext context)
        {
            await context.Channel.SendEmbedAsync(SettingsModel.DebugSettingsMessage);
        }

        #endregion
        #region /settings debug

        private const string CMDKEYS_SETTINGS_DEBUG = "settings debug";
        private const string CMDSYNTAX_SETTINGS_DEBUG = "/settings debug";
        private const string CMDSUMMARY_SETTINGS_DEBUG = "Enables/Disables debug messages for a debug category";
        private const string CMDARGS_SETTINGS_DEBUG =
                "    <Category>\n" +
                "Available debug categories are: 'misc', 'timing' & 'joinleave'\n" +
                "    <Enable>\n" +
                "Enable the category by setting 'true', disable by 'false'";

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
                    message = string.Format("{0} debug logging for '{1}'", SettingsModel.debugLogging[(int)debugcategory] ? "Enabled" : "Disabled", context.Args[2].ToUpper());
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

        #endregion
        #region /settings role

        private const string CMDKEYS_SETTINGS_ROLE = "settings role";
        private const string CMDSYNTAX_SETTINGS_ROLE = "/settings role <AccessLevel> <@Role>";
        private const string CMDSUMMARY_SETTINGS_ROLE = "Sets the pilot/moderator role used to handle access to bot commands";
        private const string CMDARGS_SETTINGS_ROLE =
                "    <AccessLevel>\n" +
                "Which of the access levels you want to assign a role to. Available are 'pilot' & 'moderator'\n" +
                "    <@Role>\n" +
                "Ping the role here that you want to give the access level";

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

        #endregion
        #region /settings setjoinmsg

        private const string CMDKEYS_SETTINGS_SETJOINMSG = "settings setjoinmsg";
        private const string CMDSYNTAX_SETTINGS_SETJOINMSG = "/settings setjoinmsg {<Words>}";
        private const string CMDSUMMARY_SETTINGS_SETJOINMSG = "Sets the welcoming message";
        private const string CMDARGS_SETTINGS_SETJOINMSG =
                "    {<Words>}\n" +
                "All words following the initial arguments will be the new join message. Insert '{0}' wherever you want the new user pinged!";

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

        #endregion
        #region /settings channel

        private const string CMDKEYS_SETTINGS_CHANNEL = "settings channel";
        private const string CMDSYNTAX_SETTINGS_CHANNEL = "/settings channel <Channel> <ChannelId>";
        private const string CMDSUMMARY_SETTINGS_CHANNEL = "Sets the channel used for debug, welcoming and the mission channel category";
        private const string CMDARGS_SETTINGS_CHANNEL =
                "    <Channel>\n" +
                "Which default channel setting you wish to override. Available are 'debug', 'welcoming' & 'missioncategory'\n" +
                "    <ChannelId>\n" +
                "The uInt64 Id of the subject channel. Get Ids by using '/debug channels'";

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

        #endregion
        #region /settings setmissionnumber

        private const string CMDKEYS_SETTINGS_SETMISSIONNUMBER = "settings setmissionnumber";
        private const string CMDSYNTAX_SETTINGS_SETMISSIONNUMBER = "/settings setmissionnumber <Number>";
        private const string CMDSUMMARY_SETTINGS_SETMISSIONNUMBER = "Sets the number for the next created mission";
        private const string CMDARGS_SETTINGS_SETMISSIONNUMBER =
                "    <Number>\n" +
                "Specify the number for the next created mission";

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

        #endregion
        #region /settings setmissionchanneltopic

        private const string CMDKEYS_SETTINGS_SETMISSIONCHANNELTOPIC = "settings setmissionchanneltopic";
        private const string CMDSYNTAX_SETTINGS_SETMISSIONCHANNELTOPIC = "/settings setmissionchanneltopic {<Words>}";
        private const string CMDSUMMARY_SETTINGS_SETMISSIONCHANNELTOPIC = "Sets the default mission channel topic";
        private const string CMDARGS_SETTINGS_SETMISSIONCHANNELTOPIC =
                "    {<Words>}\n" +
                "All words following the initial arguments will be the mission channel explorer questions. Insert '{0}' wherever you want the explorers mentioned!";

        private static async Task HandleMissionTopicCommand(CommandContext context)
        {
            string nDefaultTopic = context.Message.Content.Substring(32);

            MissionSettingsModel.DefaultTopic = nDefaultTopic;
            await MissionSettingsModel.SaveMissionSettings();
            await context.Channel.SendEmbedAsync("Default mission channel topic successfully updated!");
        }

        #endregion
        #region /settings setexplorerquestions

        private const string CMDKEYS_SETTINGS_SETEXPLORERQUESTIONS = "settings ";
        private const string CMDSYNTAX_SETTINGS_SETEXPLORERQUESTIONS = "/settings setexplorerquestions {<Words>}";
        private const string CMDSUMMARY_SETTINGS_SETEXPLORERQUESTIONS = "Sets the mission channel explorer questions";
        private const string CMDARGS_SETTINGS_SETEXPLORERQUESTIONS =
                "    {<Words>}\n" +
                "All words following the initial arguments will be the mission channel explorer questions. Insert '{0}' wherever you want the explorers mentioned!";

        private static async Task HandleMissionExplorerQuestionsCommand(CommandContext context)
        {
            string nExplorerQuestions = context.Message.Content.Substring(30);

            MissionSettingsModel.ExplorerQuestions = nExplorerQuestions;
            await MissionSettingsModel.SaveMissionSettings();
            await context.Channel.SendEmbedAsync("Default mission channel explorer questions successfully updated!");
        }

        #endregion
    }
}
