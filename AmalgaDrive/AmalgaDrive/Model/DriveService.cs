using System;
using System.Collections;
using System.IO;
using System.Security;
using System.Windows.Media;
using AmalgaDrive.Configuration;
using AmalgaDrive.Drive;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Model
{
    public class DriveService : DriveObject
    {
        public static readonly string AllRootsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AmalgaDrive");

        private Lazy<ImageSource> _icon;
        private Lazy<IDriveService> _service;
        private Lazy<OnDemandSynchronizer> _onDemandSynchronizer;

        public DriveService()
            : this(null)
        {
        }

        public DriveService(DriveServiceSettings settings)
        {
            if (settings != null)
            {
                TypeName = settings.TypeName;
                Name = settings.Name;
                Login = settings.Login;
                BaseUrl = settings.BaseUrl;
                Password = settings.Password;
            }

            _service = new Lazy<IDriveService>(GetService, true);
            _icon = new Lazy<ImageSource>(GetIcon, true);
            _onDemandSynchronizer = new Lazy<OnDemandSynchronizer>(GetSynchronizer, true);
            SyncPeriod = 300;
        }

        public OnDemandSynchronizer OnDemandSynchronizer => _onDemandSynchronizer.Value;
        public string Name { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public string Login { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public string BaseUrl { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public SecureString Password { get => DictionaryObjectGetPropertyValue<SecureString>(); set => DictionaryObjectSetPropertyValue(value); }
        public int SyncPeriod { get => DictionaryObjectGetPropertyValue<int>(); set => DictionaryObjectSetPropertyValue(value); }

        public OnDemandRegistration OnDemandRegistration
        {
            get
            {
                var reg = new OnDemandRegistration();
                reg.ProviderName = "AmalgaDrive - " + Name;
                return reg;
            }
        }

        public string RootPath
        {
            get
            {
                string path = Path.Combine(AllRootsPath, IOUtilities.PathToValidFileName(Name));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public string TypeName
        {
            get => DictionaryObjectGetPropertyValue<string>();
            set
            {
                if (DictionaryObjectSetPropertyValue(value))
                {
                    // reset
                    _service = new Lazy<IDriveService>(GetService, true);
                    _icon = new Lazy<ImageSource>(GetIcon, true);
                    OnPropertyChanged(this, nameof(Service));
                    OnPropertyChanged(this, nameof(Icon));
                }
            }
        }

        public IDriveService Service => _service.Value;
        public ImageSource Icon => _icon.Value;

        private IDriveService GetService()
        {
            if (string.IsNullOrWhiteSpace(TypeName))
                return null;

            var type = Type.GetType(TypeName, false);
            if (type == null)
                return null;

            return Activator.CreateInstance(type) as IDriveService;
        }

        private ImageSource GetIcon() => _service.Value.Icon;

        private OnDemandSynchronizer GetSynchronizer()
        {
            var sync = new OnDemandSynchronizer(RootPath, Service);
            return sync;
        }

        public override string ToString() => Name;

        protected override IEnumerable DictionaryObjectGetErrors(string propertyName)
        {
            if (propertyName == null || propertyName == nameof(Name))
            {
                if (string.IsNullOrWhiteSpace(Name))
                    yield return "Name cannot be empty.";
            }

            if (propertyName == null || propertyName == nameof(TypeName))
            {
                if (string.IsNullOrWhiteSpace(TypeName))
                    yield return "Type cannot be empty.";
            }

            if (propertyName == null || propertyName == nameof(BaseUrl))
            {
                if (!string.IsNullOrWhiteSpace(BaseUrl))
                {
                    if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
                        (!uri.Scheme.EqualsIgnoreCase("http") && !uri.Scheme.EqualsIgnoreCase("https")))
                        yield return "Url is invalid.";
                }
            }
        }
    }
}
