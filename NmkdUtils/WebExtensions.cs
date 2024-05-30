using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
