namespace InternetVotingApplication.Configuration
{
    /// <summary>
    /// Block-signing key settings bound from the <c>Signing</c> section. The private key must live
    /// outside the database (user secrets, environment variable, Key Vault or a protected file).
    /// </summary>
    public class SigningOptions
    {
        public const string SectionName = "Signing";

        /// <summary>ECDSA P-256 private key in PKCS#8 PEM format. Takes precedence over <see cref="KeyFilePath"/>.</summary>
        public string? PrivateKeyPem { get; set; }

        /// <summary>Path of a PEM file with the private key. Relative paths resolve against the content root.</summary>
        public string KeyFilePath { get; set; } = "App_Data/signing-key.pem";

        /// <summary>Generate the key file when it does not exist (development only).</summary>
        public bool AutoGenerateKey { get; set; }
    }
}
