using SecureFileManager.Core.Files;
using SecureFileManager.Core.Interfaces;
namespace SecureFileManager.Wpf.ViewModels;
public sealed record ArchiveListItem(string Name, string Size, string ModifiedAt);
public sealed class ArchivesViewModel : SectionViewModel
{
    public FilesViewModel Browser { get; }
    public FilesViewModel Sources { get; }
    public ArchivesViewModel(IFileService? files = null, IFileDialogs? dialogs = null, IArchiveService? archives = null, IArchiveDialogs? archiveDialogs = null)
        : base("Архивы", "Просмотр ZIP и распаковка в новую личную папку", "Icon.Archives")
    {
        Browser = new(files, dialogs, archives: archives, archiveDialogs: archiveDialogs, zipOnly: true);
        Sources = new(files, dialogs, archives: archives, archiveDialogs: archiveDialogs, archiveSources: true);
    }
    public override Task ActivateAsync() => Task.WhenAll(Browser.ActivateAsync(), Sources.ActivateAsync());
    public override void UpdateSession(SessionIdentity? identity) { base.UpdateSession(identity); Browser.UpdateSession(identity); Sources.UpdateSession(identity); }
    public override void ResetUserState() { base.ResetUserState(); Browser.ResetUserState(); Sources.ResetUserState(); }
}
