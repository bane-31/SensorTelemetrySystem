using SharedContracts.Enums;

namespace SharedContracts.DTOs
{
    public class SensorRegisterDTO
    {
        public Guid Id { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public SensorQuality Quality { get; set; }
    }
}
