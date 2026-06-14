using IngestionService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedContracts.DTOs;

namespace IngestionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorsController : ControllerBase
    {
        private readonly ISensorService _sensorService;

        public SensorsController(ISensorService sensorService)
        {
            _sensorService = sensorService;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterSensor([FromBody] SensorRegisterDTO dto)
        {
            var sensor = await _sensorService.RegisterSensorAsync(dto);
            return Ok(sensor);
        }

        [HttpPost("{sensorId:guid}/block")]
        public async Task<IActionResult> BlockSensor(Guid sensorId)
        {
            await _sensorService.BlockSensorAsync(sensorId);
            return Ok(new { Message = $"Sensor {sensorId} blocked for 30 seconds." });
        }
    }
}