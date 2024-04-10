﻿using System.ComponentModel;
using System.Diagnostics;
using Manuela.Expressions;
using Manuela.Things;

namespace Manuela.Styling.ConditionalStyles;

[ContentProperty(nameof(Setters))]
public class ConditionalStyle
{
    private ManuelaSettersDictionary? _setters;
    private XamlCondition? _condition;

    public ManuelaSettersDictionary? Setters
    {
        set
        {
            if (_setters is not null)
                foreach (var visualElement in InitializedElements)
                    ClearValues(visualElement, _setters.Keys);

            _setters = value; ReApply();
        }
    }

    protected internal XamlCondition Condition
    {
        get => _condition ?? throw new Exception("Manuela was not able to find the state condition.");
        set { _condition = value; ReApply(); }
    }

    // initialization must be per visual.
    // to avoid a possible issue when a resource is shared using x:StaticResource.
    public HashSet<VisualElement> InitializedElements { get; } = [];

    public void Initialize(VisualElement visual)
    {
        if (InitializedElements.Contains(visual)) return;

        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnThemeChanged;

        OnInitialized(visual);

        // a "trigger" is an object that has at least 2 things:
        //   1. An INotifyPropertyChanged object.
        //   2. A HashSet containing the property names in the INPC object that fire an update
        // finally we attach a handler to each INPC and listen for changes in the target properties.

        foreach (var trigger in Condition.Triggers)
        {
            // null notifier are valid. the user has the freedom to pass a null intance.
            if (trigger.Notifier is null) continue;

            // save a reference to the handler, this handler has a capture on the "visual" reference.
            // this way we should be able to unsubscribe from the PropertyChanged event when the style is disposed.

            trigger.NotifierHandler = (sender, e) =>
            {
                if (e.PropertyName is null || !trigger.Properties.Contains(e.PropertyName))
                    return;

                // at this point we know that a property that was declared as a trigger has changed.
                Apply(visual);
            };

            trigger.Notifier.PropertyChanged += trigger.NotifierHandler;
        }

        _ = InitializedElements.Add(visual);
        Apply(visual);
    }

    public virtual ManuelaSettersDictionary? GetSetters()
    {
        return _setters;
    }

    public void Apply(VisualElement? visual)
    {
        if (visual is null || !InitializedElements.Contains(visual)) return;

        var keys = GetSetters()?.Keys;
        if (keys is null) return;

        var allStyles = (StatesCollection?)visual.GetValue(Has.StatesProperty);
        var transitions = (TransitionsCollection?)visual.GetValue(Has.TransitionsProperty);

        foreach (var property in keys)
        {
            var bindableProperty = ManuelaThings.GetBindableProperty(visual, property);

            if (bindableProperty is null)
            {
#if DEBUG
                Trace.WriteLine($"Property {property} is not supported on {visual.GetType().Name}");
#endif
                continue;
            }

            var conditionMet = ApplyPropertyIfMet(visual, property, bindableProperty, transitions);

            if (!conditionMet)
            {
                var anyOtherStateMet = allStyles?.ApplyPropertyIfMet(visual, property, bindableProperty, transitions)
                    ?? false;

                if (!anyOtherStateMet)
                    visual.ClearValue(bindableProperty);
            }
        }
    }

    public bool ApplyPropertyIfMet(
        VisualElement visual,
        ManuelaProperty property,
        BindableProperty bindableProperty,
        TransitionsCollection? transitions)
    {
        var setters = GetSetters();

        if (setters is null || !setters.TryGetValue(property, out var value)) return false;
        if (!Condition?.Predicate(visual) ?? false) return false;

        value = ManuelaThings.TryConvert(visual, property, value);

        if (                                                                                    // do not animate if:
            value is not null &&                                                                // target value is null
            transitions is not null &&                                                          // there are no transitions
            transitions.TryGetValue(visual, property, out var transition, out var isFirst) &&   // the property does not have a transition
            !isFirst                                                                            // is the first time the property is being set
            )
        {
            var animation = ManuelaThings.GetAnimation(visual, bindableProperty, value);
            animation.Commit(visual, $"{property} animation", easing: transition.Easing, length: transition.Duration);
        }
        else
        {
            visual.SetValue(bindableProperty, value);
        }

        return true;
    }

    public void ClearValues(VisualElement visualElement, Dictionary<ManuelaProperty, object?>.KeyCollection? keys = null)
    {
        keys ??= GetSetters()?.Keys;
        if (keys is null) return;

        foreach (var key in keys)
        {
            var bindableProperty = ManuelaThings.GetBindableProperty(visualElement, key);
            if (bindableProperty is null) continue;

            visualElement.SetValue(bindableProperty, bindableProperty.DefaultValue);
        }
    }

    public virtual void Dispose()
    {
        foreach (var visualElement in InitializedElements)
            ClearValues(visualElement);

        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged -= OnThemeChanged;

        foreach (var trigger in Condition.Triggers)
        {
            if (trigger.Notifier is null) continue;
            trigger.Notifier.PropertyChanged -= trigger.NotifierHandler;
        }

        Condition = null!;
        InitializedElements.Clear();
    }

    protected void ReApply()
    {
        foreach (var visualElement in InitializedElements)
            Apply(visualElement);
    }

    protected virtual void OnInitialized(VisualElement visualElement) { }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        ReApply();
    }
}
