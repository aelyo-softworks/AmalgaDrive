using System;
using System.Collections;
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using AmalgaDrive.Configuration;
using AmalgaDrive.Drive;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Model
{
    /// <summary>
    /// The DriveService class is the model for a remote drive, used in WPF's MVVM.
    /// </summary>
    /// <seealso cref="AmalgaDrive.Model.DriveObject" />
    public class DriveService : DriveObject, IDisposable
    {
        /// <summary>
        /// All drives root directory name
        /// </summary>
        public const string AllRootsName = "AmalgaDrive";

        /// <summary>
        /// All drives root full directory path.
        /// </summary>
        public static readonly string AllRootsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AllRootsName);

        private bool _disposedValue = false;
        private Lazy<ImageSource> _icon;
        private Lazy<IDriveService> _service;
        private Lazy<OnDemandSynchronizer> _onDemandSynchronizer;

        public DriveService()
            : this(null)
        {
        }

        public DriveService(DriveServiceSettings settings)
        {
            _service = new Lazy<IDriveService>(GetService, true);
            _icon = new Lazy<ImageSource>(GetIcon, true);

            if (settings != null)
            {
                TypeName = settings.TypeName;
                Name = settings.Name;
                Login = settings.Login;
                BaseUrl = settings.BaseUrl;
                Password = settings.Password;
            }

            _onDemandSynchronizer = new Lazy<OnDemandSynchronizer>(GetSynchronizer, true);
            SyncPeriod = settings != null ? settings.SyncPeriod : 300;
        }

        public OnDemandSynchronizer OnDemandSynchronizer => _onDemandSynchronizer.Value;
        public string Name { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public string Login { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public string BaseUrl { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public Uri BaseUri => new Uri(BaseUrl, UriKind.Absolute);
        public SecureString Password { get => DictionaryObjectGetPropertyValue<SecureString>(); set => DictionaryObjectSetPropertyValue(value); }
        public bool Synchronizing { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public string SynchronizingText { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public string FileName => IOUtilities.PathToValidFileName(Name);

        public void Unregister()
        {
            ResetOnDemandSynchronizer();
            OnDemandSynchronizer.Unregister(RootPath, OnDemandRegistration);
        }

        public void ResetOnDemandSynchronizer()
        {
            if (!_onDemandSynchronizer.IsValueCreated)
                return;

            _onDemandSynchronizer.Value.Synchronizing -= OnSynchronizing;
            _onDemandSynchronizer.Value.Dispose();
            _onDemandSynchronizer = new Lazy<OnDemandSynchronizer>(GetSynchronizer, true);
        }

        public int SyncPeriod
        {
            get => DictionaryObjectGetPropertyValue<int>();
            set
            {
                if (DictionaryObjectSetPropertyValue(value))
                {
                    if (Name != null)
                    {
                        _onDemandSynchronizer.Value.SyncPeriod = SyncPeriod;
                    }
                }
            }
        }

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
                string path = Path.Combine(AllRootsPath, FileName);
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

            var service = Activator.CreateInstance(type) as IDriveService;
            service.Initialize(this, null);
            return service;
        }

        private ImageSource GetIcon() => _service.Value.Icon;

        private OnDemandSynchronizer GetSynchronizer()
        {
            OnDemandSynchronizer.EnsureRegistered(RootPath, OnDemandRegistration);
            var sync = new OnDemandSynchronizer(RootPath, Service);
            sync.Synchronizing += OnSynchronizing;
            sync.SyncPeriod = SyncPeriod;
            sync.Logger = ((App)Application.Current)?.Logger;
            return sync;
        }

        // this is used to change the green sync icon dynamically
        private void OnSynchronizing(object sender, OnDemandSynchronizerEventArgs e) =>
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                switch (e.Type)
                {
                    case OnDemandSynchronizerEventType.SynchronizingAll:
                        Synchronizing = true;
                        SynchronizingText = "Synchronizing...";
                        break;

                    case OnDemandSynchronizerEventType.SynchronizedAll:
                        Synchronizing = false;
                        SynchronizingText = "Synchronization paused.";
                        break;
                }
            });

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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    if (_onDemandSynchronizer.IsValueCreated)
                    {
                        _onDemandSynchronizer.Value?.Dispose();
                    }
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                _disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DriveService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
    }
}
