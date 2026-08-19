using System;
using UnityEngine;

namespace com.ctn_originals.unity_drawif_attributes
{
    public enum ComparisonType
    {
        Equals = 0,
        NotEqual = 1,
        GreaterThan = 2,
        SmallerThan = 3,
        SmallerOrEqual = 4,
        GreaterOrEqual = 5
    }

    public enum DisablingType
    {
        ReadOnly = 2,
        DontDraw = 3
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class DrawIfAttribute : PropertyAttribute
{
    public string ComparedPropertyName { get; private set; }
    public object ComparedValue { get; private set; }
    public com.ctn_originals.unity_drawif_attributes.ComparisonType ComparisonType { get; private set; }
    public com.ctn_originals.unity_drawif_attributes.DisablingType DisablingType { get; private set; }

    public DrawIfAttribute(string comparedPropertyName, object comparedValue, com.ctn_originals.unity_drawif_attributes.ComparisonType comparisonType = com.ctn_originals.unity_drawif_attributes.ComparisonType.Equals, com.ctn_originals.unity_drawif_attributes.DisablingType disablingType = com.ctn_originals.unity_drawif_attributes.DisablingType.DontDraw)
    {
        ComparedPropertyName = comparedPropertyName;
        ComparedValue = comparedValue;
        ComparisonType = comparisonType;
        DisablingType = disablingType;
    }
}
