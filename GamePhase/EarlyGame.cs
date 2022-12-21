class EarlyGame : GamePhase
{
    string command = "";

    public override void Execute (GameBoard gameBoard)
    {
        base.Execute (gameBoard);
        command = "";

        BuildSpeedUps ();
        BuildArmee ();
        MoveFromAttackPoint ();
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
            int max = rowMappedFields.Keys.Max ();
            int min = rowMappedFields.Keys.Min ();

            int bestIndexMax = Player.PlayDirection == 1 ? rowMappedFields[max].Count - 1 : 0;
            int bestIndexMin = Player.PlayDirection == 1 ? rowMappedFields[min].Count - 1 : 0;

            bool canSpawnOnBothSides = gameBoard.MyMatter >= Consts.BuildCost * 2;
            bool maxRowFree = (max != GameBoard.height - 1);
            bool minRowFree = (min != 0);

            Console.Error.WriteLine ($"in {max} von {GameBoard.height - 1} = free {maxRowFree}");
            if (maxRowFree)
                if (canSpawnOnBothSides || !canSpawnOnBothSides && Player.GameStep % 2 == 0)
                    //if (!gameBoard.MyUnits.Contains (rowMappedFields[max][bestIndexMax]))
                    command += ActionsBuilder.Spawn (rowMappedFields[max][bestIndexMax], 1);

            if (minRowFree)
                if (canSpawnOnBothSides || !canSpawnOnBothSides && Player.GameStep % 2 == 1 && minRowFree)
                    //if (!gameBoard.MyUnits.Contains (rowMappedFields[min][bestIndexMin]))
                    command += ActionsBuilder.Spawn (rowMappedFields[min][bestIndexMin], 1);

        }
    }

    void MoveFromAttackPoint ()
    {
        Dictionary<Field, int> AlreadySelected = new Dictionary<Field, int> ();
        //TODO : loop from center to borders
        Field AttackPoint = gameBoard[Player.middle, Player.MyBasePosition.Y];
        Console.Error.WriteLine ($"base at {Player.MyBasePosition.Y}: {AttackPoint.PositionLog()}");

        Field bestAttackField = Player.MyBasePosition;
        int minDistance = 100;
        foreach (Field unit in gameBoard.MyUnits)
        {
            if (AlreadySelected.ContainsKey (unit) && AlreadySelected[unit] <= 0)
                continue;
            int distance = L2Distance (AttackPoint, unit);
            Console.Error.WriteLine ($"{unit.PositionLog()} in {distance}");
            if (distance < minDistance)
            {
                bestAttackField = unit;
                minDistance = distance;
            }
        }
        foreach (Field spawn in gameBoard.GetSpawnFields ())
        {
            int distance = L2Distance (AttackPoint, spawn) + 2;
            if (distance < minDistance)
            {
                bestAttackField = spawn;
                minDistance = distance;
            }
        }
        Console.Error.WriteLine ($" Best Field {bestAttackField.PositionLog()} ");
        bool foundMoveFieldBefore = AlreadySelected.ContainsKey (bestAttackField);
        if (bestAttackField.units != 0 && (foundMoveFieldBefore && AlreadySelected[bestAttackField] < bestAttackField.units) || !foundMoveFieldBefore)
        {
            if (!foundMoveFieldBefore)
                AlreadySelected.Add (bestAttackField, bestAttackField.units);
            AlreadySelected[bestAttackField]--;
            command += ActionsBuilder.Move (bestAttackField, AttackPoint, 1);
        }
        else if (bestAttackField.units == 0 || foundMoveFieldBefore && AlreadySelected[bestAttackField] == bestAttackField.units)
        {
            command += ActionsBuilder.Spawn (bestAttackField, 1);
        }
    }

    int L2Distance (Field field, Field other)
    {
        return Math.Abs (field.X - other.X) + Math.Abs (field.Y - other.Y);
    }

    void Move ()
    {
        foreach (List<Field> fields in rowMappedUnits.Values)
        {
            bool first = true;
            if (Player.PlayDirection == 1)
                fields.Reverse ();

            foreach (Field field in fields)
            {
                int AwayCenterDirection = Math.Clamp (field.Y.CompareTo (Player.MyBasePosition.Y), -1, 1);
                Console.Error.WriteLine ($"base at {Player.MyBasePosition.Y}: {field.PositionLog() } {AwayCenterDirection}");
                for (int i = 0; i < field.units; i++)
                {
                    //TODO LOOP as long as Fields are not possible
                    if (first)
                    {
                        first = false;
                        command += ActionsBuilder.Move (field, field.X + Player.PlayDirection, field.Y, 1);
                    }
                    else
                    {
                        first = true;
                        //Assume the way is blocked so search
                        Field TargetField = gameBoard[field.X, field.Y + AwayCenterDirection];
                        bool found = FindNextFreeFieldInRow (TargetField, out Field newTarget);
                        if (found)
                            command += ActionsBuilder.Move (field, field.X, field.Y + AwayCenterDirection, 1);
                        else
                            Console.Error.WriteLine ("EndReached");

                    }
                }
            }
        }

        //moveCommands += ActionsBuilder.Move (myUnit.x, myUnit.y, (byte) (myUnit.x + direction.x), (byte) (myUnit.y + direction.y), myUnit.count);
    }

    bool FindNextFreeFieldInRow (Field field, out Field newTarget)
    {
        newTarget = field;
        for (int x = field.X; x >= 0 && x < GameBoard.width; x += Player.PlayDirection)
        {
            Field TargetField = gameBoard[x, field.Y];
            if (TargetField.mine || TargetField.scrapAmount == 0)
            {
                newTarget = TargetField;
                return true;
            }
        }
        return false;
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