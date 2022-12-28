struct Direction{
    public sbyte X;
    public sbyte Y;

    public Direction(sbyte x, sbyte y)
    {
        X = x;
        Y = y;
    }
    public static implicit operator (sbyte x, sbyte y)(Direction direction)
    {
        return (direction.X,direction.Y);
    }
}