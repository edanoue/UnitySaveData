#nullable enable
namespace Edanoue.SaveData
{
    using System.IO;
    using System.Text;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    public class Cryptor
    {
        public static void Encrypt(ref byte[] inout)
        {
            Encrypt(ref inout, in _EncryptionKey, in _EncryptionIV);
        }

        public static void Encrypt(ref byte[] inout, in string key, in string iv)
        {
            using var aes = new AesManaged();
            SetAesParams(aes, key, iv);
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            {
                using var encryptedStream = new MemoryStream();
                {
                    using var cryptStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write);
                    cryptStream.Write(inout, 0, inout.Length);
                }
                inout = encryptedStream.ToArray();
            }
        }

        #region EncryptAsync

        public static Task<byte[]> EncryptAsync(byte[] input)
        {
            return EncryptAsync(input, _EncryptionKey, _EncryptionIV);
        }

        public static Task<byte[]> EncryptAsync(byte[] input, CancellationToken cancellationToken)
        {
            return EncryptAsync(input, _EncryptionKey, _EncryptionIV, cancellationToken);
        }

        public static Task<byte[]> EncryptAsync(byte[] input, string key, string iv)
        {
            return EncryptAsync(input, key, iv, CancellationToken.None);
        }

        public static async Task<byte[]> EncryptAsync(byte[] input, string key, string iv, CancellationToken cancellationToken)
        {
            using var aes = new AesManaged();
            SetAesParams(aes, key, iv);
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            {
                using var encryptedStream = new MemoryStream();
                {
                    using var cryptStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write);
                    await cryptStream.WriteAsync(input, 0, input.Length, cancellationToken);
                }
                return encryptedStream.ToArray();
            }
        }

        #endregion // EncryptAsync

        public static void Decrypt(ref byte[] inout)
        {
            Decrypt(ref inout, in _EncryptionKey, in _EncryptionIV);
        }

        public static void Decrypt(ref byte[] inout, in string key, in string iv)
        {
            using var aes = new AesManaged();
            SetAesParams(aes, key, iv);
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            {
                using var decryptedStream = new MemoryStream();
                {
                    using var encryptedStream = new MemoryStream(inout);
                    {
                        using var cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read);
                        cryptoStream.CopyTo(decryptedStream);
                    }
                }
                inout = decryptedStream.ToArray();
            }
        }

        #region DecryptAsync

        public static Task<byte[]> DecryptAsync(byte[] input)
        {
            return DecryptAsync(input, _EncryptionKey, _EncryptionIV);
        }

        public static Task<byte[]> DecryptAsync(byte[] input, CancellationToken cancellationToken)
        {
            return DecryptAsync(input, _EncryptionKey, _EncryptionIV, cancellationToken);
        }

        public static Task<byte[]> DecryptAsync(byte[] input, string key, string iv)
        {
            return DecryptAsync(input, key, iv, CancellationToken.None);
        }

        public static async Task<byte[]> DecryptAsync(byte[] input, string key, string iv, CancellationToken cancellationToken)
        {
            using var aes = new AesManaged();
            SetAesParams(aes, key, iv);
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            {
                using var decryptedStream = new MemoryStream();
                {
                    using var encryptedStream = new MemoryStream(input);
                    {
                        using var cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read);
                        await cryptoStream.CopyToAsync(
                            destination: decryptedStream,
                            bufferSize: 81920, // Stream default buffer size
                            cancellationToken: cancellationToken
                        );
                    }
                }
                return decryptedStream.ToArray();
            }
        }

        #endregion // DecryptAsync

        #region 内部処理用

        static readonly int _KeySize = 256;
        static readonly int _BlockSize = 128;

        // TODO: 後で CI/CD で設定された環境変数から読み込むようにする
        static readonly string _EncryptionKey = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        // TODO: 後で CI/CD で設定された環境変数から読み込むようにする
        static readonly string _EncryptionIV = "0123456789ABCDEF";

        static void SetAesParams(AesManaged aes, string key, string iv)
        {
            aes.KeySize = _KeySize;
            aes.BlockSize = _BlockSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            aes.Key = Encoding.UTF8.GetBytes(CreateKeyFromString(key));
            aes.IV = Encoding.UTF8.GetBytes(CreateIVFromString(iv));
        }

        static string CreateKeyFromString(string str)
        {
            return PaddingString(str, _KeySize / 8);
        }

        static string CreateIVFromString(string str)
        {
            return PaddingString(str, _BlockSize / 8);
        }

        static string PaddingString(string str, int len)
        {
            const char PaddingCharacter = '.';

            if (str.Length < len)
            {
                string key = str;
                for (int i = 0; i < len - str.Length; ++i)
                {
                    key += PaddingCharacter;
                }
                return key;
            }
            else if (str.Length > len)
            {
                return str.Substring(0, len);
            }
            else
            {
                return str;
            }
        }

        #endregion
    }
}
