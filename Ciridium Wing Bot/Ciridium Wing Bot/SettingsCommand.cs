using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Ciridium
{
    class SettingsCommand
    {
        public async Task HandleCommand(SocketCommandContext context)
        {
            await context.Channel.SendMessageAsync(SettingsModel.DebugSettingsMessage);
        }

        private static async Task HandleDebugLoggingCommand(CommandContext context)
        {
            string message;
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
                    SettingsModel.debugLogging[(int)debugcategory] = oldsetting;
                    message = "Do you want it turned on or off? I am confused";
                }
            }
            else
            {
                message = "I don't know that debug logging category";
            }
            await context.Channel.SendMessageAsync(message);
        }

        private static async Task HandleSettingsSaveCommand(CommandContext context)
        {
            await SettingsModel.SaveSettings();
            await MissionSettingsModel.SaveMissionSettings();
            await context.Channel.SendMessageAsync("Settings Saved.");
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
            if (roles.Count > 0)
            {
                switch (context.Args[2])
                {
                    case "pilot":
                        SettingsModel.pilotRole = roles[0].Id;
                        await context.Channel.SendMessageAsync("Pilot Role updated!");
                        break;
                    case "moderator":
                        SettingsModel.moderatorRole = roles[0].Id;
                        await context.Channel.SendMessageAsync("Moderator Role updated!");
                        break;
                    default:
                        await context.Channel.SendMessageAsync("Unknown Role Identifier");
                        break;
                }
            }
            else
            {
                await context.Channel.SendMessageAsync("Mention a role you want to set");
            }
        }

        /// <summary>
        /// Handles the "/settings setwelcomingmessage" command
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static async Task HandleWelcomingMessageCommand(CommandContext context)
        {
            string nwelcomingMessage = context.Message.Content.Substring(30);

            if (!nwelcomingMessage.Contains("{0}"))
            {
                await context.Channel.SendMessageAsync("You need to specify locations for:```" +
                    "{0} : User that joined\n" +
                    "```");
            }
            else
            {
                SettingsModel.welcomingMessage = nwelcomingMessage;
                await context.Channel.SendMessageAsync("Welcoming Message updated successfully. Here is how it will look:");

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
            if (context.ArgCnt == 4)
            {
                ulong Id = 0;
                if (ulong.TryParse(context.Args[3], out Id))
                {
                    switch (context.Args[2])
                    {
                        case "debug":
                            SettingsModel.DebugMessageChannelId = Id;
                            await SettingsModel.SaveSettings();
                            message = "Debug Channel successfully set " + Var.client.GetChannel(Id).ToString();
                            break;
                        case "welcoming":
                            SettingsModel.WelcomeMessageChannelId = Id;
                            await SettingsModel.SaveSettings();
                            message = "Welcoming Channel successfully set to " + Var.client.GetChannel(Id).ToString();
                            break;
                        case "missioncategory":
                            MissionSettingsModel.MissionCategoryId = Id;
                            await MissionSettingsModel.SaveMissionSettings();
                            message = "Mission Category successfully set to " + Var.client.GetChannel(Id).ToString();
                            break;
                        default:
                            message = "I don't know that default channel!";
                            break;
                    }
                }
                else
                {
                    message = "Cannot Parse the supplied Id as an uInt64 Value!";
                }
            } else
            {
                message = "Wrong ARG CNT";
            }

            await context.Channel.SendMessageAsync(message);
        }

        private static async Task HandleMissionNumberCommand(CommandContext context)
        {
            string message = "";
            await context.Channel.SendMessageAsync(message);
        }

        public void RegisterCommand(CommandService service)
        {
            string summary = "Lists current settings.";
            CommandService s = Var.cmdService;
            AccessLevel mod = AccessLevel.Moderator;
            AccessLevel bAdmin = AccessLevel.BotAdmin;
            s.AddCommand(new CommandKeys("settings"), HandleCommand, mod, summary, "/settings");
            summary = "Saves the current settings to the bots config file";
            s.AddCommand(new CommandKeys("settings save"), HandleSettingsSaveCommand, mod, summary, "/settings save");
            summary = "Enables/Disables debug messages for a debug category";
            s.AddCommand(new CommandKeys("settings debug", 4, 4), HandleDebugLoggingCommand, bAdmin,  summary, "/settings debug <Category> true/false");
            summary = "Sets the channel used for debug/welcoming to the channel the command was issued from.";
            s.AddCommand(new CommandKeys("settings channel", 4, 4), HandleDefaultChannelCommand, mod, summary, "/settings channel debug/welcoming");
            summary = "Sets the pilot/moderator role used to handle access to bot commands";
            s.AddCommand(new CommandKeys("settings role", 4, 4), HandleSetRoleCommand, bAdmin, summary, "/settings role pilot/moderator <@Role>");
            summary = "Sets the welcoming message to whatever is after the initial command args. The joining user will be pinged wherever you put {0}";
            s.AddCommand(new CommandKeys("settings setjoinmsg", 3, 1000), HandleWelcomingMessageCommand, mod, summary, "/settings setjoinmsg {<Words>}");
            summary = "Sets the number for the next created mission. Mission number automatically increments upon creating missions!";
            s.AddCommand(new CommandKeys("settings setmissionnumber", 3, 3), HandleMissionNumberCommand, mod, summary, "/settings setmissionnumber <Number>");
        }
    }

    public enum DebugCategories
    {
        misc,
        timing,
        joinleave
    }
}
