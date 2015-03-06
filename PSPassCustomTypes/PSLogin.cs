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
