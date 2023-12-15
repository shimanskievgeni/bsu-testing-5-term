namespace Execution.Compiled;

public class FuncDef
{
    public int CodeIndex { get; set; }

    public Dictionary<string, VariableDef> localVariables = new();

    public FuncDef()
    {
        //Name = ""; // "Undefined";
        ParamCount = 0;
    }
    public FuncDef(string name)
    {
        Name = name;
    }
    public string? Name  {  set;  get;  }
    public int ParamCount { set; get; }
    public int LocalCount { get => localVariables.Count - ParamCount; }

    public VariableDef? AddLocalVariable(string name, bool isParameter = false)
    {
        if (localVariables.ContainsKey(name))
            return null;
        var def = new LocalVariableDef(localVariables.Count, isParameter);
        if (!localVariables.TryAdd(name, def))
            return null;
        if (isParameter) { ParamCount++; }
        return def;
    }
    public VariableDef? AddParameterVariable(string name)
    {
        return AddLocalVariable(name, true);
    }
}
