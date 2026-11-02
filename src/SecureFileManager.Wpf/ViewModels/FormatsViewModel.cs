using SecureFileManager.Core.Files;
using SecureFileManager.Core.Interfaces;
namespace SecureFileManager.Wpf.ViewModels;
// Reuses the browser/editor; holds no independent document content.
public sealed class FormatsViewModel : SectionViewModel
{
    public FilesViewModel Browser { get; }
    public FormatsViewModel(IFileService? files = null, IFileDialogs? dialogs = null)
        : base("JSON / XML", "Выберите документ личного хранилища для общего редактора", "Icon.JsonXml")
    { Browser = new(files, dialogs, structuredOnly: true); }
    public override Task ActivateAsync() => Browser.ActivateAsync();
    public override void ResetUserState() { base.ResetUserState(); Browser.ResetUserState(); }
    public override void UpdateSession(SessionIdentity? identity)
    { base.UpdateSession(identity); Browser.UpdateSession(identity); }
}
