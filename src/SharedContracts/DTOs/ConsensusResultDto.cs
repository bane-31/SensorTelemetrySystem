namespace SharedContracts.DTOs
{
    public class ConsensusResultDto
    {
        public DateTime WindowStart { get; set; }

        public DateTime WindowEnd { get; set; }

        public double Value { get; set; }

        public int IncludedSensorsCount { get; set; }

        public int ExcludedSensorsCount { get; set; }
    }
}