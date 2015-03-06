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
