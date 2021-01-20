using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Lumpy.Lib.Common.Notification
{
    public class LineNotify
    {
        private ILogger _log;
        private static readonly HttpClient Client = new HttpClient();
        public string Token { get; set; }
        public string Url { get; set; }
        public LineNotify(string token, ILogger log, string url = "https://notify-api.line.me/api/notify")
        {
            Url = url;
            Token = token;
            _log = log;
        }
        public async void SendMessageAsync(string msg)
        {
            var cts = new CancellationTokenSource();
            try
            {
                var values = new Dictionary<string, string>() { { "message", msg } };
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
                await Client.SendAsync(httpRequestMessage,cts.Token);
            }
            catch (Exception e)
            {
                _log.Error("[LineNotify.SendMessageAsync] Exception: {e}", e);
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}
