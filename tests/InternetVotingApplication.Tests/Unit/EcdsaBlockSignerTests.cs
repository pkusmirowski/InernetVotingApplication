using InternetVotingApplication.Blockchain;

namespace InternetVotingApplication.Tests.Unit
{
    public class EcdsaBlockSignerTests
    {
        [Fact]
        public void Signature_round_trips_and_rejects_changes()
        {
            using var signer = EcdsaBlockSigner.Generate();
            var signature = signer.Sign("ABC");

            Assert.True(signer.Verify("ABC", signature));
            Assert.False(signer.Verify("ABD", signature));
            Assert.False(signer.Verify("ABC", "not-base64!"));
            Assert.False(signer.Verify("ABC", ""));
        }

        [Fact]
        public void Public_key_only_signer_verifies_but_cannot_sign()
        {
            using var signer = EcdsaBlockSigner.Generate();
            var signature = signer.Sign("data");

            using var verifier = EcdsaBlockSigner.FromPublicKeyPem(signer.PublicKeyPem);
            Assert.Equal(signer.KeyId, verifier.KeyId);
            Assert.True(verifier.Verify("data", signature));
            Assert.Throws<InvalidOperationException>(() => verifier.Sign("data"));
        }

        [Fact]
        public void Private_key_pem_round_trips()
        {
            var pem = EcdsaBlockSigner.GeneratePrivateKeyPem();
            Assert.Contains("BEGIN PRIVATE KEY", pem, StringComparison.Ordinal);

            using var first = EcdsaBlockSigner.FromPrivateKeyPem(pem);
            using var second = EcdsaBlockSigner.FromPrivateKeyPem(pem);
            Assert.Equal(first.KeyId, second.KeyId);
            Assert.True(second.Verify("x", first.Sign("x")));
            Assert.Equal(16, first.KeyId.Length);
        }
    }
}
