namespace Execution.Compiled;

public abstract class VariableDef
{
    //public VariableDef(string name)
    //{
    //    //Name = name;
    //}

    //public string Name { get; set; }

    //public TypedValue typedValue = new TypedValue();

    public abstract TypedValue VarValue { get; set; }
}
public class GlobalVariableDef : VariableDef
{
    private TypedValue typedValue;
    //public override TypedValue TypedValue { get => typedValue; set => typedValue = value; }
    public override TypedValue VarValue
    {
        get
        {
            return typedValue;
        }
        set
        {
            typedValue = value;
        }
    }
}

public class LocalVariableDef : VariableDef
{       
    public int StackIndex = 0; // StackIndex for caller address to return = 0
    public override TypedValue VarValue { get => GetLocalVarValue(); set => SetLocalVarValue(value); }

    private TypedValue GetLocalVarValue() { return new TypedValue(); }
    private void SetLocalVarValue(TypedValue typedValue) { return; }
}

/**
public class VariableDef
{
    public VariableDef(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public TypedValue typedValue = new TypedValue();
}

public class LocalVariableDef
{
    public VariableDef(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public TypedValue typedValue = new TypedValue();
}
**/