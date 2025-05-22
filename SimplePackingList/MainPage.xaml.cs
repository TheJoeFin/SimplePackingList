using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SimplePackingList.ViewModels;

namespace SimplePackingList;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadState();
    }
}
