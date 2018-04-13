using System;
using System.Collections.Generic;
using System.Linq;
using AmalgaDrive.Utilities;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Configuration
{
    public class Settings : Serializable<Settings>
    {
        private static Lazy<Settings> _current = new Lazy<Settings>(() => Deserialize(DefaultConfigurationFilePath), true);
        public static Settings Current => _current.Value;
        public static void CurrentSerialize() => Current.Serialize();

        private List<DriveServiceSettings> _driveServices = new List<DriveServiceSettings>();

        public DriveServiceSettings[] DriveServices { get => _driveServices.ToArray(); set => _driveServices = new List<DriveServiceSettings>(value ?? (new DriveServiceSettings[0])); }

        public void Serialize() => Serialize(DefaultConfigurationFilePath);

        public void SetDriveService(DriveServiceSettings driveService)
        {
            if (driveService == null)
                throw new ArgumentNullException(nameof(driveService));

            var existing = _driveServices.FirstOrDefault(s => s.Name.EqualsIgnoreCase(driveService.Name));
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

        public bool RemoveDriveService(string driveServiceName)
        {
            if (driveServiceName == null)
                throw new ArgumentNullException(nameof(driveServiceName));

            int index = _driveServices.FindIndex(d => d.Name.EqualsIgnoreCase(driveServiceName));
            if (index < 0)
                return false;

            _driveServices.RemoveAt(index);
            Serialize();
            return true;
        }
    }
}
