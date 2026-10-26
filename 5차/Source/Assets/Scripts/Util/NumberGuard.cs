public static class NumberGuard
{
    public static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    public static int Score(long value){return value<=0?0:value>=int.MaxValue?int.MaxValue:(int)value;}
}
