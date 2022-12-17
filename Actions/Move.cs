class Move : Action
{
    byte x, y, toX, toY, amount;

    public Move (byte x, byte y, byte toX, byte toY, byte amount)
    {
        this.x = x;
        this.y = y;
        this.toX = toX;
        this.toY = toY;
        this.amount = amount;
    }

    public override void Execute (GameBoard board)
    {
        Field newField = board[toX, toY];
        if (newField.scrapAmount == 0 || newField.recycler)
        {
            Console.Error.WriteLine ($"IllegaleMove : {Build()}");
            return;
        }
        Field field = board[x, y];
        field.units -= amount;

        if (newField.enemies)
        {
            if (amount < newField.units)
                newField.units -= amount;
            else if (amount > newField.units)
            {
                newField.units = (byte) (amount - newField.units);
                newField.mine = true;
                newField.enemies = false;
            }
            else
            {
                newField.units = 0;
            }
        }
        else
        {
            newField.units = amount;
            newField.mine = true;
        }

        board[x, y] = field;
        board[toX, toY] = newField;
        //Console.Error.WriteLine ("Old :" + field.Info ());
        //Console.Error.WriteLine ("New :" + newField.Info ());
    }

    public override string Build ()
    {
        return ActionsBuilder.Move (x, y, toX, toY, amount);
    }
}