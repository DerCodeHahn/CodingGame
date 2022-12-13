using System.Numerics;

struct Field
{
    public Vector2 position;
    public int scrapAmount;
    public int owner;
    public int units;
    public bool recycler;
    public bool canBuild;
    public bool canSpawn;
    public bool inRangeOfRecycler;

    public int TotalCollectableScrap;

    public Field (Vector2 position, int scrapAmount, int owner, int units, bool recycler, bool canBuild, bool canSpawn, bool inRangeOfRecycler)
    {
        this.position = position;
        this.scrapAmount = scrapAmount;
        this.owner = owner;
        this.units = units;
        this.recycler = recycler;
        this.canBuild = canBuild;
        this.canSpawn = canSpawn;
        this.inRangeOfRecycler = inRangeOfRecycler;
        TotalCollectableScrap = 0;
    }

    public static int SortByTotalCollectableScrap (Field x, Field y)
    {
        return x.TotalCollectableScrap.CompareTo (y.TotalCollectableScrap) * -1;
    }

    public string Info ()
    {
        return $"{Actions.Print(position)}  {TotalCollectableScrap}";
    }
}