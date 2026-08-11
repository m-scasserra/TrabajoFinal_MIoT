namespace Backend.Common.Security;

public interface IAppKeyCipher
{
    byte[] Encrypt(string appKeyHex);
    string Decrypt(byte[] encrypted);
}