using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Std.Network
{
    /// <summary>
    /// Additional HTTP Utility
    /// </summary>
    /// <remarks>
    /// coded by KazuV3. (NYSL License)<para />
    /// http://d.hatena.ne.jp/kazuv3/20080605/1212656674
    /// </remarks>
    public static class HttpUtility
    {
        /// <summary>
        /// Encode string with URL encoding method by UTF8
        /// </summary>
        /// <remarks>
        /// This method will replace space to ' '.
        /// </remarks>
        /// <param name="s">string for encode</param>
        /// <returns>encoded string</returns>
        public static string UrlEncode(string s)
        {
            return UrlEncode(s, Encoding.UTF8);
        }
        
        /// <summary>
        /// Encode string with URL encoding method
        /// </summary>
        /// <remarks>
        /// This method will replace space to ' '.
        /// </remarks>
        /// <param name="s">string for encode</param>
        /// <param name="enc">using encoding</param>
        /// <returns>encoded string</returns>
        public static string UrlEncode(string s, Encoding enc)
        {
            StringBuilder rt = new StringBuilder();
            foreach (byte i in enc.GetBytes(s))
                if (i == 0x20)
                    rt.Append('+');
                else if (i >= 0x30 && i <= 0x39 || i >= 0x41 && i <= 0x5a || i >= 0x61 && i <= 0x7a)
                    rt.Append((char)i);
                else
                    rt.Append("%" + i.ToString("X2"));
            return rt.ToString();
        }

        /// <summary>
        /// Decode string with URL decoding method by UTF8
        /// </summary>
        /// <param name="s">string for decode</param>
        /// <returns>decoded string</returns>
        public static string UrlDecode(string s)
        {
            return UrlDecode(s, Encoding.UTF8);
        }

        /// <summary>
        /// Decode string with URL decoding method
        /// </summary>
        /// <param name="s">string for decode</param>
        /// <param name="enc">using encoding</param>
        /// <returns>decoded string</returns>
        public static string UrlDecode(string s, Encoding enc)
        {
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '%')
                    bytes.Add((byte)int.Parse(s[++i].ToString() + s[++i].ToString(), NumberStyles.HexNumber));
                else if (c == '+')
                    bytes.Add((byte)0x20);
                else
                    bytes.Add((byte)c);
            }
            return enc.GetString(bytes.ToArray(), 0, bytes.Count);
        }

    }
}
