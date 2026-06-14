using IngestionService.Services.Interfaces;
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

        public MetricsController(IMetricService metricService)
        {
            _metricService = metricService;
        }

        [HttpPost]
        public async Task<IActionResult> IngestMetric([FromBody] MetricSubmissionDTO dto)
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("temporarily blocked"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Error = ex.Message
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Bad sensor"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Error = ex.Message
                });
            }
        }
    }
}