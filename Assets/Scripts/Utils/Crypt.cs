using System.Security.Cryptography;
using System.Text;
using System;

namespace Cortex
{

    /// <summary>
    /// Basic cryptography functions using the inbuilt crypto algorithms.
    /// Note: While these use inbuilt functions, the simplified usage may not hold up under serious security concerns. The user is encouraged to analyze the use case and decide on the best way to use encryption
    /// </summary>
    public static class Crypt
    {
        private static string key = "3aee65790070e46e159cfb1b5e7b33c0"; //set any string of 32 chars
        private static string iv = "b87623a414de8fb8"; //set any string of 16 chars

        /// <summary>
        /// Encrypt a string using AES. To decode the result, see <seealso cref="AesDecrypt"/>
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The encrypted input string</returns>
        public static string AesEncrypt(string input)
        {
            AesCryptoServiceProvider AEScryptoProvider = new()
            {
                BlockSize = 128,
                KeySize = 256,
                Key = Encoding.UTF8.GetBytes(key),
                IV = Encoding.UTF8.GetBytes(iv),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };

            byte[] txtByteData = Encoding.UTF8.GetBytes(input);
            ICryptoTransform trnsfrm = AEScryptoProvider.CreateEncryptor(AEScryptoProvider.Key, AEScryptoProvider.IV);

            byte[] result = trnsfrm.TransformFinalBlock(txtByteData, 0, txtByteData.Length);
            return Convert.ToBase64String(result);
        }
        /// <summary>
        /// Decrypt a string using AES. To encode the result, see <seealso cref="AesEncrypt"/>
        /// </summary>
        /// <param name="input">The encrypted input string</param>
        /// <returns>The decrypted input string</returns>
        public static string AesDecrypt(string input)
        {
            AesCryptoServiceProvider AEScryptoProvider = new()
            {
                BlockSize = 128,
                KeySize = 256,
                Key = Encoding.UTF8.GetBytes(key),
                IV = Encoding.UTF8.GetBytes(iv),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };

            byte[] txtByteData = Convert.FromBase64String(input);
            ICryptoTransform trnsfrm = AEScryptoProvider.CreateDecryptor();

            byte[] result = trnsfrm.TransformFinalBlock(txtByteData, 0, txtByteData.Length);
            return Encoding.UTF8.GetString(result);
        }

        /// <summary>
        /// Create an MD5 hash of the given string
        /// </summary>
        /// <param name="data">The input</param>
        /// <returns>The hashed input</returns>
        public static string CreateMD5Hash(string data)
        {
            return CreateMD5Hash(Encoding.UTF8.GetBytes(data));
        }
        /// <summary>
        /// Create an MD5 hash of the given data
        /// </summary>
        /// <param name="data">The input</param>
        /// <returns>The hashed input</returns>
        public static string CreateMD5Hash(byte[] data)
        {
            MD5CryptoServiceProvider oMD5Hasher =
           new();

            var hashData = oMD5Hasher.ComputeHash(data);
            var hashString = BitConverter.ToString(hashData);
            hashString = hashString.Replace("-", "");

            return hashString;
        }
    }

} // End namespace cortex