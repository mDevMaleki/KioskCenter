using Microsoft.AspNetCore.SignalR;

namespace KioskCenter.Hubs
{
    public class PaymentHub : Hub
    {
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }

        public async Task NotifyPaymentVerified(string orderId, string refId)
        {
            await Clients.Group($"order_{orderId}").SendAsync("PaymentVerified", new
            {
                OrderId = orderId,
                RefId = refId,
                Status = "success"
            });
        }
    }
}