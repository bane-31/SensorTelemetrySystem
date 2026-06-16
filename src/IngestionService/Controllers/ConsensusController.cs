using IngestionService.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedContracts.DTOs;

namespace IngestionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsensusController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ConsensusController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetConsensusResults()
        {
            var results = await _context.ConsensusResults
                .OrderByDescending(c => c.WindowStart)
                .Take(20)
                .Select(c => new ConsensusResultDto
                {
                    WindowStart = c.WindowStart,
                    WindowEnd = c.WindowEnd,
                    Value = c.Value,
                    IncludedSensorsCount = c.IncludedSensorsCount,
                    ExcludedSensorsCount = c.ExcludedSensorsCount
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}