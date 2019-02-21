using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium
{
    static class ResourcesModel
    {
        public static readonly string BaseDirectory;
        public static readonly string SettingsFilePath;
        public static readonly string MissionSettingsFilePath;
        public static readonly string MissionsFilePath;

        static ResourcesModel()
        {
            BaseDirectory = Environment.CurrentDirectory + @"\Settings\";
            SettingsFilePath = BaseDirectory + "Settings.json";
            MissionSettingsFilePath = BaseDirectory + "MissionSettings.json";
            MissionsFilePath = BaseDirectory + "Missions.json";
        }

        public static bool CheckSettingsFilesExistence()
        {
            return File.Exists(SettingsFilePath) && File.Exists(MissionSettingsFilePath);
        }

        public static async Task InitiateBasicFiles()
        {
            Directory.CreateDirectory(BaseDirectory);
            await SettingsModel.SaveSettings();
            await MissionSettingsModel.SaveMissionSettings();
            await MissionModel.SaveMissions();
        }

        #region Save/Load

        public static async Task<LoadFileOperation> LoadToJSONObject(string path)
        {
            LoadFileOperation operation = new LoadFileOperation()
            {
                Success = false,
                Result = null
            };
            if (File.Exists(path))
            {
                string fileContent = "";
                try
                {
                    fileContent = await File.ReadAllTextAsync(path, Encoding.UTF8);
                    operation.Result = new JSONObject(fileContent);
                    operation.Success = true;
                    return operation;
                }
                catch (Exception e)
                {
                    await Program.Logger(new Discord.LogMessage(Discord.LogSeverity.Critical, "Save/Load", "Failed to load " + path, e));
                }
            }
            return operation;
        }

        public static async Task WriteJSONObjectToFile(string path, JSONObject json)
        {
            try
            {
                await File.WriteAllTextAsync(path, json.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                await Program.Logger(new Discord.LogMessage(Discord.LogSeverity.Critical, "Save/Load", "Failed to save " + path, e));
            }
        }
        #endregion
        #region MacroMethods
        
        public static string GetMentionsFromUserIdList(List<ulong> userIds)
        {
            StringBuilder result = new StringBuilder();
            if (userIds.Count == 1)
            {
                result.Append(GetMentionFromUserId(userIds[0]));
            } else if (userIds.Count >= 2)
            {
                foreach (ulong userId in userIds)
                {
                    result.Append(GetMentionFromUserId(userId));
                    result.Append(" ");
                }
            }
            return result.ToString().TrimEnd();
        }

        public static string GetMentionFromUserId(ulong userId)
        {
            return Var.client.GetUser(userId).Mention;
        }

        #endregion
    }

    public struct LoadFileOperation
    {
        public bool Success;
        public JSONObject Result;
    }
}