using System;
using System.Net;

namespace NmkdUtils
{
    public class WebUtils
    {
        public static string CacheDir = "";
        public static string CachePfxHttp = "http_";

        private static HttpClient _http = new();

        static WebUtils ()
        {
            CacheDir = PathUtils.GetCommonSubdir(PathUtils.CommonDir.Cache);
        }

        public static bool IsHttpResponseCachable(HttpStatusCode statusCode)
        {
            return new[] {
                HttpStatusCode.OK,
                HttpStatusCode.NonAuthoritativeInformation,
                HttpStatusCode.MultipleChoices,
                HttpStatusCode.MovedPermanently,
                HttpStatusCode.NotModified,
                HttpStatusCode.TemporaryRedirect
            }.Contains(statusCode);
        }

        public static bool IsCached(string url)
        {
            return File.Exists(GetCacheFilename(url));
        }

        public static string GetCacheFilename(string url)
        {
            return Path.Combine(CacheDir, $"{CachePfxHttp}{CryptUtils.GetHashSha256(url)}.txt");
        }

        /// <summary> Returns HTTP response body, with optional caching (on by default) </summary>
        public static async Task<string> GetHttpResponse(string requestUrl, HttpClient? client = null, bool allowCacheRead = true, bool allowCacheWrite = true)
        {
            client ??= _http;

            string cacheFilename = GetCacheFilename(requestUrl);

            if (allowCacheRead && File.Exists(cacheFilename))
            {
                Logger.Log($"Cached HTTP request: {requestUrl}", Logger.Level.Debug);
                return File.ReadAllText(cacheFilename);
            }

            Logger.Log($"HTTP request: {requestUrl}", Logger.Level.Verbose);
            var response = await client.GetAsync(requestUrl);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!allowCacheWrite)
            {
                return responseBody;
            }

            if (IsHttpResponseCachable(response.StatusCode))
            {
                File.WriteAllText(cacheFilename, responseBody);
            }
            else
            {
                Logger.Log($"Won't cache HTTP response because status code is {response.StatusCode}", Logger.Level.Verbose);
            }

            return responseBody;
        }

        public static void DownloadFile(string url, string filePath, HttpClient? client = null, bool log = false)
        {
            if(log)
            {
                Logger.Log($"Downloading file from {url} to {filePath} {(client == null ? "using new HttpClient" : "with existing HttpClient")}", Logger.Level.Info);
            }

            try
            {
                client ??= _http;
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = client.Send(request);
                using var stream = response.Content.ReadAsStream();
                using var fileStream = new FileStream(filePath, FileMode.CreateNew);
                stream.CopyTo(fileStream);
            }
            catch(Exception ex)
            {
                Logger.Log(ex, "Download failed");
            }
        }

        public static void DownloadFileCurl(string url, string filePath, bool log = false)
        {
            if (log)
            {
                Logger.Log($"Downloading file using curl from {url} to {filePath}", Logger.Level.Info);
            }

            string cmd = $"curl -L {url.Wrap()} -o {filePath.Wrap()}";
            var result = OsUtils.RunCommandShell(cmd);

            if(result.ExitCode != 0)
            {
                Logger.LogErr($"curl exited with code {result.ExitCode}.");
                Logger.LogErr(result.Output);
            }
        }

        public static Stream LoadStream(string url, HttpClient? client = null)
        {
            client ??= _http;
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = client.Send(request);
            return response.Content.ReadAsStream();
        }
    }
}
