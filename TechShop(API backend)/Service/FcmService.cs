using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using TechShop_API_backend_.Data.Authenticate;

using TechShop_API_backend_.Models.Authenticate;

namespace TechShop_API_backend_.Service
{
    public class FcmService
    {
        private readonly UserFcmRepository _fcmRepository;

        public FcmService(UserFcmRepository fcmRepository)
        {
            _fcmRepository = fcmRepository;

            // Initialize Firebase only once
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(
                        Path.Combine(AppContext.BaseDirectory, "firebase", "webdev-project-22467-firebase-adminsdk-fbsvc-b6b185bb88.json")
                    )
                });
            }
        }

        // ✅ Register or update FCM token for a user
        public async Task<UserFcm> RegisterTokenAsync(int userId, string token)
        {
            return await _fcmRepository.UpsertTokenAsync(userId, token);
        }

        // 🚀 Send notification to a specific user
        public async Task<bool> SendMessageToUserAsync(
            int userId,
            string title,
            string body,
            Dictionary<string, string>? data = null)
        {
            var record = await _fcmRepository.GetByUserIdAsync(userId);

            if (record == null || string.IsNullOrWhiteSpace(record.FcmToken))
                return false;

            var message = new Message
            {
                Token = record.FcmToken,

                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },

                Data = data ?? new Dictionary<string, string>()
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine($"FCM Sent: {response}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Error: {ex.Message}");
                return false;
            }
        }

        
    }
}