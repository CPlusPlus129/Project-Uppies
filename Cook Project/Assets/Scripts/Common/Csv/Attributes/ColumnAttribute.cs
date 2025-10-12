using System;

[AttributeUsage(AttributeTargets.Field)]
public class ColumnAttribute : Attribute
{
    public string name;
    public ColumnAttribute(string name) => this.name = name;
}

