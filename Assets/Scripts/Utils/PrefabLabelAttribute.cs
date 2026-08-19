using System;

[AttributeUsage(AttributeTargets.Class)]
public class PrefabLabelAttribute : Attribute
{
    public PrefabLabelAttribute(string label)
    {
        Label = label;
    }

    public string Label { get; }
}