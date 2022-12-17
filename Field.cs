using System.Numerics;

struct Field
{
    public byte X;
    public byte Y;
    public byte scrapAmount;
    public bool mine;
    public bool enemies;
    public byte units;
    public bool recycler;
    public bool canBuild;
    public bool canSpawn;
    public bool inRangeOfRecycler;
    public byte TotalCollectableScrap;
    public bool SuroundingStays;
    public bool GoodSpawn;

    static (sbyte x, sbyte y) [] MoveDirections = new (sbyte, sbyte) []
    {
        (0, -1), (0, 1), (-1, 0), (1, 0)
    };

    public Field (byte x, byte y, byte scrapAmount, bool mine, bool enemies, byte units, bool recycler, bool canBuild, bool canSpawn, bool inRangeOfRecycler)
    {
        X = x;
        Y = y;
        this.scrapAmount = scrapAmount;
        this.mine = mine;
        this.enemies = enemies;
        this.units = units;
        this.recycler = recycler;
        this.canBuild = canBuild;
        this.canSpawn = canSpawn;
        this.inRangeOfRecycler = inRangeOfRecycler;
        TotalCollectableScrap = 0;
        SuroundingStays = false;
        GoodSpawn = false;
    }

    public static int SortByTotalCollectableScrap (Field x, Field y)
    {
        return x.TotalCollectableScrap.CompareTo (y.TotalCollectableScrap) * -1;
    }

    public string PositionLog ()
    {
        return $"{X} {Y}";
    }

    public string Info()
    {
        return $"{PositionLog()} scrap {scrapAmount} mine {mine} enemies {enemies} units {units} recycler {recycler} canBuild {canBuild}";
    }


    public List < (sbyte, sbyte) > GetNeighbourDirection ()
    {
        List < (sbyte, sbyte) > possibleDirection = new ();
        foreach ((sbyte x, sbyte y) direction in MoveDirections)
        {
            byte x = (byte) (X + direction.x);
            byte y = (byte) (Y + direction.y);

            if (x > 0 && y > 0 && x < GameBoard.width && y < GameBoard.height)
                possibleDirection.Add(direction);
        }
        return possibleDirection;
    }

    public List < (sbyte, sbyte) > GetPossibleMoveDirection (GameBoard board)
    {
        List < (sbyte, sbyte) > possibleDirection = new();
        foreach ((sbyte x, sbyte y) direction in GetNeighbourDirection())
        {
            byte x = (byte) (X + direction.x);
            byte y = (byte) (Y + direction.y);

            Field targetField = board[x, y];
            if (targetField.scrapAmount != 0 && !targetField.recycler)
            {
                possibleDirection.Add (direction);
            }
        }

        return possibleDirection;
    }
}