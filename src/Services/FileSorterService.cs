using FileSorter.Contracts;
using FileSorter.Enums;
using FileSorter.Models;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter.Services
{
    public class FileSorterService : IFileSorterService
    {
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<ProgressReport>? ProgressChanged;

        public FileConflictResolution ConflictResolution { get; set; } = FileConflictResolution.AddSuffix;
        public FileMoveMode MoveMode { get; set; } = FileMoveMode.Move;

        // Helper method to cancel the operation
        public void CancelSort() =>
            _cancellationTokenSource?.Cancel();

        /// <summary>
        /// Method to sort files based on the specified rules and move them to corresponding folders
        /// </summary>
        /// <param name="directoryPath"> The path to the source directory containing files </param>
        /// <param name="rules"> The list of rules for sorting files </param>
        /// <param name="destinationPath">
        /// The path to the directory where sorted files will be moved
        /// </param>
        /// <param name="cancellationToken"> Cancellation token to interrupt the method execution </param>
        public async Task SortFilesAndMoveByRulesAsync(string directoryPath, List<RuleModel> rules, string destinationPath, CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Find files with specified extensions in the given directory
            var foundFilesByExtensions = FindFilesByExtensions(directoryPath, rules);

            // Process rules for each extension
            foreach (var rule in rules)
            {
                foreach (var extension in rule.Extensions)
                {
                    // Get the list of files with the current extension, if any
                    var files = foundFilesByExtensions.ContainsKey(extension) ? foundFilesByExtensions[extension] : new List<string>();

                    // Sort files by creation date and last write date
                    var sortedFiles = SortFilesByDate(files);

                    // Move sorted files to corresponding subfolders in the destination directory
                    await MoveOrCopyFilesByRulesAsync(destinationPath, sortedFiles, rule, _cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// Method to find files with specified extensions in the specified directory and group them
        /// by extensions
        /// </summary>
        /// <param name="directoryPath"> The path to the source directory where to search </param>
        /// <param name="rules"> The list of rules for finding files with corresponding extensions </param>
        /// <returns>
        /// A dictionary where the key is the file extension, and the value is a list of file paths
        /// with that extension
        /// </returns>
        private static Dictionary<string, List<string>> FindFilesByExtensions(string directoryPath, List<RuleModel> rules)
        {
            // Dictionary where the key is the file extension, and the value is a list of file paths
            // with that extension
            var foundFilesByExtensions = new Dictionary<string, List<string>>();

            // Check if the specified directory exists
            if (Directory.Exists(directoryPath))
            {
                // Iterate through all specified rules to find files with corresponding extensions
                foreach (var rule in rules)
                {
                    // Iterate through all extensions in the current rule
                    foreach (var extension in rule.Extensions)
                    {
                        // Get the list of file paths with the current extension using a search
                        // through all subdirectories
                        var filesWithExtension = new List<string>(Directory.GetFiles(directoryPath, $"*{extension}", SearchOption.AllDirectories));

                        // Add the found file paths with the current extension to the dictionary
                        // using the extension as the key
                        foundFilesByExtensions.Add(extension, filesWithExtension);
                    }
                }
            }

            // Return the dictionary containing found files grouped by extensions
            return foundFilesByExtensions;
        }

        /// <summary>
        /// Method to sort files by creation date and last write date
        /// </summary>
        /// <param name="files"> The list of file paths to be sorted </param>
        /// <returns> The sorted list of file paths </returns>
        private static List<string> SortFilesByDate(List<string> files)
        {
            files.Sort((file1, file2) =>
            {
                // Get the creation date and last write date of file 1
                var file1CreationTime = File.GetCreationTime(file1);
                var file1LastWriteTime = File.GetLastWriteTime(file1);

                // Get the creation date and last write date of file 2
                var file2CreationTime = File.GetCreationTime(file2);
                var file2LastWriteTime = File.GetLastWriteTime(file2);

                // Compare the creation dates of files 1 and 2
                var dateComparisonResult = DateTime.Compare(file1CreationTime, file2CreationTime);

                // If the creation dates are different, return the comparison result
                if (dateComparisonResult != 0)
                {
                    return dateComparisonResult;
                }
                else
                {
                    // If the creation dates are the same, compare the last write dates of files 1
                    // and 2
                    return DateTime.Compare(file1LastWriteTime, file2LastWriteTime);
                }
            });

            // Return the sorted list of files
            return files;
        }

        /// <summary>
        /// Method to move or copy files to the specified folder
        /// </summary>
        /// <param name="destinationFolderPath">
        /// The path to the destination folder where files will be moved
        /// </param>
        /// <param name="files"> The list of file paths to be moved </param>
        /// <param name="rule"> The rule to determine the subfolder destination </param>
        private async Task MoveOrCopyFilesByRulesAsync(string destinationFolderPath, List<string> files, RuleModel rule, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                int processedFiles = 0;
                foreach (string filePath in files)
                {
                    // Check if the operation has been canceled
                    cancellationToken.ThrowIfCancellationRequested();

                    // Update progress information and raise the ProgressChanged event
                    OnProgressChanged(files.Count, processedFiles++);

                    // Get the creation date and last write date of the file
                    var creationDate = File.GetCreationTime(filePath);
                    var lastWriteDate = File.GetLastWriteTime(filePath);

                    // Choose the earlier date between the creation date and last write date
                    var selectedDate = creationDate < lastWriteDate ? creationDate : lastWriteDate;

                    // Create the subfolder name inside the destination folder based on the year and
                    // month of the selected date
                    var yearMonthFolderName = Path.Combine(destinationFolderPath, rule.Destination, selectedDate.ToString("yyyy"));

                    // Create the subfolder name with the month inside the year folder
                    var monthFolderName = selectedDate.ToString("MM MMMM", CultureInfo.InvariantCulture);
                    var monthFolderPath = Path.Combine(yearMonthFolderName, monthFolderName);

                    // Create the month subfolder if it doesn't exist and there are files to move
                    if (files.Any() && !Directory.Exists(monthFolderPath))
                        Directory.CreateDirectory(monthFolderPath);

                    // Get the file name without the path
                    var fileName = Path.GetFileName(filePath);

                    // Form the full file path inside the subfolder
                    var destinationFilePath = Path.Combine(monthFolderPath, fileName);

                    // Handle conflict resolution
                    if (File.Exists(destinationFilePath))
                    {
                        switch (ConflictResolution)
                        {
                            case FileConflictResolution.AddSuffix:
                                var uniqueFileName = GetUniqueFileName(monthFolderPath, fileName);
                                destinationFilePath = Path.Combine(monthFolderPath, uniqueFileName);
                                break;

                            case FileConflictResolution.Overwrite:
                                // Option: Overwrite the file with the same name
                                File.Copy(filePath, destinationFilePath, overwrite: true);
                                if (MoveMode == FileMoveMode.Move)
                                    File.Delete(filePath);

                                continue;

                            case FileConflictResolution.Skip:
                                // Option: Skip the file with the same name (do nothing)
                                continue;
                        }
                    }

                    if (MoveMode == FileMoveMode.Copy)
                        File.Copy(filePath, destinationFilePath);
                    if (MoveMode == FileMoveMode.Move)
                        File.Move(filePath, destinationFilePath); // Delete the source file after copying, if it's move mode
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Method to get a unique file name by adding a suffix if a file with the same name already exists
        /// </summary>
        /// <param name="folderPath">
        /// The path to the folder where to check for a file with a unique name
        /// </param>
        /// <param name="fileName"> The file name for which to get a unique name </param>
        /// <returns> The unique file name </returns>
        private static string GetUniqueFileName(string folderPath, string fileName)
        {
            var uniqueFileName = fileName;
            var counter = 1;

            // Check if a file with the same name exists in the folder
            while (File.Exists(Path.Combine(folderPath, uniqueFileName)))
            {
                // If a file with the same name already exists, add a suffix (e.g., "(1)", "(2)", etc.)
                var extension = Path.GetExtension(fileName);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                // Form the unique file name with a suffix
                uniqueFileName = $"{fileNameWithoutExtension} ({counter}){extension}";

                // Increase the counter for the next iteration to add a new suffix if a file with
                // the same name exists again
                counter++;
            }

            return uniqueFileName;
        }

        private void OnProgressChanged(int totalFiles, int processedFiles)
        {
            var progressReport = new ProgressReport
            {
                TotalFiles = totalFiles,
                ProcessedFiles = processedFiles
            };

            ProgressChanged?.Invoke(this, progressReport);
        }
    }
}