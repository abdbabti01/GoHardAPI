using System.Net.Http.Json;
using System.Text.Json;
using GoHardApp.Models;

namespace GoHardApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly AuthService _authService;

        public ApiService()
        {
            // For Android physical device, use your PC's IP address
            // Make sure your phone and PC are on the same WiFi network
            // IMPORTANT: BaseAddress must end with trailing slash for relative URIs to work!
#if ANDROID
            _baseUrl = "http://10.0.0.4:5121/api/";
#else
            _baseUrl = "http://localhost:5121/api/";
#endif

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(10) // 10 second timeout
            };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _authService = new AuthService();
        }

        private async Task SetAuthorizationHeaderAsync()
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        // Exercise Templates
        public async Task<List<ExerciseTemplate>> GetExerciseTemplatesAsync(string? category = null, string? muscleGroup = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== API: Fetching from {_baseUrl}/exercisetemplates ===");

                var query = "";
                if (!string.IsNullOrEmpty(category))
                    query += $"?category={category}";
                if (!string.IsNullOrEmpty(muscleGroup))
                    query += string.IsNullOrEmpty(query) ? $"?muscleGroup={muscleGroup}" : $"&muscleGroup={muscleGroup}";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var url = $"{_baseUrl}/exercisetemplates{query}";
                System.Diagnostics.Debug.WriteLine($"=== API: Full URL: {url} ===");

                // Use GetAsync to get more control over the response
                var httpResponse = await _httpClient.GetAsync($"exercisetemplates{query}", cts.Token);

                System.Diagnostics.Debug.WriteLine($"=== API: HTTP Status: {httpResponse.StatusCode} ===");

                httpResponse.EnsureSuccessStatusCode();

                var jsonContent = await httpResponse.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"=== API: JSON Length: {jsonContent.Length} ===");
                System.Diagnostics.Debug.WriteLine($"=== API: JSON Content: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))} ===");

                var response = JsonSerializer.Deserialize<List<ExerciseTemplate>>(jsonContent, _jsonOptions);

                System.Diagnostics.Debug.WriteLine($"=== API: Deserialized {response?.Count ?? 0} exercises ===");

                if (response != null && response.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"=== API: First exercise: {response[0].Name} ===");
                }

                return response ?? new List<ExerciseTemplate>();
            }
            catch (OperationCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== API CANCELLED/TIMEOUT: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"=== API STACK: {ex.StackTrace} ===");
                throw new Exception($"Request timed out after 5 seconds");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== API HTTP ERROR: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"=== API STACK: {ex.StackTrace} ===");
                throw new Exception($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== API ERROR: {ex.GetType().Name}: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"=== API STACK: {ex.StackTrace} ===");
                throw;
            }
        }

        public async Task<ExerciseTemplate?> GetExerciseTemplateAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExerciseTemplate>($"exercisetemplates/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching exercise template: {ex.Message}");
                return null;
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<string>>("exercisetemplates/categories", _jsonOptions);
                return response ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching categories: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetMuscleGroupsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<string>>("exercisetemplates/musclegroups", _jsonOptions);
                return response ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching muscle groups: {ex.Message}");
                return new List<string>();
            }
        }

        // Users
        public async Task<List<User>> GetUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<User>>("users", _jsonOptions);
                return response ?? new List<User>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching users: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<User?> GetUserAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<User>($"users/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching user: {ex.Message}");
                return null;
            }
        }

        public async Task<User?> CreateUserAsync(User user)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("users", user);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<User>(_jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
                return null;
            }
        }

        // Sessions
        public async Task<List<Session>> GetSessionsAsync()
        {
            try
            {
                await SetAuthorizationHeaderAsync();
                var response = await _httpClient.GetFromJsonAsync<List<Session>>("sessions", _jsonOptions);
                return response ?? new List<Session>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching sessions: {ex.Message}");
                return new List<Session>();
            }
        }

        public async Task<Session?> GetSessionAsync(int id)
        {
            try
            {
                await SetAuthorizationHeaderAsync();
                return await _httpClient.GetFromJsonAsync<Session>($"sessions/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching session: {ex.Message}");
                return null;
            }
        }

        public async Task<Session?> CreateSessionAsync(Session session)
        {
            try
            {
                await SetAuthorizationHeaderAsync();
                var response = await _httpClient.PostAsJsonAsync("sessions", session);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Session>(_jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating session: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteSessionAsync(int id)
        {
            try
            {
                await SetAuthorizationHeaderAsync();
                var response = await _httpClient.DeleteAsync($"sessions/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting session: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSessionStatusAsync(int id, string status)
        {
            try
            {
                await SetAuthorizationHeaderAsync();
                var response = await _httpClient.PatchAsJsonAsync($"sessions/{id}/status", status);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating session status: {ex.Message}");
                return false;
            }
        }

        public async Task<Exercise?> AddExerciseToSessionAsync(int sessionId, int exerciseTemplateId)
        {
            try
            {
                await SetAuthorizationHeaderAsync();
                var response = await _httpClient.PostAsJsonAsync($"sessions/{sessionId}/exercises", exerciseTemplateId);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Exercise>(_jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding exercise to session: {ex.Message}");
                return null;
            }
        }

        // Exercise Sets
        public async Task<List<ExerciseSet>> GetExerciseSetsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExerciseSet>>("exercisesets", _jsonOptions);
                return response ?? new List<ExerciseSet>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching exercise sets: {ex.Message}");
                return new List<ExerciseSet>();
            }
        }

        public async Task<List<ExerciseSet>> GetExerciseSetsByExerciseAsync(int exerciseId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExerciseSet>>($"exercisesets/exercise/{exerciseId}", _jsonOptions);
                return response ?? new List<ExerciseSet>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching exercise sets: {ex.Message}");
                return new List<ExerciseSet>();
            }
        }

        public async Task<ExerciseSet?> GetExerciseSetAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExerciseSet>($"exercisesets/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching exercise set: {ex.Message}");
                return null;
            }
        }

        public async Task<ExerciseSet?> CreateExerciseSetAsync(ExerciseSet exerciseSet)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("exercisesets", exerciseSet);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExerciseSet>(_jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating exercise set: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateExerciseSetAsync(int id, ExerciseSet exerciseSet)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"exercisesets/{id}", exerciseSet);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating exercise set: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CompleteExerciseSetAsync(int id)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"exercisesets/{id}/complete", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error completing exercise set: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteExerciseSetAsync(int id)
        {
            try
            {
                await SetAuthorizationHeaderAsync();
                var response = await _httpClient.DeleteAsync($"exercisesets/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting exercise set: {ex.Message}");
                return false;
            }
        }

        // Authentication
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("auth/login", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Login failed: {errorContent}");
                    return null;
                }

                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);

                if (authResponse != null)
                {
                    // Save token to secure storage
                    await _authService.SaveTokenAsync(authResponse.Token, authResponse.UserId, authResponse.Name, authResponse.Email);
                    // Set authorization header for future requests
                    await SetAuthorizationHeaderAsync();
                }

                return authResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during login: {ex.Message}");
                return null;
            }
        }

        public async Task<AuthResponse?> SignupAsync(SignupRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("auth/signup", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Signup failed: {errorContent}");
                    return null;
                }

                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);

                if (authResponse != null)
                {
                    // Save token to secure storage
                    await _authService.SaveTokenAsync(authResponse.Token, authResponse.UserId, authResponse.Name, authResponse.Email);
                    // Set authorization header for future requests
                    await SetAuthorizationHeaderAsync();
                }

                return authResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during signup: {ex.Message}");
                return null;
            }
        }

        public async Task LogoutAsync()
        {
            _authService.ClearToken();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
