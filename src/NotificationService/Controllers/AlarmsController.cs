using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using SharedContracts.DTOs;
using System;
using System.Threading.Tasks;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlarmsController : ControllerBase
    {
        private readonly IHubContext<AlarmHub> _hubContext;

        public AlarmsController(IHubContext<AlarmHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerAlarm([FromBody] MetricSubmissionDTO alarmDto)
        {
            if (alarmDto == null)
            {
                return BadRequest("Alarm data cannot be null.");
            }

            await _hubContext.Clients.All.SendAsync("ReceiveAlarm", alarmDto);

            Console.ForegroundColor = alarmDto.Priority switch
            {
                3 => ConsoleColor.Red,
                2 => ConsoleColor.DarkYellow,
                1 => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [NOTIFICATION PUSH] Sensor: {alarmDto.SensorId} | Value: {alarmDto.Value}°C | Priority: {alarmDto.Priority}");
            Console.ResetColor();

            return Ok(new { Message = "Alarm successfully broadcasted to all operators via SignalR." });
        }
    }
}