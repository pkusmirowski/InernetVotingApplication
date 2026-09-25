namespace InternetVotingApplication.Blockchain
{
    /// <summary>
    /// Signs block hashes with a key that lives outside the database, so that a party with database access
    /// alone cannot rewrite the chain. Verification needs only the public key.
    /// </summary>
    public interface IBlockSigner
    {
        /// <summary>Short identifier of the current key (fingerprint prefix).</summary>
        string KeyId { get; }

        /// <summary>Public key in SubjectPublicKeyInfo PEM format, safe to publish.</summary>
        string PublicKeyPem { get; }

        /// <summary>Returns a base64 ECDSA signature of the UTF-8 bytes of <paramref name="data"/>.</summary>
        string Sign(string data);

        bool Verify(string data, string signatureBase64);
    }
}
