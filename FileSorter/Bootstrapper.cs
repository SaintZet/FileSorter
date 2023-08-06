using Autofac;
using FileSorter.Contracts;
using FileSorter.Services;
using FileSorter.ViewModels;

public class Bootstrapper
{
    public IContainer Build()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<FolderDialogService>().As<IFolderDialogService>();
        builder.RegisterType<FileSorterService>().As<IFileSorterService>();
        builder.RegisterType<MainWindowViewModel>();

        return builder.Build();
    }
}