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
}
