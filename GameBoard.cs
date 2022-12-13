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
                field.TotalCollectableScrap = 10;
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

    public Field GetRandomMyField ()
    { 
        Random rnd = new Random();
        return MyFields[rnd.Next(MyFields.Count)];
    }

    void GetSurroundingValues (ref Field field)
    {
        int count = field.scrapAmount;
        if (field.position.Y - 1 >= 0)
            count += this [field.position - Vector2.UnitY].scrapAmount;
        if (field.position.Y + 1 < height)
            count += this [field.position + Vector2.UnitY].scrapAmount;
        if (field.position.X - 1 >= 0)
            count += this [field.position - Vector2.UnitX].scrapAmount;
        if (field.position.X + 1 < width)
            count += this [field.position + Vector2.UnitX].scrapAmount;

        field.TotalCollectableScrap = count;
    }

    internal Field NextFree(Vector2 myUnit)
    {
        List<Field> notmine = new List<Field> (EnemieFields);
        notmine.AddRange (FreeField);
        float MinDistance = float.MaxValue;
        Field clostest = new Field();
        foreach(Field field in notmine)
        {
            float distance = Vector2.Distance(field.position, myUnit);
            if(distance < MinDistance)
            {
                MinDistance = distance;
                clostest = field;
            }
        }
        return clostest;
    }
}