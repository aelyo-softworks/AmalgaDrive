using System.ComponentModel;
using System.Security;
using System.Xml.Serialization;
using AmalgaDrive.Utilities;

namespace AmalgaDrive.Configuration
{
    public class DriveServiceSettings
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string BaseUrl { get; set; }

        public string User { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute(AttributeName = "Password")]
        [Browsable(false)]
        public byte[] EncryptedPassword { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public SecureString SecurePassword { get => EncryptedPassword.DecryptSecureString(); set => EncryptedPassword = value.EncryptSecureString(); }

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
            return changed;
        }
    }
}
