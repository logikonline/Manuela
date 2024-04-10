﻿using System.Text;
using Microsoft.CodeAnalysis;

namespace Manuela.Generation;

public class TriggerTemplate
{
    public static void Generate(SourceProductionContext context, TriggersMap map)
    {
        context.AddSource(
            $"{map.ContainingTypeName}.g.cs",
            @$"// <auto-generated />
using System.ComponentModel;

namespace {map.ContainingTypeNamespace};

public partial class {map.ContainingTypeName}
{{
    protected override void OnInitialized()
    {{
        Condition = new Manuela.Expressions.XamlCondition(IsActive);
        var triggers = new Dictionary<INotifyPropertyChanged, Manuela.Expressions.Trigger>();
        var evaluation = GetNotifiers(visual, triggers);
        Condition.Triggers = [.. triggers.Values];
    }}

    private static T Notify<T>(
        T notifier,
        string propertyName,
        Dictionary<INotifyPropertyChanged, Manuela.Expressions.Trigger> triggers)
            where T : INotifyPropertyChanged
    {{
        if (!triggers.TryGetValue(notifier, out var trigger))
            triggers.Add(notifier, trigger = new(notifier, [propertyName]));

        _ = trigger.Properties.Add(propertyName);

        return notifier;
    }}
}}
");
    }

    private static string AsTriggers(TriggersMap map)
    {
        var sb = new StringBuilder();

        _ = sb.Append("\r\n                [");

        var notifierCount = 0;
        foreach (var notifier in map.PropertiesByNotifier)
        {
            var notifierName = notifier.Key;

            _ = sb.Append("\r\n                    new(");
            _ = sb.Append(notifierName.PadRight(30));
            _ = sb.Append(", [");

            var depCount = 0;
            foreach (var dependentProperty in notifier.Value)
            {
                _ = sb.Append("\"");
                _ = sb.Append(dependentProperty);
                _ = sb.Append("\"");
                if (depCount < notifier.Value.Count - 1) _ = sb.Append(", ");

                depCount++;
            }

            _ = sb.Append("])");
            if (notifierCount < map.PropertiesByNotifier.Count - 1) _ = sb.Append(",");

            notifierCount++;
        }

        _ = sb.Append("\r\n                ];");

        return sb.ToString();
    }
}
