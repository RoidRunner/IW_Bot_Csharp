using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium.WebRequests
{
    class WebCommands
    {
        public static string ERROR = "ERROR";

        public WebCommands(CommandService service)
        {
            service.AddCommand(new CommandKeys(CMDKEYS_SYSTEMINFO, 2, 10), HandleSystemInfoCommand, AccessLevel.Basic, CMDSUMMARY_SYSTEMINFO, CMDSYNTAX_SYSTEMINFO, CMDARGS_SYSTEMINFO);
            service.AddCommand(new CommandKeys(CMDKEYS_DISTANCE, 3, 20), HandleDistanceCommand, AccessLevel.Basic, CMDSUMMARY_DISTANCE, CMDSYNTAX_DISTANCE, CMDARGS_DISTANCE);
            service.AddCommand(new CommandKeys(CMDKEYS_CMDR, 2, 10), HandleCMDRCommand, AccessLevel.Basic, CMDSUMMARY_CMDR, CMDSYNTAX_CMDR, CMDARGS_CMDR);
        }

        #region /systeminfo

        private const string CMDKEYS_SYSTEMINFO = "systeminfo";
        private const string CMDSYNTAX_SYSTEMINFO = "/systeminfo <SystemName>";
        private const string CMDSUMMARY_SYSTEMINFO = "Prints out detailed information about a system";
        private const string CMDARGS_SYSTEMINFO =
                "    {<SystemName>}\n" +
                "Write out the full SystemName here";

        public async Task HandleSystemInfoCommand(CommandContext context)
        {
            bool printJSON = context.Args[1].Equals("json");

            string requestedSystem;
            if (printJSON)
            {
                requestedSystem = context.Message.Content.Substring(CMDKEYS_SYSTEMINFO.Length + 7);
            }
            else
            {
                requestedSystem = context.Message.Content.Substring(CMDKEYS_SYSTEMINFO.Length + 2);
            }
            RequestJSONResult requestResultSystem = await WebRequestService.GetWebJSONAsync(WebRequests_URL_Methods.EDSM_SystemInfo_URL(requestedSystem, true, true, true, true, true));
            RequestJSONResult requestResultStations = await WebRequestService.GetWebJSONAsync(WebRequests_URL_Methods.EDSM_SystemStations_URL(requestedSystem));
            RequestJSONResult requestResultTraffic = await WebRequestService.GetWebJSONAsync(WebRequests_URL_Methods.EDSM_SystemTraffic_URL(requestedSystem));
            RequestJSONResult requestResultDeaths = await WebRequestService.GetWebJSONAsync(WebRequests_URL_Methods.EDSM_SystemDeaths_URL(requestedSystem));
            if (requestResultSystem.IsSuccess)
            {
                if (requestResultSystem.Object.IsArray && requestResultSystem.Object.Count < 1)
                {
                    await context.Channel.SendEmbedAsync("System not found in database!", true);
                }
                else
                {
                    if (printJSON)
                    {
                        await context.Channel.SendEmbedAsync("General System Info", string.Format("```json\n{0}```", requestResultSystem.Object.Print(true).MaxLength(2037)));
                        await context.Channel.SendEmbedAsync("Station Info", string.Format("```json\n{0}```", requestResultStations.Object.Print(true).MaxLength(2037)));
                    }
                    await context.Channel.SendEmbedAsync(FormatMessage_SystemInfo(requestResultSystem.Object, requestResultStations.Object, requestResultTraffic.Object, requestResultDeaths.Object));
                }
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

        public static EmbedBuilder FormatMessage_SystemInfo(JSONObject system, JSONObject stations, JSONObject traffic, JSONObject deaths)
        {
            EmbedBuilder message = new EmbedBuilder();

            // Gathering Information

            string system_name = string.Empty;
            string system_name_link = string.Empty;
            long system_id = 0;
            string system_startype = string.Empty;
            string security = string.Empty;
            bool system_requirepermit = false;
            string system_permit = string.Empty;

            if (system.GetField(ref system_name, "name") && system.GetField(ref system_id, "id"))
            {
                // Gathering General System information
                system_name_link = system_name.Replace(' ', '+');
                JSONObject system_information = system["information"];
                if (!((system_information != null) && system_information.GetField(ref security, "security")))
                {
                    security = "Anarchy (Unpopulated)";
                }
                string startype = "Not found";
                JSONObject primaryStar = system["primaryStar"];
                if ((primaryStar != null) && primaryStar.Count > 0)
                {
                    startype = system["primaryStar"]["type"].str;
                    if (!system["primaryStar"]["isScoopable"].b)
                    {
                        startype += " **(Unscoopable)**";
                    }
                }
                system.GetField(out system_requirepermit, "requirePermit", false);
                if (!system.GetField(ref system_permit, "permitName"))
                {
                    system_permit = "Unknown Permit";
                }

                // Gathering information about stations
                StationInfo bestOrbitalLarge = null;
                StationInfo bestOrbitalMedium = null;
                StationInfo bestPlanetary = null;
                if ((stations != null) && stations.HasField("stations"))
                {
                    foreach (JSONObject station in stations["stations"])
                    {
                        JSONObject otherservices = station["otherServices"];
                        string station_name = string.Empty;
                        long station_id = 0;
                        string station_type = "Unresolved";
                        float station_distance = float.PositiveInfinity;
                        string station_gov = string.Empty;
                        if (station.GetField(ref station_name, "name") && station.GetField(ref station_id, "id"))
                        {
                            station.GetField(ref station_type, "type");
                            station.GetField(ref station_distance, "distanceToArrival");
                            StationInfo info = new StationInfo(station_name, station_id, system_name_link, system_id, station_type, station_distance);
                            if (station.GetField(ref station_gov, "government"))
                            {
                                if (station_gov.Equals("Workshop (Engineer)"))
                                {
                                    info.Type = StationType.Unlandable;
                                }
                            }
                            station.GetField(ref info.HasShipyard, "haveShipyard");
                            station.GetField(ref info.HasOutfitting, "haveOutfitting");
                            if (otherservices != null)
                            {
                                foreach (JSONObject service in otherservices)
                                {
                                    switch (service.str)
                                    {
                                        case "Restock":
                                            info.HasRestock = true;
                                            break;
                                        case "Repair":
                                            info.HasRepair = true;
                                            break;
                                        case "Refuel":
                                            info.HasRefuel = true;
                                            break;
                                        case "Universal Cartographics":
                                            info.HasUniversalCartographics = true;
                                            break;
                                    }
                                }
                            }
                            if (info.HasLargePadOrbital)
                            {
                                if (bestOrbitalLarge == null)
                                {
                                    bestOrbitalLarge = info;
                                }
                                else
                                    if (info.Distance < bestOrbitalLarge.Distance)
                                {
                                    bestOrbitalLarge = info;
                                }
                            }
                            if (info.HasMedPadOrbital)
                            {
                                if (bestOrbitalMedium == null)
                                {
                                    bestOrbitalMedium = info;
                                }
                                else
                                    if (info.Distance < bestOrbitalMedium.Distance)
                                {
                                    bestOrbitalMedium = info;
                                }
                            }
                            if (info.IsPlanetary)
                            {
                                if (bestPlanetary == null)
                                {
                                    bestPlanetary = info;
                                }
                                else if (info.Distance < bestPlanetary.Distance)
                                {
                                    bestPlanetary = info;
                                }
                            }
                        }
                    }
                }

                // Getting Information about traffic
                int traffic_week = -1;
                int traffic_day  = -1;

                if ((traffic != null) && traffic.HasField("traffic"))
                {
                    traffic["traffic"].GetField(ref traffic_week, "week");
                    traffic["traffic"].GetField(ref traffic_day, "day");
                }

                // Getting Information about CMDR deaths
                int deaths_week = -1;
                int deaths_day = -1;

                if ((deaths != null) && deaths.HasField("deaths"))
                {
                    deaths["deaths"].GetField(ref deaths_week, "week");
                    deaths["deaths"].GetField(ref deaths_day, "day");
                }

                // Constructing message
                message.Color = Var.BOTCOLOR;
                message.Title = string.Format("__**System Info for {0}**__", system_name);
                message.Url = string.Format("https://www.edsm.net/en/system/id/{0}/name/{1}", system_id, system_name_link);
                message.AddField("General Info", string.Format("{0}Star Type: {1}\nSecurity: {2}", system_requirepermit ? string.Format("**Requires Permit**: {0}\n", system_permit) : string.Empty, startype, security));

                bool provideInfoOnLarge = bestOrbitalLarge != null;
                bool provideInfoOnMedium = (!provideInfoOnLarge && bestOrbitalMedium != null) || (provideInfoOnLarge && bestOrbitalMedium != null && bestOrbitalLarge.Distance > bestOrbitalMedium.Distance);
                bool provideInfoOnPlanetary = (!provideInfoOnLarge && bestPlanetary != null) || (provideInfoOnLarge && bestPlanetary != null && bestOrbitalLarge.Distance > bestPlanetary.Distance);
                if (provideInfoOnLarge)
                {
                    message.AddField("Closest Orbital Large Pad", bestOrbitalLarge.ToString());
                }
                if (provideInfoOnMedium)
                {
                    message.AddField("Closest Orbital Medium Pad", bestOrbitalMedium.ToString());
                }
                if (provideInfoOnPlanetary)
                {
                    message.AddField("Closest Planetary Large Pad", bestPlanetary.ToString());
                }
                if (!provideInfoOnLarge && !provideInfoOnMedium && !provideInfoOnPlanetary)
                {
                    message.AddField("No Stations in this System!", "- / -");
                }
                if (traffic_day != -1 && traffic_week != -1)
                {
                    message.AddField("Traffic Report", string.Format("Last 7 days: {0} CMDRs, last 24 hours: {1} CMDRs", traffic_week, traffic_day));
                }
                else
                {
                    message.AddField("No Traffic Report Available", "- / -");
                }
                if (deaths_day != -1 && deaths_week != -1)
                {
                    message.AddField("CMDR Deaths Report", string.Format("Last 7 days: {0} CMDRs, last 24 hours: {1} CMDRs", deaths_week, deaths_day));
                }
                else
                {
                    message.AddField("No CMDR Deaths Report Available", "- / -");
                }
            }
            else
            {
                message.Description = string.Format("Could not get name & id from JSON. Here have the source json data: ```json\n{0}```", system.Print(true));
                message.Color = Var.ERRORCOLOR;
            }

            return message;
        }

        private class StationInfo
        {
            public string Name;
            public long Id;
            public StationType Type;
            public int Distance;
            public bool HasRestock;
            public bool HasRefuel;
            public bool HasRepair;
            public bool HasShipyard;
            public bool HasOutfitting;
            public bool HasUniversalCartographics;
            private string SystemName;
            private long SystemId;

            public bool HasLargePadOrbital
            {
                get
                {
                    return Type == StationType.Asteroid || Type == StationType.Coriolis || Type == StationType.Ocellus || Type == StationType.Orbis || Type == StationType.Megaship;
                }
            }
            public bool IsPlanetary
            {
                get
                {
                    return Type == StationType.Planetary;
                }
            }
            public bool HasMedPadOrbital
            {
                get
                {
                    return Type == StationType.Outpost || HasLargePadOrbital;
                }
            }

            public string EDSMLink
            {
                get
                {
                    return string.Format("https://www.edsm.net/en/system/stations/id/{0}/name/{1}/details/idS/{2}/nameS/{3}", SystemId, SystemName, Id, Name.Replace(' ', '+'));
                }
            }

            public StationInfo(string name, long id, string systemName, long systemId, string type, float distance)
            {
                Name = name;
                Id = id;
                SystemName = systemName;
                SystemId = systemId;
                Type = ParseStationType(type);
                Distance = (int)distance;
            }

            public override string ToString()
            {
                string distanceFormatted = Distance.ToString("N", new CultureInfo("en-US"));
                string result = string.Format("**{0} [{1}]({2})**: {3}, {4} ls\n     {5}{6}{7}{8}{9}{10}", STATIONEMOJI[(int)Type], Name, EDSMLink, STATIONTYPENAMES[(int)Type], distanceFormatted.Substring(0, distanceFormatted.Length - 3),
                    HasRestock ? "Restock, " : string.Empty, HasRefuel ? "Refuel, " : string.Empty, HasRepair ? "Repair, " : string.Empty, HasShipyard ? "Shipyard, " : string.Empty, HasOutfitting ? "Outfitting, " : string.Empty, HasUniversalCartographics ? "Universal Cartographics" : string.Empty);
                if (result.EndsWith(", "))
                {
                    result = result.Substring(0, result.Length - 2);
                }
                return result;
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
                    return StationType.Planetary;
                case "Mega ship":
                    return StationType.Megaship;
                case "Planetary Engineer Base":
                    return StationType.Unlandable;
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

        public static readonly string[] STATIONEMOJI = new string[] { "<:orbis:553217808960454656>", "<:coriolis:553217377421230092>", "<:ocellus:553217808918511627>", "<:asteroid:553219501077037076>", "<:outpost:553219500951076864>", "<:planetary:553226417647910912>", "<:megaship:553219500787630082>", "<:unknown:553220103764705300>", "<:unknown:553220103764705300>" };
        public static readonly string[] STATIONTYPENAMES = new string[] { "Orbis Starport", "Coriolis Starport", "Ocellus Starport", "Asteroid Base", "Outpost", "Planetary", "Megaship", "Unlandable", "Other" };

        #endregion
        #region /distance


        private const string CMDKEYS_DISTANCE = "distance";
        private const string CMDSYNTAX_DISTANCE = "/distance <SystemName1>, <SystemName2>";
        private const string CMDSUMMARY_DISTANCE = "Calculates raw lightyear distance between two systems";
        private const string CMDARGS_DISTANCE =
                "    <SystemName1>\n" +
                "Write out the full SystemName for your starting system here" +
                "    <SystemName2>\n" +
                "Write out the full SystemName for your destination system here";

        public async Task HandleDistanceCommand(CommandContext context)
        {
            // Parse input systems
            string requestedSystem1 = string.Empty;
            string requestedSystem2 = string.Empty;
            bool commaEncountered = false;
            for (int i = 1; i < context.ArgCnt; i++)
            {
                string partial = context.Args[i];
                if (partial.EndsWith(','))
                {
                    if (!string.IsNullOrEmpty(requestedSystem1))
                    {
                        requestedSystem1 += '+';
                    }
                    requestedSystem1 += partial.Substring(0, partial.Length - 1);
                    commaEncountered = true;
                }
                else if (commaEncountered)
                {
                    if (!string.IsNullOrEmpty(requestedSystem2))
                    {
                        requestedSystem2 += '+';
                    }

                    requestedSystem2 += partial;
                }
                else
                {
                    if (!string.IsNullOrEmpty(requestedSystem1))
                    {
                        requestedSystem1 += '+';
                    }
                    requestedSystem1 += partial;
                }
            }

            // two systems found, make the request
            if (commaEncountered)
            {
                string requestURL = WebRequests_URL_Methods.EDSM_MultipleSystemsInfo_URL(new string[] { requestedSystem1, requestedSystem2 }, false, true, false, false, false);
                RequestJSONResult requestResult = await WebRequestService.GetWebJSONAsync(requestURL);
                if (requestResult.IsSuccess)
                {
                    if (requestResult.Object.Count > 1)
                    {
                        await context.Channel.SendEmbedAsync(FormatMessage_SystemDistance(requestResult.Object[0], requestResult.Object[1]));
                    }
                    else
                    {
                        if (requestResult.Object.Count > 0)
                        {
                            string systemnameFound = requestResult.Object[0]["name"].str;
                            if (requestedSystem1.Equals(systemnameFound, StringComparison.CurrentCultureIgnoreCase))
                            {
                                await context.Channel.SendEmbedAsync(string.Format("Only found system `{0}`. Could not find system `{1}`!", systemnameFound, requestedSystem2), true);
                            }
                            else
                            {
                                await context.Channel.SendEmbedAsync(string.Format("Only found system `{0}`. Could not find system `{1}`!", systemnameFound, requestedSystem1), true);
                            }
                        }
                        else
                        {
                            await context.Channel.SendEmbedAsync("Could not find both of your mentioned systems!", true);
                        }
                    }
                }
            }
            else
            {
                await context.Channel.SendEmbedAsync("You need to supply two systems, separated by a comma!", true);
            }
        }

        public static string FormatMessage_SystemDistance(JSONObject system1, JSONObject system2)
        {
            Vector3 coords1 = ParseCoords(system1["coords"]);
            Vector3 coords2 = ParseCoords(system2["coords"]);
            string system1_name = string.Empty, system2_name = string.Empty;
            system1.GetField(ref system1_name, "name");
            system2.GetField(ref system2_name, "name");
            return string.Format("Distance **{0}** <-> **{1}** ```{2} ly```", system1_name, system2_name, Vector3.Distance(coords1, coords2).ToString("N"));
        }

        public static Vector3 ParseCoords(JSONObject Coordinates)
        {
            if ((Coordinates != null) && Coordinates.HasFields(new string[] { "x", "y", "z"}))
            {
                return new Vector3(Coordinates["x"].f, Coordinates["y"].f, Coordinates["z"].f);
            }
            else return new Vector3();
        }

        #endregion
        #region /cmdr


        private const string CMDKEYS_CMDR = "cmdr";
        private const string CMDSYNTAX_CMDR = "/cmdr <CMDRName>";
        private const string CMDSUMMARY_CMDR = "Locates the inara profile of a commander";
        private const string CMDARGS_CMDR =
                "    <CMDRName>\n" +
                "Write out the full exact CMDR Name here";

        public async Task HandleCMDRCommand(CommandContext context)
        {
            string cmdrName = context.Message.Content.Substring(CMDKEYS_CMDR.Length + 2);
            JSONObject RequestContent = WebRequests_URL_Methods.Inara_CMDR_Profile(cmdrName);
        }

        #endregion
    }
}
