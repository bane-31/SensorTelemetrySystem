using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace NotificationService.Hubs
{
    public class AlarmHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DASHBOARD] New operator dashboard connected: {Context.ConnectionId}");
            Console.ResetColor();
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DASHBOARD] Dashboard {Context.ConnectionId} disconnected from network.");
            Console.ResetColor();
            return base.OnDisconnectedAsync(exception);
        }
    }
}