using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

class GameBoard
{
    public static int width, height;
    public Field[,] fields;

    public HashSet<Field> MyFields;
    public HashSet<Field> EnemieFields;
    public HashSet<Field> FreeField;

    public HashSet<Field> EnemieBorderFields;
    public HashSet<Field> MyBorderFields;

    public int MyMatter;

    public List<Field> MyUnits;
    public List<Field> EnemyUnits;

    public string CommandGettingHere;
    public float score;

    public List<Action> CurrentCommands = new List<Action>();

    public Field this[int x, int y]
    {
        get
        {
            return fields[x, y];
        }
        set { fields[x, y] = value; }
    }

    public GameBoard(Field[,] fields)
    {
        this.fields = fields;
        MyFields = new();
        EnemieFields = new();
        FreeField = new();
        MyUnits = new();
        MyBorderFields = new();
        EnemieBorderFields = new();
        CommandGettingHere = "";
        MyMatter = 0;
        EnemyUnits = new();
    }

    public GameBoard(GameBoard board)
    {
        fields = (Field[,])board.fields.Clone();
        MyFields = board.MyFields;
        EnemieFields = new HashSet<Field>();
        FreeField = new HashSet<Field>();
        MyUnits = new List<Field>();
        CommandGettingHere = board.CommandGettingHere;
        MyMatter = board.MyMatter;
        CurrentCommands = new List<Action>();
    }

    public static int SortByScore(GameBoard board1, GameBoard board2)
    {
        return board1.score.CompareTo(board2.score) * -1;
    }

    public void Analize()
    {
        MyFields.Clear();
        EnemieFields.Clear();
        FreeField.Clear();
        MyBorderFields.Clear();
        EnemieBorderFields.Clear();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Field field = fields[x, y];
                //Console.Error.Write(field.GetHashCode()+", ");

                if (field.scrapAmount == 0)
                    continue;

                GetSurroundingValues(ref field);

                fields[x, y] = field;

                if (field.mine)
                {
                    MyFields.Add(field);
                    if (field.units >= 1)
                    {
                        MyUnits.Add(field);
                    }
                }
                else if (field.enemies)
                {
                    if(field.units >= 1)
                        EnemyUnits.Add(field);
                    EnemieFields.Add(field);
                }
                else
                    FreeField.Add(field);
            }
        }

        CalculateScore();
    }

    private void CalculateScore()
    {
        int verticalScore = VerticalScore();

        int pureScore = MyFields.Count - EnemieFields.Count;
        score = pureScore + verticalScore / 2;
        //Console.Error.WriteLine ($"Current Score : {score} : MyFields {MyFields.Count} , Enemie {EnemieFields.Count}, MyUnits :{MyUnits.Count}");
    }

    int VerticalScore()
    {
        HashSet<Field> set = new(); // use Set for perfomance unique
        foreach (var unit in MyUnits)
        {
            for (byte vPos = unit.X; vPos >= 0 && vPos < width; vPos = (byte)(vPos + Player.PlayDirection * -1)) // Looking back to my base
            {
                if (fields[vPos, unit.Y].scrapAmount != 0) // take fields wich i can own into account
                {
                    bool isNew = set.Add(fields[vPos, unit.Y]); // add unique to set 
                    if (isNew) // if we found one we can skip the rest of the line
                        break;
                }
            }
        }
        set.ExceptWith(MyFields);
        return set.Count;
    }

    public List<Field> GetHigherSurroundingsFields()
    {
        List<Field> mine = new List<Field>(MyFields);
        mine = mine.FindAll((c) => { return c.SuroundingStays; });
        return mine;
    }

    public List<Field> GetSpawnFields()
    {
        List<Field> spawns = new();
        foreach (Field f in MyFields)
            if (f.GoodSpawn)
                spawns.Add(f);
        return spawns;
    }

    void GetSurroundingValues(ref Field field)
    {
        field.TotalCollectableScrap = field.scrapAmount;
        field.SuroundingStays = true;
        field.Pressure = (field.mine) ? field.units : -field.units;
        foreach (Field direction in field.GetPossibleMoveDirection(this))
        {
            CheckNeighbour(ref field, direction);
        }
    }

    private void CheckNeighbour(ref Field field, Field neighbour)
    {
        //Max Amount Clamped by own scrapAmount
        field.TotalCollectableScrap += Math.Clamp(neighbour.scrapAmount, (byte)0, field.scrapAmount);

        if (field.mine && neighbour.enemies)
            field.Pressure -= neighbour.units;
        if (field.enemies && neighbour.mine)
            field.Pressure += neighbour.units;

        if (!CheckForHigherSurrounding(field, neighbour))
            field.SuroundingStays = false;
        if(field.canSpawn && neighbour.enemies)
            field.OffenceSpawn = true;
        if (field.canSpawn && !neighbour.mine && !neighbour.enemies)
        {
            field.GoodSpawn = true;
        }

        if (field.mine && !neighbour.mine)
        {
            if (!MyBorderFields.Contains(neighbour))
                MyBorderFields.Add(neighbour);
        }

        if (field.enemies && !neighbour.enemies)
        {
            if (!EnemieBorderFields.Contains(neighbour))
                EnemieBorderFields.Add(neighbour);
        }
    }

    bool CheckForHigherSurrounding(Field field, Field surroundedField)
    {
        if (surroundedField.scrapAmount != 0 && surroundedField.scrapAmount <= field.scrapAmount)
            return false;
        else
            return true;
    }

    internal void ExecuteCommands(List<Action> moveCommands)
    {
        CurrentCommands = moveCommands;

        foreach (Action action in moveCommands)
        {
            action.Execute(this);
        }
        Analize(); // TODO : Coud move Everthing into Execute to not iterate over all Fields again
        //TODO: Increase Metal
        //TODO: Haverst Matel from colletors
    }

    public string GetBuildString()
    {
        string command = "";
        // foreach (Action action in CurrentCommands)
        // {
        //     command += action.Build();
        // }
        return command;
    }

    // internal Field NextFree (Vector2 myUnit)
    // {
    //     List<Field> notmine = new List<Field> (EnemieFields);
    //     notmine.AddRange (FreeField);
    //     float MinDistance = float.MaxValue;
    //     Field clostest = new Field ();
    //     foreach (Field field in notmine)
    //     {
    //         float distance = Vector2.Distance (field.position, myUnit);
    //         if (distance < MinDistance)
    //         {
    //             MinDistance = distance;
    //             clostest = field;
    //         }
    //     }
    //     return clostest;
    // }
}