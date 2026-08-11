using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Backend.Common.Security;

public sealed class AppKeyCipher : IAppKeyCipher
{
    private readonly byte[] _masterKey;

    public AppKeyCipher(IOptions<CryptoSettings> options)
    {
        _masterKey = Convert.FromBase64String(options.Value.MasterKeyBase64);
        if (_masterKey.Length != 32)
            throw new InvalidOperationException("Master key must be 32 bytes (AES-256).");
    }
    public byte[] Encrypt(string appKeyHex)
    {
        var plainText = Encoding.UTF8.GetBytes(appKeyHex);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipherText = new byte[plainText.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_masterKey, 16);
        aes.Encrypt(nonce, plainText, cipherText, tag);

        var blob = new byte[12 + 16 + cipherText.Length];
        nonce.CopyTo(blob, 0);
        tag.CopyTo(blob, 12);
        cipherText.CopyTo(blob, 28);
        return blob;
    }
    public string Decrypt(byte[] encrypted)
    {
        var nonce = encrypted[..12];
        var tag = encrypted[12..28];
        var cipherText = encrypted[28..];
        var plainText = new byte[cipherText.Length];

        using var aes = new AesGcm(_masterKey, 16);
        aes.Decrypt(nonce, cipherText, tag, plainText);
        return Encoding.UTF8.GetString(plainText);
    }
}