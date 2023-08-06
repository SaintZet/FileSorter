using Autofac;
using FileSorter.ViewModels;
using FileSorter.Views;
using System.Windows;

namespace FileSorter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var bootstrapper = new Bootstrapper();
            var container = bootstrapper.Build();

            Current.Resources.Add("Container", container);

            var mainViewModel = container.Resolve<MainWindowViewModel>();
            var mainWindow = new MainWindowView { DataContext = mainViewModel };
            mainWindow.Show();
        }
    }
}