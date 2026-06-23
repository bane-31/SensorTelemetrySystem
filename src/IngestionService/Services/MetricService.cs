using IngestionService.Entities;
using IngestionService.Persistence;
using IngestionService.Services.Interfaces;
using SharedContracts.DTOs;
using SharedContracts.Enums;
using Microsoft.Extensions.Configuration;

namespace IngestionService.Services
{
    public class MetricService : IMetricService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _notificationClient = new HttpClient();
        public MetricService(AppDbContext context,IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<Metric?> IngestMetricAsync(MetricSubmissionDTO dto)
        {
            var sensor = await _context.Sensors.FindAsync(dto.SensorId);
            if (sensor == null)
            {
                throw new KeyNotFoundException("Sensor not found. Device provisioning is required before sending metrics.");
            }

            var now = DateTime.UtcNow;

            if (sensor.Quality == SensorQuality.BAD)
            {
                throw new InvalidOperationException("Bad sensor is not allowed to submit metrics.");
            }
            if (sensor.IsBlocked && sensor.BlockedUntil.HasValue && sensor.BlockedUntil.Value > now)
            {
                throw new InvalidOperationException("Sensor is temporarily blocked.");
            }
            if (sensor.IsBlocked && sensor.BlockedUntil.HasValue && sensor.BlockedUntil.Value <= now)
            {
                sensor.IsBlocked = false;
                sensor.BlockedUntil = null;
            }

            if (dto.Timestamp > now.AddSeconds(30))
            {
                throw new InvalidOperationException("Message timestamp is in the future.");
            }

            if (dto.Timestamp < now.AddMinutes(-2))
            {
                throw new InvalidOperationException("Message timestamp is too old.");
            }

            if (dto.MessageId <= sensor.LastMessageId)
            {
                throw new InvalidOperationException($"Replay attack detected or out-of-order message. Expected MessageId > {sensor.LastMessageId}, but got {dto.MessageId}.");
            }

            if (!sensor.IsActive)
            {
                sensor.LastSeen = now;
                sensor.LastMessageId = dto.MessageId;
                await _context.SaveChangesAsync();

                return null;
            }

            var metric = new Metric
            {
                SensorId = dto.SensorId,
                Value = dto.Value,
                Timestamp = dto.Timestamp,
                Priority = dto.Priority,
                IsConsensus = false
            };

            sensor.LastSeen = DateTime.UtcNow;
            sensor.LastMessageId = dto.MessageId;

            _context.Metrics.Add(metric);
            await _context.SaveChangesAsync();

            if (dto.Priority > 0)
            {
                _ = LogAlarmAsync(dto);
            }

            return metric;
        }

        private async Task LogAlarmAsync(MetricSubmissionDTO dto)
        {
            Console.ForegroundColor = dto.Priority switch
            {
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.DarkYellow,
                3 => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [ALARM DETECTED] Sensor: {dto.SensorId} | Value: {dto.Value}°C | Priority: {dto.Priority}");
            Console.ResetColor();

            try
            {
                using (var client = new HttpClient())
                {
                    string notificationBaseUrl = _configuration["NOTIFICATION_URL"] ?? "http://localhost:5112";
                    string notificationUrl = $"{notificationBaseUrl.TrimEnd('/')}/api/alarms/trigger";

                    var response = await client.PostAsJsonAsync(notificationUrl, dto);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [NETWORK ERROR] NotificationService returned status code: {response.StatusCode}");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [NETWORK CRITICAL] Failed to communicate with NotificationService: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}