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
        public static string DefaultTopic = "NO DEFAULT TOPIC SET. USE '/settings missiontopic' TO SET ONE!";
        public static string ExplorerQuestions = "NO EXPLORER QUESTION MESSAGE SET. USE '/settings explorerquestions' TO SET ONE! GONNA ALSO PING EXPLORERS FOR YA: {0}";

        public static int NextMissionNumber
        {
            get
            {
                return ++lastMissionNumber;
            }
            set
            {
                lastMissionNumber = value - 1;
            }
        }

        public static OverwritePermissions ExplorerPerms { get; private set; }

        private static void InitPermissionsAndDefaultChannelProperties()
        {
            ExplorerPerms = new OverwritePermissions(viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow, sendMessages: PermValue.Allow, embedLinks: PermValue.Allow, attachFiles: PermValue.Allow);
        }

        static MissionSettingsModel()
        {
            InitPermissionsAndDefaultChannelProperties();
        }

        #region JSON Save/Load

        private const string JSON_MISSIONNUMBER = "MissionNumber";
        private const string JSON_MISSIONCATEGORYID = "MissionCategoryId";
        private const string JSON_DEFAULTTOPIC = "DefaultTopic";
        private const string JSON_EXPLORERQUESTIONS = "ExplorerQuestions";

        public static async Task LoadMissionSettings()
        {
            LoadFileOperation operation = await ResourcesModel.LoadToJSONObject(ResourcesModel.MissionSettingsFilePath);
            if (operation.Success)
            {
                JSONObject json = operation.Result;
                json.GetField(ref lastMissionNumber, JSON_MISSIONNUMBER);
                string text = "";
                string jMissionCategoryID = "";
                json.GetField(ref jMissionCategoryID, JSON_MISSIONCATEGORYID);
                ulong.TryParse(jMissionCategoryID, out MissionCategoryId);
                if (json.GetField(ref text, JSON_DEFAULTTOPIC))
                {
                    DefaultTopic = text;
                }
                if (json.GetField(ref text, JSON_EXPLORERQUESTIONS))
                {
                    ExplorerQuestions = text;
                }
            }
        }

        public static async Task SaveMissionSettings()
        {
            JSONObject json = new JSONObject();
            json.AddField(JSON_MISSIONNUMBER, lastMissionNumber);
            json.AddField(JSON_MISSIONCATEGORYID, MissionCategoryId.ToString());
            json.AddField(JSON_DEFAULTTOPIC, DefaultTopic);
            json.AddField(JSON_EXPLORERQUESTIONS, ExplorerQuestions);
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.MissionSettingsFilePath, json);
        }

        #endregion
    }
}
