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
            await _metricService.IngestMetricAsync(dto);
            return Ok();
        }
    }
}