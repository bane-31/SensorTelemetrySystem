using IngestionService.Entities;
using IngestionService.Persistence;
using IngestionService.Services.Interfaces;
using SharedContracts.DTOs;
using SharedContracts.Enums;

namespace IngestionService.Services
{
    public class MetricService : IMetricService
    {
        private readonly AppDbContext _context;

        public MetricService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Metric?> IngestMetricAsync(MetricSubmissionDTO dto)
        {
            var sensor = await _context.Sensors.FindAsync(dto.SensorId);
            if (sensor == null)
            {
                throw new KeyNotFoundException("Sensor not found. Device provisioning is required before sending metrics.");
            }

            if (sensor.Quality == SensorQuality.BAD)
            {
                throw new InvalidOperationException("Bad sensor is not allowed to submit metrics.");
            }
            if (sensor.IsBlocked && sensor.BlockedUntil.HasValue && sensor.BlockedUntil.Value > DateTime.UtcNow)
            {
                throw new InvalidOperationException("Sensor is temporarily blocked.");
            }
            if (sensor.IsBlocked && sensor.BlockedUntil.HasValue && sensor.BlockedUntil.Value <= DateTime.UtcNow)
            {
                sensor.IsBlocked = false;
                sensor.BlockedUntil = null;
            }

            var now = DateTime.UtcNow;

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

        private Task LogAlarmAsync(MetricSubmissionDTO dto)
        {
            return Task.Run(() =>
            {
                Console.ForegroundColor = dto.Priority switch
                {
                    1 => ConsoleColor.Yellow,
                    2 => ConsoleColor.DarkYellow,
                    3 => ConsoleColor.Red,
                    _ => ConsoleColor.White
                };

                Console.WriteLine($"[ALARM] Sensor: {dto.SensorId} | Value: {dto.Value}°C | Priority: {dto.Priority}");
                Console.ResetColor();
            });
        }
    }
}