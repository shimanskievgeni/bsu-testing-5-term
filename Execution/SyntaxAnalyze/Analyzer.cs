using System.Text;
using Execution.Compiled;

namespace Execution.SyntaxAnalyze;

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

    private string? error;
    public string? Error { get => error; }

    public CompiledCode CompiledCode { get; private set; }

    internal class ParserException : ApplicationException
    {
        public ParserException() : base() { }
        public ParserException(string message) : base(message) { }
        public ParserException(string message, Exception inner) : base(message, inner) { }
    }

    public Analyzer(string expression)
    {
        this.expression = expression;
        position = 0;
        error = null;
        CompiledCode = new CompiledCode();
    }

    public bool StopOnError(string msg)
    {
        error = msg;
        throw new ParserException();
    }

    public void LogError(string Error)
    {
        Console.WriteLine("!!! Parsing error:");
        Console.WriteLine(Error);
        Console.WriteLine($"at position {position} .");
        if (EndCode())
        {
            Console.WriteLine($"at the end of code (last 30 chars): ");
            Console.WriteLine(expression[Math.Max(expression.Length - 30, 0)..]);
        }
        else
        {
            Console.WriteLine($"at text (30 chars before and after): ");
            Console.WriteLine(expression[Math.Max(0, position - 30)..Math.Min(expression.Length, position + 30)]);
        }
    }


    public bool Parse()
    {
        try
        {
            return _Parse();
        }
        catch (ParserException)
        {
            LogError(Error ?? "");
            return false;
        }
    }


    public bool _Parse()
    {
        position = 0;
        error = null;
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
        if (!f)
        {
            StopOnError("expected End Of Code."); return false;
        }
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
                StopOnError("qqqError"); return false;
            }
        } while (ParseChar(','));

        if (!ParseChar(';'))
        {
            StopOnError("qqqError"); return false;
        }
        return true;
    }

    public bool ParseReturn()
    {
        if (!ParseKeyWord("return"))
        {
            return false;
        }

        if (!ParseChar(';'))
        {
            ParseExpression();
            if (!ParseChar(';'))
            {
                StopOnError("Expected ';' "); return false;
            }
        }
        CompiledCode.AddReturn();
        return true;
    }

    public bool ParseIf()
    {
        if (!ParseKeyWord("if"))
        {
            return false;
        }

        ParseExpression();

        CompiledCode.AddExpressionEnd(); // -1 just placeholder
        CompiledCode.AddGotoIf(-1); // -1 just placeholder
        var indexTokenWithGoto = CompiledCode.LastIndex;

        ParseBlock();

        if (ParseKeyWord("else"))
        {
            (CompiledCode.tokens[indexTokenWithGoto] as TokenGoto).toToken = CompiledCode.LastIndex + 2;
            CompiledCode.AddGoto(-1); // -1 just placeholder
            indexTokenWithGoto = CompiledCode.LastIndex;

            if (!ParseIf())
            {
                ParseBlock();
            }

            (CompiledCode.tokens[indexTokenWithGoto] as TokenGoto).toToken = CompiledCode.LastIndex + 1;
        }
        else
        {
            (CompiledCode.tokens[indexTokenWithGoto] as TokenGoto).toToken = CompiledCode.LastIndex + 1;
        }
        return true;
    }

    public bool ParseWhile()
    {
        if (!ParseKeyWord("while"))
        {
            return false;
        }

        ParseExpression();

        ParseBlock();

        return true;

    }

    public bool ParseBlock()
    {
        if (!ParseChar('{'))
        {
            StopOnError("Expected '{'"); return false;
        }

        ParseOperators();

        if (!ParseChar('}'))
        {
            StopOnError("Expected '}'"); return false;
        }
        return true;
    }


    public bool ParseOperators()
    {
        bool f;

        do
        {
            f = ParseReturn();
            if (!f)
            {
                f = ParseIf();
            }
            if (!f)
            {
                f = ParseWhile();
            }
            if (!f)
            {
                f = ParseAssigment();
            }

        }
        while (f);

        f = EndCode();
        return f;
    }


    private bool ParseFunction()
    {
        // if (!ParseWordAndBlank("function"))
        if (!ParseKeyWord("function"))
        {
            return false;
        }

        ParseFunctionHeader();

        ParseBlock();

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
            StopOnError("qqqError"); return -1;
        }
        _funcName = funcName;

        if (!ParseChar('('))
        {
            StopOnError("qqqError"); return -1;
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
                    StopOnError("qqqError"); return -1;
                }

                AddFuncVar(name, funcName);

                argcount++;

            } while (ParseChar(','));
        }

        if (!ParseChar(')'))
        {
            StopOnError("qqqError"); return -1;
        }
        return argcount;
    }

    private void AddFunc(string name)
    {
        functions.TryAdd(name, new FuncDef(name));
    }

    private FuncDef GetFunc(string name)
    {
        //return functions[name];
        if (functions.TryGetValue(name, out FuncDef? v))
            return v;
        else
            return null;
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

    private VariableDef? GetVar(string name, string? funcName)
    {
        VariableDef? v;
        if (funcName != null)
        {
            var localVars = functions[funcName].localVariables;  // functions.TryGetValue(funcName, out _);
            if (localVars.TryGetValue(name, out v))
                return v;
        }
        if (variables.TryGetValue(name, out v))
            return v;
        else
            return null;
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


    // for testing only
    public bool IsValidExpression()
    {
        try
        {
            position = 0;
            error = "";
            ParseExpression();
            if (!EndCode())
            {
                error = "expected End Of Code.";
                LogError(Error ?? "");
                return false;
            }
            return true;
        }
        catch (ParserException)
        {
            LogError(Error ?? "");
            return false;
        }
    }


    private bool ParseExpression()
    {
        if (ParseUnaryOperation())
        {
            if (!ParseOperand())
            {
                StopOnError("qqqError"); return false;
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
                StopOnError("qqqError"); return false;
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

    private bool ParseString(out string str)
    {
        SkipBlanks();

        bool Escape = true;

        str = "";
        if (ParseChar('@'))
        {
            Escape = false;

            if (!ParseChar('\''))
            {
                StopOnError("qqqError"); return false;
            }
        }
        else
        {
            if (!ParseChar('\''))
            {
                return false;
            }
        }

        int p1 = position;

        while (!EndCode()) // don't use ParseChar() or SkipBlanks() here, they skip comments!
        {
            if (Escape && CurrentChar() == '\\')
            {
                position++;

                if (position >= expression.Length)
                {
                    StopOnError("qqqError"); return false;
                }

                if (!EscapedSymbols.Contains(CurrentChar()))
                {
                    StopOnError("qqqError"); return false;
                }
            }
            else if (CurrentChar() == '\'')
            {
                position++;
                str = expression[p1..position];
                return true;
            }

            position++;
        }

        StopOnError("qqqError"); return false;
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
            StopOnError("qqqError"); return false;
        }

        AddVar(name, _funcName);
        var def = GetVar(name, _funcName);
        CompiledCode.AddRefGlobalVar(name, def);
        return true;
    }



    //--------------------------------------------------
    private bool ParseUnaryOperation()
    {
        SkipBlanks();
        int p1 = position;
        if (ParseChars("!")
           || ParseChars("-")
           || ParseChars("+")

           )
        {
            var operation = expression[p1..position];
            if (operation == "+" || operation == "-")
            {
                operation = "Unary" + operation;
            }
            CompiledCode.AddOperation(operation);
        }
        else
        {
            return false;
        }
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
        CompiledCode.AddOperation(operation);
        return true;

        //SkipBlanks();
        //if (position < expression.Length && Validators.IsOperation(expression[position]))
        //{
        //    position++;
        //    return true;
        //}
        //return false;
    }


    private bool ParseOperand()
    {
        if (ParseChar('('))
        {
            CompiledCode.AddOperation("(");

            ParseExpression();

            if (!ParseChar(')'))
            {
                StopOnError("qqqError"); return false;
            }
            CompiledCode.AddOperation(")");

            return true;
        }

        string str = "";
        if (ParseString(out str))
        {
            //Type = ExpressionType.Str;
            CompiledCode.AddString(str);
            return true;
        }

        //if (ParseIntNumber(out str))
        //{
        //    if (!int.TryParse(str, out int intVal))
        //    {
        //        StopOnError(@"Error on parsing number: {str} "); return false;
        //    }
        //    CompiledCode.AddInt(intVal);
        //    return true;
        //}

        if (ParseNumber(out str, out bool isDouble))
        {
            if (isDouble)
            {
                if (double.TryParse(str, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double doubleVal))
                    CompiledCode.AddDouble(doubleVal);
                else
                {
                    StopOnError(@"Error on parsing double(?) number: {str} "); return false;
                }
            }
            else if (int.TryParse(str, out int intVal))
            {
                CompiledCode.AddInt(intVal);
            }
            else
            {
                StopOnError(@"Error on parsing int(?) number: {str} "); return false;
            }
            return true;
        }

        string? name = ParseVariable();

        if (name == null)
        {
            StopOnError("qqqError"); return false;
        }

        if (ParseChar('(')) // function call, not var
        {
            if (GetFunc(name) == null)
            {
                StopOnError("qqqError"); return false;
            }

            ParseArguments(name);

            if (!ParseChar(')'))
            {
                StopOnError("qqqError"); return false;
            }


            return true;
        }

        var def = GetVar(name, _funcName);

        if (def == null)
        {
            StopOnError("Underfined variable: {" + name + "}"); return false;
        }

        CompiledCode.AddGlobalVarValue(name, def);

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
            //    StopOnError("qqqError"); return false;
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

    private bool ParseKeyWord(string str)
    {
        if (ParseWordAndBlank(str))
        {
            return true;
        }

        int p1 = position;
        bool f = ParseChars(str);

        if (f && !char.IsLetterOrDigit(expression[position]) && expression[position] != '_')
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

    private bool ParseIntNumber(out string str)
    {
        StringBuilder number = new();

        while (position < expression.Length && Validators.IsDigit(expression[position]))
        {
            number.Append(expression[position]);
            position++;
        }
        str = number.ToString();

        return number.Length > 0;
    }
    private bool ParseNumber(out string str, out bool isDouble)
    {
        isDouble = false;
        StringBuilder number = new();

        var p1 = position;
        while (position < expression.Length && Validators.IsDigit(expression[position]))
        {
            number.Append(expression[position]);
            position++;
        }
        if (position < expression.Length && expression[position] == '.')
        {
            number.Append(expression[position]);
            position++;
            isDouble = true;
        }
        while (position < expression.Length && Validators.IsDigit(expression[position]))
        {
            number.Append(expression[position]);
            position++;
        }
        if (number.Length > 0 && position < expression.Length && (expression[position] == 'e' || expression[position] == 'E'))
        {
            number.Append(expression[position]);
            position++;
            if (position < expression.Length && (expression[position] == '+' || expression[position] == '-'))
            {
                number.Append(expression[position]);
                position++;
            }
            while (position < expression.Length && Validators.IsDigit(expression[position]))
            {
                number.Append(expression[position]);
                position++;
            }
            isDouble = true;
        }
        str = number.ToString();
        if (position < expression.Length && (char.IsLetter(expression[position]) || expression[position] == '_' || expression[position] == '.'))
        {
            position = p1;
            return false;
        }
        return number.Length > 0;
    }
}