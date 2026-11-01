using SecureFileManager.Wpf.ViewModels;

namespace SecureFileManager.Tests;

public sealed class NavigationTests
{
    [Fact]
    public void NavigationSelectsEveryRegisteredSectionAndNotifiesBinding()
    {
        var model = new MainWindowViewModel();
        Assert.Equal("Мои файлы", model.SelectedSection.Title);
        Assert.Equal(6, model.Sections.Count);
        var changes = new List<string?>();
        model.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        foreach (var section in model.Sections)
        {
            model.SelectedSection = section;
            Assert.Same(section, model.SelectedSection);
        }
        Assert.Equal(6, changes.Count);
        Assert.All(changes, name => Assert.Equal(nameof(model.SelectedSection), name));
    }

    [Fact]
    public void InvalidOrRepeatedSelectionDoesNotLoseCurrentScreenOrNotify()
    {
        var model = new MainWindowViewModel();
        var original = model.SelectedSection;
        var changes = 0;
        model.PropertyChanged += (_, _) => changes++;
        model.SelectedSection = new SectionViewModel("Неизвестный", "");
        model.SelectedSection = null!;
        model.SelectedSection = original;
        Assert.Same(original, model.SelectedSection);
        Assert.Equal(0, changes);
    }
}
