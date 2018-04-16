using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security;
using System.Windows.Media;
using AmalgaDrive.Utilities;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Drive.WebDav
{
    [DisplayName("WebDav")]
    public class WebDavDriveService : IDriveService
    {
        private string _login;
        private SecureString _password;

        public ImageSource Icon => StockIcon.GetStockBitmap(StockIconId.MYNETWORK, StockIcon.SHGSI.SHGSI_LARGEICON);

        public void Initialize(IDictionary<string, object> dictionary)
        {
            _login = dictionary.GetValue<string>("login", null);
            if (string.IsNullOrWhiteSpace(_login))
            {
                _login = dictionary.GetValue<string>("username", null);
            }

            _password = dictionary.GetValue<SecureString>("password", null);
            if (string.IsNullOrWhiteSpace(_login))
            {
                _password = SecurityUtilities.ToSecureString(dictionary.GetValue<string>("password", null));
            }
        }

        public IReadOnlyList<IDriveResource> EnumResources(string parentResourcePath) => throw new System.NotImplementedException();
        public IDriveResource GetResource(string path) => throw new System.NotImplementedException();
        public void CreateFolderResource(string path) => throw new System.NotImplementedException();
        public Stream OpenReadResource(string path) => throw new System.NotImplementedException();
        public Stream OpenWriteResource(string path) => throw new System.NotImplementedException();
    }
}
