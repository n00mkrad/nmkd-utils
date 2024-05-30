using Microsoft.VisualBasic;
using System.Net;

namespace NmkdUtils
{
    public class WebUtils
    {
        private static string _cacheDir = "";
        public static string CacheDir { get => _cacheDir; set { Directory.CreateDirectory(value); _cacheDir = value; } }
        public static string CachePfxHttp = "http_";

        private static HttpClient _http = new();

        static WebUtils ()
        {
            CacheDir = Path.Combine(AppContext.BaseDirectory, "Cache");
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

        public static void DownloadToFile(string url, string filePath, HttpClient? client = null)
        {
            client ??= _http;
            client.DownloadFile(url, filePath);
        }
    }
}
