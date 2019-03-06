using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium.WebRequests
{
    class WebCommands
    {
        public WebCommands(CommandService service)
        {
            service.AddCommand(new CommandKeys(CMDKEYS_SYSTEMINFO, 2, 5), HandleSystemInfoCommand, AccessLevel.Basic, CMDSUMMARY_SYSTEMINFO, CMDSYNTAX_SYSTEMINFO, CMDARGS_SYSTEMINFO);
        }

        #region /systeminfo

        private const string CMDKEYS_SYSTEMINFO = "systeminfo";
        private const string CMDSYNTAX_SYSTEMINFO = "/systeminfo {<SystemName>}";
        private const string CMDSUMMARY_SYSTEMINFO = "Prints out detailed information about a system";
        private const string CMDARGS_SYSTEMINFO =
                "    {<SystemName>}\n" +
                "Write out the full SystemName here";

        public async Task HandleSystemInfoCommand(CommandContext context)
        {
            string requestedSystem = context.Message.Content.Substring(CMDKEYS_SYSTEMINFO.Length + 2);
            RequestJSONResult requestResultSystem = await WebRequestService.GetWebJSONAsync(WebRequests_URL_Methods.EDSM_SystemInfo_URL(requestedSystem, true, true, true, true));
            RequestJSONResult requestResultStations = await WebRequestService.GetWebJSONAsync(WebRequests_URL_Methods.EDSM_SystemStations_URL(requestedSystem));
            if (requestResultSystem.IsSuccess)
            {
                await context.Channel.SendEmbedAsync(FormatMessage_SystemInfo(requestResultSystem.Object, requestResultStations.Object));
            }
            else if (requestResultSystem.IsException)
            {
                await context.Channel.SendEmbedAsync(string.Format("Could not connect to EDSMs services. Exception Message: `{0}`", requestResultSystem.ThrownException.Message), true);
            }
            else
            {
                await context.Channel.SendEmbedAsync(string.Format("Could not connect to EDSMs services. HTTP Error Message: `{0} {1}`", (int)requestResultSystem.Status, requestResultSystem.Status.ToString()), true);
            }
        }

        #endregion
        #region JSON_Handling

        public static string ERROR = "ERROR";

        public static EmbedBuilder FormatMessage_SystemInfo(JSONObject system, JSONObject stations)
        {
            EmbedBuilder message = new EmbedBuilder();
            message.Title = string.Format("__**System Info for {0}**__", system["name"].str);
            message.AddField("General Info", string.Format("Star Type: {0}\nRequires Permit: {1}\nSecurity: {2}", system["primaryStar"]["type"].str, system["requirePermit"].b, system["information"]["security"].str));
            StringBuilder stationsString = new StringBuilder();
            foreach (JSONObject station in stations["stations"])
            {
                StationInfo info = new StationInfo(station["name"].str, station["type"].str, station["distanceToArrival"].f);
                stationsString.AppendLine(info.ToString());
            }
            message.AddField("Stations", stationsString.ToString());
            return message;
        }

        private struct StationInfo
        {
            public string Name;
            public StationType Type;
            public int Distance;

            public StationInfo(string name, string type, float distance)
            {
                Name = name;
                Type = ParseStationType(type);
                Distance = (int)distance;
            }

            public override string ToString()
            {
                string distanceFormatted = Distance.ToString("N", new CultureInfo("en-US"));
                return string.Format("**{0}**: {1}, {2} ls", Name, STATIONTYPENAMES[(int)Type], distanceFormatted.Substring(0, distanceFormatted.Length - 3));
            }
        }

        public static StationType ParseStationType(string input)
        {
            switch (input)
            {
                case "Orbis Starport":
                    return StationType.Orbis;
                case "Coriolis Starport":
                    return StationType.Coriolis;
                case "Ocellus Starport":
                    return StationType.Ocellus;
                case "Asteroid base":
                    return StationType.Asteroid;
                case "Outpost":
                    return StationType.Outpost;
                case "Planetary Port":
                case "Planetary Outpost":
                case "Planetary Settlement":
                case "Planetary Engineer Base":
                    return StationType.Planetary;
                case "Megaship":
                    return StationType.Megaship;
                default:
                    return StationType.Other;
            }
        }

        public enum StationType
        {
            Orbis,
            Coriolis,
            Ocellus,
            Asteroid,
            Outpost,
            Planetary,
            Megaship,
            Unlandable,
            Other
        }

        public static readonly string[] STATIONTYPENAMES = new string[] { "(L) Orbis Starport", "(L) Coriolis Starport", "(L) Ocellus Starport", "(L) Asteroid Base", "(M) Outpost", "(L) Planetary", "(L) Megaship", "(X) Unlandable", "(?) Other" };

        #endregion
    }
}
