using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace BetterSR.Services;

public class SayingService
{
    private const string ApiUrl = "https://uapis.cn/api/v1/saying";

    public async Task<string> GetSayingAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var json = await client.GetStringAsync(ApiUrl);
            dynamic? result = JsonConvert.DeserializeObject(json);
            if (result?.text != null)
            {
                return result.text.ToString();
            }
        }
        catch
        {
            // Network failure fallback.
        }
        return "每一次录制，都是一次创作。";
    }
}
