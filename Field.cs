using System.Diagnostics.CodeAnalysis;
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
    public bool OffenceSpawn;
    public int Pressure;
    public int PressureChangeForecast;

    static Direction[] MoveDirectionsLeftToRight = new Direction[]
    {
        new Direction(-1, 0), new Direction(1, 0),new Direction(0, -1), new Direction(0, 1)
    };
    static Direction[] MoveDirectionsRightToLeft = new Direction[]
    {
        new Direction(1, 0), new Direction(-1, 0),new Direction(0, -1), new Direction(0, 1)
    };

    public Field(byte x, byte y, byte scrapAmount, bool mine, bool enemies, byte units, bool recycler, bool canBuild, bool canSpawn, bool inRangeOfRecycler)
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
        OffenceSpawn = false;
        Pressure = 0;
        PressureChangeForecast = 0;
    }

    public static int SortByTotalCollectableScrap(Field x, Field y)
    {
        return x.TotalCollectableScrap.CompareTo(y.TotalCollectableScrap) * -1;
    }

    public static int SortByPressure(Field x, Field y)
    {
        return x.Pressure.CompareTo(y.Pressure);
    }

    public static int SortByGameDirection(Field x, Field y)
    {
        int xSort = x.X.CompareTo(y.X);
        xSort *= Player.PlayDirection * -1;
        return xSort;
    }

    public string PositionLog()
    {
        return $"{X} {Y}";
    }

    public string Info()
    {
        return $"{PositionLog()} scrap {scrapAmount} mine {mine} enemies {enemies} units {units} recycler {recycler} canBuild {canBuild}";
    }

    public List<Field> GetPossibleMoveDirection(GameBoard board, bool invertDirection = false)
    {
        List<Field> possibleDirection = new();
        int Direction = Player.PlayDirection;
        if(invertDirection)
            Direction *= -1;
        Direction[] MoveDirections = Direction == -1 ? MoveDirectionsLeftToRight : MoveDirectionsRightToLeft;
        foreach (Direction direction in MoveDirections)
        {
            byte x = (byte)(X + direction.X);
            byte y = (byte)(Y + direction.Y);
            if (!UTIL.CheckForInBound(x, y))
                continue;
            Field targetField = board[x, y];
            if (targetField.scrapAmount != 0 && !targetField.recycler)
            {
                possibleDirection.Add(targetField);
            }
        }

        return possibleDirection;
    }

    public bool GetFieldInDirection(bool AttackDirection, GameBoard gameboard, out Field directionField )
    {
        int direction = AttackDirection ? Player.PlayDirection : Player.PlayDirection * -1;
        int x = X + direction;
        bool inbound = UTIL.CheckForInBound(x, Y);
        directionField = gameboard[0,0];
        if(!inbound)
        {
            return false;
        }
        else
        {
            directionField = gameboard[x,Y];
            return true;
        }
    }



    static public bool operator ==(Field self, Field other)
    {
        return self.X == other.X && self.Y == other.Y;
    }

    static public bool operator !=(Field self, Field other)
    {
        return self.X != other.X || self.Y != other.Y;
    }

    public override int GetHashCode()
    {
        int x = X;
        int y = Y;
        int hash = x << 8 | y;
        return hash;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj == null)
            return false;
        if (typeof(Field) != obj.GetType())
            return false;
        Field other = (Field)obj;

        return X == other.X && Y == other.Y;
    }
}