using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using AmalgaDrive.Configuration;

namespace AmalgaDrive.Drive
{
    public interface IRemoteDriveService
    {
        ImageSource Icon { get; }

        void Initialize(DriveServiceSettings settings, IDictionary<string, object> dictionary);
        IEnumerable<IRemoteResource> EnumResources(string parentPath);
        IRemoteResource GetResource(string path);
        void CreateFolderResource(string path);
        Stream OpenReadResource(string path);
        Stream OpenWriteResource(string path);
    }
}
