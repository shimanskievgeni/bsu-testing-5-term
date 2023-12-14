namespace Execution.Compiled;

public class FuncDef
{
    public int CodeIndex { get; set; }

    public Dictionary<string, VariableDef> localVariables = new();

    public FuncDef()
    {
        Name = "Undefined";
        ParamCount = 0;
    }

    public FuncDef(string name)
    {
        Name = name;
    }

    public string Name
    {
        set;
        get;
    }

    public int ParamCount
    {
        set;
        get;
    }
}
