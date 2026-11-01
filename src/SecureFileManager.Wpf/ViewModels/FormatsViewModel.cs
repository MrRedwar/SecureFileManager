namespace SecureFileManager.Wpf.ViewModels;
public sealed class FormatsViewModel() : SectionViewModel("JSON / XML", "Документы, структура и проверка форматов", "Icon.JsonXml")
{
    public string FileLabel => "Файл не выбран";
    public string Content => string.Empty;
}
