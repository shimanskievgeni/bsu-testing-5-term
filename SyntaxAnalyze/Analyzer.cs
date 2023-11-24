using System.Text;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace SyntaxAnalyze;

public class Analyzer
{
    private static readonly char[] BlankSymbols = { ' ', '\n', '\t', '\r' };
    private static readonly char[] EscapedSymbols = { 'n', 't', 'r', '\\', '\'' };
    private const string SingleLineComment = "//";

    private readonly string expression;
    private int position;
    private readonly Dictionary<string, VariableDef> variables = new();
    private readonly Dictionary<string, FuncDef> functions = new();
    private string? _funcName;

    public Analyzer(string expression)
    {
        this.expression = expression;
        this.position = 0;
    }

    public bool Parse()
    {
        position = 0;
        _funcName = null;

        bool f;

        // declare global vars
        do 
        {
            f = ParseVar();
        }
        while (f);

        do
        {
            f = ParseFunction();
        }
        while (f);

        // top level statements
        ParseOperators();
        
        f = EndCode();
        return f;
    }

    public bool ParseVar()
    {
        if (!ParseWordAndBlank("var"))
        {
            return false;
        }

        do
        {
            if (!ParseAssigment(true))
            {
                throw new InvalidOperationException();
            }
        } while (ParseChar(','));

        if (!ParseChar(';'))
        {
            throw new InvalidOperationException();
        }
        return true;
    }

    public bool ParseOperators()
    {
        bool f;

        do
        {
            f = ParseAssigment();
        }
        while (f);

        f = EndCode();
        return f;
    }


    private bool ParseFunction()
    {
        SkipBlanks();
        if (!ParseWordAndBlank("function"))
        {
            return false;
        }

        ParseFunctionHeader();
        if (!ParseChar('{'))
        {
            throw new InvalidOperationException();
        }

        ParseOperators();

        if (!ParseChar('}'))
        {
            throw new InvalidOperationException();
        }
        _funcName = null;

        return true;
    }

    private int ParseFunctionHeader()
    {
        /*
        var func = GetVar(funcName);
        if (func.Operation != "func")
        {
            return false;
        }
        */

        string? funcName = ParseVariable();
        if (funcName == null)
        {
            throw new InvalidOperationException();
        }
        _funcName = funcName;

        if (!ParseChar('('))
        {
            throw new InvalidOperationException();
        }

        AddFunc(_funcName);

        int argcount = 0;
        string? name;

        if (!ParseChar(')'))
        {
            do
            {
                name = ParseVariable();
                if (name == null)
                {
                    throw new InvalidOperationException();
                }

                AddFuncVar(name, funcName);

                argcount++;

            } while (ParseChar(','));
        }

        if (!ParseChar(')'))
        {
            throw new InvalidOperationException();
        }
        return argcount;
    }

    private void AddFunc(string name)
    {
        functions.TryAdd(name, new FuncDef(name));
    }

    private FuncDef GetFunc(string name)
    {
        return functions[name];
    }

    private void AddVar(string name, string? funcName = null)
    {
        if (funcName == null)
            variables.TryAdd(name, new VariableDef(name));
        else
            AddFuncVar(name, funcName);
    }

    private void AddFuncVar(string name, string funcName)
    {
        var localVars = functions[funcName].localVariables;  // functions.TryGetValue(funcName, out _);
        localVars.TryAdd(name, new VariableDef(name));
    }

    private VariableDef GetVar(string name, string? funcName)
    {
        if (funcName == null)
            return variables[name];
        else
        {
            var localVars = functions[funcName].localVariables;  // functions.TryGetValue(funcName, out _);
            if (localVars.TryGetValue(name, out VariableDef? v))
                return v;
            else
                return variables[name];
        }
    }


    //-------------------------------------------------------
    private bool EndCode()
    {
        SkipBlanks();

        if (position == expression.Length)
        {
            return true;
        }
        return false;
    }

    public bool IsValidExpression()
    {
        position = 0;

        ParseExpression();

        if (position == expression.Length)
        {
            return true;
        }

        throw new InvalidOperationException();
    }



    private bool ParseExpression()
    {
        if (ParseUnaryOperation())
        {
            if (!ParseOperand())
            {
                throw new InvalidOperationException();
            }
        }
        else if (!ParseOperand())
        {  
            return false; 
        }

        while (expression.Length >= position)
        {
            if (!ParseOperation())
            {
                return false;
            }

            if (!ParseOperand())
            {
                throw new InvalidOperationException();
            }
        }

        return true;
    }

    private void SkipBlanks()
    {
        while (position < expression.Length && BlankSymbols.Contains(expression[position]))
        {
            position++;
        }

        while (expression.Substring(position).StartsWith(SingleLineComment))
        {
            while (position < expression.Length && expression[position] != '\n')
            {
                position++;
            }

            if (position < expression.Length)
            {
                position++;
            }

            SkipBlanks();
        }
    }

    private char CurrentChar()
    {
        if (position < expression.Length)
            return expression[position];
        else 
            return '\0';
    }

    private bool ParseString()
    {
        SkipBlanks();

        bool Escape = true;

        if (ParseChar('@'))
        {
            Escape = false;

            if (!ParseChar('\''))
            {
                throw new InvalidOperationException();
            }
        }
        else
        {
            if (!ParseChar('\''))
            {
                return false;
            }
        }

        int pos1 = position;

        while (!EndCode()) // don't use ParseChar() or SkipBlanks() here, they skip comments!
        {
            if (Escape && CurrentChar() == '\\')
            {
                position++;

                if (position >= expression.Length)
                    throw new InvalidOperationException();

                if (!EscapedSymbols.Contains(CurrentChar()))
                {
                    throw new InvalidOperationException();
                }
            }
            else if (CurrentChar() == '\'')
            {
                position++;
                return true;
            }

            position++;
        }

        throw new InvalidOperationException();
    }


    // is used also in var declaration
    private bool ParseAssigment(bool varOnly = false)
    {
        string? name = ParseVariable();

        if (name == null)
        {
            return false;
        }

        if (!ParseChar('='))
        {
            if (varOnly)
            {
                AddVar(name, _funcName);
                return true;
            }
            return false;
        }

        ParseExpression();

        if (!varOnly && !ParseChar(';'))
        {
            throw new InvalidOperationException();
        }

        AddVar(name, _funcName);

        return true;
    }



    //--------------------------------------------------
    private bool ParseUnaryOperation()
    {
        SkipBlanks();
        int p1 = position;
        if (
              ParseChars("!")
           || ParseChars("-")
           )
        {
            ;
        }
        else
        {
            return false;
        }
        var operation = expression[p1..position];
        return true;
    }

        private bool ParseOperation() // Binary
    {
        SkipBlanks();
        int p1 = position;
        if (
              ParseChars("==")
           || ParseChars("!=")
           || ParseChars("<=")
           || ParseChars(">=")
           || ParseChars("&&")
           || ParseChars("||")
           || ParseChars("<")
           || ParseChars(">")
           )
        {
            ;
        }
        //else if (
        //      ParseChars("++")
        //   || ParseChars("--")
        //   )
        //{
        //    ;
        //}
        else if (
              ParseChars("+")
           || ParseChars("-")
           || ParseChars("*")
           || ParseChars("/")
           || ParseChars("%")
           )
        {
            ;
        }
        else
        {
            return false;
        }
        var operation = expression[p1..position];
        return true;

        //SkipBlanks();
        //if (position < expression.Length && Validators.IsOperation(expression[position]))
        //{
        //    position++;
        //    return true;
        //}
        //return false;
    }

    private static ExpressionType ResultingOperationType(string operation, ExpressionType type1, ExpressionType type2)
    {
        if (type1 == type2)
        {
            if (operation == "=="
             || operation == "!="
             || operation == "<="
             || operation == ">="
             || operation == "<"
             || operation == ">"
                )
            {
                return ExpressionType.Bool;
            }
             
            if (operation == "+") // plus or string concat   
            {
                return type1;
            }
        }

        if (type1 == ExpressionType.Num)
        {
            if (operation == "++" || operation == "--")
            {
                return type1;
            }

            if (type2 == ExpressionType.Num)
            {
                if (   operation == "+"
                    || operation == "-"
                    || operation == "*"
                    || operation == "/"
                    || operation == "%"
                    )
                {
                    return type1;
                }
            }
        }

        if (type1 == ExpressionType.Bool)
        {   
            if (operation == "!")
            {
                return type1;
            }
            if (type2 == ExpressionType.Bool)
            {
                if (operation == "&&" || operation == "||")
                {
                    return type1;
                }
            }
        }
        
        throw new InvalidOperationException();
    }


    private bool ParseOperand()
    {
        if (ParseChar('('))
        {
            ParseExpression();

            if (!ParseChar(')'))
            {
                throw new InvalidOperationException();
            }

            return true;
        }

        if (ParseString())
        {
            //Type = ExpressionType.Str;
            return true;
        }

        if (ParseNumber())
        {
            //Type = ExpressionType.Num;
            return true;
        }

        string? name = ParseVariable();

        if (name == null)
        {
            throw new InvalidOperationException();
        }

        if (ParseChar('(')) // function call, not var
        {
            if (GetFunc(name) == null)
            {
                throw new InvalidOperationException();
            }
            
            ParseArguments(name);

            if (!ParseChar(')'))
            {
                throw new InvalidOperationException();
            }

            return true;
        }

        if (GetVar(name, _funcName) == null)
        {
            throw new InvalidOperationException();
        }

        return true;
    }

    private bool ParseArguments(string funcName)
    {
        /*
        var func = GetVar(funcName);
        if (func.Operation != "func")
        {
            return false;
        }
        */
        int argcount = 0;

        while (!EndCode()) 
        {
            ParseExpression();

            argcount++;

            // TO DO check argcount
            //if (argcount !=)
            //{
            //    throw new InvalidOperationException();
            //}

            if (!ParseChar(','))
            {
                return false;
            }
        }
        return false;
    }

    private bool ParseChar(char symbol)
    {
        SkipBlanks();

        if (position < expression.Length && expression[position] == symbol)
        {
            position++;
            return true;
        }

        return false;
    }

    private bool ParseChars(string str)
    {
        SkipBlanks();

        if (position < expression.Length && expression[position..].StartsWith(str))
        {
            position += str.Length;
            return true;
        }

        return false;
    }

    private bool ParseWordAndBlank(string str)
    {
        // ParseWordAndBlank('var')
        //  var x;  // true
        //  var1 = x;  // false
        //  var;  // false
        //  var(  // false

        SkipBlanks();
        int p1 = position;
        bool f = ParseChars(str); 
        int p2 = position;
        SkipBlanks();
        if (f && position > p2)
        {
            return true;
        }
        position = p1;
        return false;
    }

    private string? ParseVariable()
    {
        SkipBlanks();
        if (position >= expression.Length)
        {
            return null;
        }

        if (!char.IsLetterOrDigit(expression[position]) && expression[position] != '_')
        {
            return null;
        }

        var p1 = position;
        position++;

        while (position < expression.Length && (char.IsLetterOrDigit(expression[position]) || expression[position] == '_')) // IsValidVariableSymbol())
        {
            position++;
        }

        string v = expression[p1..position];
        return v;
    }

    private bool ParseNumber()
    {
        StringBuilder number = new();

        while (position < expression.Length && Validators.IsDigit(expression[position]))
        {
            number.Append(expression[position]);
            position++;
        }

        return number.Length > 0;
    }
}