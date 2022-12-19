class EarlyGame : GamePhase
{
    string command = "";

    public override void Execute (GameBoard gameBoard)
    {
        base.Execute(gameBoard);
        command = "";

        BuildSpeedUps ();
        BuildArmee ();
        Move ();
        Console.WriteLine (command);
    }

    void BuildSpeedUps ()
    {
        if (gameBoard.MyMatter >= Consts.BuildCost)
        {
            List<Field> higherSurroundingFields = gameBoard.GetHigherSurroundingsFields ();
            foreach (Field field in higherSurroundingFields)
            {
                if (field.canBuild && gameBoard.MyMatter >= Consts.BuildCost && field.TotalCollectableScrap >= 20)
                {
                    gameBoard.MyMatter -= 10;
                    command += ActionsBuilder.Build (field);
                }
            }
        }
    }

    void BuildArmee ()
    {
        if (gameBoard.MyMatter >= Consts.BuildCost)
        {
            int max = rowMappedFields.Keys.Max();
            int min = rowMappedFields.Keys.Min();

            int bestIndexMax = Player.PlayDirection == 1 ? rowMappedFields[max].Count - 1: 0;
            int bestIndexMin = Player.PlayDirection == 1 ? rowMappedFields[min].Count - 1: 0;

            bool canSpawnOnBothSides = gameBoard.MyMatter >= Consts.BuildCost * 2;
            bool maxRowFree = (max != GameBoard.height - 1);
            bool minRowFree = (min != 0);

            Console.Error.WriteLine($"in {max} von {GameBoard.height - 1} = free {maxRowFree}");
            if(maxRowFree)
                if(canSpawnOnBothSides || !canSpawnOnBothSides && Player.GameStep % 2 == 0)
                    command += ActionsBuilder.Spawn (rowMappedFields[max][bestIndexMax], 1);

            if(minRowFree)
                if(canSpawnOnBothSides || !canSpawnOnBothSides && Player.GameStep % 2 == 1 && minRowFree)
                    command += ActionsBuilder.Spawn (rowMappedFields[min][bestIndexMin], 1);
            
            


        }
    }

    void Move ()
    {
        foreach (List<Field> fields in rowMappedUnits.Values)
        {
            bool first = true;
            if (Player.PlayDirection == 1)
                fields.Reverse();
            foreach (Field field in fields)
            {
                int AwayCenterDirection = field.Y < Player.MyBasePosition.y ? -1 : 1;
                for (int i = 0; i < field.units; i++)
                {
                    //TODO LOOP as long as Fields are not possible
                    if (first)
                    {
                        first = false;
                        command += ActionsBuilder.Move(field, field.X + Player.PlayDirection, field.Y, 1);
                    }
                    else
                    {
                        command += ActionsBuilder.Move(field, field.X, field.Y + AwayCenterDirection, 1);
                    }
                }
            }
        }

        //moveCommands += ActionsBuilder.Move (myUnit.x, myUnit.y, (byte) (myUnit.x + direction.x), (byte) (myUnit.y + direction.y), myUnit.count);
    }

    

    protected override bool CheckTransition ()
    {
        return false;
    }

    protected override GamePhase Transition ()
    {
        return new EarlyGame ();
    }
}