using System;
using System.ComponentModel;
using System.Security;
using System.Xml.Serialization;
using AmalgaDrive.Model;
using AmalgaDrive.Utilities;

namespace AmalgaDrive.Configuration
{
    public class DriveServiceSettings
    {
        // for xml serialization
        public DriveServiceSettings()
            : this(null)
        {
        }

        public DriveServiceSettings(DriveService service)
        {
            if (service != null)
            {
                Name = service.Name;
                TypeName = service.TypeName;
                Login = service.Login;
                Password = service.Password;
                BaseUrl = service.BaseUrl;
                SyncPeriod = service.SyncPeriod;
            }
        }

        public string Name { get; set; }
        public string TypeName { get; set; }
        public string BaseUrl { get; set; }
        public string Login { get; set; }
        public int SyncPeriod { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute(AttributeName = "Password")]
        [Browsable(false)]
        public byte[] EncryptedPassword { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public SecureString Password { get => EncryptedPassword.DecryptSecureString(); set => EncryptedPassword = value.EncryptSecureString(); }

        public override string ToString() => Name;

        public bool CopyFrom(DriveService service)
        {
            bool changed = false;

            if (service.SyncPeriod != SyncPeriod)
            {
                SyncPeriod = service.SyncPeriod;
                changed = true;
            }

            if (service.BaseUrl != BaseUrl)
            {
                BaseUrl = service.BaseUrl;
                changed = true;
            }

            if (service.Login != Login)
            {
                Login = service.Login;
                changed = true;
            }

            if (service.TypeName != TypeName)
            {
                TypeName = service.TypeName;
                changed = true;
            }

            if (!SecurityUtilities.EqualsOrdinal(service.Password, Password))
            {
                Password = service.Password;
                changed = true;
            }
            return changed;
        }
    }
}
