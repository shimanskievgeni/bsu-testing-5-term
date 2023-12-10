using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Execution.Compiled;

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
    Call,
    ExpressionEnd // really nop, needed in IF and WHILE w/o (..)
}

public class Token
{
    public TokenType Type { get; private set; }

    public Token(TokenType type)
    { Type = type; }
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
        Operation = value;
    }

}

public class TokenVar : Token
{
    public string name { get; private set; }
    public VariableDef def { get; private set; }

    public TokenVar(string name, VariableDef def, TokenType type) : base(type)
    {
        this.name = name;
        this.def = def;
    }

}

public class TokenGoto : Token
{
    //public readonly Token ToToken;
    public int toToken;

    public TokenGoto(TokenType type, int toToken) : base(type)
    {
        this.toToken = toToken;
    }
}

public class CompiledCode
{
    public readonly IList<Token> tokens = new List<Token>();

    public int LastIndex { get => tokens.Count - 1; }
    public void AddReturn()
    {
        tokens.Add(new Token(TokenType.Ret));
    }
    public void AddExpressionEnd()
    {
        tokens.Add(new Token(TokenType.ExpressionEnd));
    }
    public void AddGoto(int toToken)
    {
        tokens.Add(new TokenGoto(TokenType.Goto, toToken));
    }
    public void AddGotoIf(int toToken)
    {
        tokens.Add(new TokenGoto(TokenType.GotoIf, toToken));
    }
    public void AddOperation(string operation)
    {
        tokens.Add(new TokenOperation(operation));
    }

    public void AddGlobalVarValue(string name, VariableDef def)
    {
        tokens.Add(new TokenVar(name, def, TokenType.ValueGlobalVar));
    }

    public void AddRefGlobalVar(string name, VariableDef def)
    {
        tokens.Add(new TokenVar(name, def, TokenType.RefGlobalVar));
    }
    public void AddString(string value)
    {
        tokens.Add(new TokenConstant<string>(value, ExpressionType.Str));
        //this.tokens.Add(new TokenString(value));
    }

    public void AddInt(int value)
    {
        tokens.Add(new TokenConstant<int>(value, ExpressionType.Int));
        //this.tokens.Add(new TokenInt(value));
    }

    public void AddDouble(double value)
    {
        tokens.Add(new TokenConstant<double>(value, ExpressionType.Double));
        //this.tokens.Add(new TokenDouble(value));
    }
    public void AddBool(bool value)
    {
        tokens.Add(new TokenConstant<bool>(value, ExpressionType.Bool));
        //this.tokens.Add(new TokenBool(value));
    }
}

