using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace NmkdUtils
{
    public static class WebExtensions
    {
        public static async Task DownloadFileAsync(this HttpClient client, string uri, string path)
        {
            using var stream = await client.GetStreamAsync(uri);
            using var fileStream = new FileStream(path, FileMode.CreateNew);
            await stream.CopyToAsync(fileStream);
        }

        public static void DownloadFile(this HttpClient client, string uri, string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = client.Send(request);
            using var stream = response.Content.ReadAsStream();
            using var fileStream = new FileStream(path, FileMode.CreateNew);
            stream.CopyTo(fileStream);
        }

        public static JObject GetJson(this HttpClient client, string url, Logger.Level logLvl = Logger.Level.Verbose)
        {
            Logger.Log($"GET {url}", logLvl);
            var response = client.GetAsync(url).Result;
            var json = response.Content.ReadAsStringAsync().Result;
            return JObject.Parse(json);
        }

        public static JObject PostJson<T>(this HttpClient client, string url, T payload, out TimeSpan time)
        {
            var sw = Stopwatch.StartNew();
            var result = PostJson(client, url, payload);
            time = sw.Elapsed;
            return result;
        }

        public static JObject PostJson<T>(this HttpClient client, string url, T payload)
        {
            var jsonString = payload is JObject jo ? jo.ToString() : JsonConvert.SerializeObject(payload);
            using var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            using var response = client.PostAsync(url, content).Result;

            if (response == null || !response.IsSuccessStatusCode)
            {
                Logger.LogErr($"Error posting JSON: {response?.StatusCode} - {response?.ReasonPhrase}");
                return null;
            }

            var body = response.Content.ReadAsStringAsync().Result;

            try
            {
                return JObject.Parse(body);
            }
            catch(JsonReaderException ex)
            {
                Logger.LogErr($"Failed to parse JSON response: {ex.Message}");
                Logger.Log(body, Logger.Level.Verbose);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return null;
            }
        }
    }
}
