using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace AmalgaDrive.Drive
{
    public interface IDriveService
    {
        ImageSource Icon { get; }

        void Initialize(IDictionary<string, object> dictionary);
        IReadOnlyList<IDriveResource> EnumResources(string parentPath);
        IDriveResource GetResource(string path);
        void CreateFolderResource(string path);
        Stream OpenReadResource(string path);
        Stream OpenWriteResource(string path);
    }
}
