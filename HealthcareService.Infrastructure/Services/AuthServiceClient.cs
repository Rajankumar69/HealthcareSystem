using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace HealthcareService.Services
{
    public class AuthServiceClient : IAuthServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthServiceClient> _logger;

        public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<UserInfoDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                // Service-to-service HTTP REST call to AuthService
                var response = await _httpClient.GetAsync($"/api/auth/users/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserInfoDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Service-to-service call to AuthService failed: {Message}", ex.Message);
            }

            return null;
        }
    }
}
