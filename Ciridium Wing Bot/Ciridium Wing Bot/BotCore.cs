﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Ciridium;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Ciridium.WebRequests;
using System.Threading;

// dotnet publish -c Release -r win10-x64
// dotnet publish -c Release -r linux-x64

public static class Var
{
    internal static string INARA_APPNAME = "CiridiumWingBot";
    internal readonly static Version VERSION = new Version(1, 2);
    /// <summary>
    /// When put to false will stop the program
    /// </summary>
    internal static bool running = true;
    /// <summary>
    /// The client wrapper used to communicate with discords servers
    /// </summary>
    internal static DiscordSocketClient client;
    /// <summary>
    /// Commandservice storing all commands
    /// </summary>
    internal static Ciridium.CommandService cmdService;
    /// <summary>
    /// Embed color used for the bot
    /// </summary>
    internal static readonly Color BOTCOLOR = new Color(71, 71, 255);
    /// <summary>
    /// Embed color used for bot error messages
    /// </summary>
    internal static readonly Color ERRORCOLOR = new Color(255, 0, 0);
    /// <summary>
    /// Path containing the restart location
    /// </summary>
    internal static string RestartPath = string.Empty;
}
namespace Ciridium {
    public class BotCore
    {
        static void Main(string[] args) => new BotCore().MainAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Main Programs method running asynchronously
        /// </summary>
        /// <returns></returns>
        public async Task MainAsync()
        {
            Console.Title = "Ciridium Wing Bot v" + Var.VERSION.ToString();
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            Var.client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            bool filesExist = false;
            bool foundToken = false;
            if (ResourcesModel.CheckSettingsFilesExistence())
            {
                filesExist = true;
                if (await SettingsModel.LoadSettingsAndCheckToken(Var.client))
                {
                    foundToken = true;
                }
            }

            if (foundToken)
            {
                await MissionSettingsModel.LoadMissionSettings();
                await MissionModel.LoadMissions();

                Var.client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info
                });

                InitCommands();

#if WELCOMING_MESSAGES
            Var.client.UserJoined += HandleUserJoined;
            Var.client.UserLeft += HandleUserLeft;
#endif
                Var.client.Log += Logger;
                SettingsModel.DebugMessage += Logger;
                Var.client.Connected += ScheduleConnectDebugMessage;

                await Var.client.LoginAsync(TokenType.Bot, SettingsModel.token);
                await Var.client.StartAsync();

                await TimingThread.UpdateTimeActivity();

                while (Var.running)
                {
                    await Task.Delay(100);
                }

                if (string.IsNullOrEmpty(Var.RestartPath))
                {
                    await SettingsModel.SendDebugMessage("Shutting down ...", DebugCategories.misc);
                }
                else
                {
                    await SettingsModel.SendDebugMessage("Restarting ...", DebugCategories.misc);
                }

                Var.client.Dispose();
            }
            else
            {
                if (!filesExist)
                {
                    await Logger(new LogMessage(LogSeverity.Critical, "SETTINGS", string.Format("Could not find config files! Standard directory is \"{0}\".\nReply with 'y' if you want to generate basic files now!", ResourcesModel.BaseDirectory)));
                    if (Console.ReadLine().ToCharArray()[0] == 'y')
                    {
                        await ResourcesModel.InitiateBasicFiles();
                    }
                }
                else
                {
                    await Logger(new LogMessage(LogSeverity.Critical, "SETTINGS", string.Format("Could not find a valid token in Settings file ({0}). Press any key to exit!", ResourcesModel.SettingsFilePath)));
                    Console.ReadLine();
                }
            }

            if (!string.IsNullOrEmpty(Var.RestartPath))
            {
                System.Diagnostics.Process.Start(Var.RestartPath);
            }
        }

        /// <summary>
        /// Sends a connect message to the assigned debug channel
        /// </summary>
        private async Task SendConnectDebugMessage()
        {
            await SettingsModel.SendDebugMessage("I'm online, suckazzz", DebugCategories.misc);
        }

        /// <summary>
        /// Sending the connect message immediately on login fails. This schedules the debug connect message 500ms ahead.
        /// </summary>
        private Task ScheduleConnectDebugMessage()
        {
            TimingThread.AddScheduleDelegate(SendConnectDebugMessage, 500);
            return Task.CompletedTask;
        }

        private async Task HandleUserLeft(SocketGuildUser arg)
        {
            await SettingsModel.SendDebugMessage(string.Format("{0} left", arg.Mention), DebugCategories.joinleave);
        }

        /// <summary>
        /// Handles Welcoming Messages
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task HandleUserJoined(SocketGuildUser arg)
        {
            SocketUser user = arg as SocketUser;
            if (user != null)
            {
                await SettingsModel.WelcomeNewUser(user);
            }
            await SettingsModel.SendDebugMessage(string.Format("{0} joined", arg.Mention), DebugCategories.joinleave);
        }

        /// <summary>
        /// Logs messages to the console
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static Task Logger(LogMessage message)
        {
            var cc = Console.ForegroundColor;
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Error:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message}");

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initiates and registers commands
        /// </summary>
        private void InitCommands()
        {
            Var.cmdService = new Ciridium.CommandService('/');

            UtilityCommands utilityCmds = new UtilityCommands(Var.cmdService);
            DebugCommands debugCmds = new DebugCommands(Var.cmdService);
            SettingsCommands settingsCmds = new SettingsCommands(Var.cmdService);
            ShutdownCommands shutdownCmds = new ShutdownCommands(Var.cmdService);
            HelpCommands helpCmds = new HelpCommands(Var.cmdService);
            MissionCommands missionCmds = new MissionCommands(Var.cmdService);
            WebCommands webCmds = new WebCommands(Var.cmdService);

            Var.client.MessageReceived += HandleCommandAsync;
        }

        /// <summary>
        /// Handles commmands
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task HandleCommandAsync(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            var msg = arg as SocketUserMessage;
            if (msg == null)
            {
                return;
            }

            await Var.cmdService.HandleCommand(msg);
        }

    }

    public enum AccessLevel
    {
        Basic,
        Pilot,
        Dispatch,
        Director,
        BotAdmin
    }
}