using InternetVotingApplication.Configuration;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Blockchain
{
    /// <summary>
    /// Builds the application's <see cref="IBlockSigner"/> from configuration: inline PEM, a key file, or
    /// (development only) a freshly generated key persisted to the key file.
    /// </summary>
    public static class SigningKeyProvider
    {
        public static IBlockSigner Create(IOptions<SigningOptions> options, IHostEnvironment environment, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(environment);
            ArgumentNullException.ThrowIfNull(logger);
            var settings = options.Value;

            if (!string.IsNullOrWhiteSpace(settings.PrivateKeyPem))
            {
                var signer = EcdsaBlockSigner.FromPrivateKeyPem(settings.PrivateKeyPem);
                logger.LogInformation("Block signing key {KeyId} loaded from configuration", signer.KeyId);
                return signer;
            }

            var path = Path.IsPathRooted(settings.KeyFilePath)
                ? settings.KeyFilePath
                : Path.Combine(environment.ContentRootPath, settings.KeyFilePath);

            if (File.Exists(path))
            {
                var signer = EcdsaBlockSigner.FromPrivateKeyPem(File.ReadAllText(path));
                logger.LogInformation("Block signing key {KeyId} loaded from {Path}", signer.KeyId, path);
                return signer;
            }

            if (settings.AutoGenerateKey)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, EcdsaBlockSigner.GeneratePrivateKeyPem());
                var signer = EcdsaBlockSigner.FromPrivateKeyPem(File.ReadAllText(path));
                logger.LogWarning("Generated a new block signing key {KeyId} at {Path}. Back it up: blocks signed with it cannot be verified without it.", signer.KeyId, path);
                return signer;
            }

            throw new InvalidOperationException(
                $"No block signing key configured. Set Signing:PrivateKeyPem, place a PKCS#8 PEM at '{path}', or enable Signing:AutoGenerateKey in development.");
        }
    }
}
