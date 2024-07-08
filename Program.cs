using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static string configFileUrl = "https://raw.githubusercontent.com/burakdorman/UnfetteredDiscordAnnouncer/main/config.json";
    static string webhookUrl = "X";
    static async Task Main(string[] args)
    {
        await SendDiscordEmbedMessage("Yoda", 10, 1);
    }

    public static async Task SendDiscordEmbedMessage(string username, double prize, int gamemode)
    {
        var config = await LoadConfigAsync(configFileUrl);

        double minPrize = config["minPrize"]?.ToObject<double>() ?? 0.0;
        double maxPrize = config["maxPrize"]?.ToObject<double>() ?? 1000.0;

        if (prize < minPrize)
        {
            Console.WriteLine("Prize değeri minPrize'dan küçük. Duyuru iptal ediliyor.");
            return;
        }

        string role = "Ashtannian";
        string modeName = "Unknown";
        string imageUrl = "";
        string thumbnailUrl = "";
        string description = $"Hey {role}, congratulations to {username}! They won ${prize} from {modeName}./nCheck <#1198969169325072394> if you want to join them.";

        JArray gifUrls = null;
        JArray messages = null;

        if (prize > maxPrize)
        {
            role = "@everyone";
            gifUrls = config["gifs"]?["fireworks"]?.ToObject<JArray>();
            messages = config["messages"]?["rich"]?.ToObject<JArray>();
        }
        else if (prize > ((minPrize + maxPrize) / 2))
        {
            role = "<@&1019896148602929183>";
            gifUrls = config["gifs"]?["rich"]?.ToObject<JArray>();
            messages = config["messages"]?["poor"]?.ToObject<JArray>();
        }
        else
        {
           // gifUrls = config["gifs"]?.ToObject<JArray>();
           // messages = config["messages"]?.ToObject<JArray>();
            gifUrls = config["gifs"]?["poor"]?.ToObject<JArray>();
            messages = config["messages"]?["poor"]?.ToObject<JArray>();
        }

        if (gifUrls != null && gifUrls.Count > 0)
        {
            var random = new Random();
            int index = random.Next(gifUrls.Count);
            imageUrl = gifUrls[index].ToString();
        }

        // Gamemode değerine göre thumbnail ve modeName seçimi
        var modeUrls = config["modes"]?.ToObject<JArray>();
        if (modeUrls != null && modeUrls.Count > 0)
        {
            var random = new Random();
            int index = random.Next(gifUrls.Count);
            switch (gamemode)
            {
                case 1:
                    thumbnailUrl = modeUrls[0].ToString();
                    modeName = "Solo Game Mode";
                    break;
                case 2:
                    thumbnailUrl = modeUrls[1].ToString();
                    modeName = "Team Game Mode";
                    break;
                case 3:
                    thumbnailUrl = modeUrls[2].ToString();
                    modeName = "Box";
                    break;
                default:
                    Console.WriteLine("Geçersiz gamemode değeri. Varsayılan thumbnail kullanılacak.");
                    break;
            }
        }

        // Mesajlardan rastgele birini seçme
        if (messages != null && messages.Count > 0)
        {
            var randomMessageIndex = new Random().Next(messages.Count);
            description = messages[randomMessageIndex].ToString()
                .Replace("{username}", username)
                .Replace("{prize}", prize.ToString())
                .Replace("{mode}", modeName)
                .Replace("{role}", role)
                .Replace("{guide}", config["guide_channel"]?.ToString());
        }

        int color = CalculateColor(prize, minPrize, maxPrize);

        var embed = new JObject
        {
            ["title"] = config["embed_title"]?.ToString() ?? "Prize Announcement",
            ["url"] = config["embed_url"]?.ToString() ?? "https://theunfettered.io/wiki",
            ["description"] = description,
            ["color"] = color,
            ["footer"] = new JObject
            {
                ["text"] = "Unfettered Awakening",
                ["icon_url"] = "https://cdn-longterm.mee6.xyz/plugins/embeds/images/915296741471957064/be859ca0db4872ad361ba31d70523456ea52cea88ccbe357f14cecab874ab30f.png"
            },
            ["image"] = new JObject
            {
                ["url"] = imageUrl
            },
            ["thumbnail"] = new JObject
            {
                ["url"] = thumbnailUrl
            }
        };

        var data = new JObject
        {
            ["username"] = config["bot_name"] ?? "Announcer",
            ["embeds"] = new JArray(embed)
        };

        //Console.WriteLine("Gönderilen JSON:");
        //Console.WriteLine(data.ToString());

        using (var client = new HttpClient())
        {
            var content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(webhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                //Console.WriteLine("Mesaj başarıyla gönderildi!");
            }
            else
            {
                Console.WriteLine("Mesaj gönderilemedi. Durum kodu: " + response.StatusCode);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Hata mesajı: " + responseContent);
            }
        }
    }
    static int CalculateColor(double prize, double minPrize, double maxPrize)
    {
        double normalizedPrize = (prize - minPrize) / (maxPrize - minPrize);
        int colorValue = (int)(normalizedPrize * 255);
        int color = (colorValue << 16) | ((255 - colorValue) << 8);
        return color & 0xFFFFFF;
    }
    static async Task<JObject> LoadConfigAsync(string fileUrl)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetStringAsync(fileUrl);
            return JObject.Parse(response);
        }
    }
}
