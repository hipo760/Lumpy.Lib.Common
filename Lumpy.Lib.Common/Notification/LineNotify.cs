using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lumpy.Lib.Common.Notification
{
    public class LineNotify
    {
        private static HttpClient _client;
        public string Token { get; set; }
        public string Url { get; set; }
        public string PrefixMsg { get; set; }

        public LineNotify(string token, string url = "https://notify-api.line.me/api/notify",string prefixMsg = "", int httpClientTimeoutSec = 30)
        {
            Url = url;
            PrefixMsg = prefixMsg;
            Token = token;
            _client = new HttpClient() { Timeout = TimeSpan.FromSeconds(httpClientTimeoutSec) };
        }
        public async Task SendMessageAsync(string msg)
        {
            var cts = new CancellationTokenSource();
            try
            {
                var values = new Dictionary<string, string>() { { "message", PrefixMsg + msg } };
                var content = new FormUrlEncodedContent(values);
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(Url),
                    Headers = {
                        { HttpRequestHeader.Authorization.ToString(), $"Bearer {Token}" },
                    },
                    Content = content
                };
                await _client.SendAsync(httpRequestMessage,cts.Token);
            }
            catch (Exception e)
            {
                Log.Logger.Error("[LineNotify.SendMessageAsync] Exception: {e}", e);
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}
