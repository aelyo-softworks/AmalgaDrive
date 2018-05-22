using System.Collections.Generic;
using System.Windows.Media;
using AmalgaDrive.Model;
using ShellBoost.Core;

namespace AmalgaDrive.Drive
{
    public interface IDriveService : IRemoteFileSystem
    {
        ImageSource Icon { get; }
        void Initialize(DriveService driveService, IDictionary<string, object> dictionary);
    }
}
