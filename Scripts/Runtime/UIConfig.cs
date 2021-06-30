using System;

public class UIConfig : Attribute
{
    public string Prefab { get; set; }
    public int Layer { get; set; }
}