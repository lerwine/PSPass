$TypeCsCode = @(
@'
using System.ComponentModel;

namespace PSPassCustomTypes
{
    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, args);
        }
    }
}

'@,
@'
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

'@,
@'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PSPassCustomTypes
{
    [Serializable]
    public class PSLogin : NotifyPropertyChanged
    {
        public const string File_Extension = ".xml";

        #region Notes Property

        private string _notes = "";

        [XmlElement("Notes", Namespace = Utility.AppStorage_XmlNamespace)]
        public string Notes
        {
            get { return this._notes; }
            set
            {
                if (this.Name == "_")
                    throw new InvalidOperationException("Object is not attached to a login file.");

                string s = (value == null) ? "" : value;
                if (s == this._notes)
                    return;

                this._notes = s;
                this.RaiseSerializablePropertyChanged("Notes");
            }
        }

        #endregion

        #region Login Property

        private string _login = "";

        [XmlAttribute("Login")]
        public string Login
        {
            get { return this._login; }
            set
            {
                if (this.Name == "_")
                    throw new InvalidOperationException("Object is not attached to a login file.");

                string s = (value == null) ? "" : value;
                if (s == this._login)
                    return;

                this._login = s;
                this.RaiseSerializablePropertyChanged("Login");
            }
        }

        #endregion

        #region CreationTime Property

        [NonSerialized]
        private DateTime _creationTime = DateTime.MaxValue;

        [XmlIgnore]
        public DateTime CreationTime
        {
            get { return this._creationTime; }
            private set
            {
                if (this._creationTime == value)
                    return;

                this._creationTime = value;
                this.RaisePropertyChanged("CreationTime");
            }
        }

        #endregion

        #region LastWriteTime Property

        [NonSerialized]
        private DateTime _lastWriteTime = DateTime.MaxValue;

        [XmlIgnore]
        public DateTime LastWriteTime
        {
            get { return this._lastWriteTime; }
            private set
            {
                if (this._lastWriteTime == value)
                    return;

                this._lastWriteTime = value;
                this.RaisePropertyChanged("LastWriteTime");
            }
        }

        #endregion

        #region File Property

        [NonSerialized]
        private FileInfo _file = null;

        protected FileInfo File
        {
            get
            {
                if (this._file == null)
                {
                    if (this._folderPath == null)
                    {
                        this._folderPath = Utility.DirectorySeparator;
                        this._name = "_";
                    }

                    this._file = new FileInfo(System.IO.Path.Combine(Utility.RoamingAppDataPath, System.IO.Path.Combine(Utility.FromSafeFilesystemName(this.FolderPath, true),
                        Utility.FromSafeFilesystemName(this.Name, false) + PSLogin.File_Extension)));

                    this.RefreshFromFile();
                }

                return this._file;
            }
            private set
            {
                if (String.Compare(value.Extension, PSLogin.File_Extension, true) != 0)
                    throw new InvalidOperationException("Extension is not for a login file.");

                if (!value.Exists)
                    throw new FileNotFoundException("File does not exist.", value.FullName);

                this._folderPath = Utility.GetAppPathString(value.Directory);
                this._name = Utility.FromSafeFilesystemName(System.IO.Path.GetFileNameWithoutExtension(value.Name), false);
                this._file = value;
                
                this.RefreshFromFile();
            }
        }

        #endregion

        #region Name Property

        private string _name = null;

        [XmlIgnore]
        public string Name
        {
            get { return this._name; }
        }

        #endregion

        #region FolderPath Property

        private string _folderPath = null;

        [XmlIgnore]
        public string FolderPath
        {
            get { return this._folderPath; }
        }

        #endregion

        public PSLogin() { }

        public static PSLogin Create(DirectoryInfo parent, string name)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            if (name == null)
                throw new ArgumentNullException("name");

            string s = name.Trim();
            if (s.Length == 0)
                throw new ArgumentException("Name cannot be empty.", "name");

            throw new NotImplementedException();
        }

        public static PSLogin Load(FileInfo fileInfo)
        {
            if (!fileInfo.Exists)
                throw new FileNotFoundException("File does not exist.", fileInfo.FullName);

            PSLogin psLogin = Utility.Load<PSLogin>(fileInfo.FullName);
            psLogin.File = fileInfo;
            return psLogin;
        }

        private void RefreshFromFile()
        {
            if (this.File.Exists)
            {
                this.CreationTime = this.File.CreationTime;
                this.LastWriteTime = this.File.LastWriteTime;
            }
            else
            {
                this.CreationTime = DateTime.MaxValue;
                this.LastWriteTime = DateTime.MaxValue;
            }
        }

        protected void RaiseSerializablePropertyChanged(string propertyName)
        {
            this.SaveChanges();
            this.RaisePropertyChanged(propertyName);
        }

        protected void SaveChanges()
        {
            if (this.Name == "_" || !this.File.Directory.Exists)
                return;

            Utility.Save<PSLogin>(this, this.File.FullName);
            this.File.Refresh();
            this.RefreshFromFile();
        }
    }
}

'@,
@'
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Xml.Serialization;

namespace PSPassCustomTypes
{
    [Serializable]
    public class PSPassFolder : IEquatable<PSPassFolder>, IComparable<PSPassFolder>
    {
        [Serializable]
        [XmlRoot("PSPassFolder", Namespace = Utility.AppStorage_XmlNamespace)]
        public class FolderConfig : NotifyPropertyChanged, IEquatable<FolderConfig>
        {
            public const string SerializedFileName = "Folder.config";

            private string _notes = "";

            [XmlElement("Notes", Namespace = Utility.AppStorage_XmlNamespace)]
            public string Notes
            {
                get { return this._notes; }
                set
                {
                    string s = (value == null) ? "" : value;
                    if (s == this._notes)
                        return;

                    this._notes = s;
                    this.RaisePropertyChanged("Notes");
                }
            }

            public bool Equals(FolderConfig other)
            {
                return other != null && (Object.ReferenceEquals(this, other) || this.Notes == other.Notes);
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as FolderConfig);
            }

            public override int GetHashCode()
            {
                return this.ToString().GetHashCode();
            }

            public override string ToString()
            {
                return this.Notes;
            }
        }

        private FolderConfig _config = null;

        protected FolderConfig Config
        {
            get
            {
                if (this._config == null)
                {
                    if (this.File.Exists)
                        this._config = Utility.Load<FolderConfig>(this.File.FullName);
                    else
                    {
                        this._config = new FolderConfig();
                        this.SaveConfig();
                    }

                    this._config.PropertyChanged += this.Config_PropertyChanged;
                }

                return this._config;
            }
            private set
            {
                FolderConfig config = (value == null) ? new FolderConfig() : value;
                if (config.Equals(this._config))
                {
                    if (Object.ReferenceEquals(this._config, config))
                        return;

                    this._config.PropertyChanged -= this.Config_PropertyChanged;
                    config.PropertyChanged += this.Config_PropertyChanged;

                    this._config = config;
                    return;
                }

                if (this._config != null)
                    this._config.PropertyChanged -= this.Config_PropertyChanged;

                config.PropertyChanged += this.Config_PropertyChanged;
                this._config = config;
                this.SaveConfig();
            }
        }

        public string Notes
        {
            get { return this.Config.Notes; }
            set
            {
                if (this._config == null)
                    this.Config = new FolderConfig { Notes = value };
                else
                    this.Config.Notes = value;
            }
        }

        [NonSerialized]
        private FileInfo _file = null;

        protected FileInfo File
        {
            get
            {
                if (this._file == null)
                    this._file = new FileInfo(System.IO.Path.Combine(this.Directory.FullName, FolderConfig.SerializedFileName));

                return this._file;
            }
            private set { this._file = value; }
        }

        [NonSerialized]
        private DirectoryInfo _directory = null;

        protected DirectoryInfo Directory
        {
            get
            {
                if (this._directory == null)
                    this._directory = new DirectoryInfo(Utility.ToAppDataPath(Utility.ToSafeFilesystemName(this.Path, true, true), false));

                return this._directory;
            }
            private set { this._directory = value; }
        }

        [NonSerialized]
        private string _name = null;

        public string Name
        {
            get
            {
                if (this._name == null)
                    this._name = Utility.FromSafeFilesystemName(this.Directory.Name, false);

                return this._name;
            }
            private set { this._name = value; }
        }

        private string _path = null;

        public string Path
        {
            get
            {
                if (this._path == null)
                    this._path = Utility.DirectorySeparator;

                return this._path;
            }
            private set { this._path = value; }
        }

        public DateTime CreationTime { get { return this.Directory.CreationTime; } }

        public DateTime LastWriteTime { get { return this.File.LastWriteTime; } }

        public PSPassFolder() { }

        public PSPassFolder(string path)
        {
            string normalizedPath;
            this.Directory = new DirectoryInfo(Utility.ToAppDataPath(Utility.ToSafeFilesystemName(path, true, true), false, out normalizedPath));
            this.Path = Utility.FromSafeFilesystemName(normalizedPath, true);
        }

        public PSLogin[] GetLogins()
        {
            if (!this.Directory.Exists)
                return new PSLogin[0];

            List<PSLogin> result = new List<PSLogin>();

            foreach (FileInfo fileInfo in this.Directory.GetFiles("*" + PSLogin.File_Extension, SearchOption.TopDirectoryOnly))
                result.Add(PSLogin.Load(fileInfo));

            return result.ToArray();
        }

        public PSPassFolder[] GetFolders()
        {
            if (!this.Directory.Exists)
                return new PSPassFolder[0];

            List<PSPassFolder> result = new List<PSPassFolder>();

            foreach (DirectoryInfo directoryInfo in this.Directory.GetDirectories())
                result.Add(PSPassFolder.Load(directoryInfo));

            return result.ToArray();
        }

        public static PSPassFolder Load(DirectoryInfo directoryInfo)
        {
            return new PSPassFolder
            {
                Directory = directoryInfo,
                Path = Utility.GetAppPathString(directoryInfo)
            };
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.SaveConfig();
        }

        private void SaveConfig()
        {
            Utility.Save<FolderConfig>(this.Config, this.File.FullName);
            this.File.Refresh();
        }

        public bool Equals(PSPassFolder other)
        {
            return other != null && (Object.ReferenceEquals(this, other) || String.Compare(this.Path, other.Path, true) == 0);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as PSPassFolder);
        }

        public int GetHashCode(bool ignoreCase)
        {
            return (ignoreCase) ? this.Path.ToLower().GetHashCode() : this.Path.GetHashCode();
        }

        public override int GetHashCode()
        {
            return this.GetHashCode(true);
        }

        public override string ToString()
        {
            return this.Path;
        }

        public int CompareTo(PSPassFolder other, bool ignoreCase)
        {
            if (other == null)
                return 1;

            if (Object.ReferenceEquals(this, other))
                return 0;

            int result = String.Compare(this.Path, other.Path, true);
            if (result != 0 || ignoreCase)
                return result;

            return String.Compare(this.Path, other.Path, false);
        }

        public int CompareTo(PSPassFolder other)
        {
            return this.CompareTo(other, true);
        }
    }
}

'@,
@'
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

'@
);
$TypeCsFiles = @();
Try {
    $TypeCsCode | ForEach-Object {
        $TempFile = [System.IO.Path]::GetTempPath() | Join-Path -ChildPath:(([Guid]::NewGuid().ToString("n") + ".cs"));
        [System.IO.File]::WriteAllText($TempFile, $_);
        $TypeCsFiles += $TempFile;
    }
    Add-Type -Path:$TypeCsFiles;
} Catch {
    throw;
} Finally {
    foreach ($f in $TypeCsFiles) {
        Try {
            [System.IO.File]::Delete($f);
        } Catch { }
    }
}
