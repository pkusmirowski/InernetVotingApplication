using System.Security.Cryptography;
using System.Text;

namespace InternetVotingApplication.Blockchain
{
    /// <summary>
    /// ECDSA P-256 / SHA-256 signer. Construct from a PKCS#8 PEM private key or a public-key-only PEM (verify only).
    /// </summary>
    public sealed class EcdsaBlockSigner : IBlockSigner, IDisposable
    {
        private readonly ECDsa _key;
        private readonly bool _canSign;

        private EcdsaBlockSigner(ECDsa key, bool canSign)
        {
            _key = key;
            _canSign = canSign;
            PublicKeyPem = key.ExportSubjectPublicKeyInfoPem();
            KeyId = ComputeKeyId(key);
        }

        public string KeyId { get; }

        public string PublicKeyPem { get; }

        public static EcdsaBlockSigner FromPrivateKeyPem(string pem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pem);
            var key = ECDsa.Create();
            key.ImportFromPem(pem);
            return new EcdsaBlockSigner(key, canSign: true);
        }

        public static EcdsaBlockSigner FromPublicKeyPem(string pem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pem);
            var key = ECDsa.Create();
            key.ImportFromPem(pem);
            return new EcdsaBlockSigner(key, canSign: false);
        }

        public static EcdsaBlockSigner Generate()
        {
            return new EcdsaBlockSigner(ECDsa.Create(ECCurve.NamedCurves.nistP256), canSign: true);
        }

        public static string GeneratePrivateKeyPem()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return key.ExportPkcs8PrivateKeyPem();
        }

        public string Sign(string data)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (!_canSign)
            {
                throw new InvalidOperationException("This signer holds only a public key.");
            }

            var signature = _key.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            return Convert.ToBase64String(signature);
        }

        public bool Verify(string data, string signatureBase64)
        {
            if (data is null || string.IsNullOrWhiteSpace(signatureBase64))
            {
                return false;
            }

            Span<byte> buffer = stackalloc byte[128];
            if (!Convert.TryFromBase64String(signatureBase64, buffer, out var written))
            {
                return false;
            }

            return _key.VerifyData(Encoding.UTF8.GetBytes(data), buffer[..written], HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }

        public static string ComputeKeyId(ECDsa key)
        {
            ArgumentNullException.ThrowIfNull(key);
            var fingerprint = SHA256.HashData(key.ExportSubjectPublicKeyInfo());
            return Convert.ToHexString(fingerprint)[..16];
        }

        public void Dispose()
        {
            _key.Dispose();
        }
    }
}
