using System.ComponentModel.DataAnnotations;

namespace IngestionService.Entities
{
    public class ConsensusResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime WindowStart { get; set; }

        public DateTime WindowEnd { get; set; }

        public double Value { get; set; }

        public int IncludedSensorsCount { get; set; }

        public int ExcludedSensorsCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}