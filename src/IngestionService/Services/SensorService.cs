using IngestionService.Entities;
using IngestionService.Persistence;
using IngestionService.Services.Interfaces;
using SharedContracts.DTOs;

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

            var sensor = new Sensor
            {
                Id = dto.Id,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                Quality = dto.Quality,
                LastSeen = DateTime.UtcNow,
                LastMessageId = 0
            };

            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();

            return sensor;
        }
    }
}
