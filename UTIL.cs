static class UTIL
{
    public static int Distance(Field a, Field b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.X - b.X);
    }

    public static int Distance(Field a, (int X, int Y) b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.X - b.X);
    }

    public static bool CheckForInBound(int x, int y)
    {
        return x >= 0 && y >= 0 && x < GameBoard.width && y < GameBoard.height;
    }

    public static Field GetFurthestField(List<Field> fields)
    {
        bool toTheRight = Player.PlayDirection == 1;
        int startIndex = toTheRight ? fields.Count - 1 : 0;
        int target = toTheRight ? 0 : fields.Count - 1;

        for(int i = startIndex; toTheRight && i >= 0 || !toTheRight && i <= fields.Count - 1; i+=Player.PlayDirection)
        {
            if (fields[i].inRangeOfRecycler && fields[i].scrapAmount == 1)
                continue;
            return fields[i];
        }
        //FallBack
        if (Player.PlayDirection == 1)
            return fields[fields.Count - 1];
        else
            return fields[0];
    }
}