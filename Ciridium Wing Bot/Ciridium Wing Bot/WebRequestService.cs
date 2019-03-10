using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ciridium.WebRequests
{
    static class WebRequestService
    {
        private static readonly HttpClient httpClient;

        static WebRequestService()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        #region WebRequest Methods

        public static async Task<RequestJSONResult> GetWebJSONAsync(string url, JSONObject requestinfo)
        {
            RequestJSONResult loadresult = new RequestJSONResult();
            try
            {
                using (HttpRequestMessage requestmessage = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    requestmessage.Version = new Version(1, 1);
                    string requestcontent = requestinfo.Print(true);
                    requestmessage.Content = new StringContent(requestcontent, Encoding.UTF8, "application/json");
                    using (HttpResponseMessage responsemessage = await httpClient.SendAsync(requestmessage))
                    {
                        loadresult.Status = responsemessage.StatusCode;
                        loadresult.IsSuccess = responsemessage.IsSuccessStatusCode;
                        if (responsemessage.IsSuccessStatusCode)
                        {
                            string content = await responsemessage.Content.ReadAsStringAsync();
                            loadresult.Object = new JSONObject(content);
                        }
                    }
                }
            } catch (Exception e)
            {
                loadresult.IsException = true;
                loadresult.ThrownException = e;
            }
            return loadresult;
        }

        public static async Task<RequestJSONResult> GetWebJSONAsync(string url)
        {
            RequestJSONResult loadresult = new RequestJSONResult();
            try
            {
                using (HttpRequestMessage requestmessage = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    requestmessage.Version = new Version(1, 1);
                    using (HttpResponseMessage responsemessage = await httpClient.SendAsync(requestmessage))
                    {
                        loadresult.Status = responsemessage.StatusCode;
                        loadresult.IsSuccess = responsemessage.IsSuccessStatusCode;
                        if (responsemessage.IsSuccessStatusCode)
                        {
                            string content = await responsemessage.Content.ReadAsStringAsync();
                            loadresult.Object = new JSONObject(content);
                        }
                    }
                }
            } catch (Exception e)
            {
                loadresult.IsException = true;
                loadresult.ThrownException = e;
            }
            return loadresult;
        }

        #endregion
        #region Macro Methods for generating requests

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

        public static string EDSM_Commander_Location(string commanderName, bool showId, bool showCoordinates)
        {
            StringBuilder result = new StringBuilder();
            result.Append("https://www.edsm.net/api-logs-v1/get-position?commanderName=");
            result.Append(commanderName.Replace(' ', '+'));
            if (showId)
            {
                result.Append("&showId=1");
            }
            if (showCoordinates)
            {
                result.Append("&showCoordinates=1");
            }
            return result.ToString();
        }

        internal static JSONObject Inara_CMDR_Profile(string cmdrName)
        {
            JSONObject result = Inara_Base_Request();
            JSONObject events = result["events"];
            JSONObject singleevent = new JSONObject();
            singleevent.AddField("eventName", "getCommanderProfile");
            singleevent.AddField("eventTimestamp", DateTime.UtcNow.ToString("s") + "Z");
            JSONObject eventdata = new JSONObject();
            eventdata.AddField("searchName", cmdrName);
            singleevent.AddField("eventData", eventdata);
            events.Add(singleevent);
            return result;
        }

        internal static JSONObject Inara_Base_Request()
        {
            JSONObject result = new JSONObject();
            result.AddField("events", new JSONObject());
            JSONObject header = new JSONObject();
            header.AddField("appName", Var.INARA_APPNAME);
            header.AddField("appVersion", Var.VERSION.ToString());
            header.AddField("isDeveloped", true);
            header.AddField("APIkey", SettingsModel.Inara_APIkey);
            result.AddField("header", header);
            return result;
        }


        #endregion
    }

    public class RequestJSONResult
    {
        public JSONObject Object = null;
        public HttpStatusCode Status = HttpStatusCode.Continue;
        public Exception ThrownException = null;

        public bool IsSuccess = false;
        public bool IsException = false;
    }
}
