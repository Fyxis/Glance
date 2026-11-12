using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Glance
{
    /// <summary>
    /// HTTP Client untuk mengambil lirik lagu tersinkronisasi dari API publik lrclib.net.
    /// </summary>
    public static class LrcLibApiClient
    {
        private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
        private static readonly JsonHelper json = new JsonHelper();

        public static List<LrcSearchItem> FetchCandidates(string url)
        {
            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.TryAddWithoutValidation("User-Agent", "Glance/1.0");
                    using (var resp = http.Send(req))
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            using (var sr = new StreamReader(resp.Content.ReadAsStream(), Encoding.UTF8))
                            {
                                string body = sr.ReadToEnd();
                                var items = json.Deserialize<List<LrcSearchItem>>(body);
                                if (items != null) return items;
                            }
                        }
                    }
                }
            }
            catch { }
            return new List<LrcSearchItem>();
        }
    }
}
