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
            }
        }

        public string Name { get; set; }
        public string TypeName { get; set; }
        public string BaseUrl { get; set; }
        public string Login { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute(AttributeName = "Password")]
        [Browsable(false)]
        public byte[] EncryptedPassword { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public SecureString Password { get => EncryptedPassword.DecryptSecureString(); set => EncryptedPassword = value.EncryptSecureString(); }

        public override string ToString() => Name;

        public bool CopyTo(DriveServiceSettings settings)
        {
            if (settings == this)
                return false;

            bool changed = false;
            if (settings.BaseUrl != BaseUrl)
            {
                settings.BaseUrl = BaseUrl;
                changed = true;
            }

            if (settings.Login != Login)
            {
                settings.Login = Login;
                changed = true;
            }

            if (settings.TypeName != TypeName)
            {
                settings.TypeName = TypeName;
                changed = true;
            }

            if (!SecurityUtilities.EqualsOrdinal(settings.Password, Password))
            {
                settings.Password = Password;
                changed = true;
            }
            return changed;
        }
    }
}
