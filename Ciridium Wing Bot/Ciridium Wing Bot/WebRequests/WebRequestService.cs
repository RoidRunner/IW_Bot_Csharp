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

        public static async Task<RequestJSONResult> GetWebJSONAsync(string url, JSONObject requestinfo)
        {
            RequestJSONResult loadresult = new RequestJSONResult();
            try
            {
                using (HttpRequestMessage requestmessage = new HttpRequestMessage(HttpMethod.Get, url))
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
