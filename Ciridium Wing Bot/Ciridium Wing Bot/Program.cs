using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Ciridium;
using System;
using System.Reflection;
using System.Threading.Tasks;

public static class Var
{
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
    internal static readonly Color BOTCOLOR = new Color(71, 71, 255);
    internal static readonly Color ERRORCOLOR = new Color(255, 0, 0);
}

public class Program
{

    static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

    /// <summary>
    /// Main Programs method running asynchronously
    /// </summary>
    /// <returns></returns>
    public async Task MainAsync()
    {
        await ResourcesModel.Init();
        await MissionSettingsModel.Init();
        await MissionModel.Init();

        Var.client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info
        });

        Var.client.Log += Logger;

        if (await SettingsModel.Init(Var.client))
        {
            SettingsModel.DebugMessage += Logger;
            TimingThread.Init(Var.client);
            InitCommands();

            Var.client.UserJoined += HandleUserJoined;
            Var.client.UserLeft += HandleUserLeft;
            Var.client.Connected += ScheduleConnectDebugMessage;

            await Var.client.LoginAsync(TokenType.Bot, SettingsModel.token);
            await Var.client.StartAsync();

            await TimingThread.UpdateTimeActivity();

            while (Var.running)
            {
                await Task.Delay(100);
            }
        }
    }

    private async Task SendConnectDebugMessage()
    {
        await SettingsModel.SendDebugMessage("I'm online, suckazzz", DebugCategories.misc);
    }

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
    /// Logs messages to
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
                Console.ForegroundColor = ConsoleColor.DarkGray;
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
        SettingsCommand settingsCmds = new SettingsCommand(Var.cmdService);
        ShutdownCommand shutdownCmds = new ShutdownCommand(Var.cmdService);
        HelpCommand helpCmds = new HelpCommand(Var.cmdService);
        MissionCommands missionCmds = new MissionCommands(Var.cmdService);

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
        if (msg == null) return;

        await Var.cmdService.HandleCommand(msg);
    }
}