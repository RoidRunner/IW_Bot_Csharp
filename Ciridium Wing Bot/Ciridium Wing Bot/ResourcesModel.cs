using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium
{
    static class ResourcesModel
    {
        public static string Path { get; private set; }

        public static Task Init()
        {
            Path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Ciridium Wing Bot\";
            return Task.CompletedTask;
        }


        #region Save/Load Constants
        #endregion
        #region Save/Load

        public static async Task<JSONObject> LoadToJSONObject(string path)
        {
            if (File.Exists(path))
            {
                string fileContent = "";
                try
                {
                    fileContent = await File.ReadAllTextAsync(path, Encoding.UTF8);
                    return new JSONObject(fileContent);
                }
                catch (Exception e)
                {
                    await Program.Logger(new Discord.LogMessage(Discord.LogSeverity.Critical, "Save/Load", "Failed to load " + path, e));
                }
            }
            return null;
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
}