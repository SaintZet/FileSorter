using FileSorter.Contracts;
using System.Windows.Forms;

namespace FileSorter.Services
{
    public class FolderDialogService : IFolderDialogService
    {
        public string? ShowFolderDialog()
        {
            using var folderDialog = new FolderBrowserDialog();

            DialogResult result = folderDialog.ShowDialog();

            if (result == DialogResult.OK)
                return folderDialog.SelectedPath;

            return null;
        }
    }
}