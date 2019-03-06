using System;
using System.Collections.Generic;
using System.Text;

namespace Ciridium.WebRequests
{
    static class WebRequests_URL_Methods
    {
        public static string EDSM_SystemInfo_URL(string systemName, bool showCoords, bool showPermit, bool showInformation, bool showPrimaryStar)
        {
            StringBuilder result = new StringBuilder();
            result.Append("https://www.edsm.net/api-v1/system?sysname=");
            result.Append(systemName.Replace(' ', '+'));
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
    }
}
