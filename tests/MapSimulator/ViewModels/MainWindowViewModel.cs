using Avalonia.Media.Imaging;
using ReactiveUI;

namespace MapSimulator.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private WriteableBitmap? _cover;

    public WriteableBitmap? cover {
        get => _cover;
        set => this.RaiseAndSetIfChanged(ref _cover, value);
    }
}