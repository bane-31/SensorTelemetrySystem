using IngestionService.Services.Interfaces;
using IngestionService.Services.Security;
using Microsoft.AspNetCore.Mvc;
using SharedContracts.DTOs;

namespace IngestionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorsController : ControllerBase
    {
        private readonly ISensorService _sensorService;
        private readonly SecurePayloadService _securePayloadService;

        public SensorsController(ISensorService sensorService, SecurePayloadService securePayloadService)
        {
            _sensorService = sensorService;
            _securePayloadService = securePayloadService;
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

        [HttpPost("secure")]
        public async Task<IActionResult> RegisterSecureSensor([FromBody] SecureEnvelopeDTO envelope)
        {
            var dto = _securePayloadService.UnprotectRegistration(envelope);

            await _sensorService.RegisterSensorAsync(dto);

            return Ok(new
            {
                Message = "Secure sensor registration completed."
            });
        }
    }
}