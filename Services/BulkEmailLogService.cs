using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class BulkEmailLogService : IBulkEmailLogService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;

        public BulkEmailLogService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<BulkEmailLogModel>> GetAllLogsAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync("email/bulk/history");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<BulkEmailLogModel>();
                }

                response.EnsureSuccessStatusCode();
                var logs = await response.Content.ReadFromJsonAsync<List<BulkEmailLogModel>>(_jsonOptions);
                return logs ?? new List<BulkEmailLogModel>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error fetching email history: {ex.Message}");
            }
        }

        public async Task<BulkEmailLogModel?> GetLogByIdAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"email/bulk/history/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BulkEmailLogModel>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error fetching email log details: {ex.Message}");
            }
        }
    }
}
