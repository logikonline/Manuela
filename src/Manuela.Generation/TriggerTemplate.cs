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
        Condition = new(IsActive)
        {{
            Triggers = {map.VisualElementParameterName} =>
            {{
                 var t = new HashSet<System.ComponentModel.INotifyPropertyChanged>();

                //var dummyCondition = IsActive({map.VisualElementParameterName});


                return {AsTriggers(map)}
            }}
        }};
    }}

    private static T IsNotifier<T>(
        T notifier,
        HashSet<INotifyPropertyChanged> hashSet)
            where T : INotifyPropertyChanged
    {{
        _ = hashSet.Add(notifier);
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
