using IngestionService.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedContracts.DTOs;
using SharedContracts.Security;

namespace IngestionService.Services.Security
{
    public class SecurePayloadService
    {
        private readonly AppDbContext _context;
        private readonly string _serverPrivateKeyPath;
        private readonly string _serverPublicKeyPath;

        public SecurePayloadService(AppDbContext context)
        {
            _context = context;

            _serverPrivateKeyPath = Path.GetFullPath(Path.Combine("keys", "server_private.pem"));
            _serverPublicKeyPath = Path.GetFullPath(Path.Combine("keys", "server_public.pem"));

            RsaKeyUtils.EnsureKeyPair(_serverPrivateKeyPath, _serverPublicKeyPath);
        }

        public SensorRegisterDTO UnprotectRegistration(SecureEnvelopeDTO envelope)
        {
            if (string.IsNullOrWhiteSpace(envelope.ClientPublicKey))
            {
                throw new InvalidOperationException("Client public key is required for secure registration.");
            }

            using var serverPrivateKey = RsaKeyUtils.CreateFromPrivateKeyPem(
                File.ReadAllText(_serverPrivateKeyPath));

            using var sensorPublicKey = RsaKeyUtils.CreateFromPublicKeyPem(
                envelope.ClientPublicKey);

            var dto = CryptoEnvelopeService.Unprotect<SensorRegisterDTO>(
                envelope,
                serverPrivateKey,
                sensorPublicKey);

            if (dto.Id != envelope.SensorId)
            {
                throw new InvalidOperationException("SensorId mismatch in secure registration.");
            }

            if (dto.PublicKey != envelope.ClientPublicKey)
            {
                throw new InvalidOperationException("Public key mismatch in secure registration.");
            }

            return dto;
        }

        public async Task<MetricSubmissionDTO> UnprotectMetricAsync(SecureEnvelopeDTO envelope)
        {
            var sensor = await _context.Sensors
                .FirstOrDefaultAsync(s => s.Id == envelope.SensorId);

            if (sensor == null)
            {
                throw new KeyNotFoundException("Sensor not found. Device provisioning is required before sending metrics.");
            }

            if (string.IsNullOrWhiteSpace(sensor.PublicKey))
            {
                throw new InvalidOperationException("Sensor public key is missing.");
            }

            using var serverPrivateKey = RsaKeyUtils.CreateFromPrivateKeyPem(
                File.ReadAllText(_serverPrivateKeyPath));

            using var sensorPublicKey = RsaKeyUtils.CreateFromPublicKeyPem(sensor.PublicKey);

            var dto = CryptoEnvelopeService.Unprotect<MetricSubmissionDTO>(
                envelope,
                serverPrivateKey,
                sensorPublicKey);

            if (dto.SensorId != envelope.SensorId)
            {
                throw new InvalidOperationException("SensorId mismatch in secure metric payload.");
            }

            return dto;
        }
    }
}