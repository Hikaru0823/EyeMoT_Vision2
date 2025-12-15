//using NUnit.Framework;

[System.Serializable]
public class ClampedValue<T> where T : struct, System.IComparable<T>
{
    public T Value;
    public T Min;
    public T Max;

    public ClampedValue() {}
    public ClampedValue(T value, T min, T max)
    {
        Min = min;
        Max = max;
        Value = Clamp(value);
    }

    public ClampedValue(T min, T max)
    {
        Min = min;
        Max = max;
        Value = Clamp(default(T));
    }

    public void Set(T v)
    {
        Value = Clamp(v);
    }

    private T Clamp(T v)
    {
        if (v.CompareTo(Min) < 0) return Min;
        if (v.CompareTo(Max) > 0) return Max;
        return v;
    }
}