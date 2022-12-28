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

    public static bool CheckForInBound(int x, int y)
    {
        return x >= 0 && y >= 0 && x < GameBoard.width && y < GameBoard.height;
    }
}