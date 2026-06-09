using IngestionService.Entities;
using SharedContracts.DTOs;

namespace IngestionService.Services.Interfaces
{
    public interface ISensorService
    {
        Task<Sensor> RegisterSensorAsync(SensorRegisterDTO dto);
    }
}
