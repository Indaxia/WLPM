using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace wlpm.Repository
{
    public class Downloader
    {
        public static async Task downloadFileAsync(string url, string targetFilePath)
        {
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (Stream contentStream = await (await client.SendAsync(request)).Content.ReadAsStreamAsync(), stream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }
            }
        }
    }
}