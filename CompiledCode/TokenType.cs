using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace CompiledCode;

public enum TokenType
{
    Goto,
    GotoIf,
    Operation,
    Constant,
    //ConstantInt,
    //ConstantDouble,
    //ConstantString,
    RefGlobalVar,
    RefLocalVar,
    ValueGlobalVar,
    ValueLocalVar,
    Ret,
    Call
}

public class Token
{
    public TokenType Type { get; private set; }

    public Token(TokenType type)
    { this.Type = type; }
}

/****
public class TokenConstant<T> : Token
{
    private readonly T? value;
    private readonly ExpressionType valueType;

    public TokenConstant(T value, ExpressionType valueType) : base(TokenType.Constant)
    {
        this.value = value;
        this.valueType = valueType;
    }
}
****/

public class TokenConstant : Token
{
    public readonly ExpressionType valueType;

    public TokenConstant(ExpressionType valueType) : base(TokenType.Constant)
    {
        this.valueType = valueType;
    }
}
public class TokenInt : TokenConstant
{
    public readonly int value;
    public TokenInt(int value) : base(ExpressionType.Int) => this.value = value; 
}

public class TokenDouble : TokenConstant
{
    public readonly double value;

    public TokenDouble(double value) : base(ExpressionType.Double) => this.value = value; 
}

public class TokenString : TokenConstant
{
    public readonly string value;

    public TokenString(string value) : base(ExpressionType.Str) => this.value = value; 
}

public class TokenBool : TokenConstant
{
    public readonly bool value;

    public TokenBool(bool value) : base(ExpressionType.Bool) => this.value = value;
}


public class TokenOperation : Token
{
    public string Operation { get; private set; }

    public TokenOperation(string value) : base(TokenType.Operation)
    {
        this.Operation = value;
    }
}

public class TokenGoto: Token
{
    //public readonly Token ToToken;
    public readonly int ToToken;

    public TokenGoto(TokenType type, int ToToken) : base(type)
    {
        this.ToToken = ToToken;
    }
}

public class CompiledCode
{
    public readonly IList<Token> tokens = new List<Token>();

    public void AddGoto(int ToToken)
    {
        this.tokens.Add(new TokenGoto(TokenType.Goto, ToToken));
    }
    public void AddGotoIf(int ToToken)
    {
        this.tokens.Add(new TokenGoto(TokenType.GotoIf, ToToken));
    }
    public void AddOperation(string operation)
    {
        this.tokens.Add(new TokenOperation(operation));
    }

    public void AddString(string value)
    {
        //this.tokens.Add(new TokenConstant<string>(value, ExpressionType.Str));
        this.tokens.Add(new TokenString(value));
    }

    public void AddInt(int value)
    {
        //this.tokens.Add(new TokenConstant<int>(value, ExpressionType.Int));
        this.tokens.Add(new TokenInt(value));
    }

    public void AddDouble(double value)
    {
        //this.tokens.Add(new TokenConstant<double>(value, ExpressionType.Double));
        this.tokens.Add(new TokenDouble(value));
    }
    public void AddBool(bool value)
    {
        //this.tokens.Add(new TokenConstant<double>(value, ExpressionType.Double));
        this.tokens.Add(new TokenBool(value));
    }
}

