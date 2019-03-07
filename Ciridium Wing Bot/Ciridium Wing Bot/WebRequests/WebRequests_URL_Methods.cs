using System;
using System.Collections.Generic;
using System.Text;

namespace Ciridium.WebRequests
{
    static class WebRequests_URL_Methods
    {
        public static string EDSM_SystemInfo_URL(string systemName, bool showId, bool showCoords, bool showPermit, bool showInformation, bool showPrimaryStar)
        {
            StringBuilder result = new StringBuilder();
            result.Append("https://www.edsm.net/api-v1/system?sysname=");
            result.Append(systemName.Replace(' ', '+'));
            if (showId)
            {
                result.Append("&showId=1");
            }
            if (showCoords)
            {
                result.Append("&showCoordinates=1");
            }
            if (showPermit)
            {
                result.Append("&showPermit=1");
            }
            if (showInformation)
            {
                result.Append("&showInformation=1");
            }
            if (showPrimaryStar)
            {
                result.Append("&showPrimaryStar=1");
            }
            return result.ToString();
        }

        public static string EDSM_MultipleSystemsInfo_URL(string[] systemNames, bool showId, bool showCoords, bool showPermit, bool showInformation, bool showPrimaryStar)
        {
            StringBuilder result = new StringBuilder();
            result.Append("https://www.edsm.net/api-v1/systems?");
            bool first = true;
            foreach (string systemName in systemNames)
            {
                if (first)
                {
                    first = false;
                    result.Append("systemName[]=");
                }
                else
                {
                    result.Append("&systemName[]=");
                }
                result.Append(systemName);
            }
            if (showId)
            {
                result.Append("&showId=1");
            }
            if (showCoords)
            {
                result.Append("&showCoordinates=1");
            }
            if (showPermit)
            {
                result.Append("&showPermit=1");
            }
            if (showInformation)
            {
                result.Append("&showInformation=1");
            }
            if (showPrimaryStar)
            {
                result.Append("&showPrimaryStar=1");
            }
            return result.ToString();
        }

        public static string EDSM_SystemStations_URL(string systemName)
        {
            StringBuilder result = new StringBuilder();
            result.Append("https://www.edsm.net/api-system-v1/stations?systemName=");
            result.Append(systemName.Replace(' ', '+'));
            return result.ToString();
        }

        public static string EDSM_SystemTraffic_URL(string systemName)
        {
            return "https://www.edsm.net/api-system-v1/traffic?systemName=" + systemName.Replace(' ', '+');
        }

        public static string EDSM_SystemDeaths_URL(string systemName)
        {
            return "https://www.edsm.net/api-system-v1/deaths?systemName=" + systemName.Replace(' ', '+');
        }

        internal static JSONObject Inara_CMDR_Profile(string cmdrName)
        {
            JSONObject result = new JSONObject();
            result.AddField("eventCustomID", 13458);
            result.AddField("eventName", "getCommanderProfile");
            result.AddField("eventTimestamp", DateTime.UtcNow.ToString("s") + "z");
            JSONObject eventdata = new JSONObject();
            eventdata.AddField("searchName", cmdrName);
            result.AddField("eventData", eventdata);
            return result;
        }
    }
}
