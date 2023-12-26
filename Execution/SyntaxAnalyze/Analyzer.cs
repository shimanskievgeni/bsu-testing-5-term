﻿using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml.Linq;
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
    //private FuncDef? _funcDef;

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

        while (ParseVar()) ; // declare global vars

        var codeIndexBeforeFunction = CompiledCode.AddUndefinedGoto(); // to bypass functions 

        while (ParseFunction()) ;

        // top level statements
        CompiledCode.DefineGotoHereFrom(codeIndexBeforeFunction);

        while (ParseVar()) ; // again declare global vars
        ParseOperators();

        if (!EndCode())
        {
            StopOnError("Expected end of code."); return false;
        }
        return true;
    }

    public bool ParseVar()
    {
        if (!ParseKeyWord("var"))
        {
            return false;
        }

        do
        {
            if (!ParseAssigment(isVarDeclare: true))
            {
                StopOnError("Invalid variable declaration."); return false;
            }
        } while (ParseChar(','));

        ParseMandatoryChar(';');

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
            CompiledCode.AddEndOfExpression();

            ParseMandatoryChar(';');
        }
        else
        {
            CompiledCode.AddInt(0); // return 0 by default
        }
        var def = GetFunc(_funcName ?? "");
        CompiledCode.AddReturn((def?.ParamCount) ?? 0, (def?.LocalVarCount) ?? 0);
        return true;
    }

    public bool ParseIf()
    {
        if (!ParseKeyWord("if"))
        {
            return false;
        }

        ParseExpression();
        CompiledCode.AddEndOfExpression();

        var indexTokenToCorrect = CompiledCode.AddUndefinedGotoIf();

        ParseBlock();

        if (ParseKeyWord("else"))
        {
            CompiledCode.DefineGoto(indexTokenToCorrect, CompiledCode.LastIndex + 2);
            indexTokenToCorrect = CompiledCode.AddUndefinedGoto();

            if (!ParseIf())
            {
                ParseBlock();
            }
        }

        CompiledCode.DefineGotoHereFrom(indexTokenToCorrect);

        return true;
    }

    public bool ParseWhile()
    {
        if (!ParseKeyWord("while"))
        {
            return false;
        }

        var indexTokenStartWhile = CompiledCode.LastIndex + 1;

        ParseExpression();
        CompiledCode.AddEndOfExpression();

        var indexTokenToCorrect = CompiledCode.AddUndefinedGotoIf();

        ParseBlock();

        CompiledCode.AddGoto(indexTokenStartWhile); // loop
        CompiledCode.DefineGotoHereFrom(indexTokenToCorrect);

        return true;
    }

    public bool ParseBlock(bool isVarsPossible = false)
    {
        if (!ParseChar('{'))
        {
            StopOnError("Expected '{'"); return false;
        }

        if (isVarsPossible)
            while (ParseVar()) ;
        ParseOperators();

        ParseMandatoryChar('}');
        return true;
    }


    public bool ParseOperators()
    {
        bool f;

        do
        {
            // with leading keywords
            f = ParseReturn();
            if (!f) f = ParseIf();
            if (!f) f = ParseWhile();

            // w/o leading keywords, any name leading
            if (!f)
            {
                string? name = ParseName();
                if (name == null)
                {
                    break;
                }
                if (!f) f = ParseProcedureCall(name);
                if (!f) f = ParseAssigment(name);
            }
        }
        while (f);

        return f;
    }

    private bool ParseProcedureCall(string name)
    {
        //SkipBlanks();
        //var p1 = position;
        //string? name = ParseName();

        //if (name == null)
        //{
        //    position = p1;
        //    return false;
        //}

        var p1 = position;
        if (!ParseCall(name))
        {
            position = p1;
            return false;
        }

        ParseMandatoryChar(';');

        CompiledCode.AddPopOperand(); // pop returned value
        return true;
    }

    private bool ParseFunction()
    {
        if (!ParseKeyWord("function"))
        {
            return false;
        }

        string? funcName = ParseName();
        if (funcName == null)
        {
            StopOnError("Expected function name."); return false;
        }
        if (GetFunc(funcName) != null)
        {
            StopOnError($"Duplicated function name: {funcName}."); return false;
        }
        _funcName = funcName;
        var funcDef = AddFunc(funcName);
        if (funcDef == null)
        {
            StopOnError($"Cannot add function {funcName}"); return false;
        }

        ParseFunctionHeader(funcDef);

        funcDef.CodeIndex = CompiledCode.LastIndex + 1;

        ParseBlock(isVarsPossible: true);

        // return can be missing in use code, so should be added anyway
        CompiledCode.AddInt(0);
        CompiledCode.AddReturn((funcDef?.ParamCount) ?? 0, (funcDef?.LocalVarCount) ?? 0);

        funcDef?.SetStackIndexForLocalVars();

        _funcName = null;

        return true;
    }

    private bool ParseFunctionHeader(FuncDef funcDef)
    {
        ParseMandatoryChar('(');
        string? name;

        if (!ParseChar(')'))
        {
            do
            {
                name = ParseName();
                if (name == null)
                {
                    StopOnError("Expected parameter name"); return false;
                }

                if (GetLocalVar(name, _funcName) != null)
                {
                    StopOnError($"Duplicated parameter name: {name}."); return false;
                }

                funcDef.AddParameterVariable(name);

            } while (ParseChar(','));

            ParseMandatoryChar(')');
        }
        return true;
    }

    //------------------------------------------
    private FuncDef? AddFunc(string name)
    {
        if (functions.TryGetValue(name, out FuncDef? def))
            return def;

        def = new FuncDef();
        if (functions.TryAdd(name, def))
            return def;
        else
            return null;
    }

    private FuncDef? GetFunc(string name)
    {
        //return functions[name];
        if (functions.TryGetValue(name, out FuncDef? v))
            return v;
        else
            return null;
    }
    private VariableDef? AddGlobalVarDeclare(string name)
    {
        var def = new GlobalVariableDef();
        if (variables.TryAdd(name, def))
            return def;
        else
            return null;
    }
    private VariableDef? AddLocalVarDeclare(string name, string funcName)
    {
        var def = functions[funcName].AddLocalVariable(name);
        if (def != null)
        {
            CompiledCode.AddLocalVarDeclare(name, def);
        }
        return def;
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

    private VariableDef? GetLocalVar(string name, string? funcName)
    {
        if (funcName != null)
        {
            var localVars = functions[funcName].localVariables;  // functions.TryGetValue(funcName, out _);
            if (localVars.TryGetValue(name, out VariableDef? v))
                return v;
        }
        return null;
    }


    //-------------------------------------------------------
    private bool EndCode()
    {
        SkipBlanks();

        return (position == expression.Length);
    }


    // for testing only
    public bool IsValidExpression()
    {
        try
        {
            position = 0;
            error = "";
            ParseExpression();
            CompiledCode.AddEndOfExpression();
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
        //if (ParseUnaryOperation())
        //{
        //    if (!ParseOperand())
        //    {
        //        StopOnError("qqqError"); return false;
        //    }
        //}
        //else 
        if (!ParseOperand())
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
                StopOnError("Expected string literal."); return false;
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
                    StopOnError("Unexpected end of string literal."); return false;
                }

                if (!EscapedSymbols.Contains(CurrentChar()))
                {
                    StopOnError($"Symbol {CurrentChar()} cannot be escaped."); return false;
                }
            }
            else if (CurrentChar() == '\'')
            {
                str = expression[p1..position];
                position++;
                return true;
            }

            position++;
        }

        StopOnError("qqqError"); return false;
    }


    // is used also in var declaration
    private bool ParseAssigment(string name = "", bool isVarDeclare = false)
    {
        if (name == "") 
        {
            name = ParseName() ?? "";
            if (name == "")
            {
                return false;
            }
        }

        VariableDef? def;
        if (isVarDeclare)
        {
            if (_funcName != null)
            {
                if (!functions[_funcName].localVariables.TryGetValue(name, out def))
                    def = AddLocalVarDeclare(name, _funcName);
            }
            else
            {
                if (!variables.TryGetValue(name, out def))
                    def = AddGlobalVarDeclare(name);
            }

            if (def == null)
            {
                StopOnError($"Cannot declare variable: {name}"); return false;
            }

        }

        if (!ParseChar('='))
        {
            return isVarDeclare;
        }

        ParseExpression();
        CompiledCode.AddEndOfExpression();

        if (!isVarDeclare)
        {
            ParseMandatoryChar(';');
        }

        def = GetVar(name, _funcName);
        if (def == null) // strict mode
        {
            StopOnError($"Variable is not declared: {name}"); return false;
        }
        CompiledCode.AddSetVar(name, def);
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
        ParseUnaryOperation();

        if (ParseChar('('))
        {
            CompiledCode.AddOperation("(");

            ParseExpression();

            ParseMandatoryChar(')');
            CompiledCode.AddOperation(")");

            return true;
        }

        string str;
        if (ParseString(out str))
        {
            CompiledCode.AddString(str);
            return true;
        }

        if (ParseNumber(out str, out bool isDouble))
        {
            if (isDouble)
            {
                if (double.TryParse(str, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double doubleVal))
                    CompiledCode.AddDouble(doubleVal);
                else
                {
                    StopOnError($"Error on parsing double(?) number: {str} "); return false;
                }
            }
            else if (int.TryParse(str, out int intVal))
            {
                CompiledCode.AddInt(intVal);
            }
            else
            {
                StopOnError($"Error on parsing int(?) number: {str} "); return false;
            }
            return true;
        }

        if (ParseKeyWord("true"))
        {
            CompiledCode.AddBool(true);
            return true;
        }

        if (ParseKeyWord("false"))
        {
            CompiledCode.AddBool(false);
            return true;
        }

        string? name = ParseName();

        if (name == null)
        {
            StopOnError("Expected operand."); return false;
        }

        if (ParseCall(name))
        {
            return true;
        }

        var def = GetVar(name, _funcName);

        if (def == null)
        {
            StopOnError($"Undeclared variable name: {name}."); return false;
        }

        CompiledCode.AddGetVarValue(name, def);

        return true;
    }

    public bool ParseCall(string name)
    {
        if (!ParseChar('(')) // function call, not var
        {
            return false;
        }

        var def = GetFunc(name);
        if (def == null)
        {
            StopOnError($"Undefined function name: {name}."); return false;
        }

        CompiledCode.AddOperation("PrepareCall"); // just marker with priority -1
        ParseArguments(name, def);
        CompiledCode.AddCall(def);

        ParseMandatoryChar(')');

        return true;
    }

    private bool ParseMandatoryChar(char ch)
    {
        if (!ParseChar(ch))
        {
            StopOnError($"Expected '{ch}' "); return false;
        }
        return true;
    }

    private bool ParseArguments(string funcName, FuncDef funcDef)
    {
        int argcount = 0;

        while (!EndCode() && CurrentChar() != ')')
        {
            ParseExpression();
            CompiledCode.AddEndOfExpression();

            argcount++;

            if (argcount > funcDef.ParamCount)
            {
                StopOnError($"Too many arguments for function {funcName}"); return false;
            }

            if (!ParseChar(','))
            {
                return false;
            }
        }

        if (argcount < funcDef.ParamCount)
        {
            StopOnError($"Too few arguments for function {funcName}"); return false;
        }

        return true;
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

    private string? ParseName()
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