using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SharedContracts.DTOs;

namespace SharedContracts.Security
{
    public static class CryptoEnvelopeService
    {
        private const int AesKeySizeBytes = 32;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static SecureEnvelopeDTO Protect<T>(
            T payload,
            Guid sensorId,
            RSA senderPrivateKey,
            RSA serverPublicKey,
            string? clientPublicKey = null)
        {
            var plainJson = JsonSerializer.Serialize(payload, JsonOptions);
            var plainBytes = Encoding.UTF8.GetBytes(plainJson);

            var signatureBytes = senderPrivateKey.SignData(
                plainBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var aesKey = RandomNumberGenerator.GetBytes(AesKeySizeBytes);
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            var cipherText = new byte[plainBytes.Length];
            var tag = new byte[TagSizeBytes];

            using var aes = new AesGcm(aesKey, TagSizeBytes);
            aes.Encrypt(nonce, plainBytes, cipherText, tag);

            var encryptedAesKey = serverPublicKey.Encrypt(
                aesKey,
                RSAEncryptionPadding.OaepSHA256);

            return new SecureEnvelopeDTO
            {
                SensorId = sensorId,
                EncryptedAesKey = Convert.ToBase64String(encryptedAesKey),
                Nonce = Convert.ToBase64String(nonce),
                CipherText = Convert.ToBase64String(cipherText),
                Tag = Convert.ToBase64String(tag),
                Signature = Convert.ToBase64String(signatureBytes),
                ClientPublicKey = clientPublicKey
            };
        }

        public static T Unprotect<T>(
            SecureEnvelopeDTO envelope,
            RSA serverPrivateKey,
            RSA senderPublicKey)
        {
            var encryptedAesKey = Convert.FromBase64String(envelope.EncryptedAesKey);
            var nonce = Convert.FromBase64String(envelope.Nonce);
            var cipherText = Convert.FromBase64String(envelope.CipherText);
            var tag = Convert.FromBase64String(envelope.Tag);
            var signature = Convert.FromBase64String(envelope.Signature);

            var aesKey = serverPrivateKey.Decrypt(
                encryptedAesKey,
                RSAEncryptionPadding.OaepSHA256);

            var plainBytes = new byte[cipherText.Length];

            using var aes = new AesGcm(aesKey, TagSizeBytes);
            aes.Decrypt(nonce, cipherText, tag, plainBytes);

            var validSignature = senderPublicKey.VerifyData(
                plainBytes,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (!validSignature)
            {
                throw new InvalidOperationException("Invalid digital signature.");
            }

            var json = Encoding.UTF8.GetString(plainBytes);

            var payload = JsonSerializer.Deserialize<T>(json, JsonOptions);

            if (payload == null)
            {
                throw new InvalidOperationException("Failed to deserialize secure payload.");
            }

            return payload;
        }
    }
}