using System.Security.Cryptography;

namespace SharedContracts.Security
{
    public static class RsaKeyUtils
    {
        public static RSA CreateFromPrivateKeyPem(string privateKeyPem)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);
            return rsa;
        }

        public static RSA CreateFromPublicKeyPem(string publicKeyPem)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            return rsa;
        }

        public static void EnsureKeyPair(string privateKeyPath, string publicKeyPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(privateKeyPath)!);

            if (File.Exists(privateKeyPath) && File.Exists(publicKeyPath))
            {
                return;
            }

            using var rsa = RSA.Create(2048);

            var privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
            var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();

            File.WriteAllText(privateKeyPath, privateKeyPem);
            File.WriteAllText(publicKeyPath, publicKeyPem);
        }
    }
}