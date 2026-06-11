using IngestionService.Entities;
using SharedContracts.DTOs;

namespace IngestionService.Services.Interfaces
{
    public interface IMetricService
    {
        Task<Metric?> IngestMetricAsync(MetricSubmissionDTO dto);
    }
}
