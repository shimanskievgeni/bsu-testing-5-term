using static System.Runtime.InteropServices.JavaScript.JSType;

using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;
using System;
using Execution.Compiled;
using System.Reflection.Metadata.Ecma335;
using static Execution.SyntaxAnalyze.Analyzer;
using System.Linq.Expressions;

namespace Execution;

public class Calculator
{
    private string? error;
    public string? Error { get => error; }

    public readonly CompiledCode compiledCode;

    internal class CalculatorException : ApplicationException
    {
        public CalculatorException() : base() { }
        public CalculatorException(string message) : base(message) { }
        public CalculatorException(string message, Exception inner) : base(message, inner) { }
    }

    public Calculator(CompiledCode compiledCode)
    {
        error = null;
        this.compiledCode = compiledCode;
    }
    public bool StopOnError(string msg)
    {
        error = msg;
        throw new CalculatorException();
    }

    public void LogError(string Error)
    {
        Console.WriteLine("!!! Calculator error:");
        Console.WriteLine(Error);
    }

    public TypedValue? Compute()
    {
        try
        {
            return _Compute();
        }
        catch (CalculatorException)
        {
            LogError(Error ?? "");
            return null;
        }
    }

    private TypedValue? _Compute()
    {
        if (compiledCode?.tokens == null)
            return null;

        Stack<TypedValue> operands = new();
        Stack<string> operators = new();

        string suspect;// = "";

        TypedValue retval; // = new(); // 

        int ip = 0; // instruction pointer
        int bp = 0; // base pointer

        while (ip < compiledCode.tokens.Count)
        {
            Token token = compiledCode.tokens[ip];
            if (token == null)
                return null;

            suspect = "";

            if (token.Type == TokenType.Ret)
            {
                if (operators.Count > 0 && operators.Peek() == "PrepareCall")
                {
                    operators.Pop(); // pop PrepareCall
                    retval = operands.Pop();
                    for (int i = 0; i < ((TokenRet)token).localVarCount; i++)
                        operands.Pop();
                    bp = operands.Pop().IntValue;
                    ip = operands.Pop().IntValue;
                    for (int i = 0; i < ((TokenRet)token).paramCount; i++)
                        operands.Pop();
                    operands.Push(retval);
                    continue;
                }
                else
                {
                    break; // top level return;
                }
            }
            else if (token.Type == TokenType.Call)
            {
                operands.Push(new TypedValue(ip + 1));
                ip = ((TokenCall)token).toToken;
                operands.Push(new TypedValue(bp)); // push bp;             //operands.Count););
                bp = operands.Count;                    // mov  bp, sp
                continue;
            }
            else if (token.Type == TokenType.Goto)
            {
                ip = ((TokenGoto)token).toToken;
                continue;
            }
            else if (token.Type == TokenType.GotoIf)
            {
                var val1 = operands.Pop().BoolValue;
                if (!val1)
                {
                    ip = ((TokenGoto)token).toToken;
                    continue;
                }
            }
            else if (token.Type == TokenType.SetGlobalVar)
            {
                var sourceVal = operands.Pop();
                ((GlobalVariableDef)(((TokenVar)token).def)).VarValue.SetFrom(sourceVal);
                //((GlobalVariableDef)(((TokenVar)token).def)).VarValue = new(sourceVal); // for struct, constructor new just set values
                ip++;
                continue;
            }
            else if (token.Type == TokenType.GetGlobalVarValue)
            {
                var v = ((GlobalVariableDef)(((TokenVar)token).def)).VarValue;
                operands.Push(new(v)); // for correct calc with side effects we replace ref with value
                ip++;
                continue;
            }
            else if (token.Type == TokenType.SetLocalVar)
            {
                var sourceVal = operands.Pop();
                var local = operands.ElementAt((
                        ((LocalVariableDef)(((TokenVar)token).def)).stackIndex + (operands.Count - bp)
                        ));
                local.SetFrom(sourceVal);
                ip++;
                continue;
            }
            else if (token.Type == TokenType.GetLocalVarValue)
            {
                var local = operands.ElementAt((
                        ((LocalVariableDef)(((TokenVar)token).def)).stackIndex + (operands.Count - bp)
                        ));
                operands.Push(new(local));// for correct calc with side effects we replace ref with value
                ip++;
                continue;
            }
            else if (token.Type == TokenType.LocalVarDeclare)
            {
                operands.Push(new TypedValue()); // undefined
                ip++;
                continue;
            }
            else if (token.Type == TokenType.PopOperand)
            {
                operands.Pop();
                ip++;
                continue;
            }
            else if (token.Type == TokenType.Operation)
            {
                suspect = ((TokenOperation)token).Operation;
            }

            if (suspect == "(" || suspect == "PrepareCall")
            {
                operators.Push(suspect);
            }
            else if (token.Type == TokenType.TokenTypedValue) 
            {
                operands.Push(GetTypedValue(token));
                ip++;
                continue;
            }
            else if (suspect == ")")
            {
                while (operators.Count != 0 && operators.Peek() != "(")
                {
                    ComputeOnTheTop(operands, operators);
                }

                if (operators.Count != 0) operators.Pop();
            }
            else // EndOfExpression
            {
                var currentOperatorPriority = GetOperationPriority(suspect);

                while (operators.Count != 0 && GetOperationPriority(operators.Peek()) >= currentOperatorPriority)
                {
                    ComputeOnTheTop(operands, operators);
                }

                if (suspect != "")
                    operators.Push(suspect);
            }
            ip++;
        }

        if (operands.Count != 0)
        {
            retval = operands.Pop();
        }
        else
        {
            retval = new(0); 
        }
        if (operands.Count > 1)
        {
            StopOnError($"operands stack is not empty at the end: ((op))");
        }

        return retval;
    }

    public static void ComputeOnTheTop(Stack<TypedValue> operands, Stack<string> operators)
    {
        string op = operators.Pop();

        TypedValue val2 = new();

        if (!IsUnaryOperatioin(op))
        {
            val2 = operands.Pop();
        }
        var val1 = operands.Pop();

        operands.Push(TypedValueOperate(val1, val2, op));
    }

    private static bool IsUnaryOperatioin(string operation)
    {
        if (operation == "!" || operation.StartsWith("Unary"))
            return true;
        return false;
    }

    private static TypedValue GetTypedValue(Token token) 
    {
        if (token is TokenTypedValue tv)
        {
            return tv.typedValue;
        }
        else
        {
            throw new InvalidOperationException($"Invalid token type (it is not operand): {token.Type}. ");
        }
    }

    private static TypedValue TypedValueOperate(TypedValue typedValue1, TypedValue typedValue2, string operation)
    {
        var resultType = TypeResolver.ResultingOperationType(operation, typedValue1.type, typedValue2.type);
        if (resultType == TypeOfValue.Undefined)
            throw new InvalidOperationException($"Incompatible types: {typedValue1.type} {typedValue2.type}. Operation: {operation} ");

        bool err = false;

        TypedValue res = new();

        if (typedValue1.type == TypeOfValue.Double || typedValue2.type == TypeOfValue.Double)
        {
            if (typedValue1.type == TypeOfValue.Int)
            {
                typedValue1.DoubleValue = typedValue1.IntValue;
            }
            else if (typedValue2.type == TypeOfValue.Int)
            {
                typedValue2.DoubleValue = typedValue2.IntValue;
            }
        }

        if (resultType == TypeOfValue.Bool)
        {
            if (operation == "!" && typedValue1.type == TypeOfValue.Bool)
                res.BoolValue = !typedValue1.BoolValue;
            else if (typedValue1.type == typedValue2.type && typedValue1.type == TypeOfValue.Bool)
            {
                if (operation == "&&") res.BoolValue = typedValue1.BoolValue && typedValue2.BoolValue;
                else if (operation == "||") res.BoolValue = typedValue1.BoolValue || typedValue2.BoolValue;
                else err = true;
            }
            else if (typedValue1.type == typedValue2.type && typedValue1.type == TypeOfValue.Int)
            {
                if (operation == "==") res.BoolValue = typedValue1.IntValue == typedValue2.IntValue;
                else if (operation == "!=") res.BoolValue = typedValue1.IntValue != typedValue2.IntValue;
                else if (operation == "<=") res.BoolValue = typedValue1.IntValue <= typedValue2.IntValue;
                else if (operation == ">=") res.BoolValue = typedValue1.IntValue >= typedValue2.IntValue;
                else if (operation == "<") res.BoolValue = typedValue1.IntValue < typedValue2.IntValue;
                else if (operation == ">") res.BoolValue = typedValue1.IntValue > typedValue2.IntValue;
                else err = true;
            }
            else if (typedValue1.type == TypeOfValue.Double || typedValue2.type == TypeOfValue.Double)
            {
                if (operation == "==") res.BoolValue = AlmostEquals(typedValue1.DoubleValue, typedValue2.DoubleValue, 1e-100);
                else if (operation == "!=") res.BoolValue = typedValue1.DoubleValue != typedValue2.DoubleValue;
                else if (operation == "<=") res.BoolValue = typedValue1.DoubleValue <= typedValue2.DoubleValue;
                else if (operation == ">=") res.BoolValue = typedValue1.DoubleValue >= typedValue2.DoubleValue;
                else if (operation == "<") res.BoolValue = typedValue1.DoubleValue < typedValue2.DoubleValue;
                else if (operation == ">") res.BoolValue = typedValue1.DoubleValue > typedValue2.DoubleValue;
                else err = true;
            }
            else if (typedValue1.type == typedValue2.type && typedValue1.type == TypeOfValue.Str)
            {
                if (operation == "==") res.BoolValue = typedValue1.StringValue == typedValue2.StringValue;
                else if (operation == "!=") res.BoolValue = typedValue1.StringValue != typedValue2.StringValue;
                else if (operation == "<=") res.BoolValue = typedValue1.StringValue?.CompareTo(typedValue2.StringValue) <= 0;
                else if (operation == ">=") res.BoolValue = typedValue1.StringValue?.CompareTo(typedValue2.StringValue) >= 0;
                else if (operation == "<") res.BoolValue = typedValue1.StringValue?.CompareTo(typedValue2.StringValue) < 0;
                else if (operation == ">") res.BoolValue = typedValue1.StringValue?.CompareTo(typedValue2.StringValue) > 0;
                else err = true;
            }
            else
            {
                err = true;
            }
        }
        else if (resultType == TypeOfValue.Int)
        {
            if (typedValue1.type == TypeOfValue.Int && operation == "Unary-")
            {
                res.IntValue = -typedValue1.IntValue;
            }
            else if (typedValue1.type == TypeOfValue.Int && operation == "Unary+")
            {
                res.IntValue = +typedValue1.IntValue;
            }
            else if (typedValue1.type == typedValue2.type && typedValue1.type == TypeOfValue.Int)
            {
                if (operation == "+") res.IntValue = typedValue1.IntValue + typedValue2.IntValue;
                else if (operation == "-") res.IntValue = typedValue1.IntValue - typedValue2.IntValue;
                else if (operation == "*") res.IntValue = typedValue1.IntValue * typedValue2.IntValue;
                else if (operation == "/") res.IntValue = typedValue1.IntValue / typedValue2.IntValue;
                else if (operation == "%") res.IntValue = typedValue1.IntValue % typedValue2.IntValue;
                else err = true;
            }
            else err = true;
        }
        else if (resultType == TypeOfValue.Double)
        {
            if (typedValue1.type == TypeOfValue.Double && operation == "Unary-")
            {
                res.DoubleValue = -typedValue1.DoubleValue;
            }
            else if (typedValue1.type == TypeOfValue.Double && operation == "Unary+")
            {
                res.DoubleValue = +typedValue1.DoubleValue;
            }
            else if (operation == "+") res.DoubleValue = typedValue1.DoubleValue + typedValue2.DoubleValue;
            else if (operation == "-") res.DoubleValue = typedValue1.DoubleValue - typedValue2.DoubleValue;
            else if (operation == "*") res.DoubleValue = typedValue1.DoubleValue * typedValue2.DoubleValue;
            else if (operation == "/") res.DoubleValue = typedValue1.DoubleValue / typedValue2.DoubleValue;
            else err = true;
        }
        else if (resultType == TypeOfValue.Str)
        {
            if (typedValue1.type == typedValue2.type && typedValue1.type == TypeOfValue.Str)
                if (operation == "+") res.StringValue = typedValue1.StringValue + typedValue2.StringValue;
                else err = true;
        }
        else
        {
            err = true;
        }

        if (err)
        {
            throw new InvalidOperationException($"Invalid operation: {operation} {typedValue1.type} {typedValue2.type} ");
        }

        res.type = resultType;
        return res;
    }

    public static bool AlmostEquals(double x, double y, double tolerance)
    {
        // https://roundwide.com/equality-comparison-of-floating-point-numbers-in-csharp/

        var diff = Math.Abs(x - y);
        return diff <= tolerance ||
               diff <= Math.Max(Math.Abs(x), Math.Abs(y)) * tolerance;
    }

    private static int GetOperationPriority(string operation) =>
    operation switch
    {
       "||" => 70,
       "&&" => 80,
       "<" or ">" or "==" or "!=" or "<=" or ">=" => 100,
       "+" or "-" => 200,
       "*" or "/" or "%" => 300,
       "Unary-" or "Unary+" or "!" => 1000,
        //EndOfExpression => 0, // needed as marker with priority 0 to evaluate operations in stack (...expression... {  } )
        "PrepareCall" => -1,
           _ => 0 //EndOfExpression => 0, // needed as marker with priority 0 to evaluate operations in stack (...expression... {  } )
    };

    /******
    public double ComputeString(string source)
    {
        Stack<double> operands = new();
        Stack<char> operators = new();

        for (var i = 0; i < source.Length; i++)
        {
            char suspect = source[i];
            if (suspect == ' ') continue;

            if (suspect == '(')
            {
                operators.Push(suspect);
            }
            else if (IsDigit(suspect))
            {
                double value = 0;

                while (i < source.Length && IsDigit(source[i]))
                {
                    value = value * 10 + (source[i] - '0');
                    i++;
                }

                operands.Push(value);
                i--;
            }
            else if (suspect == ')')
            {
                while (operators.Count != 0 && operators.Peek() != '(')
                {
                    var val2 = operands.Pop();

                    var val1 = operands.Pop();

                    var op = operators.Pop();

                    operands.Push(Operate(val1, val2, op));
                }

                if (operators.Count != 0) operators.Pop();
            }
            else
            {
                var currentOperatorPriority = GetOperationPriority(suspect);

                while (operators.Count != 0 && GetOperationPriority(operators.Peek()) >= currentOperatorPriority)
                {
                    var val2 = operands.Pop();

                    var val1 = operands.Pop();

                    char op = operators.Pop();

                    operands.Push(Operate(val1, val2, op));
                }

                operators.Push(suspect);
            }
        }


        while (operators.Count != 0 && operands.Count > 1)
        {
            var val2 = operands.Pop();

            var val1 = operands.Pop();

            char op = operators.Pop();

            operands.Push(Operate(val1, val2, op));
        }

        if (operators.Count != 0)
        {
            char op = operators.Pop();
            throw new InvalidOperationException($"operators stack is not empty at the end: {op}");
            //return double.NaN;
        }

        if (operands.Count > 1)
        {
            double op = operands.Pop();
            throw new InvalidOperationException($"operands stack is not empty at the end: {op}");
            //return double.NaN;
        }

        return operands.Pop();
    }

     private static double Operate(double left, double right, string op) =>
        op switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => right switch
            {
                0 => throw new DivideByZeroException(),
                _ => left / right
            },
            _ => throw new InvalidOperationException($"Invalid operator: {op}")
        };
     ***********/


    /// <summary>
    /// Check if the given char is digit
    /// </summary>
    /// <param name="char">char to be determined</param>
    /// <returns>true if the given char is digit, otherwise false</returns>
    //private static bool IsDigit(char @char) => @char is >= '0' and <= '9';
}