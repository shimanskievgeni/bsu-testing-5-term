namespace SyntaxAnalyze;

public class VariableDef
{
    public VariableDef()
    {
        this.Name = null;
    }

    public VariableDef(string name)
    {
        this.Name = name;
        this.Type = ExpressionType.Undefined;
    }

    public VariableDef(string? name, ExpressionType type, double? value)
    {
        this.Name = name;
        this.Type = type;
        this.Value = value;
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

    public double? Value
    {
        get;
        set;
    }

    public VariableDef Clone()
    {
        return new VariableDef(this.Name, this.Type, this.Value);
    }
}