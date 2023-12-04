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

public class TokenConstantType : Token
{
    public readonly ExpressionType valueType;

    public TokenConstantType(ExpressionType valueType) : base(TokenType.Constant)
    {
        this.valueType = valueType;
    }
}

/****/
public class TokenConstant<T> : TokenConstantType
{
    public readonly T? value;

    public TokenConstant(T value, ExpressionType valueType) : base(valueType)
    {
        this.value = value;
    }
}
/****/

/****
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
****/

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
    public readonly int toToken;

    public TokenGoto(TokenType type, int toToken) : base(type)
    {
        this.toToken = toToken;
    }
}

public class CompiledCode
{
    public readonly IList<Token> tokens = new List<Token>();

    public void AddGoto(int toToken)
    {
        this.tokens.Add(new TokenGoto(TokenType.Goto, toToken));
    }
    public void AddGotoIf(int toToken)
    {
        this.tokens.Add(new TokenGoto(TokenType.GotoIf, toToken));
    }
    public void AddOperation(string operation)
    {
        this.tokens.Add(new TokenOperation(operation));
    }

    public void AddString(string value)
    {
        this.tokens.Add(new TokenConstant<string>(value, ExpressionType.Str));
        //this.tokens.Add(new TokenString(value));
    }

    public void AddInt(int value)
    {
        this.tokens.Add(new TokenConstant<int>(value, ExpressionType.Int));
        //this.tokens.Add(new TokenInt(value));
    }

    public void AddDouble(double value)
    {
        this.tokens.Add(new TokenConstant<double>(value, ExpressionType.Double));
        //this.tokens.Add(new TokenDouble(value));
    }
    public void AddBool(bool value)
    {
        this.tokens.Add(new TokenConstant<bool>(value, ExpressionType.Bool));
        //this.tokens.Add(new TokenBool(value));
    }
}

