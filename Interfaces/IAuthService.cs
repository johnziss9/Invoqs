namespace Invoqs.Services
{
    public interface IAuthService
    {
        Task<string?> GetTokenAsync();
        Task<bool> IsAuthenticatedAsync();
        Task LogoutAsync();
        void AddAuthorizationHeader(HttpClient httpClient, string? token);
    }
}