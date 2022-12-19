static class UTIL
{
    public static int Distance (Field a, Field b)
    {
        return Math.Abs(a.X - b.X) +  Math.Abs(a.X - b.X);
    }

    public static int Distance (Field a, (int X, int Y) b)
    {
        return Math.Abs(a.X - b.X) +  Math.Abs(a.X - b.X);
    }
}