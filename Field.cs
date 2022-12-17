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
    }

    public static int SortByTotalCollectableScrap (Field x, Field y)
    {
        return x.TotalCollectableScrap.CompareTo (y.TotalCollectableScrap) * -1;
    }

    public string PositionLog ()
    {
        return $"{X} {Y}";
    }
}