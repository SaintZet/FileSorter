using FileSorter.Contracts;
using FileSorter.Enums;
using FileSorter.Models;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private readonly IFolderDialogService _dialogService;
        private readonly IFileSorterService _fileSorterService;

        public MainWindowViewModel(IFolderDialogService dialogService, IFileSorterService fileSorterService)
        {
            _dialogService = dialogService;

            _fileSorterService = fileSorterService;
            _fileSorterService.ProgressChanged += (sender, progress) => FileSorterService_ProgressChanged(sender!, progress);

            _progressBarVisible = false;
            _progressStatus = "Waiting";

            _rules = new List<RuleModel>()
            {
                new RuleModel(new[]{".jpg", ".jpeg", ".png"}, "Photo"),
                new RuleModel(new[]{".mp4", ".avi"}, "Video"),
                new RuleModel(new[]{".gif"}, "Gif"),
                new RuleModel(new[]{".mp3"}, "Music"),
                new RuleModel(new[]{".zip", ".rar"}, "Archives"),
                new RuleModel(new[]{".doc", ".docs", ".pdf"}, "Documents"),
            };

            OpenFolderDialogWindowCommand = new RelayCommand<string>(execute: OpenFolderDialogWindow);

            StartSortFilesCommand = new RelayCommand(
                execute: async () => await SortFilesAsync(),
                canExecute: () => !string.IsNullOrEmpty(SourcePath) && !string.IsNullOrEmpty(DestinationPath)
                );

            CancelSortFilesCommand = new RelayCommand(execute: CancelSortFiles);
        }

        #region Status bar

        private int _totalFiles;
        private int _processedFiles;
        private double _progressPercentage;
        private string _progressStatus;
        private bool _progressBarVisible;

        public bool ProgressBarVisible
        {
            get { return _progressBarVisible; }
            set { Set(ref _progressBarVisible, value); }
        }
        public string ProgressStatus
        {
            get { return _progressStatus; }
            set { Set(ref _progressStatus, value); }
        }

        public int TotalFiles
        {
            get { return _totalFiles; }
            set { Set(ref _totalFiles, value); }
        }

        public int ProcessedFiles
        {
            get { return _processedFiles; }
            set
            {
                Set(ref _processedFiles, value);
                CalculateProgressPercentage();
            }
        }

        public double ProgressPercentage
        {
            get { return _progressPercentage; }
            set { Set(ref _progressPercentage, value); }
        }

        private void CalculateProgressPercentage()
        {
            if (TotalFiles == 0)
                ProgressPercentage = 0;
            else
                ProgressPercentage = (double)ProcessedFiles / TotalFiles * 100;
        }

        private void FileSorterService_ProgressChanged(object sender, ProgressReport progressReport)
        {
            TotalFiles = progressReport.TotalFiles;
            ProcessedFiles = progressReport.ProcessedFiles;
        }

        #endregion Status bar

        #region Start button

        private bool _startButtonVisible = true;

        public bool StartButtonVisible
        {
            get { return _startButtonVisible; }
            set { Set(ref _startButtonVisible, value); }
        }

        public RelayCommand StartSortFilesCommand { get; }

        private async Task SortFilesAsync()
        {
            StartButtonVisible = false;
            CancelButtonVisible = true;

            ProgressBarVisible = true;
            ProgressStatus = "Sorting files...";

            var cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await _fileSorterService.SortFilesAndMoveByRulesAsync(_sourcePath, _rules, _destinationPath, cancellationTokenSource.Token);

                ProgressBarVisible = false;
                ProgressStatus = "File sorting completed!";
                ProgressPercentage = 0;

                StartButtonVisible = true;
                CancelButtonVisible = false;
            }
            catch (OperationCanceledException)
            {
                ProgressStatus = "File sorting canceled!";
            }
        }

        #endregion Start button

        #region Cancel button

        private bool _cancelButtonVisible = false;

        public bool CancelButtonVisible
        {
            get { return _cancelButtonVisible; }
            set { Set(ref _cancelButtonVisible, value); }
        }

        public RelayCommand CancelSortFilesCommand { get; }

        private void CancelSortFiles()
        {
            StartButtonVisible = true;
            CancelButtonVisible = false;

            ProgressBarVisible = false;

            _fileSorterService.CancelSort();
        }

        #endregion Cancel button

        #region File system

        private string _sourcePath = string.Empty;
        private string _destinationPath = string.Empty;

        public string SourcePath
        {
            get { return _sourcePath; }
            set { Set(ref _sourcePath, value); }
        }

        public string DestinationPath
        {
            get { return _destinationPath; }
            set { Set(ref _destinationPath, value); }
        }

        public RelayCommand<string> OpenFolderDialogWindowCommand { get; }

        private void OpenFolderDialogWindow(string propertyName)
        {
            var selectedFolderPath = _dialogService.ShowFolderDialog();

            if (selectedFolderPath == null)
                return;

            if (!string.IsNullOrEmpty(propertyName))
            {
                Type type = GetType();
                PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;

                propertyInfo?.SetValue(this, selectedFolderPath);
            }
        }

        #endregion File system

        #region Settings

        private List<RuleModel> _rules;
        private FileConflictResolution _selectedConflictResolution;
        private FileMoveMode _selectedMoveMode;

        public List<RuleModel> Rules
        {
            get => _rules;
            set { Set(ref _rules, value); }
        }

        public FileConflictResolution SelectedConflictResolution
        {
            get => _selectedConflictResolution;
            set
            {
                Set(ref _selectedConflictResolution, value);
                _fileSorterService.ConflictResolution = value;
            }
        }

        public FileMoveMode SelectedMoveMode
        {
            get => _selectedMoveMode;
            set
            {
                Set(ref _selectedMoveMode, value);
                _fileSorterService.MoveMode = value;
            }
        }

        public static Array FileConflictResolutions => Enum.GetValues(typeof(FileConflictResolution));
        public static Array FileMoveModes => Enum.GetValues(typeof(FileMoveMode));

        #endregion Settings
    }
}