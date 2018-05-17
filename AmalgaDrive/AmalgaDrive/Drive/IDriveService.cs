using System.Collections.Generic;
using System.Windows.Media;
using AmalgaDrive.Configuration;
using ShellBoost.Core;

namespace AmalgaDrive.Drive
{
    public interface IDriveService : IRemoteFileSystem
    {
        ImageSource Icon { get; }
        void Initialize(DriveServiceSettings settings, IDictionary<string, object> dictionary);
    }
}
