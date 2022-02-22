#nullable enable

namespace Edanoue.SaveData
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// GZip 型式でバイナリ列の圧縮/解凍を行うUtilityクラス
    /// </summary>
    internal static class Compressor
    {
        /// <summary>
        /// バイナリ列の圧縮を行う (同期処理版)
        /// </summary>
        /// <param name="input"></param>
        internal static void Compress(ref byte[] input)
        {
            using var outputStream = new MemoryStream();
            {
                using var zip = new GZipStream(outputStream, CompressionMode.Compress);
                zip.Write(input, 0, input.Length);
            }
            input = outputStream.ToArray();
        }

        internal static Task<byte[]> CompressAsync(byte[] input)
        {
            return CompressAsync(input, CancellationToken.None);
        }

        /// <summary>
        /// バイナリ列の圧縮を行う (非同期処理版)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static async Task<byte[]> CompressAsync(byte[] input, CancellationToken cancellationToken)
        {
            using var outputStream = new MemoryStream();
            {
                using var zip = new GZipStream(outputStream, CompressionMode.Compress);
                await zip.WriteAsync(input, 0, input.Length, cancellationToken);
            }
            return outputStream.ToArray();
        }

        /// <summary>
        /// バイナリ列の解凍を行う
        /// </summary>
        /// <param name="input"></param>
        internal static void Decompress(ref byte[] input)
        {
            using var outputStream = new MemoryStream();
            {
                using var inputStream = new MemoryStream(input);
                {
                    using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
                    try
                    {
                        decompressionStream.CopyTo(outputStream);
                    }
                    catch (IOException)
                    {
                        // 解凍に失敗した場合ここに落ちる
                        // TODO: 適切な処理をする
                        throw new NotImplementedException();
                    }
                }
            }
            input = outputStream.ToArray();
        }

        internal static Task<byte[]> DecompressAsync(byte[] input)
        {
            return DecompressAsync(input, CancellationToken.None);
        }

        internal static async Task<byte[]> DecompressAsync(byte[] input, CancellationToken cancellationToken)
        {
            using var outputStream = new MemoryStream();
            {
                using var inputStream = new MemoryStream(input);
                {
                    using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
                    try
                    {
                        await decompressionStream.CopyToAsync(
                            destination: outputStream,
                            bufferSize: 81920, // Stream default buffer size
                            cancellationToken: cancellationToken
                        );
                    }
                    catch (IOException)
                    {
                        // 解凍に失敗した場合ここに落ちる
                        // TODO: 適切な処理をする
                        throw new NotImplementedException();
                    }
                }
            }
            return outputStream.ToArray();
        }
    }

}
