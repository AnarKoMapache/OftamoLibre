using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace OftalmoLibre.Helpers;

public static class PasswordHelper
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static string HashPassword(string password)
    {
        var salt = CreateSalt(SaltSize);
        var hash = ManagedCrypto.Pbkdf2Sha256(password, salt, Iterations, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);
            var actualHash = ManagedCrypto.Pbkdf2Sha256(password, salt, iterations, expectedHash.Length);
            return ManagedCrypto.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] CreateSalt(int size)
    {
        if (!WineCompatibility.IsRunningUnderWine())
        {
            return RandomNumberGenerator.GetBytes(size);
        }

        // Some Wine builds crash inside BCrypt-backed .NET crypto calls, so we avoid those
        // code paths here to keep the desktop app usable under Wine.
        var salt = new byte[size];
        Random.Shared.NextBytes(salt);

        var entropy = Encoding.UTF8.GetBytes(string.Join("|",
            DateTime.UtcNow.Ticks,
            Environment.TickCount64,
            Environment.ProcessId,
            Environment.CurrentManagedThreadId,
            Environment.MachineName,
            Environment.UserName,
            AppContext.BaseDirectory));

        var digest = ManagedCrypto.Sha256(entropy);
        for (var i = 0; i < salt.Length; i++)
        {
            salt[i] ^= digest[i % digest.Length];
        }

        return salt;
    }

    private static class WineCompatibility
    {
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_version")]
        private static extern IntPtr WineGetVersion();

        private static bool? _isRunningUnderWine;

        public static bool IsRunningUnderWine()
        {
            if (_isRunningUnderWine.HasValue)
            {
                return _isRunningUnderWine.Value;
            }

            try
            {
                _ = Marshal.PtrToStringAnsi(WineGetVersion());
                _isRunningUnderWine = true;
            }
            catch
            {
                _isRunningUnderWine = false;
            }

            return _isRunningUnderWine.Value;
        }
    }

    private static class ManagedCrypto
    {
        private static readonly uint[] Sha256Constants =
        [
            0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
            0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
            0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
            0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
            0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
            0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
            0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
            0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
            0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
            0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
            0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
            0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
            0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
            0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
            0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
            0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
        ];

        public static byte[] Pbkdf2Sha256(string password, byte[] salt, int iterations, int outputLength)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(salt);

            if (iterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations));
            }

            if (outputLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outputLength));
            }

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var result = new byte[outputLength];
            var blockCount = (int)Math.Ceiling(outputLength / 32d);
            var offset = 0;

            for (var blockIndex = 1; blockIndex <= blockCount; blockIndex++)
            {
                var block = F(passwordBytes, salt, iterations, blockIndex);
                var copyLength = Math.Min(block.Length, outputLength - offset);
                Buffer.BlockCopy(block, 0, result, offset, copyLength);
                offset += copyLength;
            }

            return result;
        }

        public static byte[] Sha256(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var bitLength = (ulong)data.Length * 8UL;
            var paddingLength = 64 - (int)((data.Length + 9) % 64);
            if (paddingLength == 64)
            {
                paddingLength = 0;
            }

            var padded = new byte[data.Length + 1 + paddingLength + 8];
            Buffer.BlockCopy(data, 0, padded, 0, data.Length);
            padded[data.Length] = 0x80;

            for (var i = 0; i < 8; i++)
            {
                padded[padded.Length - 1 - i] = (byte)(bitLength >> (8 * i));
            }

            uint h0 = 0x6a09e667;
            uint h1 = 0xbb67ae85;
            uint h2 = 0x3c6ef372;
            uint h3 = 0xa54ff53a;
            uint h4 = 0x510e527f;
            uint h5 = 0x9b05688c;
            uint h6 = 0x1f83d9ab;
            uint h7 = 0x5be0cd19;

            var w = new uint[64];

            for (var chunkOffset = 0; chunkOffset < padded.Length; chunkOffset += 64)
            {
                for (var i = 0; i < 16; i++)
                {
                    var index = chunkOffset + (i * 4);
                    w[i] = ((uint)padded[index] << 24) |
                           ((uint)padded[index + 1] << 16) |
                           ((uint)padded[index + 2] << 8) |
                           padded[index + 3];
                }

                for (var i = 16; i < 64; i++)
                {
                    var s0 = RotateRight(w[i - 15], 7) ^ RotateRight(w[i - 15], 18) ^ (w[i - 15] >> 3);
                    var s1 = RotateRight(w[i - 2], 17) ^ RotateRight(w[i - 2], 19) ^ (w[i - 2] >> 10);
                    w[i] = unchecked(w[i - 16] + s0 + w[i - 7] + s1);
                }

                var a = h0;
                var b = h1;
                var c = h2;
                var d = h3;
                var e = h4;
                var f = h5;
                var g = h6;
                var h = h7;

                for (var i = 0; i < 64; i++)
                {
                    var s1 = RotateRight(e, 6) ^ RotateRight(e, 11) ^ RotateRight(e, 25);
                    var ch = (e & f) ^ (~e & g);
                    var temp1 = unchecked(h + s1 + ch + Sha256Constants[i] + w[i]);
                    var s0 = RotateRight(a, 2) ^ RotateRight(a, 13) ^ RotateRight(a, 22);
                    var maj = (a & b) ^ (a & c) ^ (b & c);
                    var temp2 = unchecked(s0 + maj);

                    h = g;
                    g = f;
                    f = e;
                    e = unchecked(d + temp1);
                    d = c;
                    c = b;
                    b = a;
                    a = unchecked(temp1 + temp2);
                }

                h0 = unchecked(h0 + a);
                h1 = unchecked(h1 + b);
                h2 = unchecked(h2 + c);
                h3 = unchecked(h3 + d);
                h4 = unchecked(h4 + e);
                h5 = unchecked(h5 + f);
                h6 = unchecked(h6 + g);
                h7 = unchecked(h7 + h);
            }

            return new byte[]
            {
                (byte)(h0 >> 24), (byte)(h0 >> 16), (byte)(h0 >> 8), (byte)h0,
                (byte)(h1 >> 24), (byte)(h1 >> 16), (byte)(h1 >> 8), (byte)h1,
                (byte)(h2 >> 24), (byte)(h2 >> 16), (byte)(h2 >> 8), (byte)h2,
                (byte)(h3 >> 24), (byte)(h3 >> 16), (byte)(h3 >> 8), (byte)h3,
                (byte)(h4 >> 24), (byte)(h4 >> 16), (byte)(h4 >> 8), (byte)h4,
                (byte)(h5 >> 24), (byte)(h5 >> 16), (byte)(h5 >> 8), (byte)h5,
                (byte)(h6 >> 24), (byte)(h6 >> 16), (byte)(h6 >> 8), (byte)h6,
                (byte)(h7 >> 24), (byte)(h7 >> 16), (byte)(h7 >> 8), (byte)h7
            };
        }

        public static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }

        private static byte[] F(byte[] passwordBytes, byte[] salt, int iterations, int blockIndex)
        {
            var blockIndexBytes = new byte[]
            {
                (byte)(blockIndex >> 24),
                (byte)(blockIndex >> 16),
                (byte)(blockIndex >> 8),
                (byte)blockIndex
            };

            var saltBlock = new byte[salt.Length + blockIndexBytes.Length];
            Buffer.BlockCopy(salt, 0, saltBlock, 0, salt.Length);
            Buffer.BlockCopy(blockIndexBytes, 0, saltBlock, salt.Length, blockIndexBytes.Length);

            var u = HmacSha256(passwordBytes, saltBlock);
            var output = new byte[u.Length];
            Buffer.BlockCopy(u, 0, output, 0, u.Length);

            for (var i = 1; i < iterations; i++)
            {
                u = HmacSha256(passwordBytes, u);
                for (var j = 0; j < output.Length; j++)
                {
                    output[j] ^= u[j];
                }
            }

            return output;
        }

        private static byte[] HmacSha256(byte[] key, byte[] message)
        {
            const int blockSize = 64;

            byte[] normalizedKey;
            if (key.Length > blockSize)
            {
                normalizedKey = Sha256(key);
            }
            else
            {
                normalizedKey = new byte[key.Length];
                Buffer.BlockCopy(key, 0, normalizedKey, 0, key.Length);
            }

            var paddedKey = new byte[blockSize];
            Buffer.BlockCopy(normalizedKey, 0, paddedKey, 0, normalizedKey.Length);

            var innerPad = new byte[blockSize];
            var outerPad = new byte[blockSize];

            for (var i = 0; i < blockSize; i++)
            {
                innerPad[i] = (byte)(paddedKey[i] ^ 0x36);
                outerPad[i] = (byte)(paddedKey[i] ^ 0x5c);
            }

            var innerMessage = new byte[blockSize + message.Length];
            Buffer.BlockCopy(innerPad, 0, innerMessage, 0, blockSize);
            Buffer.BlockCopy(message, 0, innerMessage, blockSize, message.Length);
            var innerHash = Sha256(innerMessage);

            var outerMessage = new byte[blockSize + innerHash.Length];
            Buffer.BlockCopy(outerPad, 0, outerMessage, 0, blockSize);
            Buffer.BlockCopy(innerHash, 0, outerMessage, blockSize, innerHash.Length);
            return Sha256(outerMessage);
        }

        private static uint RotateRight(uint value, int shift)
        {
            return (value >> shift) | (value << (32 - shift));
        }
    }
}
