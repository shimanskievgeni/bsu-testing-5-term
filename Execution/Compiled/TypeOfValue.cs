﻿using System.Runtime.InteropServices;

namespace Execution.Compiled;

public enum TypeOfValue
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
    private object? objectValue;
    public int IntValue
    {
        get => (int)objectValue;
        set
        {
            this.type = TypeOfValue.Int;
            objectValue = value;
        }
    }
    public double DoubleValue
    {
        get => (double)objectValue;
        set
        {
            this.type = TypeOfValue.Double;
            objectValue = value;
        }
    }
    public string? StringValue { 
        get => objectValue.ToString(); 
        set
        {
            this.type = TypeOfValue.Str;
            if (value != null)
            {
                objectValue = value;
            }
            else
            {
                this.type = TypeOfValue.Undefined;
                //StopOnError($"Null string used");
            }
        }
    }
    public bool BoolValue { 
        get => (bool)objectValue;
        set
        {
            this.type = TypeOfValue.Bool;
            objectValue = value;
        }
    }

    public object? ObjectValue { get => objectValue; set => objectValue = value; }

    public TypeOfValue type = TypeOfValue.Undefined;

    public TypedValue()
    {
        type = TypeOfValue.Undefined;
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
            type = TypeOfValue.Int;
        }
        else if (token is TokenConstant<double> td)
        {
            DoubleValue = td.value;
            type = TypeOfValue.Double;
        }
        else if (token is TokenConstant<string> ts)
        {
            StringValue = ts.value;
            type = TypeOfValue.Str;
        }
        else if (token is TokenConstant<bool> tb)
        {
            BoolValue = tb.value;
            type = TypeOfValue.Bool;
        }
    }
    ****/
}

public static class TypeResolver
{
    public static TypeOfValue ResultingOperationType(string operation, TypeOfValue type1, TypeOfValue type2)
    {
        if (type1 == TypeOfValue.Int || type1 == TypeOfValue.Double)
        { 
            if (operation == "Unary-"
                || operation == "Unary+"
               )
            return type1;
        }

        if (type1 == TypeOfValue.Int && type2 == TypeOfValue.Double)
            type1 = TypeOfValue.Double;
        else if (type1 == TypeOfValue.Double && type2 == TypeOfValue.Int)
            type2 = TypeOfValue.Double;

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
                return TypeOfValue.Bool;
            }

            if (operation == "+") // plus or string concat   
            {
                return type1;
            }
        }

        if (type1 == TypeOfValue.Int || type1 == TypeOfValue.Double) 
        {
            //if (operation == "++" || operation == "--")
            //{
            //    return type1;
            //}

            if (type2 == TypeOfValue.Int || type2 == TypeOfValue.Double)
            {
                if (operation == "+"
                    || operation == "-"
                    || operation == "*"
                    || operation == "/"
                    )
                {
                    if (type1 == TypeOfValue.Double || type2 == TypeOfValue.Double)
                        return TypeOfValue.Double;
                    else
                        return TypeOfValue.Int;
                }

                if (operation == "%")
                {
                    if (type1 == TypeOfValue.Int)
                        return type1;
                }
            }
        }

        if (type1 == TypeOfValue.Bool)
        {
            if (operation == "!")
            {
                return type1;
            }
            if (type2 == TypeOfValue.Bool)
            {
                if (operation == "&&" || operation == "||")
                {
                    return type1;
                }
            }
        }

        //StopOnError("qqqError");
        return TypeOfValue.Undefined;
    }

}