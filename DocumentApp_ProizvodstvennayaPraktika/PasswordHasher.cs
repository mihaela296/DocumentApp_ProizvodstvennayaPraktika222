using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DocumentApp_ProizvodstvennayaPraktika
{
    internal class PasswordHasher
    {
        private const int SaltSize = 32;
        private const int HashSize = 32;
        private const int Iterations = 250_000;

        public static string CreateHash(string password, out string salt)
        {
            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            byte[] hashBytes = new Rfc2898DeriveBytes(
            password: Encoding.UTF8.GetBytes(password),
            salt: saltBytes,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA512
            ).GetBytes(HashSize);

            byte[] combinedBytes = new byte[SaltSize + HashSize];
            Array.Copy(saltBytes, 0, combinedBytes, 0, SaltSize);
            Array.Copy(hashBytes, 0, combinedBytes, SaltSize, HashSize);

            salt = Convert.ToBase64String(saltBytes);
            return Convert.ToBase64String(combinedBytes);
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);

            byte[] hashBytes = new Rfc2898DeriveBytes(
            password: Encoding.UTF8.GetBytes(password),
            salt: saltBytes,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA512
            ).GetBytes(HashSize);

            if (storedHashBytes.Length != SaltSize + HashSize)
                return false;

            for (int i = 0; i < HashSize; i++)
            {
                if (storedHashBytes[SaltSize + i] != hashBytes[i])
                    return false;
            }

            return true;
        }
    }
}
