using FileSorter.Enums;
using FileSorter.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter.Contracts
{
    public interface IFileSorterService
    {
        event EventHandler<ProgressReport> ProgressChanged;

        FileConflictResolution ConflictResolution { get; set; }
        FileMoveMode MoveMode { get; set; }

        Task SortFilesAndMoveByRulesAsync(string directoryPath, List<RuleModel> rules, string destinationPath, CancellationToken cancellationToken);

        void CancelSort();
    }
}