namespace SharedContracts.DTOs
{
    public class MetricSubmissionDTO
    {
        public long MessageId { get; set; }

        public Guid SensorId { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public int Priority { get; set; }
    }
}
