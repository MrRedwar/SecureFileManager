using System.Collections.ObjectModel;
namespace SecureFileManager.Wpf.ViewModels;
public sealed record ArchiveListItem(string Name, string Size, string ModifiedAt);
public sealed class ArchivesViewModel() : SectionViewModel("Архивы", "Бережное хранение и безопасная распаковка", "Icon.Archives")
{
    public override void ResetUserState() { base.ResetUserState(); Items.Clear();  }
    public ObservableCollection<ArchiveListItem> Items { get; } = [];
}
