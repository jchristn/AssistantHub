namespace AssistantHub.Core.Services.Crawlers
{
    using System;
    using AssistantHub.Core.Enums;

    internal static class NfsVersionConverter
    {
        internal static Blobject.NFS.NfsVersionEnum ToBlobjectNfsVersion(NfsVersionEnum version)
        {
            switch (version)
            {
                case NfsVersionEnum.V2:
                    return Blobject.NFS.NfsVersionEnum.V2;
                case NfsVersionEnum.V3:
                    return Blobject.NFS.NfsVersionEnum.V3;
                case NfsVersionEnum.V4:
                    return Blobject.NFS.NfsVersionEnum.V4;
                default:
                    throw new ArgumentException("Unknown NFS version '" + version.ToString() + "'.");
            }
        }
    }
}
