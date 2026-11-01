using System.Collections.ObjectModel;
namespace SecureFileManager.Wpf.ViewModels;
public sealed record AuditListItem(string OccurredAt, string Actor, string Action, string Target, string Result)
{
    public string ResultCode => Result switch { "Успех" => "Success", "Отказано" => "Denied", "Ошибка" => "Failed", _ => "Neutral" };
}
public sealed class JournalViewModel() : SectionViewModel("Журнал", "История собственных действий", "Icon.Journal")
{
    public ObservableCollection<AuditListItem> Items { get; } = [];
    public IReadOnlyList<string> ResultFilters { get; } = ["Все результаты", "Успех", "Отказано", "Ошибка"];
    public IReadOnlyList<string> ActionFilters { get; } = ["Все операции", "Файлы", "Вход и выход", "Архивы", "Проверка форматов"];
}
