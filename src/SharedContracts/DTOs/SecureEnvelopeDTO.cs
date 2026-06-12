namespace SharedContracts.DTOs
{
    public class SecureEnvelopeDTO
    {
        public Guid SensorId { get; set; }

        public string EncryptedAesKey { get; set; } = string.Empty;

        public string Nonce { get; set; } = string.Empty;

        public string CipherText { get; set; } = string.Empty;

        public string Tag { get; set; } = string.Empty;

        public string Signature { get; set; } = string.Empty;

        public string? ClientPublicKey { get; set; }
    }
}