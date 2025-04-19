using Microsoft.UI.Xaml;

namespace SimplePackingList;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
        m_window.ExtendsContentIntoTitleBar = true;
    }

    public Window? m_window;
}
