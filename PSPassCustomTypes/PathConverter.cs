using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PSPassCustomTypes
{
    public static class PathConverter
    {
        #region Regular Expressions

        private static Regex _trimTrailingSlashRegex = null;

        public static Regex TrimTrailingSlashRegex
        {
            get
            {
                if (PathConverter._trimTrailingSlashRegex == null)
                {
                    string p = Regex.Escape(new String(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }));
                    PathConverter._trimTrailingSlashRegex = new Regex(String.Format(@"^(?<p>.*[^{0}])\s*[{0}]+\s*$", p), RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }

                return PathConverter._trimTrailingSlashRegex;
            }
        }

        private static Regex _escapeSequenceRegex = null;

        public static Regex EscapeSequenceRegex
        {
            get
            {
                if (PathConverter._escapeSequenceRegex == null)
                    PathConverter._escapeSequenceRegex = new Regex(@"_0x(?<hex>[\da-f]{2})_", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                return PathConverter._escapeSequenceRegex;
            }
        }

        private static Regex _normalizeFromUserPathRegex = null;

        public static Regex NormalizeFromUserPathRegex
        {
            get
            {
                if (PathConverter._normalizeFromUserPathRegex == null)
                {
                    string p = Regex.Escape(new String(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }));
                    PathConverter._normalizeFromUserPathRegex = new Regex(String.Format(@"(?(?=\s*[{0}])\s*[{0}]+\s*|[{0}]+\s+)", p), RegexOptions.Compiled);
                }

                return PathConverter._normalizeFromUserPathRegex;
            }
        }

        private static Regex _normalizePathRegex = null;

        public static Regex NormalizeUserPathRegex
        {
            get
            {
                if (PathConverter._normalizePathRegex == null)
                {
                    string p = Regex.Escape(new String(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }));
                    PathConverter._normalizePathRegex = new Regex(String.Format(@"(?(?=\s*[{0}])\s*[{0}]+\s*|\s+)", p), RegexOptions.Compiled);
                }

                return PathConverter._normalizePathRegex;
            }
        }

        private static Regex _whiteSpacePathRegex = null;

        public static Regex WhiteSpacePathRegex
        {
            get
            {
                if (PathConverter._whiteSpacePathRegex == null)
                {
                    string p = Regex.Escape(new String(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }));
                    PathConverter._whiteSpacePathRegex = new Regex(String.Format(@"[{0}]\s+(?=[{0}])", p), RegexOptions.Compiled);
                }

                return PathConverter._whiteSpacePathRegex;
            }
        }

        public static Regex _directorySeparatorRegex = null;

        public static Regex _whiteSpaceRegex = null;

        public static Regex WhiteSpaceRegex
        {
            get
            {
                if (PathConverter._whiteSpaceRegex == null)
                    PathConverter._whiteSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

                return PathConverter._whiteSpaceRegex;
            }
        }

        #endregion

        private static List<char> _invalidFileNameChars = null;
        private static List<char> _invalidPathChars = null;

        #region Encoded Values

        private static string _encodedSpaceChar = null;

        public static string EncodedSpaceChar
        {
            get
            {
                if (PathConverter._encodedSpaceChar == null)
                    PathConverter._encodedSpaceChar = PathConverter.EncodeChar(' ');

                return PathConverter._encodedSpaceChar;
            }
        }

        private static string _encodedEscapeSequenceLead = null;

        public static string EncodedEscapeSequenceLead
        {
            get
            {
                if (PathConverter._encodedEscapeSequenceLead == null)
                    PathConverter._encodedEscapeSequenceLead = String.Format("_0{0}", PathConverter.EncodeChar('x'));

                return PathConverter._encodedEscapeSequenceLead;
            }
        }

        public static string EncodeEscapeSequenceMatchEvaluator(Match match)
        {
            return PathConverter.EncodedEscapeSequenceLead + match.Value.Substring(3);
        }

        #endregion

        private static string _directorySeparator = null;

        public static string DirectorySeparator
        {
            get
            {
                if (PathConverter._directorySeparator == null)
                    PathConverter._directorySeparator = new String(new char[] { Path.DirectorySeparatorChar });

                return PathConverter._directorySeparator;
            }
        }

        private static string _altDirectorySeparator = null;

        public static string AltDirectorySeparator
        {
            get
            {
                if (PathConverter._altDirectorySeparator == null)
                    PathConverter._altDirectorySeparator = new String(new char[] { Path.AltDirectorySeparatorChar });

                return PathConverter._altDirectorySeparator;
            }
        }

        public static string EncodeChar(char c)
        {
            return String.Format("_0x{0:x2}_", (int)c);
        }

        public static string TrimTrailingSlash(string text)
        {
            if (String.IsNullOrEmpty(text))
                return "";

            Match m = PathConverter.TrimTrailingSlashRegex.Match(text);

            if (m.Success)
                return m.Groups["p"].Value;

            return text;
        }

        public static string NormalizeFileName(string text)
        {
            string s;
            if (text == null || (s = text.Trim()).Length == 0)
                return "";

            return PathConverter.WhiteSpaceRegex.Replace(s, " ");
        }

        public static string NormalizeUserPath(string text)
        {
            string s;
            if (text == null || (s = text.Trim()).Length == 0)
                return PathConverter.AltDirectorySeparator;

            s = PathConverter.TrimTrailingSlash(s).Trim();

            s = PathConverter.NormalizeUserPathRegex.Replace(s, PathConverter.NormalizeUserPathMatchEvaluator);

            if (s.StartsWith(PathConverter.AltDirectorySeparator))
                return s;

            return PathConverter.AltDirectorySeparator + s;
        }

        public static string ItemNameToFileName(string name)
        {
            string n = PathConverter.NormalizeFileName(name);

            if (n == "")
                return PathConverter.EncodedSpaceChar + PSLogin.File_Extension;

            if (PathConverter._invalidFileNameChars == null)
                PathConverter._invalidFileNameChars = new List<char>(Path.GetInvalidFileNameChars());

            n = PathConverter.EscapeSequenceRegex.Replace(n, new MatchEvaluator(PathConverter.EncodeEscapeSequenceMatchEvaluator));

            List<char> result = new List<char>();
            foreach (char c in n.ToCharArray())
            {
                if (PathConverter._invalidFileNameChars.Contains(c))
                    result.AddRange(PathConverter.EncodeChar(c).ToCharArray());
                else
                    result.Add(c);
            }

            n = new String(result.ToArray());
            if (n.Length < PSLogin.File_Extension.Length || String.Compare(n.Substring(n.Length - PSLogin.File_Extension.Length), PSLogin.File_Extension, true) != 0)
                return n + PSLogin.File_Extension;
            return n;
        }

        public static string FileNameToItemName(string fileName)
        {   
            if (String.IsNullOrEmpty(fileName))
                return "";

            string f = Path.GetFileNameWithoutExtension(fileName);
            if (String.Compare(f, PathConverter.EncodedSpaceChar, true) == 0)
                return "";

            return PathConverter.EscapeSequenceRegex.Replace(f, new MatchEvaluator(PathConverter.DecodeCharSequenceMatchEvaluator));
        }

        public static string FolderPathToDirectoryName(string path)
        {
            string n = PathConverter.TrimTrailingSlash(path).Trim();

            n = PathConverter.NormalizeUserPath(n);

            if (n == "")
                return PathConverter.AltDirectorySeparator;

            if (PathConverter._invalidPathChars == null)
                PathConverter._invalidPathChars = new List<char>(Path.GetInvalidPathChars());

            n = PathConverter.EscapeSequenceRegex.Replace(n, new MatchEvaluator(PathConverter.EncodeEscapeSequenceMatchEvaluator));

            List<char> result = new List<char>();
            foreach (char c in n.ToCharArray())
            {
                if (PathConverter._invalidPathChars.Contains(c))
                    result.AddRange(PathConverter.EncodeChar(c).ToCharArray());
                else
                    result.Add(c);
            }

            if (n == PathConverter.AltDirectorySeparator)
                return n;

            n = PathConverter.WhiteSpacePathRegex.Replace((new String(result.ToArray())), PathConverter.AltDirectorySeparator + PathConverter.EncodedSpaceChar);

            n = PathConverter.NormalizeFromUserPathRegex.Replace((new String(result.ToArray())), PathConverter.AltDirectorySeparator);

            if (n.StartsWith(PathConverter.AltDirectorySeparator))
                return n;

            return PathConverter.AltDirectorySeparator + n;
        }

        public static string DirectoryNameToFolderPath(string path)
        {
            if (String.IsNullOrEmpty(path) || String.Compare(path, PathConverter.EncodedSpaceChar, true) == 0)
                return PathConverter.DirectorySeparator;

            string p = PathConverter.EscapeSequenceRegex.Replace(PathConverter.TrimTrailingSlash(path), new MatchEvaluator(PathConverter.DecodeCharSequenceMatchEvaluator));
            if (p.StartsWith(PathConverter.DirectorySeparator))
                return p;

            return PathConverter.DirectorySeparator + p;
        }

        #region Match Evaluators

        public static string NormalizeUserPathMatchEvaluator(Match match)
        {
            return (match.Value.Trim().Length == 0) ? " " : ((Char.IsWhiteSpace(match.Value[0])) ? " " : "") +
                PathConverter.AltDirectorySeparator + ((Char.IsWhiteSpace(match.Value[match.Value.Length - 1])) ? " " : "");
        }

        public static string DecodeCharSequenceMatchEvaluator(Match match)
        {
            return new String(new char[] { (char)(Convert.ToInt32(match.Groups["hex"].Value, 16)) });
        }

        #endregion
    }
}
