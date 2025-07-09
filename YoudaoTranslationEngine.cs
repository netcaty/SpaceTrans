using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpaceTrans.Engines
{
    public class YoudaoTranslationEngine : ITranslationEngine
    {
        private readonly string appKey;
        private readonly string appSecret;
        private readonly HttpClient httpClient;

        public string Name => "Youdao";
        public string Description => "Youdao Translation API";

        public YoudaoTranslationEngine(string appKey, string appSecret, HttpClient httpClient)
        {
            this.appKey = appKey;
            this.appSecret = appSecret;
            this.httpClient = httpClient;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var testResult = await TranslateAsync("test", "auto", "en");
                return !string.IsNullOrEmpty(testResult);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage)
        {
            try
            {
                var salt = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                var curtime = salt;
                var input = Truncate(text);
                var str1 = $"{appKey}{input}{salt}{curtime}{appSecret}";
                var sign = ComputeSHA256Hash(str1);

                var postData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("q", text),
                    new KeyValuePair<string, string>("from", fromLanguage),
                    new KeyValuePair<string, string>("to", toLanguage),
                    new KeyValuePair<string, string>("appKey", appKey),
                    new KeyValuePair<string, string>("salt", salt),
                    new KeyValuePair<string, string>("sign", sign),
                    new KeyValuePair<string, string>("signType", "v3"),
                    new KeyValuePair<string, string>("curtime", curtime)
                });

                var response = await httpClient.PostAsync("https://openapi.youdao.com/api", postData);
                var responseContent = await response.Content.ReadAsStringAsync();

                var jsonDoc = JsonDocument.Parse(responseContent);
                if (jsonDoc.RootElement.TryGetProperty("translation", out var translation) && translation.GetArrayLength() > 0)
                {
                    return translation[0].GetString() ?? text;
                }

                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Youdao translation error: {ex.Message}");
                return text;
            }
        }

        private static string ComputeSHA256Hash(string rawData)
        {
            using var sha256Hash = SHA256.Create();
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Truncate(string q)
        {
            var len = q.Length;
            if (len <= 20)
                return q;
            return q[..10] + len + q[^10..];
        }
    }
}