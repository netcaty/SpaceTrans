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

                HttpResponseMessage response;
                try
                {
                    response = await httpClient.PostAsync("https://openapi.youdao.com/api", postData);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    // 网络层错误（连接失败、SSL 错误、超时等）附加友好提示后抛出，绝不静默返回原文
                    throw new HttpRequestException($"无法连接有道服务器（网络错误）：{ex.Message}", ex);
                }
                var responseContent = await response.Content.ReadAsStringAsync();

                // 非 2xx 响应一律视为翻译失败
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"有道 API 错误 (HTTP {(int)response.StatusCode}): {Snippet(responseContent)}");
                }

                var jsonDoc = JsonDocument.Parse(responseContent);

                // 有道 API 级错误通过 HTTP 200 + errorCode 字段返回，非 "0" 即为失败（如 101=缺少参数、108=应用Key无效）
                if (jsonDoc.RootElement.TryGetProperty("errorCode", out var errorCodeProp))
                {
                    var errorCode = errorCodeProp.ToString();
                    if (errorCode != "0")
                    {
                        throw new HttpRequestException($"有道翻译失败 (错误码 {errorCode}): {GetYoudaoErrorMessage(errorCode)}");
                    }
                }

                if (jsonDoc.RootElement.TryGetProperty("translation", out var translation) && translation.GetArrayLength() > 0)
                {
                    var translated = translation[0].GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(translated))
                    {
                        return translated;
                    }
                }

                // 响应缺少翻译结果时同样视为失败，绝不把原文当作翻译结果返回
                throw new InvalidOperationException("Youdao returned an empty translation or an unexpected response format.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Youdao translation error: {ex.Message}", ex);
                throw;
            }
        }

        private static string Snippet(string text, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length > maxLength ? text[..maxLength] + "..." : text;
        }

        private static string GetYoudaoErrorMessage(string errorCode)
        {
            return errorCode switch
            {
                "101" => "缺少必填参数",
                "102" => "不支持的语言类型",
                "103" => "翻译文本过长",
                "108" => "应用 Key 无效",
                "111" => "开发者账号无效",
                "113" => "待翻译文本（q）不能为空",
                "202" => "签名校验失败",
                "203" => "访问 IP 不在可访问列表",
                "206" => "时间戳校验失败",
                "207" => "盐值校验失败",
                "302" => "翻译查询失败",
                "303" => "服务异常，请稍后重试",
                "401" => "账户已欠费或停费",
                "411" => "访问频率受限",
                _ => "请参考有道开放平台错误码说明"
            };
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