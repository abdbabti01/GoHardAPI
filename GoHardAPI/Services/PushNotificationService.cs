using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GoHardAPI.Services
{
    public interface IPushNotificationService
    {
        Task SendMessageNotificationAsync(string fcmToken, string senderName, string messageContent, int senderId);
    }

    public class PushNotificationService : IPushNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PushNotificationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendMessageNotificationAsync(string fcmToken, string senderName, string messageContent, int senderId)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                _logger.LogDebug("No FCM token provided, skipping push notification");
                return;
            }

            // Check environment variable first, then config file
            var serverKey = Environment.GetEnvironmentVariable("FIREBASE_SERVER_KEY")
                ?? _configuration["Firebase:ServerKey"];

            if (string.IsNullOrWhiteSpace(serverKey))
            {
                _logger.LogWarning("Firebase server key not configured, skipping push notification");
                return;
            }

            try
            {
                var notification = new
                {
                    to = fcmToken,
                    notification = new
                    {
                        title = senderName,
                        body = messageContent.Length > 100
                            ? messageContent.Substring(0, 97) + "..."
                            : messageContent
                    },
                    data = new
                    {
                        type = "message",
                        senderId = senderId.ToString(),
                        senderName = senderName,
                        click_action = "FLUTTER_NOTIFICATION_CLICK"
                    },
                    android = new
                    {
                        priority = "high"
                    },
                    apns = new
                    {
                        payload = new
                        {
                            aps = new
                            {
                                sound = "default"
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(notification);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("key", "=" + serverKey);

                var response = await _httpClient.PostAsync(
                    "https://fcm.googleapis.com/fcm/send",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Push notification sent successfully to {Token}",
                        fcmToken.Substring(0, Math.Min(10, fcmToken.Length)) + "...");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to send push notification: {StatusCode} - {Response}",
                        response.StatusCode, responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification");
            }
        }
    }
}
