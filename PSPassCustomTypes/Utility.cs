using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace PSPassCustomTypes
{
    public static class Utility
    {
        public const string PSDrive_Name = "PSPass";
        public const string AppStorage_XmlNamespace = "urn:Erwine.Leonard.T:PSPass.PowerShell:AppStorage";
        public const string FolderName_Company = "Leonard T. Erwine";
        public const string FolderName_Module = "PSPass";

        private static string _directorySeparator = null;

        public static string DirectorySeparator
        {
            get
            {
                if (Utility._directorySeparator == null)
                    Utility._directorySeparator = new String(new char[] { Path.DirectorySeparatorChar });

                return Utility._directorySeparator;
            }
        }

        private static char[] EnsureChar(char[] source, char target, params char[] additionalTargets)
        {
            List<char> result = new List<char>(source);
            char[] targets;
            if (additionalTargets == null || additionalTargets.Length == 0)
                targets = new char[] { target };
            else
            {
                targets = new char[additionalTargets.Length + 1];
                additionalTargets.CopyTo(targets, 0);
                targets[additionalTargets.Length] = target;
            }
            bool[] exists = new bool[targets.Length];

            for (int i = 0; i < exists.Length; i++)
                exists[i] = false;
            foreach (char c in source)
            {
                bool finished = true;

                for (int i = 0; i < exists.Length; i++)
                {
                    if (!exists[i])
                    {
                        if (c == targets[i])
                            exists[i] = true;
                        else
                            finished = false;
                    }
                }

                if (finished)
                    break;
            }

            for (int i = 0; i < exists.Length; i++)
            {
                if (!exists[i])
                    result.Add(targets[i]);
            }

            return result.ToArray();
        }

        private static char[] _invalidFileNameCharsExtensionsAllowed = null;

        public static char[] InvalidFileNameCharsExtensionsAllowed
        {
            get
            {
                if (Utility._invalidFileNameCharsExtensionsAllowed == null)
                    Utility._invalidFileNameCharsExtensionsAllowed = Utility.EnsureChar(Path.GetInvalidFileNameChars(), '_');

                return Utility._invalidFileNameCharsExtensionsAllowed;
            }
        }

        private static char[] _invalidFileNameCharsNoExtensions = null;

        public static char[] InvalidFileNameCharsNoExtensions
        {
            get
            {
                if (Utility._invalidFileNameCharsNoExtensions == null)
                    Utility._invalidFileNameCharsNoExtensions = Utility.EnsureChar(Utility.InvalidFileNameCharsExtensionsAllowed, '.');

                return Utility._invalidFileNameCharsNoExtensions;
            }
        }

        private static char[] _invalidPathCharsExtensionsAllowed = null;

        public static char[] InvalidPathCharsExtensionsAllowed
        {
            get
            {
                if (Utility._invalidPathCharsExtensionsAllowed == null)
                    Utility._invalidPathCharsExtensionsAllowed = Utility.EnsureChar(Path.GetInvalidPathChars(), '_', ':');

                return Utility._invalidPathCharsExtensionsAllowed;
            }
        }

        private static char[] _invalidPathCharsNoExtensions = null;

        public static char[] InvalidPathCharsNoExtensions
        {
            get
            {
                if (Utility._invalidPathCharsNoExtensions == null)
                    Utility._invalidPathCharsNoExtensions = Utility.EnsureChar(Utility.InvalidPathCharsExtensionsAllowed, '.');

                return Utility._invalidPathCharsNoExtensions;
            }
        }

        private static Regex _fromSafeRegex = null;

        public static Regex FromSafeRegex
        {
            get
            {
                if (Utility._fromSafeRegex == null)
                    Utility._fromSafeRegex = new Regex(@"_0x(?<hex>[\da-f]{2})_", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                return Utility._fromSafeRegex;
            }
        }

        private static string _roamingAppDataPath = null;

        public static string RoamingAppDataPath
        {
            get
            {
                if (Utility._roamingAppDataPath == null)
                    Utility._roamingAppDataPath = Utility.GetAppDataPath(false);

                return Utility._roamingAppDataPath;
            }
        }

        private static string _localAppDataPath = null;

        public static string LocalAppDataPath
        {
            get
            {
                if (Utility._localAppDataPath == null)
                    Utility._localAppDataPath = Utility.GetAppDataPath(true);

                return Utility._localAppDataPath;
            }
        }

        public static string NormalizeWhitespace(string path)
        {
            string s;
            if (path == null || (s = path.Trim()).Length == 0)
                return "";

            List<char> result = new List<char>();
            bool lastCharIsWhiteSpace = false;
            foreach (char c in s.ToCharArray())
            {
                if (Char.IsWhiteSpace(c))
                {
                    lastCharIsWhiteSpace = true;
                    continue;
                }

                if (lastCharIsWhiteSpace)
                {
                    result.Add(' ');
                    lastCharIsWhiteSpace = false;
                }

                result.Add(c);
            }

            return new String(result.ToArray());
        }

        public static string NormalizePathSeparators(string path)
        {
            string s;
            if (path == null || (s = path.Trim()).Length == 0)
                return "";

            List<char> result = new List<char>();
            bool previousIsSeparator = false;
            bool lastCharIsWhiteSpace = false;
            foreach (char c in path)
            {
                if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
                {
                    previousIsSeparator = true;
                    lastCharIsWhiteSpace = true;
                    continue;
                }

                if (Char.IsWhiteSpace(c))
                {
                    lastCharIsWhiteSpace = true;
                    continue;
                }

                if (previousIsSeparator)
                {
                    previousIsSeparator = false;
                    lastCharIsWhiteSpace = false;
                    result.Add(Path.DirectorySeparatorChar);
                }
                else if (lastCharIsWhiteSpace)
                {
                    lastCharIsWhiteSpace = false;
                    result.Add(' ');
                }

                result.Add(c);
            }

            if (previousIsSeparator && result.Count == 0)
                return Utility.DirectorySeparator;

            return new String(result.ToArray());
        }

        private static string GetAppDataPath(bool isLocal)
        {
            return Path.Combine(
                Path.Combine(
                    Path.Combine(
                        Environment.GetFolderPath((isLocal) ? Environment.SpecialFolder.ApplicationData : Environment.SpecialFolder.LocalApplicationData),
                        Utility.FolderName_Company
                    ),
                    "PowerShell"
                ),
                Utility.FolderName_Module
            );
        }

        public static string ToSafeFilesystemName(string text, bool allowExtension, bool isPath)
        {
            string[] result = Utility.ToSafeFilesystemNames(new string[] { text }, allowExtension, isPath);
            return (result.Length > 0) ? result[0] : null;
        }

        public static string[] ToSafeFilesystemNames(string[] text, bool allowExtension, bool isPath)
        {
            if (text == null || text.Length == 0)
                return new string[0];

            List<string> output = new List<string>();
            List<char> invalidChars = new List<char>((isPath) ?
                ((allowExtension) ? Utility.InvalidPathCharsExtensionsAllowed : Utility.InvalidPathCharsNoExtensions) :
                ((allowExtension) ? Utility.InvalidFileNameCharsExtensionsAllowed : Utility.InvalidFileNameCharsNoExtensions));

            foreach (string s in text)
            {
                if (s == null)
                    continue;

                if (s.Length == 0)
                {
                    output.Add(s);
                    continue;
                }

                List<char> result = new List<char>();

                foreach (char c in s.ToCharArray())
                {
                    if (invalidChars.Contains(c))
                        result.AddRange(String.Format("_0x{0:x2}_", (int)c).ToCharArray());
                    else
                        result.Add(c);
                }

                output.Add(new String(result.ToArray()));
            }

            return output.ToArray();
        }

        private static string ReplaceEncodedMatch(Match match)
        {
            return new String(new char[] { (char)(Convert.ToInt32(match.Groups["hex"].Value, 16)) });
        }

        public static string FromSafeFilesystemName(string text, bool isPath)
        {
            string[] result = Utility.FromSafeFilesystemNames(new string[] { text }, isPath);
            return (result.Length > 0) ? result[0] : null;
        }

        public static string[] FromSafeFilesystemNames(string[] text, bool isPath)
        {
            if (text == null || text.Length == 0)
                return new string[0];

            List<string> output = new List<string>();

            foreach (string s in text)
            {
                if (s == null)
                    continue;

                if (s.Length == 0)
                    output.Add(s);
                else
                    output.Add(Utility.FromSafeRegex.Replace(s, new MatchEvaluator(Utility.ReplaceEncodedMatch)));
            }

            return output.ToArray();
        }

        public static string NormalizeEnsureRooted(string path)
        {
            string result = Utility.NormalizePathSeparators(path);
            if (result.StartsWith(Utility.DirectorySeparator))
                return result;

            return Utility.DirectorySeparator + result;
        }

        public static string ToAppDataPath(string path, bool isLocal)
        {
            string normalizedPath;
            return Utility.ToAppDataPath(path, isLocal, out normalizedPath);
        }

        public static string ToAppDataPath(string path, bool isLocal, out string normalizedPath)
        {
            normalizedPath = Utility.NormalizeEnsureRooted(path);
            if (normalizedPath.Length == 1)
                return (isLocal) ? Utility.LocalAppDataPath : Utility.RoamingAppDataPath;

            return Path.Combine((isLocal) ? Utility.LocalAppDataPath : Utility.RoamingAppDataPath, normalizedPath.Substring(1));
        }

        public static T Load<T>(string path)
            where T : class, new()
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (path.Length == 0)
                throw new ArgumentException("Path cannot be empty.", "path");

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            T result;
            using (XmlReader reader = XmlReader.Create(path))
                result = serializer.Deserialize(reader) as T;

            return result;
        }

        public static void Save<T>(T obj, string path)
            where T : class, new()
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            if (path == null)
                throw new ArgumentNullException("path");

            if (path.Length == 0)
                throw new ArgumentException("Path cannot be empty.", "path");

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CloseOutput = false;
            settings.Encoding = Encoding.UTF8;
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                serializer.Serialize(writer, obj);
                writer.Flush();
                writer.Close();
            }
        }

        public static string GetAppPathString(DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
                throw new ArgumentNullException("directoryInfo");

            if (directoryInfo.FullName.Length >= Utility.RoamingAppDataPath.Length && directoryInfo.FullName.StartsWith(Utility.RoamingAppDataPath))
                return Utility.FromSafeFilesystemName(Utility.NormalizeEnsureRooted(directoryInfo.FullName.Substring(Utility.RoamingAppDataPath.Length)), true);

            if (directoryInfo.FullName.Length >= Utility.LocalAppDataPath.Length && directoryInfo.FullName.StartsWith(Utility.LocalAppDataPath))
                return Utility.FromSafeFilesystemName(Utility.NormalizeEnsureRooted(directoryInfo.FullName.Substring(Utility.LocalAppDataPath.Length)), true);

            throw new InvalidOperationException("Directory is not in an app data location.");
        }
    }
}
