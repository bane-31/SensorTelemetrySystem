using IngestionService.Services.Interfaces;
using IngestionService.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SharedContracts.DTOs;

namespace IngestionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("sensor_limiter")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricService _metricService;
        private readonly SecurePayloadService _securePayloadService;

        public MetricsController(IMetricService metricService, SecurePayloadService securePayloadService)
        {
            _metricService = metricService;
            _securePayloadService = securePayloadService;
        }

        [HttpPost]
        public async Task<IActionResult> IngestMetric([FromBody] MetricSubmissionDTO dto)
        {
            return await HandleMetricAsync(dto);
        }

        private async Task<IActionResult> HandleMetricAsync(MetricSubmissionDTO dto)
        {
            try
            {
                var metric = await _metricService.IngestMetricAsync(dto);

                if (metric == null)
                {
                    return Accepted(new
                    {
                        Message = "Sensor is registered, but currently inactive. LastSeen updated, metric not stored."
                    });
                }

                return Ok(new
                {
                    Message = "Metric stored successfully.",
                    MetricId = metric.Id
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "Sensor is temporarily blocked.")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Error = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "Bad sensor is not allowed to submit metrics.")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Error = ex.Message
                });
            }
        }

        [HttpPost("secure")]
        public async Task<IActionResult> IngestSecureMetric([FromBody] SecureEnvelopeDTO envelope)
        {
            var dto = await _securePayloadService.UnprotectMetricAsync(envelope);

            return await HandleMetricAsync(dto);
        }
    }
}