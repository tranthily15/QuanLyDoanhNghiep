using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace QuanLyDoanhNghiep.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;

        public TranslationService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Translation:ApiKey"];
            _endpoint = configuration["Translation:Endpoint"];
        }

        public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            try
            {
                var requestBody = new
                {
                    q = text,
                    source = fromLanguage,
                    target = toLanguage
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TranslationResponse>(responseBody);

                return result?.Data?.TranslatedText ?? text;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Translation error: {ex.Message}");
                return text;
            }
        }

        public async Task<Dictionary<string, string>> GetBilingualSkillsAsync(List<string> skills)
        {
            var result = new Dictionary<string, string>();

            foreach (var skill in skills)
            {
                // Kiểm tra xem skill có phải tiếng Việt không
                if (IsVietnamese(skill))
                {
                    // Dịch sang tiếng Anh
                    var englishSkill = await TranslateAsync(skill, "vi", "en");
                    result[skill] = englishSkill;
                }
                else
                {
                    // Dịch sang tiếng Việt
                    var vietnameseSkill = await TranslateAsync(skill, "en", "vi");
                    result[skill] = vietnameseSkill;
                }
            }

            return result;
        }

        private bool IsVietnamese(string text)
        {
            // Kiểm tra xem text có chứa ký tự tiếng Việt không
            return text.Any(c => (c >= 0x00C0 && c <= 0x00FF) || // Latin-1 Supplement
                                (c >= 0x0100 && c <= 0x017F) || // Latin Extended-A
                                (c >= 0x0180 && c <= 0x024F));  // Latin Extended-B
        }

        private class TranslationResponse
        {
            public TranslationData Data { get; set; }
        }

        private class TranslationData
        {
            public string TranslatedText { get; set; }
        }
    }
} 