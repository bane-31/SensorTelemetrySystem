using IngestionService.Entities;
using IngestionService.Persistence;
using IngestionService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedContracts.DTOs;
using SharedContracts.Enums;
using IngestionService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IngestionService.Services
{
    public class SensorService : ISensorService
    {
        private readonly AppDbContext _context;

        public SensorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Sensor> RegisterSensorAsync(SensorRegisterDTO dto)
        {
            var existingSensor = await _context.Sensors.FindAsync(dto.Id);
            if (existingSensor != null)
            {
                throw new InvalidOperationException("Sensor is already registered.");
            }

            var activeCount = await _context.Sensors
                .CountAsync(s =>
                    s.IsActive &&
                    !s.IsBlocked &&
                    s.Quality != SensorQuality.BAD);

            var shouldBeActive = activeCount < 5 && dto.Quality != SensorQuality.BAD;

            var sensor = new Sensor
            {
                Id = dto.Id,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                Quality = dto.Quality,
                LastSeen = DateTime.UtcNow,
                LastMessageId = 0,
                IsActive = shouldBeActive,
                IsBlocked = false,
                BlockedUntil = null,
                PublicKey = dto.PublicKey ?? string.Empty,
            };

            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();

            Console.ForegroundColor = shouldBeActive ? ConsoleColor.Green : ConsoleColor.DarkGray;
            Console.WriteLine(
                shouldBeActive
                    ? $"[SENSOR] Registered ACTIVE sensor {sensor.Id}"
                    : $"[SENSOR] Registered RESERVE sensor {sensor.Id}");
            Console.ResetColor();

            return sensor;
        }

        public async Task BlockSensorAsync(Guid sensorId)
        {
            var sensor = await _context.Sensors.FindAsync(sensorId);

            if (sensor == null)
            {
                throw new KeyNotFoundException("Sensor not found.");
            }

            sensor.IsBlocked = true;
            sensor.BlockedUntil = DateTime.UtcNow.AddSeconds(30);
            sensor.IsActive = false;

            await _context.SaveChangesAsync();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[BLOCK] Sensor {sensor.Id} manually blocked for 30s.");
            Console.ResetColor();
        }

        public async Task<List<Sensor>> GetAllSensorsAsync()
        {
            return await _context.Sensors
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.LastSeen)
                .ToListAsync();
        }
    }
}