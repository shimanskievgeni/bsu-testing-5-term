namespace SyntaxAnalyze;

public class ParseResult
{
    public ParseResult()
    {
        this.Name = null;
    }

    public ParseResult(string name)
    {
        this.Name = name;
        this.Type = ExpressionType.Undefined;
    }

    public ParseResult(string? name, ExpressionType type, double? value, string? operation)
    {
        this.Name = name;
        this.Type = type;
        this.Value = value;
        this.Operation = operation;
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

    public string? Operation
    {
        get;
        set;
    }

    public ParseResult Clone()
    {
        return new ParseResult(this.Name, this.Type, this.Value, this.Operation);
    }
}