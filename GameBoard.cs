using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

class GameBoard
{
    public static int width, height;
    public Field[, ] fields;

    public List<Field> MyFields;
    public List<Field> EnemieFields;
    public List<Field> FreeField;

    public List<Vector2> MyUnits;

    public Field this [Vector2 position]
    {
        get { return this [(int) position.X, (int) position.Y]; }
        set { fields[(int) position.X, (int) position.Y] = value; }
    }

    public Field this [int x, int y]
    {
        get { return fields[x, y]; }
        set { fields[x, y] = value; }
    }

    public GameBoard ()
    {
        this.fields = new Field[width, height];
        MyFields = new List<Field> ();
        EnemieFields = new List<Field> ();
        FreeField = new List<Field> ();
        MyUnits = new List<Vector2> ();
    }

    public GameBoard (Field[, ] fields)
    {
        this.fields = fields;
        MyFields = new List<Field> ();
        EnemieFields = new List<Field> ();
        FreeField = new List<Field> ();
        MyUnits = new List<Vector2> ();

    }

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

                if (field.owner == 1)
                {
                    MyFields.Add (field);
                    if (field.units >= 1)
                        for (var i = 0; i < field.units; i++)
                        {
                            MyUnits.Add (field.position);
                        }

                }
                else if (field.owner == 0)
                    EnemieFields.Add (field);
                else
                    FreeField.Add (field);
            }
        }

    }

    public List<Field> GetBestUnOwnedFields ()
    {
        List<Field> notmine = new List<Field> (EnemieFields);
        notmine.AddRange (FreeField);
        notmine.Sort (Field.SortByTotalCollectableScrap);
        return notmine;
    }

    public List<Field> GetHigherSurroundingsFields()
    {
        List<Field> mine = new List<Field> (MyFields);
        mine = mine.FindAll((c) => {return c.SuroundingStays;});
        return mine;
    }

    public Field GetRandomMyField ()
    {
        Random rnd = new Random ();
        return MyFields[rnd.Next (MyFields.Count)];
    }

    void GetSurroundingValues (ref Field field)
    {
        int count = field.scrapAmount;
        bool higherSurroundings = true;
        if (field.position.Y - 1 >= 0)
        {
            Field lower = this [field.position - Vector2.UnitY];
            count += lower.scrapAmount;
            if(!CheckForHigherSurrounding(field, lower))
                higherSurroundings = false;
        }
        if (field.position.Y + 1 < height)
        {
            Field upper = this [field.position + Vector2.UnitY];
            count += upper.scrapAmount;
           if(!CheckForHigherSurrounding(field, upper))
                higherSurroundings = false;
        }
        if (field.position.X - 1 >= 0)
        {
            Field left = this [field.position - Vector2.UnitX];
            count += left.scrapAmount;
            if(!CheckForHigherSurrounding(field, left))
                higherSurroundings = false;
        }
        if (field.position.X + 1 < width)
        {
            Field right = this [field.position + Vector2.UnitX];
            count += right.scrapAmount;
            if(!CheckForHigherSurrounding(field, right))
                higherSurroundings = false;
        }

        field.TotalCollectableScrap = count;

        field.SuroundingStays = higherSurroundings;
    }

    bool CheckForHigherSurrounding(Field field, Field surroundedField )
    {
        if(surroundedField.scrapAmount != 0 && surroundedField.scrapAmount <= field.scrapAmount)
            return false;
        else 
            return true;
    }

    internal Field NextFree (Vector2 myUnit)
    {
        List<Field> notmine = new List<Field> (EnemieFields);
        notmine.AddRange (FreeField);
        float MinDistance = float.MaxValue;
        Field clostest = new Field ();
        foreach (Field field in notmine)
        {
            float distance = Vector2.Distance (field.position, myUnit);
            if (distance < MinDistance)
            {
                MinDistance = distance;
                clostest = field;
            }
        }
        return clostest;
    }
}