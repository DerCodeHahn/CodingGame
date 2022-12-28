class MidGame : GamePhase
{
    Dictionary<Field, int> controlledUnits = new Dictionary<Field, int>();
    public override void Execute(GameBoard gameBoard)
    {
        base.Execute(gameBoard);

        controlledUnits.Clear();

        BuildDefense();

        DecideAction();
        if (command == "")
            Console.WriteLine(ActionsBuilder.Wait());
        else
            Console.WriteLine(command);
    }

    void BuildDefense()
    {
        List<Field> defenceBuildSpawn = new List<Field>();
        List<Field> offenceSpawn = new List<Field>();
        foreach (Field buildField in gameBoard.MyFields)
        {
            if (buildField.GoodSpawn)
            {
                if (buildField.Pressure < 0 )
                    if(buildField.canBuild)
                        defenceBuildSpawn.Add(buildField);
                    else
                        offenceSpawn.Add(buildField);
                else if (buildField.Pressure == 0)
                    offenceSpawn.Add(buildField);

            }

        }
        defenceBuildSpawn.Sort(Field.SortByPressure);
        foreach (Field defence in defenceBuildSpawn)
        {
            if (gameBoard.MyMatter < Consts.BuildCost)
                break;
            command += ActionsBuilder.Build(defence);
            gameBoard.MyMatter -= Consts.BuildCost;
        }

        offenceSpawn.Sort(Field.SortByGameDirection);
        foreach (Field offence in offenceSpawn)
        {
            if (gameBoard.MyMatter < Consts.BuildCost)
                break;
            
            command += ActionsBuilder.Spawn(offence,offence.Pressure * -1 + 1);
            gameBoard.MyMatter -= Consts.BuildCost;
        }
    }

    void DecideAction()
    {
        CloseBorders(controlledUnits);

        foreach (Field unit in gameBoard.MyUnits)
        {
            if (unit.Pressure < 0)
            {
                Defence(unit);
            }

            if (unit.Pressure > 0)
            {
                Attack(unit);
            }
            // if (unit.Pressure == 0)
            // {
            //     WaitPrepareAttack(unit);
            // }
        }
    }

    private void CloseBorders(Dictionary<Field, int> controlledUnits)
    {
        if (!rowMappedFields.ContainsKey(0))
        {
            for (int i = 0; i < GameBoard.height; i++)
            {
                if (rowMappedUnits.ContainsKey(i))
                {
                    Field closestUnit = rowMappedUnits[i][0];
                    Console.Error.WriteLine("Close Top Border With " + closestUnit.PositionLog());

                    controlledUnits.Add(closestUnit, 1);
                    command += ActionsBuilder.Move(closestUnit, closestUnit.X, 0, 1);
                    break;
                }
            }
        }

        if (!rowMappedFields.ContainsKey(GameBoard.height - 1))
        {
            for (int i = GameBoard.height - 1; i >= 0; i--)
            {
                if (rowMappedUnits.ContainsKey(i))
                {
                    Field closestUnit = rowMappedUnits[i][0];
                    Console.Error.WriteLine("Close Bot Border With " + closestUnit.PositionLog());
                    controlledUnits.Add(closestUnit, 1);
                    command += ActionsBuilder.Move(closestUnit, closestUnit.X, GameBoard.height - 1, 1);
                    break;
                }
            }
        }
    }

    void Defence(Field unit)
    {
        int mustSpawn = unit.Pressure * -1;
        if(Consts.BuildCost * mustSpawn <= gameBoard.MyMatter )
        {
            gameBoard.MyMatter -= Consts.BuildCost * mustSpawn;
            command += ActionsBuilder.Spawn(unit, mustSpawn);
        }
    }

    void WaitPrepareAttack(Field unit)
    {

    }

    void Attack(Field unit)
    {
        HashSet<Field> disscoverdFields = new();
        HashSet<Field> currentFields = new();
        HashSet<Field> visistedFields = new();
        bool found = controlledUnits.TryGetValue(unit, out int alreadyUsed);
        int unitsLeft = unit.units - alreadyUsed;
        if (unitsLeft <= 0)
            return;
        //Attack Nearest Enemy Field
        foreach ((sbyte x, sbyte y) direction in unit.GetPossibleMoveDirection(gameBoard))
        {
            Field checkField = gameBoard[unit.X + direction.x, unit.Y + direction.y];
            disscoverdFields.Add(checkField);
            visistedFields.Add(checkField);
            if (checkField.enemies)
            {
                unitsLeft -= unit.Pressure;
                command += ActionsBuilder.Move(unit, checkField, unit.Pressure);
                if (unitsLeft <= 0)
                    return;
            }
        }

        foreach (Field f in disscoverdFields)
            currentFields.Add(f);

        //Search for next Enemy field
        while (currentFields.Count > 0)
        {
            foreach (Field f in currentFields)
            {
                visistedFields.Add(f);
                foreach ((sbyte x, sbyte y) direction in f.GetPossibleMoveDirection(gameBoard))
                {
                    Field checkField = gameBoard[f.X + direction.x, f.Y + direction.y];
                    if (checkField.enemies)
                    {
                        unitsLeft -= unit.Pressure;
                        Console.Error.WriteLine($"{unit.PositionLog()} to {checkField.PositionLog()}");
                        command += ActionsBuilder.Move(unit, checkField, unit.Pressure);
                        return;
                    }
                    if (!visistedFields.Contains(checkField) &&
                       !disscoverdFields.Contains(checkField))
                        disscoverdFields.Add(checkField);
                }
            }
            currentFields.Clear();

            foreach (Field f in disscoverdFields)
                currentFields.Add(f);
            disscoverdFields.Clear();
        }
        foreach (Field lastFree in visistedFields)
        {
            if (!lastFree.mine)
            {
                command += ActionsBuilder.Move(unit, lastFree, unit.Pressure);
                return;
            }
        }
    }
}