using SharedContracts;
using SharedContracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace IngestionService.Entities
{
    public class Sensor
    {
        [Key]
        public Guid Id { get; set; }

        public double MinValue { get; set; }
        public double MaxValue { get; set; }

        public SensorQuality Quality { get; set; }

        public DateTime LastSeen { get; set; }

        public long LastMessageId { get; set; }

        public ICollection<Metric> Metrics { get; set; } = new List<Metric>();
   
        public bool IsBlocked { get; set; }

        public DateTime BlockedUntil { get; set; }
    }
}