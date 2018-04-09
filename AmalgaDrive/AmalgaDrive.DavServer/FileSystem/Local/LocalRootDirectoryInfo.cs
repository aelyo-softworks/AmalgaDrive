using System.IO;

namespace AmalgaDrive.DavServer.FileSystem.Local
{
    public class LocalRootDirectoryInfo : LocalDirectoryInfo
    {
        public LocalRootDirectoryInfo(LocalFileSystem system, DirectoryInfo info)
            : base(system, info)
        {
        }

        public override string Name => System.Options.RootName;
        public override IDirectoryInfo Parent => null;
        public override bool IsRoot => true;

        public override void Delete() { }
        public override void Delete(bool recursive) { }
        public override void MoveTo(string rootRelativePath) { }
        public override void CopyTo(string rootRelativePath, bool overwrite) { }
    }
}
