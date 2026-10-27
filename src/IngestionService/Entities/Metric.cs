using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IngestionService.Entities
{
    public class Metric
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(Sensor))]
        public Guid SensorId { get; set; }
        public Sensor Sensor { get; set; }

        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public int Priority { get; set; }

        public bool IsConsensus { get; set; } = false;
    }
}