using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Data.Mutation;

namespace VRCFaceTracking.Linux.ViewModels;

public partial class CalibrationViewModel : ObservableObject
{
    public ObservableCollection<TrackingMutation> Mutations { get; }

    public CalibrationViewModel(UnifiedTrackingMutator mutator)
    {
        Mutations = mutator._mutations;
    }
}
