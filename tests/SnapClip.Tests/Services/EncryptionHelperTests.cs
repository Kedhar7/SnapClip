using FluentAssertions;
using SnapClip.Helpers;
using Xunit;

namespace SnapClip.Tests.Services;

public sealed class EncryptionHelperTests
{
    private const string TestPassphrase = "TestP@ssw0rd!2024";

    [Fact]
    public void EncryptDecrypt_RoundTrip_PreservesData()
    {
        const string original = "This is sensitive clipboard content with special chars: @#$%^&*()";

        byte[] encrypted = EncryptionHelper.Encrypt(original, TestPassphrase);
        string decrypted = EncryptionHelper.Decrypt(encrypted, TestPassphrase);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void EncryptDecrypt_EmptyString_RoundTrip()
    {
        byte[] encrypted = EncryptionHelper.Encrypt("", TestPassphrase);
        string decrypted = EncryptionHelper.Decrypt(encrypted, TestPassphrase);

        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void EncryptDecrypt_UnicodeContent_Preserved()
    {
        const string original = "Unicode test: \u00e9\u00e0\u00fc\u00f1 \u4f60\u597d \ud83d\ude00";

        byte[] encrypted = EncryptionHelper.Encrypt(original, TestPassphrase);
        string decrypted = EncryptionHelper.Decrypt(encrypted, TestPassphrase);

        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_DifferentInputs_ProduceDifferentOutputs()
    {
        byte[] encrypted1 = EncryptionHelper.Encrypt("Content A", TestPassphrase);
        byte[] encrypted2 = EncryptionHelper.Encrypt("Content B", TestPassphrase);

        encrypted1.Should().NotBeEquivalentTo(encrypted2);
    }

    [Fact]
    public void Encrypt_SameInput_ProducesDifferentCiphertext()
    {
        // Due to random salt and nonce, same input should produce different ciphertext
        byte[] encrypted1 = EncryptionHelper.Encrypt("Same content", TestPassphrase);
        byte[] encrypted2 = EncryptionHelper.Encrypt("Same content", TestPassphrase);

        encrypted1.Should().NotBeEquivalentTo(encrypted2);
    }

    [Fact]
    public void Decrypt_WrongKey_ThrowsException()
    {
        byte[] encrypted = EncryptionHelper.Encrypt("Secret", TestPassphrase);

        Action act = () => EncryptionHelper.Decrypt(encrypted, "WrongPassphrase");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Decrypt_CorruptedData_ThrowsException()
    {
        byte[] encrypted = EncryptionHelper.Encrypt("Original", TestPassphrase);

        // Corrupt the ciphertext portion
        if (encrypted.Length > 50)
            encrypted[50] ^= 0xFF;

        Action act = () => EncryptionHelper.Decrypt(encrypted, TestPassphrase);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Decrypt_TooShortData_ThrowsArgumentException()
    {
        byte[] tooShort = new byte[10];

        Action act = () => EncryptionHelper.Decrypt(tooShort, TestPassphrase);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeriveKey_ProducesConsistentOutput()
    {
        byte[] salt = EncryptionHelper.GenerateSalt();
        byte[] key1 = EncryptionHelper.DeriveKey(TestPassphrase, salt);
        byte[] key2 = EncryptionHelper.DeriveKey(TestPassphrase, salt);

        key1.Should().BeEquivalentTo(key2);
    }

    [Fact]
    public void DeriveKey_DifferentSalts_ProduceDifferentKeys()
    {
        byte[] salt1 = EncryptionHelper.GenerateSalt();
        byte[] salt2 = EncryptionHelper.GenerateSalt();

        byte[] key1 = EncryptionHelper.DeriveKey(TestPassphrase, salt1);
        byte[] key2 = EncryptionHelper.DeriveKey(TestPassphrase, salt2);

        key1.Should().NotBeEquivalentTo(key2);
    }

    [Fact]
    public void GenerateSalt_ProducesUniqueValues()
    {
        byte[] salt1 = EncryptionHelper.GenerateSalt();
        byte[] salt2 = EncryptionHelper.GenerateSalt();

        salt1.Should().NotBeEquivalentTo(salt2);
    }
}
