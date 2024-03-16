using System;
using System.Text;

namespace P2Dev.ApiClient
{
    public static class Base64Url
    {
        public static string Encode(byte[] arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            var s = Convert.ToBase64String(arg);
            return s
                .Replace("=", "")
                .Replace("/", "_")
                .Replace("+", "-");
        }

        public static string ToBase64(string arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            var s = arg
                    .PadRight(arg.Length + (4 - arg.Length % 4) % 4, '=')
                    .Replace("_", "/")
                    .Replace("-", "+");

            return s;
        }

        public static string Decode(string arg)
        {
            var decrypted = ToBase64(arg);

            return Encoding.UTF8.GetString(Convert.FromBase64String(decrypted));
        }
    }
}

