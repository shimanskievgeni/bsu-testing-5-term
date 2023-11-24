namespace SyntaxAnalyze;

public class FuncDef
{
    public FuncDef()
    {
        this.Name = null;
    }

    public FuncDef(string name)
    {
        this.Name = name;
        this.Type = ExpressionType.Undefined;
    }

    public FuncDef(string? name, ExpressionType type)
    {
        this.Name = name;
        this.Type = type;
    }

    public string? Name
    {
        get;
        set;
    }

    public ExpressionType Type
    {
        get;
        set;
    }


    public Dictionary<string, VariableDef> localVariables = new();

    public FuncDef Clone()
    {
        return new FuncDef(this.Name, this.Type);
    }
}