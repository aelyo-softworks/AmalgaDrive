using System;
using System.Collections.Generic;
using System.Linq;
using AmalgaDrive.Model;
using AmalgaDrive.Utilities;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Configuration
{
    public class Settings : Serializable<Settings>
    {
        private static Lazy<Settings> _current = new Lazy<Settings>(() => Deserialize(DefaultConfigurationFilePath), true);
        public static Settings Current => _current.Value;
        public static void CurrentSerialize() => Current.Serialize();

        private List<DriveServiceSettings> _driveServices = new List<DriveServiceSettings>();

        public DriveServiceSettings[] DriveServiceSettings { get => _driveServices.ToArray(); set => _driveServices = new List<DriveServiceSettings>(value ?? (new DriveServiceSettings[0])); }

        public void Serialize() => Serialize(DefaultConfigurationFilePath);

        public void SetDriveService(DriveService driveService)
        {
            if (driveService == null)
                throw new ArgumentNullException(nameof(driveService));

            var existing = _driveServices.FirstOrDefault(s => s.Name.EqualsIgnoreCase(driveService.Name));
            if (existing != null)
            {
                if (!existing.CopyFrom(driveService))
                    return;
            }
            else
            {
                _driveServices.Add(new DriveServiceSettings(driveService));
            }

            OnDemandSynchronizer.EnsureRegistered(driveService.RootPath, driveService.OnDemandRegistration);
            Serialize();
        }

        public bool RemoveDriveService(DriveService driveService)
        {
            if (driveService == null)
                throw new ArgumentNullException(nameof(driveService));

            int index = _driveServices.FindIndex(d => d.Name.EqualsIgnoreCase(driveService.Name));
            if (index < 0)
                return false;

            driveService.Unregister();
            _driveServices.RemoveAt(index);
            Serialize();
            return true;
        }
    }
}
