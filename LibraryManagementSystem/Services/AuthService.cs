using System.Net.Http;
using System.Net.Http.Json;

namespace LibraryManagementSystem.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Login and store session
        public async Task<AuthResponse?> LoginAsync(string userName, string password)
        {
            try
            {
                var request = new LoginRequest { UserName = userName, Password = password };
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                    if (authResponse?.Success == true && authResponse.User != null)
                    {
                        // Store session data
                        var session = _httpContextAccessor.HttpContext?.Session;
                        if (session != null)
                        {
                            session.SetString("SessionId", authResponse.SessionId ?? "");
                            session.SetInt32("UserId", authResponse.User.UserId);
                            session.SetString("UserName", authResponse.User.UserName);
                            session.SetString("FullName", authResponse.User.FullName);
                            session.SetString("Email", authResponse.User.Email);
                            session.SetString("Roles", string.Join(",", authResponse.User.Roles));
                        }
                    }

                    return authResponse;
                }

                return new AuthResponse { Success = false, Message = "Login failed" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new AuthResponse { Success = false, Message = "An error occurred during login" };
            }
        }

        // Validate current session
        public async Task<bool> ValidateSessionAsync()
        {
            try
            {
                var sessionId = GetSessionId();
                if (string.IsNullOrEmpty(sessionId))
                {
                    return false;
                }

                var request = new ValidateSessionRequest { SessionId = sessionId };
                var response = await _httpClient.PostAsJsonAsync("api/auth/validate", request);

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    return authResponse?.Success == true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session");
                return false;
            }
        }

        // Logout
        public async Task<bool> LogoutAsync()
        {
            try
            {
                var sessionId = GetSessionId();
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var request = new ValidateSessionRequest { SessionId = sessionId };
                    await _httpClient.PostAsJsonAsync("api/auth/logout", request);
                }

                _httpContextAccessor.HttpContext?.Session.Clear();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        // Get current user info from session
        public int GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.Session.GetInt32("UserId") ?? 0;
        }

        public string? GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("UserName");
        }

        public string? GetCurrentUserFullName()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("FullName");
        }

        public string? GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("Email");
        }

        public List<string> GetCurrentUserRoles()
        {
            var rolesString = _httpContextAccessor.HttpContext?.Session.GetString("Roles");
            if (string.IsNullOrEmpty(rolesString))
            {
                return new List<string>();
            }
            return rolesString.Split(',').ToList();
        }

        public bool IsInRole(string roleName)
        {
            return GetCurrentUserRoles().Contains(roleName);
        }

        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(GetSessionId()) && GetCurrentUserId() > 0;
        }

        private string? GetSessionId()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("SessionId");
        }

        // User management methods (for SuperAdmin)
        public async Task<List<UserDto>?> GetAllUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/auth/users");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<UserDto>>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return null;
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/auth/users/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserDto>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user");
                return null;
            }
        }

        public async Task<AuthResponse?> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/users", request);
                return await response.Content.ReadFromJsonAsync<AuthResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return new AuthResponse { Success = false, Message = "Error creating user" };
            }
        }

        public async Task<AuthResponse?> UpdateUserAsync(UpdateUserRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/auth/users/{request.UserId}", request);
                return await response.Content.ReadFromJsonAsync<AuthResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return new AuthResponse { Success = false, Message = "Error updating user" };
            }
        }

        public async Task<List<RoleDto>?> GetAllRolesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/auth/roles");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<RoleDto>>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return null;
            }
        }

        public async Task<UserInfo?> GetUserInfoAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Fetching user info for UserId: {UserId}", userId);

                var response = await _httpClient.GetAsync($"api/auth/users/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get user info for UserId: {UserId}. Status: {StatusCode}",
                        userId, response.StatusCode);
                    return null;
                }

                var userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();

                _logger.LogInformation("Successfully retrieved user info for UserId: {UserId}", userId);

                return userInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user info for UserId: {UserId}", userId);
                return null;
            }
        }
    }

    // Add this class at the top (outside the AuthService class)


    // Then add this method INSIDE the AuthService class


    // DTOs matching the Auth API
    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ValidateSessionRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<int> RoleIds { get; set; } = new();
    }

    public class UpdateUserRequest
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public UserDto? User { get; set; }
        public string? SessionId { get; set; }
        public string? Message { get; set; }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }


}