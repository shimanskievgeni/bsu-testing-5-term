//namespace ExpressionType;

public enum ExpressionType
{
    Undefined,
    Num,
    Str,
    Bool,
    Double,
    Int
}

public class TypedValue
{
    public ExpressionType type;
    public int intValue;
    public double doubleValue;
    public string stringValue;
    public bool boolValue;
}

public static class TypeResolver
{
    public static ExpressionType ResultingOperationType(string operation, ExpressionType type1, ExpressionType type2)
    {
        if (type1 == ExpressionType.Int || type1 == ExpressionType.Double)
        { 
            if (operation == "Unary-"
                || operation == "Unary+"
               )
            return type1;
        }

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

        if (type1 == ExpressionType.Int || type1 == ExpressionType.Double) 
        {
            //if (operation == "++" || operation == "--")
            //{
            //    return type1;
            //}

            if (type2 == ExpressionType.Int || type2 == ExpressionType.Double)
            {
                if (operation == "+"
                    || operation == "-"
                    || operation == "*"
                    || operation == "/"
                    )
                {
                    if (type1 == ExpressionType.Double || type2 == ExpressionType.Double)
                        return ExpressionType.Double;
                    else
                        return ExpressionType.Int;
                }

                if (operation == "%")
                {
                    if (type1 == ExpressionType.Int || type2 == ExpressionType.Int)
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

        //StopOnError("qqqError");
        return ExpressionType.Undefined;
    }

}