﻿using System;
using System.Drawing;
using System.Xml.Linq;

namespace Std.Network.Xml
{
    /// <summary>
    /// XML node parsers
    /// </summary>
    public static class Parsers
    {
        #region for String

        public static bool ParseBool(this string s, bool def)
        {
            if (s == null)
            {
                return def;
            }
            return def ? s.ToLower() != "false" : s.ToLower() == "true";
        }

        public static long ParseLong(this string s)
        {
            long v;
            return long.TryParse(s, out v) ? v : 0;
        }

        public static DateTime ParseDateTime(this string s)
        {
            DateTime dt;
            return DateTime.TryParse(s, out dt) ? dt : DateTime.MinValue;
        }

        public static DateTime ParseDateTime(this string s, string format)
        {
            DateTime dt;
            return DateTime.TryParseExact(s,
                format,
                System.Globalization.DateTimeFormatInfo.InvariantInfo,
                System.Globalization.DateTimeStyles.None, out dt) ? dt : DateTime.MinValue;
        }

        public static DateTime ParseUnixTime(this string s)
        {
            if (s == null) return DateTime.MinValue;
            return UnixEpoch.GetDateTimeByUnixEpoch(s.ParseLong());
        }

        public static TimeSpan ParseUtcOffset(this string s)
        {
            int seconds;
            int.TryParse(s, out seconds);
            return new TimeSpan(0, 0, seconds);
        }

        public static Color ParseColor(this string s)
        {
            if (s == null || s.Length != 6)
            {
                return Color.Transparent;
            }

            int v, r, g, b;
            v = Convert.ToInt32(s, 16);
            r = v >> 16;
            g = (v >> 8) & 0xFF;
            b = v & 0xFF;
            return Color.FromArgb(r, g, b);
        }

        #endregion

        #region for Element

        public static string ParseString(this XElement e)
        {
            return e == null ? null : e.Value.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static bool ParseBool(this XElement e)
        {
            return ParseBool(e, false);
        }

        public static bool ParseBool(this XElement e, bool def)
        {
            return ParseBool(e == null ? null : e.Value, def);
        }

        public static long ParseLong(this XElement e)
        {
            return ParseLong(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XElement e)
        {
            return ParseDateTime(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XElement e, string format)
        {
            return ParseDateTime(e == null ? null : e.Value, format);
        }

        public static DateTime ParseUnixTime(this XElement e)
        {
            return ParseUnixTime(e == null ? null : e.Value);
        }

        public static TimeSpan ParseUtcOffset(this XElement e)
        {
            return ParseUtcOffset(e == null ? null : e.Value);
        }

        public static Color ParseColor(this XElement e)
        {
            return ParseColor(e == null ? null : e.Value);
        }

        public static Uri ParseUri(this XElement e)
        {
            var uri = e.ParseString();
            try
            {
                if (String.IsNullOrEmpty(uri))
                    return null;
                else
                    return new Uri(uri);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        #endregion

        #region for Attributes

        public static string ParseString(this XAttribute e)
        {
            return e == null ? null : e.Value.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static bool ParseBool(this XAttribute e)
        {
            return ParseBool(e, false);
        }

        public static bool ParseBool(this XAttribute e, bool def)
        {
            return ParseBool(e == null ? null : e.Value, def);
        }

        public static long ParseLong(this XAttribute e)
        {
            return ParseLong(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XAttribute e)
        {
            return ParseDateTime(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XAttribute e, string format)
        {
            return ParseDateTime(e == null ? null : e.Value, format);
        }

        public static DateTime ParseUnixTime(this XAttribute e)
        {
            return ParseUnixTime(e == null ? null : e.Value);
        }

        public static TimeSpan ParseUtcOffset(this XAttribute e)
        {
            return ParseUtcOffset(e == null ? null : e.Value);
        }

        public static Color ParseColor(this XAttribute e)
        {
            return ParseColor(e == null ? null : e.Value);
        }

        public static Uri ParseUri(this XAttribute e)
        {
            var uri = e.ParseString();
            try
            {
                if (String.IsNullOrEmpty(uri))
                    return null;
                else
                    return new Uri(uri);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        #endregion

        #region for XText

        public static string ParseString(this XText e)
        {
            return e == null ? null : e.Value.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static bool ParseBool(this XText e)
        {
            return ParseBool(e, false);
        }

        public static bool ParseBool(this XText e, bool def)
        {
            return ParseBool(e == null ? null : e.Value, def);
        }

        public static long ParseLong(this XText e)
        {
            return ParseLong(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XText e)
        {
            return ParseDateTime(e == null ? null : e.Value);
        }

        public static DateTime ParseDateTime(this XText e, string format)
        {
            return ParseDateTime(e == null ? null : e.Value, format);
        }

        public static DateTime ParseUnixTime(this XText e)
        {
            return ParseUnixTime(e == null ? null : e.Value);
        }

        public static TimeSpan ParseUtcOffset(this XText e)
        {
            return ParseUtcOffset(e == null ? null : e.Value);
        }

        public static Color ParseColor(this XText e)
        {
            return ParseColor(e == null ? null : e.Value);
        }

        public static Uri ParseUri(this XText e)
        {
            var uri = e.ParseString();
            try
            {
                if (String.IsNullOrEmpty(uri))
                    return null;
                else
                    return new Uri(uri);
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        #endregion
    }
}
