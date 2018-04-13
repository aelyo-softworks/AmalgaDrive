using System;
using System.Collections;
using System.Security;
using System.Windows.Media;
using AmalgaDrive.Configuration;
using AmalgaDrive.Drive;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Model
{
    public class DriveService : DriveObject
    {
        private Lazy<ImageSource> _icon;
        private Lazy<IDriveService> _service;

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
        }

        public string Name { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public string Login { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public string BaseUrl { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public SecureString Password { get => DictionaryObjectGetPropertyValue<SecureString>(); set => DictionaryObjectSetPropertyValue(value); }
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

        private ImageSource GetIcon() => _service.Value?.Icon;

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
