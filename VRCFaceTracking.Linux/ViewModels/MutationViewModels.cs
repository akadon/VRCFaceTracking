using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.Core.Params.Data.Mutation;

namespace VRCFaceTracking.Linux.ViewModels;

// ── property row (bool / float / string) ──────────────────────────────────
public partial class MutationPropertyViewModel : ObservableObject
{
    private readonly MutationProperty _prop;
    private readonly ILogger _log;

    public string Name => _prop.Name;
    public MutationPropertyType Type => _prop.Type;
    public double Min => _prop.Min;
    public double Max => _prop.Max;

    // Bool (CheckBox)
    public bool BoolValue
    {
        get => _prop.Value is bool b && b;
        set
        {
            if (BoolValue == value) return;
            _log.LogDebug("[Calibration] {Name} → {Value}", Name, value);
            _prop.Value = value;
            OnPropertyChanged();
        }
    }

    // Float (Slider / NumericUpDown)
    public double FloatValue
    {
        get => _prop.Value is float f ? (double)f : 0.0;
        set
        {
            var fval = (float)value;
            if (Math.Abs((float)FloatValue - fval) < 1e-6f) return;
            _log.LogDebug("[Calibration] {Name} → {Value:F3}", Name, fval);
            _prop.Value = fval;
            OnPropertyChanged();
        }
    }

    // String (TextBox)
    public string StringValue
    {
        get => _prop.Value?.ToString() ?? string.Empty;
        set
        {
            if (StringValue == value) return;
            _log.LogDebug("[Calibration] {Name} → \"{Value}\"", Name, value);
            _prop.Value = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public MutationPropertyViewModel(MutationProperty prop, ILogger log)
    {
        _prop = prop;
        _log = log;
        // Propagate changes from the underlying property (e.g. reset)
        prop.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MutationProperty.Value))
            {
                OnPropertyChanged(nameof(BoolValue));
                OnPropertyChanged(nameof(FloatValue));
                OnPropertyChanged(nameof(StringValue));
            }
        };
    }
}

// ── range row (two-handle min/max) ────────────────────────────────────────
public partial class MutationRangeViewModel : ObservableObject
{
    private readonly MutationRangeProperty _range;
    private readonly ILogger _log;

    public string Name => _range.Name;
    public double Min => _range.Min;
    public double Max => _range.Max;

    public double RangeStart
    {
        get => _range.Item1;
        set
        {
            var f = (float)value;
            if (Math.Abs(_range.Item1 - f) < 1e-6f) return;
            _log.LogDebug("[Calibration] {Name} start → {Value:F3}", Name, f);
            _range.Item1 = f;
            OnPropertyChanged();
        }
    }

    public double RangeEnd
    {
        get => _range.Item2;
        set
        {
            var f = (float)value;
            if (Math.Abs(_range.Item2 - f) < 1e-6f) return;
            _log.LogDebug("[Calibration] {Name} end → {Value:F3}", Name, f);
            _range.Item2 = f;
            OnPropertyChanged();
        }
    }

    public MutationRangeViewModel(MutationRangeProperty range, ILogger log)
    {
        _range = range;
        _log = log;
        range.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MutationRangeProperty.Item1)) OnPropertyChanged(nameof(RangeStart));
            if (e.PropertyName == nameof(MutationRangeProperty.Item2)) OnPropertyChanged(nameof(RangeEnd));
        };
    }
}

// ── action button ─────────────────────────────────────────────────────────
public partial class MutationActionViewModel : ObservableObject
{
    public string Name { get; }

    [RelayCommand]
    private void Execute()
    {
        _log.LogInformation("[Calibration] Action → {Name}", Name);
        _action.Execute(null);
    }

    private readonly MutationAction _action;
    private readonly ILogger _log;

    public MutationActionViewModel(MutationAction action, ILogger log)
    {
        _action = action;
        _log = log;
        Name = action.Name;
    }
}

// ── one mutation section ───────────────────────────────────────────────────
public partial class MutationViewModel : ObservableObject
{
    private readonly TrackingMutation _mutation;
    private readonly ILogger _log;

    public string Name => _mutation.Name;
    public string Description => _mutation.Description;

    public bool IsActive
    {
        get => _mutation.IsActive;
        set
        {
            if (_mutation.IsActive == value) return;
            _log.LogInformation("[Calibration] {Name} enabled → {Value}", Name, value);
            _mutation.IsActive = value;
            OnPropertyChanged();
        }
    }

    // Typed component wrappers — no more object bindings
    public ObservableCollection<object> Components { get; }

    public MutationViewModel(TrackingMutation mutation, ILogger log)
    {
        _mutation = mutation;
        _log = log;

        mutation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TrackingMutation.IsActive))
                OnPropertyChanged(nameof(IsActive));
        };

        Components = new ObservableCollection<object>(
            mutation.Components.Select<IMutationComponent, object>(c => c switch
            {
                MutationAction a     => new MutationActionViewModel(a, log),
                MutationRangeProperty r => new MutationRangeViewModel(r, log),
                MutationProperty p   => new MutationPropertyViewModel(p, log),
                _                    => c
            })
        );
    }
}
