using static System.Runtime.InteropServices.JavaScript.JSType;

using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;
using System;
using Execution.Compiled;

namespace Execution;

public class Calculator
{
    private string? error;
    public string? Error { get => error; }

    public readonly CompiledCode compiledCode;

    public Calculator(CompiledCode compiledCode)
    {
        this.compiledCode = compiledCode;
    }

    private static void Assign(Stack<Token> operands, TokenVar token)
    {
        var left = operands.Pop();

        token.def.typedValue.SetFrom((left as TokenConstantType));
    }

    public static void ComputeOnTheTop(Stack<Token> operands, Stack<string> operators)
    {
        string op = operators.Pop();

        Token? val2 = null;

        if (!IsUnaryOperatioin(op))
        {
            val2 = operands.Pop();
        }
        var val1 = operands.Pop();

        operands.Push(Operate(val1, val2, op));
    }

    public Token Compute()
    {
        if (compiledCode?.tokens == null)
            return null;

        Stack<Token> operands = new();
        Stack<string> operators = new();

        string suspect;// = "";
        int i = 0;
        while (i < compiledCode.tokens.Count)
        {
            Token token = compiledCode.tokens[i];
            if (token == null)
                return null;

            suspect = "";

            if (token.Type == TokenType.Ret)
            {
                i = i;
                //if (i = (token as TokenRet).toToken;
                //continue;
            }
            else if (token.Type == TokenType.Goto)
            {
                i = (token as TokenGoto).toToken;
                continue;
            }
            else if (token.Type == TokenType.GotoIf)
            {
                var val1 = (operands.Pop() as TokenConstant<bool>).value;
                if (!val1)
                {
                    i = (token as TokenGoto).toToken;
                    continue;
                }
            }
            else if (token.Type == TokenType.SetGlobalVar)
            {
                //operands.Push(token);
                Assign(operands, token as TokenVar);
                i++;
                continue;
            }
            else if (token.Type == TokenType.Operation)
            {
                suspect = (token as TokenOperation).Operation;
            }

            if (suspect == "(")
            {
                operators.Push(suspect);
            }
            else if (token.Type == TokenType.Constant
                  || token.Type == TokenType.GetGlobalVarValue)
            {
                operands.Push(token);
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
            i++;
        }

        while (operators.Count != 0 && operands.Count > 1)
        {
            ComputeOnTheTop(operands, operators);
        }

        if (operators.Count != 0)
        {
            string op = operators.Pop();
            throw new InvalidOperationException($"operators stack is not empty at the end: {op}");
        }

        Token tokenretval;
        /*
        TypedValue retval;
        */
        if (operands.Count != 0)
        {
            tokenretval = operands.Pop();
        }
        else
        {
            tokenretval = new TokenConstant<int>(0, ExpressionType.Int);
        }
        if (operands.Count > 1)
        {
            throw new InvalidOperationException($"operands stack is not empty at the end: ((op))");
        }

        return tokenretval;
    }


    private static bool IsUnaryOperatioin(string operation)
    {
        if (operation == "!" || operation.StartsWith("Unary"))
            return true;
        return false;
    }

    private static TokenConstantType Operate(Token left, Token? right, string operation)
    {
        //if (left.Type == TokenType.Constant && right.Type == TokenType.Constant)
        //{
        ExpressionType type1 = ExpressionType.Undefined, type2 = ExpressionType.Undefined;
        int i1 = 0, i2 = 0;
        double d1 = 0, d2 = 0;
        string? s1 = "", s2 = "";
        bool b1 = false, b2 = false;

        TypedValue typedValue1;
        TypedValue typedValue2;

        if (left is TokenVar tvar1)
        {
            typedValue1 = tvar1.def.typedValue;

            type1 = typedValue1.type;
            if (type1 == ExpressionType.Int) i1 = typedValue1.intValue;
            else if (type1 == ExpressionType.Double) d1 = typedValue1.doubleValue;
            else if (type1 == ExpressionType.Str) s1 = typedValue1.stringValue;
            else if (type1 == ExpressionType.Bool) b1 = typedValue1.boolValue;
        }
        else if (left is TokenConstant<int> ti)
        {
            type1 = ExpressionType.Int;
            i1 = ti.value;
        }
        else if (left is TokenConstant<double> td)
        {
            type1 = ExpressionType.Double;
            d1 = td.value;
        }
        else if (left is TokenConstant<string> ts)
        {
            type1 = ExpressionType.Str;
            s1 = ts.value;
        }
        else if (left is TokenConstant<bool> tb)
        {
            type1 = ExpressionType.Bool;
            b1 = tb.value;
        }

        if (right is TokenVar tvar2)
        {
            typedValue2 = tvar2.def.typedValue;

            type2 = typedValue2.type;
            if (type2 == ExpressionType.Int) i2 = typedValue2.intValue;
            else if (type2 == ExpressionType.Double) d2 = typedValue2.doubleValue;
            else if (type2 == ExpressionType.Str) s2 = typedValue2.stringValue;
            else if (type2 == ExpressionType.Bool) b2 = typedValue2.boolValue;
        }
        else if (right is TokenConstant<int> ti2)
        {
            type2 = ExpressionType.Int;
            i2 = ti2.value;
        }
        else if (right is TokenConstant<double> td2)
        {
            type2 = ExpressionType.Double;
            d2 = td2.value;
        }
        else if (right is TokenConstant<string> ts2)
        {
            type2 = ExpressionType.Str;
            s2 = ts2.value;
        }
        else if (right is TokenConstant<bool> tb2)
        {
            type2 = ExpressionType.Bool;
            b2 = tb2.value;
        }

        var resultType = TypeResolver.ResultingOperationType(operation, type1, type2);
        if (resultType == ExpressionType.Undefined)
            throw new InvalidOperationException($"Incompatible types: {operation} {type1} {type2} ");

        int intRes = 0;
        double doubleRes = 0;
        string stringRes = "";
        bool boolRes = false;
        bool err = false;

        if (type1 == ExpressionType.Double || type2 == ExpressionType.Double)
        {
            if (type1 == ExpressionType.Int)
            {
                d1 = i1;
            }
            else if (type2 == ExpressionType.Int)
            {
                d2 = i2;
            }
        }

        if (resultType == ExpressionType.Bool)
        {
            if (operation == "!" && type1 == ExpressionType.Bool)
                boolRes = !b1;
            else if (type1 == type2 && type1 == ExpressionType.Bool)
            {
                if (operation == "&&") boolRes = b1 && b2;
                else if (operation == "||") boolRes = b1 || b2;
                else err = true;
            }
            else if (type1 == type2 && type1 == ExpressionType.Int)
            {
                if (operation == "==") boolRes = i1 == i2;
                else if (operation == "!=") boolRes = i1 != i2;
                else if (operation == "<=") boolRes = i1 <= i2;
                else if (operation == ">=") boolRes = i1 >= i2;
                else if (operation == "<") boolRes = i1 < i2;
                else if (operation == ">") boolRes = i1 > i2;
                else err = true;
            }
            else if (type1 == ExpressionType.Double || type2 == ExpressionType.Double)
            {
                if (operation == "==") boolRes = d1 == d2;
                else if (operation == "!=") boolRes = d1 != d2;
                else if (operation == "<=") boolRes = d1 <= d2;
                else if (operation == ">=") boolRes = d1 >= d2;
                else if (operation == "<") boolRes = d1 < d2;
                else if (operation == ">") boolRes = d1 > d2;
                else err = true;
            }
            else if (type1 == type2 && type1 == ExpressionType.Str)
            {
                if (operation == "==") boolRes = s1 == s2;
                else if (operation == "!=") boolRes = s1 != s2;
                else if (operation == "<=") boolRes = s1.CompareTo(s2) <= 0;
                else if (operation == ">=") boolRes = s1.CompareTo(s2) >= 0;
                else if (operation == "<") boolRes = s1.CompareTo(s2) < 0;
                else if (operation == ">") boolRes = s1.CompareTo(s2) > 0;
                else err = true;
            }
            else
            {
                err = true;
            }
        }
        else if (resultType == ExpressionType.Int)
        {
            if (type1 == ExpressionType.Int && operation == "Unary-")
            {
                intRes = -i1;
            }
            else if (type1 == ExpressionType.Int && operation == "Unary+")
            {
                intRes = +i1;
            }
            else if (type1 == type2 && type1 == ExpressionType.Int)
            {
                if (operation == "+") intRes = i1 + i2;
                else if (operation == "-") intRes = i1 - i2;
                else if (operation == "*") intRes = i1 * i2;
                else if (operation == "/") intRes = i1 / i2;
                else if (operation == "%") intRes = i1 % i2;
                else err = true;
            }
            else err = true;
        }
        else if (resultType == ExpressionType.Double)
        {
            if (type1 == ExpressionType.Double && operation == "Unary-")
            {
                doubleRes = -d1;
            }
            else if (type1 == ExpressionType.Double && operation == "Unary+")
            {
                doubleRes = +d1;
            }
            else if (operation == "+") doubleRes = d1 + d2;
            else if (operation == "-") doubleRes = d1 - d2;
            else if (operation == "*") doubleRes = d1 * d2;
            else if (operation == "/") doubleRes = d1 / d2;
            else err = true;
        }
        else if (resultType == ExpressionType.Str)
        {
            if (type1 == type2 && type1 == ExpressionType.Str)
                if (operation == "+") stringRes = s1 + s2;
                else err = true;
        }
        else
        {
            err = true;
        }

        if (err)
        {
            throw new InvalidOperationException($"Invalid operation: {operation} {type1} {type2} ");
        }

        TokenConstantType resToken;

        if (resultType == ExpressionType.Bool)
            resToken = new TokenConstant<bool>(boolRes, ExpressionType.Bool);
        else if (resultType == ExpressionType.Int)
            resToken = new TokenConstant<int>(intRes, ExpressionType.Int);
        else if (resultType == ExpressionType.Double)
            resToken = new TokenConstant<double>(doubleRes, ExpressionType.Double);
        else if (resultType == ExpressionType.Str)
            resToken = new TokenConstant<string>(stringRes, ExpressionType.Str);
        else
        {
            throw new InvalidOperationException($"Invalid operation result type: {operation} {type1} {type2} {resultType} ");
        }

        return resToken;
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
        _ => 0
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