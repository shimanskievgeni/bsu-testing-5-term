using System.Runtime.InteropServices;

namespace Execution.Compiled;

public enum ExpressionType
{
    Undefined,
    Int,
    Double,
    Str,
    Bool
}

//[StructLayout(LayoutKind.Explicit)]
//public struct SampleUnion
//{
//    [FieldOffset(0)] public float bar;
//    [FieldOffset(4)] public int killroy;
//    [FieldOffset(4)] public float fubar;
//}

public class TypedValue
{
    public object? objectValue;
    public int IntValue
    {
        get => (int)objectValue;
        set
        {
            this.type = ExpressionType.Int;
            objectValue = value;
        }
    }
    public double DoubleValue
    {
        get => (double)objectValue;
        set
        {
            this.type = ExpressionType.Double;
            objectValue = value;
        }
    }
    public string? StringValue { 
        get => objectValue.ToString(); 
        set
        {
            this.type = ExpressionType.Str;
            objectValue = value;
        }
    }
    public bool BoolValue { 
        get => (bool)objectValue;
        set
        {
            this.type = ExpressionType.Bool;
            objectValue = value;
        }
    }

    public ExpressionType type = ExpressionType.Undefined;

    public TypedValue()
    {
        type = ExpressionType.Undefined;
    }

    public TypedValue(TypedValue source)
    {
        this.SetFrom(source);
    }

    public void SetFrom(TypedValue source)
    {
        this.type = source.type;
        this.objectValue = source.objectValue;

        //this.IntValue = source.IntValue;
        //this.DoubleValue = source.DoubleValue;
        //this.StringValue = source.StringValue;
        //this.BoolValue = source.BoolValue;
    }

    public TypedValue(int value)
    {
        this.IntValue = value;
    }
    public TypedValue(double value)
    {
        this.DoubleValue = value;
    }
    public TypedValue(string value)
    {
        this.StringValue = value;
    }
    public TypedValue(bool value)
    {
        this.BoolValue = value;
    }

    //public void CopyFrom(TypedValue source) // not for struct!!! use 'constructor' TypedValue(TypedValue source)
    //{
    //    this.type = source.type;
    //    this.IntValue = source.IntValue;
    //    this.DoubleValue = source.DoubleValue;
    //    this.StringValue = source.StringValue;  
    //    this.BoolValue = source.BoolValue;
    //}

    /****
    public void SetFrom(TokenConstantType token)
    {
        if (token is TokenConstant<int> ti)
        {
            IntValue = ti.value;
            type = ExpressionType.Int;
        }
        else if (token is TokenConstant<double> td)
        {
            DoubleValue = td.value;
            type = ExpressionType.Double;
        }
        else if (token is TokenConstant<string> ts)
        {
            StringValue = ts.value;
            type = ExpressionType.Str;
        }
        else if (token is TokenConstant<bool> tb)
        {
            BoolValue = tb.value;
            type = ExpressionType.Bool;
        }
    }
    ****/
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

        if (type1 == ExpressionType.Int && type2 == ExpressionType.Double)
            type1 = ExpressionType.Double;
        else if (type1 == ExpressionType.Double && type2 == ExpressionType.Int)
            type2 = ExpressionType.Double;

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
                    if (type1 == ExpressionType.Int)
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