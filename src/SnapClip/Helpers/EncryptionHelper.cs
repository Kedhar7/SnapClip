using System.Security.Cryptography;
using System.Text;

namespace SnapClip.Helpers;

/// <summary>
/// AES-256-GCM encryption for sensitive clipboard content.
/// </summary>
public static class EncryptionHelper
{
    private const int KeySize = 32;   // 256 bits
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16;   // 128-bit authentication tag

    /// <summary>
    /// Derives a 256-bit key from a passphrase using PBKDF2.
    /// </summary>
    public static byte[] DeriveKey(string passphrase, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(passphrase),
            salt,
            iterations: 100_000,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(KeySize);
    }

    /// <summary>
    /// Generates a random salt for key derivation.
    /// </summary>
    public static byte[] GenerateSalt(int size = 16)
    {
        return RandomNumberGenerator.GetBytes(size);
    }

    /// <summary>
    /// Encrypts plaintext using AES-256-GCM.
    /// Returns: salt (16) + nonce (12) + tag (16) + ciphertext.
    /// </summary>
    public static byte[] Encrypt(string plaintext, string passphrase)
    {
        byte[] salt = GenerateSalt();
        byte[] key = DeriveKey(passphrase, salt);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Pack: salt + nonce + tag + ciphertext
        byte[] result = new byte[salt.Length + nonce.Length + tag.Length + ciphertext.Length];
        int offset = 0;

        Buffer.BlockCopy(salt, 0, result, offset, salt.Length);
        offset += salt.Length;

        Buffer.BlockCopy(nonce, 0, result, offset, nonce.Length);
        offset += nonce.Length;

        Buffer.BlockCopy(tag, 0, result, offset, tag.Length);
        offset += tag.Length;

        Buffer.BlockCopy(ciphertext, 0, result, offset, ciphertext.Length);

        return result;
    }

    /// <summary>
    /// Decrypts data produced by <see cref="Encrypt"/>.
    /// </summary>
    public static string Decrypt(byte[] encryptedData, string passphrase)
    {
        if (encryptedData.Length < 16 + NonceSize + TagSize)
            throw new ArgumentException("Encrypted data is too short.", nameof(encryptedData));

        int offset = 0;

        byte[] salt = new byte[16];
        Buffer.BlockCopy(encryptedData, offset, salt, 0, salt.Length);
        offset += salt.Length;

        byte[] nonce = new byte[NonceSize];
        Buffer.BlockCopy(encryptedData, offset, nonce, 0, nonce.Length);
        offset += nonce.Length;

        byte[] tag = new byte[TagSize];
        Buffer.BlockCopy(encryptedData, offset, tag, 0, tag.Length);
        offset += tag.Length;

        byte[] ciphertext = new byte[encryptedData.Length - offset];
        Buffer.BlockCopy(encryptedData, offset, ciphertext, 0, ciphertext.Length);

        byte[] key = DeriveKey(passphrase, salt);
        byte[] plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
