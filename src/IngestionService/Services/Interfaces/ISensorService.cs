using IngestionService.Entities;
using SharedContracts.DTOs;

namespace IngestionService.Services.Interfaces
{
    public interface ISensorService
    {
        Task<Sensor> RegisterSensorAsync(SensorRegisterDTO dto);

        Task BlockSensorAsync(Guid sensorId);

        Task<List<Sensor>> GetAllSensorsAsync();
    }
}