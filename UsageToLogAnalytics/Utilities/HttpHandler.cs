using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure;
using System.Net.Http;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Utilities
{
    public static class HttpHandler
    {
        private static readonly bool _includeHeader = true;
        public static HttpClient BuildClient()
        {
            var handler = SetHandler();
            var httpClient = SetClient(handler);
            return httpClient;
        }

        public static HttpClient SetClient(WebRequestHandler handler)
        {
            HttpClient httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            var activityid = Guid.NewGuid().ToString();
            if (_includeHeader)
            {
                httpClient.DefaultRequestHeaders.Add("x-ms-correlation-id", activityid);
                httpClient.DefaultRequestHeaders.Add("x-ms-tracking-id", Guid.NewGuid().ToString());
                string authHeader = GetAuthorizationHeader();
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            }

            return httpClient;
        }

        public static WebRequestHandler SetHandler()
        {
            var handler = new WebRequestHandler();
            return handler;
        }

        public static string GetAuthorizationHeader()
        {
            var token = CryptoHelper.GetKeyVaultSecret("ccmaccesstoken");
            return $"Bearer {token}";
        }
    }
}
