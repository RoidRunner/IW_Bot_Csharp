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

        public WebCommands()
        {
            CommandService.AddCommand(new CommandKeys(CMDKEYS_SYSTEMINFO, 2, 10), HandleSystemInfoCommand, AccessLevel.Basic, CMDSUMMARY_SYSTEMINFO, CMDSYNTAX_SYSTEMINFO, CMDARGS_SYSTEMINFO, useTyping:true);
            CommandService.AddCommand(new CommandKeys(CMDKEYS_DISTANCE, 3, 20), HandleDistanceCommand, AccessLevel.Basic, CMDSUMMARY_DISTANCE, CMDSYNTAX_DISTANCE, CMDARGS_DISTANCE, useTyping: true);
            CommandService.AddCommand(new CommandKeys(CMDKEYS_CMDR, 2, 10), HandleCMDRCommand, AccessLevel.Basic, CMDSUMMARY_CMDR, CMDSYNTAX_CMDR, CMDARGS_CMDR, useTyping: true);
            CommandService.AddCommand(new CommandKeys(CMDKEYS_FACTION, 2, 10), HandleFactionCommand, AccessLevel.Basic, CMDSUMMARY_FACTION, CMDSYNTAX_FACTION, CMDARGS_FACTION, useTyping: true);
        }

        #region /systeminfo

        private const string CMDKEYS_SYSTEMINFO = "systeminfo";
        private const string CMDSYNTAX_SYSTEMINFO = "systeminfo <SystemName>";
        private const string CMDSUMMARY_SYSTEMINFO = "Prints out detailed information about a system";
        private const string CMDARGS_SYSTEMINFO =
                "    {<SystemName>}\n" +
                "Write out the full SystemName here";

        public async Task HandleSystemInfoCommand(CommandContext context)
        {
            bool printJSON = context.Args[1].Equals("json");
            bool listStations = context.Args[1].Equals("list");

            string requestedSystem;
            if ((printJSON || listStations) && context.ArgCnt > 2)
            {
                requestedSystem = context.Message.Content.Substring(CMDKEYS_SYSTEMINFO.Length + 7);
            }
            else
            {
                requestedSystem = context.Message.Content.Substring(CMDKEYS_SYSTEMINFO.Length + 2);
            }
            RequestJSONResult requestResultSystem = await WebRequestService.GetWebJSONAsync(WebRequestService.EDSM_SystemInfo_URL(requestedSystem, true, true, true, true, true));
            RequestJSONResult requestResultStations = await WebRequestService.GetWebJSONAsync(WebRequestService.EDSM_SystemStations_URL(requestedSystem));
            RequestJSONResult requestResultTraffic = await WebRequestService.GetWebJSONAsync(WebRequestService.EDSM_SystemTraffic_URL(requestedSystem));
            RequestJSONResult requestResultDeaths = await WebRequestService.GetWebJSONAsync(WebRequestService.EDSM_SystemDeaths_URL(requestedSystem));
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
                    await SendMessage_SystemInfo(context, requestResultSystem.Object, requestResultStations.Object, requestResultTraffic.Object, requestResultDeaths.Object, listStations);
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

        public static async Task SendMessage_SystemInfo(CommandContext context, JSONObject system, JSONObject stations, JSONObject traffic, JSONObject deaths, bool listStations)
        {
            EmbedBuilder infoembed = new EmbedBuilder();
            List<EmbedField> listembed = new List<EmbedField>();
            bool stationsFound = false;

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
                List<StationInfo> stationInfos = new List<StationInfo>();
                if ((stations != null) && stations.HasField("stations"))
                {
                    stationsFound = true;
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
                                    info.Type = StationType.EngineerBase;
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
                            stationInfos.Add(info);
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
                int traffic_day = -1;

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
                infoembed.Color = Var.BOTCOLOR;
                infoembed.Title = string.Format("__**System Info for {0}**__", system_name);
                infoembed.Url = string.Format("https://www.edsm.net/en/system/id/{0}/name/{1}", system_id, system_name_link);
                infoembed.AddField("General Info", string.Format("{0}Star Type: {1}\nSecurity: {2}", system_requirepermit ? string.Format("**Requires Permit**: {0}\n", system_permit) : string.Empty, startype, security));

                bool provideInfoOnLarge = bestOrbitalLarge != null;
                bool provideInfoOnMedium = (!provideInfoOnLarge && bestOrbitalMedium != null) || (provideInfoOnLarge && bestOrbitalMedium != null && bestOrbitalLarge.Distance > bestOrbitalMedium.Distance);
                bool provideInfoOnPlanetary = (!provideInfoOnLarge && bestPlanetary != null) || (provideInfoOnLarge && bestPlanetary != null && bestOrbitalLarge.Distance > bestPlanetary.Distance);
                if (provideInfoOnLarge)
                {
                    infoembed.AddField("Closest Orbital Large Pad", bestOrbitalLarge.ToString());
                }
                if (provideInfoOnMedium)
                {
                    infoembed.AddField("Closest Orbital Medium Pad", bestOrbitalMedium.ToString());
                }
                if (provideInfoOnPlanetary)
                {
                    infoembed.AddField("Closest Planetary Large Pad", bestPlanetary.ToString());
                }
                if (!provideInfoOnLarge && !provideInfoOnMedium && !provideInfoOnPlanetary)
                {
                    infoembed.AddField("No Stations in this System!", "- / -");
                }
                if (traffic_day != -1 && traffic_week != -1)
                {
                    infoembed.AddField("Traffic Report", string.Format("Last 7 days: {0} CMDRs, last 24 hours: {1} CMDRs", traffic_week, traffic_day));
                }
                else
                {
                    infoembed.AddField("No Traffic Report Available", "- / -");
                }
                if (deaths_day != -1 && deaths_week != -1)
                {
                    infoembed.AddField("CMDR Deaths Report", string.Format("Last 7 days: {0} CMDRs, last 24 hours: {1} CMDRs", deaths_week, deaths_day));
                }
                else
                {
                    infoembed.AddField("No CMDR Deaths Report Available", "- / -");
                }

                if (stationsFound && stationInfos.Count > 0)
                {
                    foreach (StationInfo stationInfo in stationInfos)
                    {
                        listembed.Add(new EmbedField(stationInfo.Title_NoLink, stationInfo.Services_Link));
                    }
                }
            }
            else
            {
                infoembed.Description = string.Format("Could not get name & id from JSON. Here have the source json data: ```json\n{0}```", system.Print(true));
                infoembed.Color = Var.ERRORCOLOR;
            }

            await context.Channel.SendEmbedAsync(infoembed);
            if (listStations && stationsFound)
            {
                await context.Channel.SendSafeEmbedList("Stations in " + system_name, listembed);
            }
        }

        private class StationInfo
        {
            private static readonly CultureInfo culture = new CultureInfo("en-us");

            public string Name;
            public long Id;
            public StationType Type;
            public float Distance;
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
                Distance = distance;
            }

            public override string ToString()
            {
                return string.Format("{0}\n     {1}", Title, Services);
            }

            public string Title
            {
                get
                {
                    string distanceFormatted = Distance.ToString("### ### ###.00").Trim();
                    string result = string.Format("**{0} [{1}]({2})**: {3}, {4} ls", STATIONEMOJI[(int)Type], Name, EDSMLink, STATIONTYPENAMES[(int)Type], distanceFormatted);
                    return result;
                }
            }

            public string Title_NoLink
            {
                get
                {
                    string distanceFormatted = Distance.ToString("### ### ###.00").Trim();
                    string result = string.Format("**{0} {1}**: {2}, {3} ls", STATIONEMOJI[(int)Type], Name, STATIONTYPENAMES[(int)Type], distanceFormatted);
                    return result;
                }
            }

            public string Services
            {
                get
                {
                    string result = string.Format("{0}{1}{2}{3}{4}{5}", HasRestock ? "Restock, " : string.Empty, HasRefuel ? "Refuel, " : string.Empty, HasRepair ? "Repair, " : string.Empty, HasShipyard ? "Shipyard, " : string.Empty, HasOutfitting ? "Outfitting, " : string.Empty, HasUniversalCartographics ? "Universal Cartographics" : string.Empty);
                    if (result.EndsWith(", "))
                    {
                        result = result.Substring(0, result.Length - 2);
                    }
                    return result;
                }
            }

            public string Services_Link
            {
                get
                {
                    string result = string.Format("[Link]({0}) - {1}{2}{3}{4}{5}{6}", EDSMLink, HasRestock ? "Restock, " : string.Empty, HasRefuel ? "Refuel, " : string.Empty, HasRepair ? "Repair, " : string.Empty, HasShipyard ? "Shipyard, " : string.Empty, HasOutfitting ? "Outfitting, " : string.Empty, HasUniversalCartographics ? "Universal Cartographics" : string.Empty);
                    if (result.EndsWith(", "))
                    {
                        result = result.Substring(0, result.Length - 2);
                    }
                    return result;
                }
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
            EngineerBase,
            Other
        }

        public static readonly string[] STATIONEMOJI = new string[] { "<:orbis:553690990964244520>", "<:coriolis:553690991022964749>", "<:ocellus:553690990901460992>", "<:asteroid:553690991245262868>",
            "<:outpost:553690991060844567>", "<:planetary:553690991123496963>", "<:megaship:553690991144599573>", "<:unknown:553690991136342026>", "<:Engineer:554018579050397698>", "<:unknown:553690991136342026>" };
        public static readonly string[] STATIONTYPENAMES = new string[] { "Orbis Starport", "Coriolis Starport", "Ocellus Starport", "Asteroid Base", "Outpost", "Planetary", "Megaship", "Unlandable", "Engineer Base", "Other" };

        #endregion
        #region /distance


        private const string CMDKEYS_DISTANCE = "distance";
        private const string CMDSYNTAX_DISTANCE = "distance <SystemName1>, <SystemName2>";
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
                        requestedSystem1 += ' ';
                    }
                    requestedSystem1 += partial.Substring(0, partial.Length - 1);
                    commaEncountered = true;
                }
                else if (commaEncountered)
                {
                    if (!string.IsNullOrEmpty(requestedSystem2))
                    {
                        requestedSystem2 += ' ';
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
                string requestURL = WebRequestService.EDSM_MultipleSystemsInfo_URL(new string[] { requestedSystem1, requestedSystem2 }, false, true, false, false, false);
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
            return string.Format("Distance **{0}** <-> **{1}** ```{2} ly```", system1_name, system2_name, Vector3.Distance(coords1, coords2).ToString("### ### ###.00").Trim());
        }

        public static Vector3 ParseCoords(JSONObject Coordinates)
        {
            if ((Coordinates != null) && Coordinates.HasFields(new string[] { "x", "y", "z" }))
            {
                return new Vector3(Coordinates["x"].f, Coordinates["y"].f, Coordinates["z"].f);
            }
            else return new Vector3();
        }

        #endregion
        #region /cmdr


        private const string CMDKEYS_CMDR = "cmdr";
        private const string CMDSYNTAX_CMDR = "cmdr <CMDRName>";
        private const string CMDSUMMARY_CMDR = "Locates the inara profile of a commander";
        private const string CMDARGS_CMDR =
                "    <CMDRName>\n" +
                "Write out the full exact CMDR Name here";

        public async Task HandleCMDRCommand(CommandContext context)
        {
            string cmdrName;
            bool printJSON = context.Args[1].Equals("json");
            if (printJSON && context.ArgCnt > 2)
            {
                cmdrName = context.Message.Content.Substring(CMDKEYS_CMDR.Length + 7);
            }
            else
            {
                cmdrName = context.Message.Content.Substring(CMDKEYS_CMDR.Length + 2);
            }
            JSONObject RequestContent = WebRequestService.Inara_CMDR_Profile(cmdrName);
            //await context.Channel.SendEmbedAsync("Request JSON", string.Format("```json\n{0}```", RequestContent.Print(true).MaxLength(2037)));
            RequestJSONResult requestResultInara = await WebRequestService.GetWebJSONAsync("https://inara.cz/inapi/v1/", RequestContent);
            RequestJSONResult requestResultEDSM = await WebRequestService.GetWebJSONAsync(WebRequestService.EDSM_Commander_Location(cmdrName, true, false));
            bool inaraResultOK = false;
            JSONObject inaraEvents = null;
            if (requestResultInara.IsSuccess)
            {
                JSONObject result = requestResultInara.Object;
                if (printJSON)
                {
                    await context.Channel.SendEmbedAsync("Inara result JSON", string.Format("```json\n{0}```", result.Print(true).MaxLength(2037)));
                }
                JSONObject header = result["header"];
                inaraEvents = result["events"];
                if (header != null && inaraEvents != null)
                {
                    int eventStatus = 0;
                    header.GetField(ref eventStatus, "eventStatus");
                    if (eventStatus == 200 && inaraEvents.IsArray && inaraEvents.Count > 0)
                    {
                        inaraResultOK = true;
                    }
                }
                if (!inaraResultOK)
                {
                    await context.Channel.SendEmbedAsync("Result not OK! Here the result JSON:", string.Format("```json\n{0}```", result.Print(true).MaxLength(2037)));
                }
            }
            else if (requestResultInara.IsException)
            {
                await context.Channel.SendEmbedAsync(string.Format("Could not connect to Inaras services. Exception Message: `{0}`", requestResultInara.ThrownException.Message), true);
            }
            else
            {
                await context.Channel.SendEmbedAsync(string.Format("Could not connect to Inaras services. HTTP Error Message: `{0} {1}`", (int)requestResultInara.Status, requestResultInara.Status.ToString()), true);
            }
            bool edsmResultOK = false;
            if (requestResultEDSM.IsSuccess)
            {
                JSONObject result = requestResultEDSM.Object;
                if (printJSON)
                {
                    await context.Channel.SendEmbedAsync("EDSM result JSON", string.Format("```json\n{0}```", result.Print(true).MaxLength(2037)));
                }
                if (result.Count > 0)
                {
                    edsmResultOK = true;
                }
            }
            if (inaraResultOK)
            {
                await context.Channel.SendEmbedAsync(FormatMessage_InaraCMDR(inaraEvents[0]));
            }
            if (edsmResultOK)
            {
                await context.Channel.SendEmbedAsync(FormatMessage_EDSMCMDR(cmdrName, requestResultEDSM.Object));
            }
        }

        private EmbedBuilder FormatMessage_EDSMCMDR(string name, JSONObject response)
        {
            EmbedBuilder result = new EmbedBuilder();

            int msgnum = 0;
            string system = string.Empty;
            response.GetField(ref msgnum, "msgnum");
            response.GetField(ref system, "system");
            if (msgnum == 100 && !string.IsNullOrEmpty(system))
            {
                result.Color = Var.BOTCOLOR;
                result.Title = string.Format("EDSM profile of {0}", name);

                int systemId = 0;
                string shiptype = string.Empty;
                string profile_url = string.Empty;
                response.GetField(ref systemId, "systemId");
                response.GetField(ref shiptype, "shipType");
                response.GetField(ref profile_url, "url");

                if (!string.IsNullOrEmpty(profile_url))
                {
                    result.Url = makeLinkSafe(profile_url);
                }
                if (!string.IsNullOrEmpty(system))
                {
                    result.AddField("Last Reported Location", string.Format("[{0}](https://www.edsm.net/en/system/id/{1}/name/{2})", system, systemId, system.Replace(' ', '+')));
                }
                if (!string.IsNullOrEmpty(shiptype))
                {
                    result.AddField("Last Reported Ship", shiptype);
                }
            }
            else
            {
                result.Color = Var.ERRORCOLOR;
                result.Description ="CMDR not found on EDSM or profile is private";
            }

            return result;
        }

        private EmbedBuilder FormatMessage_InaraCMDR(JSONObject resultevent)
        {
            int eventStatus = 0;
            resultevent.GetField(ref eventStatus, "eventStatus");
            if (eventStatus == 200 || eventStatus == 202)
            {
                JSONObject cmdr = resultevent["eventData"];

                string cmdr_name = string.Empty;
                string cmdr_inara_url = string.Empty;
                string cmdr_avatar_url = string.Empty;
                string cmdr_gameRole = string.Empty;
                string cmdr_allegiance = string.Empty;
                cmdr.GetField(ref cmdr_name, "userName");
                cmdr.GetField(ref cmdr_inara_url, "inaraURL");
                cmdr.GetField(ref cmdr_avatar_url, "avatarImageURL");
                cmdr.GetField(ref cmdr_gameRole, "preferredGameRole");
                cmdr.GetField(ref cmdr_allegiance, "preferredAllegianceName");
                cmdr_inara_url = makeLinkSafe(cmdr_inara_url);
                cmdr_avatar_url = makeLinkSafe(cmdr_avatar_url);

                JSONObject cmdr_squadron = cmdr["commanderSquadron"];
                string cmdr_squadron_name = string.Empty;
                string cmdr_squadron_rank = string.Empty;
                string cmdr_squadron_url = string.Empty;
                int cmdr_squadron_membercount = 0;
                if (cmdr_squadron != null)
                {
                    cmdr_squadron.GetField(ref cmdr_squadron_name, "squadronName");
                    cmdr_squadron.GetField(ref cmdr_squadron_rank, "squadronMemberRank");
                    cmdr_squadron.GetField(ref cmdr_squadron_url, "inaraURL");
                    cmdr_squadron.GetField(ref cmdr_squadron_membercount, "squadronMembersCount");
                    cmdr_squadron_url = makeLinkSafe(cmdr_squadron_url);
                }

                if (cmdr != null)
                {

                    EmbedBuilder message = new EmbedBuilder();
                    message.Color = Var.BOTCOLOR;
                    message.Title = string.Format("Inara profile of {0}", cmdr_name);
                    message.ThumbnailUrl = cmdr_avatar_url;
                    message.Url = cmdr_inara_url;
                    if (!string.IsNullOrEmpty(cmdr_gameRole))
                    {
                        message.AddField("Preffered Role", cmdr_gameRole);
                    }
                    if (!string.IsNullOrEmpty(cmdr_allegiance))
                    {
                        message.AddField("Allegiance", cmdr_allegiance);
                    }
                    if (!string.IsNullOrEmpty(cmdr_squadron_name))
                    {
                        message.AddField("Squadron", string.Format("**[{0}]({1})** - Rank: **{2}**, Member Count: **{3}**", cmdr_squadron_name, cmdr_squadron_url, cmdr_squadron_rank, cmdr_squadron_membercount));
                    }
                    else
                    {
                        message.AddField("No Squadron", "- / -");
                    }
                    return message;
                }
            }
            else if (eventStatus == 204)
            {
                EmbedBuilder errormessage = new EmbedBuilder();
                errormessage.Color = Var.ERRORCOLOR;
                string eventResult = resultevent["eventStatusText"].str;
                if (eventResult.Equals("No results found."))
                {
                    eventResult = "CMDR not found on Inara";
                }
                errormessage.Description = eventResult;
                return errormessage;
            }

            EmbedBuilder error = new EmbedBuilder();
            error.Title = "Unknown Error!";
            error.Description = string.Format("```json\n{0}```", resultevent.Print(true).MaxLength(2037));
            error.Color = Var.ERRORCOLOR;

            return error;
        }

        private string makeLinkSafe(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            char[] oldString = input.ToCharArray();
            int occurences = 0;
            foreach (char character in oldString)
            {
                if (character == '\\')
                {
                    occurences++;
                }
            }
            char[] newString = new char[oldString.Length - occurences];
            int offset = 0;
            for (int i = 0; i < oldString.Length; i++)
            {
                if (oldString[i] == '\\')
                {
                    offset++;
                }
                else
                {
                    newString[i - offset] = oldString[i];
                }
            }
            return new string(newString);
        }


        #endregion
        #region /faction


        private const string CMDKEYS_FACTION = "faction";
        private const string CMDSYNTAX_FACTION = "faction <FactionName>";
        private const string CMDSUMMARY_FACTION = "Gets faction status information";
        private const string CMDARGS_FACTION =
                "    <FactionName>\n" +
                "Write out the full exact Faction Name here";

        public async Task HandleFactionCommand(CommandContext context)
        {
            string factionName;
            bool printJSON = context.Args[1].Equals("json");
            if (printJSON && context.ArgCnt > 2)
            {
                factionName = context.Message.Content.Substring(CMDKEYS_FACTION.Length + 7);
            }
            else
            {
                factionName = context.Message.Content.Substring(CMDKEYS_FACTION.Length + 2);
            }

            RequestJSONResult requestFaction = await WebRequestService.GetWebJSONAsync(WebRequestService.BGSBOT_FactionStatus(factionName));
            if (requestFaction.IsSuccess)
            {
                if (printJSON)
                {
                    await context.Channel.SendEmbedAsync("Faction Info JSON Dump", string.Format("```json\n{0}```", requestFaction.Object.Print(true).MaxLength(2037)));
                }
                JSONObject docs = requestFaction.Object["docs"];
                if ((docs != null) && docs.IsArray && docs.Count > 0)
                {
                    await FactionCommand_HandleFactionResponse(docs[0], context);
                }
            }
            else if (requestFaction.IsException)
            {
                await context.Channel.SendEmbedAsync(string.Format("Could not connect to BGSBots services. Exception Message: `{0}`", requestFaction.ThrownException.Message), true);
            }
            else
            {
                await context.Channel.SendEmbedAsync(string.Format("Could not connect to BGSBots services. HTTP Error Message: `{0} {1}`", (int)requestFaction.Status, requestFaction.Status.ToString()), true);
            }
        }

        private async Task FactionCommand_HandleFactionResponse(JSONObject faction, CommandContext context)
        {
            EmbedBuilder result = new EmbedBuilder();

            JSONObject presences = faction["faction_presence"];
            if ((presences != null) && presences.IsArray && presences.Count > 0)
            {
                string faction_name = string.Empty;
                string faction_gov = string.Empty;
                string faction_allegiance = string.Empty;
                int faction_id = 0;
                faction.GetField(ref faction_name, "name");
                faction.GetField(ref faction_gov, "government");
                faction.GetField(ref faction_allegiance, "allegiance");

                string faction_edsm_link = null;

                if (faction.GetField(ref faction_id, "eddb_id"))
                {
                    faction_edsm_link = "https://eddb.io/faction/" + faction_id;
                }

                result.Color = Var.BOTCOLOR;
                result.Title = faction_name;
                result.Description = string.Format("{0}Government: **{1}**\nAllegiance: **{2}**", 
                    string.IsNullOrEmpty(faction_edsm_link) ? "" : string.Format("[**EDDB Link**]({0})\n", faction_edsm_link),
                    faction_gov.FirstToUpper(), faction_allegiance.FirstToUpper());

                List<EmbedField> presenceList = new List<EmbedField>();
                foreach (JSONObject system in presences)
                {
                    string system_name = string.Empty;
                    string state = string.Empty;
                    float influence = 0;
                    system.GetField(ref system_name, "system_name");
                    system.GetField(ref state, "state");
                    system.GetField(ref influence, "influence");
                    presenceList.Add(new EmbedField(system_name, string.Format("State: **{0}**, Influence: **{1}**", state.FirstToUpper(), influence.ToString("P"))));
                }
                await context.Channel.SendEmbedAsync(result);
                await context.Channel.SendSafeEmbedList("System presences of " + faction_name, presenceList);
            }

        }

        #endregion
    }
}
