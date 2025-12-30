using Microsoft.Maui.Storage;

namespace GoHardApp.Services
{
    public class AuthService
    {
        private const string TokenKey = "jwt_token";
        private const string UserIdKey = "user_id";
        private const string UserNameKey = "user_name";
        private const string UserEmailKey = "user_email";

        public async Task SaveTokenAsync(string token, int userId, string name, string email)
        {
            await SecureStorage.SetAsync(TokenKey, token);
            await SecureStorage.SetAsync(UserIdKey, userId.ToString());
            await SecureStorage.SetAsync(UserNameKey, name);
            await SecureStorage.SetAsync(UserEmailKey, email);
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(TokenKey);
            }
            catch
            {
                return null;
            }
        }

        public async Task<int?> GetUserIdAsync()
        {
            try
            {
                var userIdString = await SecureStorage.GetAsync(UserIdKey);
                if (int.TryParse(userIdString, out int userId))
                {
                    return userId;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetUserNameAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(UserNameKey);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetUserEmailAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(UserEmailKey);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        public void ClearToken()
        {
            SecureStorage.Remove(TokenKey);
            SecureStorage.Remove(UserIdKey);
            SecureStorage.Remove(UserNameKey);
            SecureStorage.Remove(UserEmailKey);
        }
    }
}
