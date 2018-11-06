using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium
{
    static class MissionSettingsModel
    {
        public static ulong MissionCategoryId;

        private static int lastMissionNumber;
        public static string DefaultTopic;
        public static string ExplorerQuestions;

        public static TextChannelProperties ChannelPropertiesTemplate;

        public static int NextMissionNumber
        {
            get
            {
                return ++lastMissionNumber;
            }
        }

        public static OverwritePermissions EveryonePerms { get; private set; }
        public static OverwritePermissions ExplorerPerms { get; private set; }

        private static void InitPermissionsAndDefaultChannelProperties()
        {
            EveryonePerms = new OverwritePermissions(readMessages : PermValue.Deny, sendMessages : PermValue.Deny);
            ExplorerPerms = new OverwritePermissions(sendMessages : PermValue.Allow);

            ChannelPropertiesTemplate = new TextChannelProperties
            {
                CategoryId = MissionCategoryId,
                Topic = DefaultTopic
            };
        }

        public static async Task Init()
        {
            lastMissionNumber = 0;
            DefaultTopic = "NO DEFAULT TOPIC SET. USE '/settings missiontopic' TO SET ONE!";
            ExplorerQuestions = "NO EXPLORER QUESTION MESSAGE SET. USE '/settings explorerquestions' TO SET ONE! GONNA ALSO PING EXPLORERS FOR YA: {0}";
            await LoadMissionSettings();
            InitPermissionsAndDefaultChannelProperties();
        }

        #region JSON Save/Load

        private const string JSON_MISSIONNUMBER = "MissionNumber";
        private const string JSON_DEFAULTTOPIC = "DefaultTopic";
        private const string JSON_EXPLORERQUESTIONS = "ExplorerQuestions";

        public static async Task LoadMissionSettings()
        {
            JSONObject json = await ResourcesModel.LoadToJSONObject(ResourcesModel.Path + @"MissionSettings.json");
            json.GetField(ref lastMissionNumber, JSON_MISSIONNUMBER);
            string text = "";
            if (json.GetField(ref text, JSON_DEFAULTTOPIC))
            {
                DefaultTopic = text;
            }
            if (json.GetField(ref text, JSON_EXPLORERQUESTIONS))
            {
                ExplorerQuestions = text;
            }

        }

        public static async Task SaveMissionSettings()
        {
            JSONObject json = new JSONObject();
            json.AddField(JSON_MISSIONNUMBER, lastMissionNumber);
            json.AddField(JSON_DEFAULTTOPIC, DefaultTopic);
            json.AddField(JSON_EXPLORERQUESTIONS, ExplorerQuestions);
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.Path + @"MissionSettings.json", json);
        }

        #endregion
    }
}
