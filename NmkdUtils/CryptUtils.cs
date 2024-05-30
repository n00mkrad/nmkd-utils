using System.Security.Cryptography;
using System.Text;

namespace NmkdUtils
{
    public class CryptUtils
    {

        /// <summary> Calculates SHA256 hash of string <paramref name="s"/> using specified <paramref name="encoding"/> (Default: UTF-8) </summary>
        public static string GetHashSha256(string s, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8; // Default to UTF-8 if not specified
            byte[] bytes = encoding.GetBytes(s);
            return GetHashSha256(bytes);
        }

        /// <summary> Calculates SHA256 hash of <paramref name="bytes"/> </summary>
        public static string GetHashSha256(byte[] bytes)
        {
            byte[] hashBytes = SHA256.HashData(bytes);
            var hashBuilder = new StringBuilder();

            foreach (byte b in hashBytes)
            {
                hashBuilder.AppendFormat("{0:x2}", b);
            }

            return hashBuilder.ToString();
        }
    }
}
