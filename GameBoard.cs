using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

class GameBoard
{
    public static int width, height;
    public Field[, ] fields;

    public HashSet<Field> MyFields;
    public HashSet<Field> EnemieFields;
    public HashSet<Field> FreeField;

    public byte MyMatter;

    public List < (byte x, byte y, byte n) > MyUnits; //x,y, count

    public string CommandGettingHere;
    public float score;

    public List<Action> CurrentCommands = new List<Action> ();

    public Field this [int x, int y]
    {
        get { return fields[x, y]; }
        set { fields[x, y] = value; }
    }

    public GameBoard (Field[, ] fields)
    {
        this.fields = fields;
        MyFields = new ();
        EnemieFields = new ();
        FreeField = new ();
        MyUnits = new ();
        CommandGettingHere = "";
        MyMatter = 0;
    }

    public GameBoard (GameBoard board)
    {
        this.fields = board.fields;
        MyFields = board.MyFields;
        EnemieFields = board.EnemieFields;
        FreeField = board.FreeField;
        MyUnits = board.MyUnits;
        CommandGettingHere = board.CommandGettingHere;
        MyMatter = board.MyMatter;
    }

    public static int SortByScore (GameBoard board1, GameBoard board2) { return board1.score.CompareTo (board2.score); }

    public void Analize ()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Field field = fields[x, y];
                if (field.scrapAmount == 0)
                    continue;

                GetSurroundingValues (ref field);

                if (field.mine)
                {
                    MyFields.Add (field);
                    if (field.units >= 1)
                        MyUnits.Add ((field.X, field.Y, field.units));
                }
                else if (field.enemies)
                    EnemieFields.Add (field);
                else
                    FreeField.Add (field);
            }
        }
        CalculateScore ();
    }

    private void CalculateScore ()
    {
        int verticalScore = VerticalScore ();

        int pureScore = MyFields.Count - EnemieFields.Count;
        score = pureScore + verticalScore / 2;
        Console.Error.WriteLine ($"Current Score : {score}");
    }

    int VerticalScore ()
    {
        HashSet<Field> set = new (); // use Set for perfomance unique
        foreach (var unit in MyUnits)
        {
            for (byte vPos = unit.x; vPos >= 0 && vPos < width; vPos = (byte) (vPos + Player.PlayDirection * -1)) // Looking back to my base
            {
                if (fields[vPos, unit.y].scrapAmount != 0) // take fields wich i can own into account
                {
                    bool isNew = set.Add (fields[vPos, unit.y]); // add unique to set 
                    if (isNew) // if we found one we can skip the rest of the line
                        break;
                }
            }
        }
        set.ExceptWith (MyFields);
        return set.Count;
    }

    public List<Field> GetHigherSurroundingsFields ()
    {
        List<Field> mine = new List<Field> (MyFields);
        mine = mine.FindAll ((c) => { return c.SuroundingStays; });
        return mine;
    }

    public List<Field> GetSpawnFields ()
    {
        List<Field> spawns = new ();
        foreach (Field f in MyFields)
            if (f.GoodSpawn)
                spawns.Add (f);
        return spawns;
    }

    void GetSurroundingValues (ref Field field)
    {
        field.TotalCollectableScrap = field.scrapAmount;

        if (field.Y - 1 >= 0)
        {
            CheckNeighbour (ref field, 0, -1);
        }
        if (field.Y + 1 < height)
        {
            CheckNeighbour (ref field, 0, 1);
        }
        if (field.X - 1 >= 0)
        {
            CheckNeighbour (ref field, -1, 0);
        }
        if (field.X + 1 < width)
        {
            CheckNeighbour (ref field, 1, 0);
        }
    }

    private void CheckNeighbour (ref Field field, sbyte xdelta, sbyte ydelta)
    {
        Field neighbour = fields[field.X + xdelta, field.Y + ydelta];
        //Max Amount Clamped by own scrapAmount
        field.TotalCollectableScrap += Math.Clamp (neighbour.scrapAmount, (byte) 0, field.scrapAmount);

        if (!CheckForHigherSurrounding (field, neighbour))
            field.SuroundingStays = false;

        if (field.canSpawn && (neighbour.enemies || !neighbour.mine))
            field.GoodSpawn = true;
    }

    bool CheckForHigherSurrounding (Field field, Field surroundedField)
    {
        if (surroundedField.scrapAmount != 0 && surroundedField.scrapAmount <= field.scrapAmount)
            return false;
        else
            return true;
    }

    internal void ExecuteCommands (List<Action> moveCommands)
    {
        CurrentCommands = moveCommands;

        foreach (Action action in moveCommands)
        {
            action.Execute (this);
        }
        Analize (); // TODO : Coud move Everthing into Execute to not iterate over all Fields again
        //TODO: Increase Metal
        //TODO: Haverst Matel from colletors
    }

    public string GetBuildString ()
    {
        string command = "";
        foreach (Action action in CurrentCommands)
        {
            command += action.Build ();
        }
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