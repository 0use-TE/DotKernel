using CommunityToolkit.Mvvm.ComponentModel;

namespace _AvaRef.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";
}
