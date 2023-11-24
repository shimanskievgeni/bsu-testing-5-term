namespace Calculator;

public static class Calculator
{
    public static double Compute(string source)
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
        

        return operands.Pop();
    }
    
     private static double Operate(double left, double right, char op) =>
        op switch
        {
            '+' => left + right,
            '-' => left - right,
            '*' => left * right,
            '/' => right switch
            {
                0 => throw new DivideByZeroException(),
                _ => left / right
            },
            _ => throw new InvalidOperationException($"Invalid operator: {op}")
        };
    
     private static int GetOperationPriority(char operation) =>
        operation switch
        {
            '+' or '-' => 1,
            '*' or '/' => 2,
            _ => 0
        };

    /// <summary>
    /// Check if the given char is digit
    /// </summary>
    /// <param name="char">char to be determined</param>
    /// <returns>true if the given char is digit, otherwise false</returns>
    private static bool IsDigit(char @char) => @char is >= '0' and <= '9';
}