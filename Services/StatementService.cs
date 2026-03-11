using Invoqs.Interfaces;
using Invoqs.Models;
using System.Text.Json;

namespace Invoqs.Services
{
    public class StatementService : IStatementService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;

        public StatementService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<StatementModel>> GetAllStatementsAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync("statements");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return new List<StatementModel>();
                }

                response.EnsureSuccessStatusCode();

                var statements = await response.Content.ReadFromJsonAsync<List<StatementModel>>(_jsonOptions);
                return statements ?? new List<StatementModel>();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching statements: {ex.Message}");
                return new List<StatementModel>();
            }
        }

        public async Task<StatementModel?> GetStatementByIdAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"statements/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<StatementModel>(_jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching statement {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<StatementModel?> CreateStatementAsync(CreateStatementModel model)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PostAsJsonAsync("statements", model, _jsonOptions);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Validation error: {errorContent}");
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var statement = await response.Content.ReadFromJsonAsync<StatementModel>(_jsonOptions);
                return statement;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error creating statement: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteStatementAsync(int id)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.DeleteAsync($"statements/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting statement {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]?> DownloadStatementPdfAsync(int statementId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.GetAsync($"statements/{statementId}/pdf");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"Statement {statementId} not found for PDF generation");
                    return null;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error downloading statement PDF: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SendStatementAsync(int statementId, List<string> recipientEmails)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var content = JsonContent.Create(new { RecipientEmails = recipientEmails });
                var response = await _httpClient.PostAsync($"statements/{statementId}/send", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error sending statement {statementId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkStatementAsDeliveredAsync(int statementId)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                _authService.AddAuthorizationHeader(_httpClient, token);

                var response = await _httpClient.PutAsync($"statements/{statementId}/mark-as-delivered", null);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _authService.LogoutAsync();
                    return false;
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error marking statement {statementId} as delivered: {ex.Message}");
                return false;
            }
        }
    }
}
