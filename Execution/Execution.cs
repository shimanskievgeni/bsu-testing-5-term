using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace Execution;

public static class Execution
{
    public static Token Exec(string source)
    {
        var parser = new Analyzer(source);

        if (!parser.Parse())
            return null;
        var calculator = new Calculator.Calculator(parser.CompiledCode);
        return calculator.Compute();
    }

    public static Token CalcExpression(string source)
    {
        var parser = new Analyzer(source);

        if (!parser.IsValidExpression())
            return null;
        var calculator = new Calculator.Calculator(parser.CompiledCode);
        return calculator.Compute();
    }

}