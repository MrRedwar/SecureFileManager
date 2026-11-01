using System.IO;
using SecureFileManager.Wpf.Infrastructure.FileSystem;

namespace SecureFileManager.Tests;

public sealed class StorageConfigurationTests
{
    [Fact]
    public void StorageRootRejectsVolumeRootRelativeAndNetworkLocations()
    {
        var volumeRoot = Path.GetPathRoot(Path.GetTempPath())!;
        Assert.Throws<InvalidOperationException>(() => new PersonalStorage(volumeRoot));
        Assert.Throws<InvalidOperationException>(() => new PersonalStorage(Path.Combine(volumeRoot, "test", "..")));
        Assert.Throws<InvalidOperationException>(() => new PersonalStorage("Storage"));
        Assert.Throws<InvalidOperationException>(() => new PersonalStorage(@"\\localhost\share\Storage"));
    }
}
