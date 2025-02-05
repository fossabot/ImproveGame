﻿namespace ImproveGame.UIFramework;

public static class LayerName
{
    public static class Vanilla
    {
        public const string RadialHotbars = "Radial Hotbars";
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class AutoCreateGUIAttribute(string layerName, string ownName, int priority = 0) : Attribute
{
    public string LayerName = layerName;
    public string OwnName = ownName;
    public int Priority = priority;
}
