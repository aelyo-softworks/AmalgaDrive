using System;
using System.Collections.Generic;
using System.Linq;
using AmalgaDrive.Utilities;

namespace AmalgaDrive.Configuration
{
    public class Settings : Serializable<Settings>
    {
        private static Lazy<Settings> _current = new Lazy<Settings>(() => Deserialize(DefaultConfigurationFilePath), true);
        public static Settings Current => _current.Value;

        private List<DriveServiceSettings> _driveServices = new List<DriveServiceSettings>();

        public DriveServiceSettings[] DriveServices { get => _driveServices.ToArray(); set => _driveServices = new List<DriveServiceSettings>(value ?? (new DriveServiceSettings[0])); }

        public void Serialize() => Serialize(DefaultConfigurationFilePath);

        public void Set(DriveServiceSettings driveService)
        {
            if (driveService == null)
                throw new ArgumentNullException(nameof(driveService));

            var existing = _driveServices.FirstOrDefault();
            if (existing != null)
            {
                if (!driveService.CopyTo(existing))
                    return;
            }
            else

            {
                _driveServices.Add(driveService);
            }
            Serialize();
        }

        public bool Remove(DriveServiceSettings driveService)
        {
            if (driveService == null)
                throw new ArgumentNullException(nameof(driveService));

            bool ret = _driveServices.Remove(driveService);
            Serialize();
            return ret;
        }
    }
}
