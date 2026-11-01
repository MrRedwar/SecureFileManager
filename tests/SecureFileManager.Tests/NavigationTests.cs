using SecureFileManager.Wpf.ViewModels;

namespace SecureFileManager.Tests;

public sealed class NavigationTests
{
    [Fact]
    public void NavigationCommandsRejectUnknownTargetsAndKeepSectionState()
    {
        var model = new MainWindowViewModel();
        var files = Assert.IsType<FilesViewModel>(model.SelectedSection);
        var fixture = new FileListItem("UI test fixture", "Только тест", "0", "—", "Icon.Files");
        files.Items.Add(fixture);
        files.SelectedFile = fixture;
        Assert.Equal(1, files.SelectedCount);
        model.NavigateCommand.Execute(model.Sections[0]);
        model.NavigateCommand.Execute(files);
        Assert.Same(files, model.SelectedSection);
        Assert.Same(fixture, files.SelectedFile);
        Assert.False(model.NavigateCommand.CanExecute(null));
        model.NavigateCommand.Execute(new SectionViewModel("Unknown", ""));
        Assert.Same(files, model.SelectedSection);
        Assert.All(model.Sections, section => Assert.False(section.UnavailableCommand.CanExecute(null)));
    }
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
