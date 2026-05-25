using Microsoft.AspNetCore.SignalR;

namespace RewardSystem.Hubs
{
    public class NotificationHub : Hub
    {
        // Kullanıcı bağlandığında kendi grubuna katılır
        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }
    }
}