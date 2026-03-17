using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data;

namespace VRCFaceTracking.Linux.ViewModels;

public partial class CalibrationViewModel : ObservableObject
{
    public ObservableCollection<MutationViewModel> Mutations { get; }

    public CalibrationViewModel(UnifiedTrackingMutator mutator, ILogger<CalibrationViewModel> logger)
    {
        logger.LogDebug("[Calibration] Building ViewModel wrappers for {Count} mutations",
            mutator._mutations.Count);

        Mutations = new ObservableCollection<MutationViewModel>(
            mutator._mutations.Select(m => new MutationViewModel(m, logger))
        );
    }
}
