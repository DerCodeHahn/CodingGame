class MidGame : GamePhase
{
    Dictionary<Field, int> controlledUnits = new Dictionary<Field, int>();
    public override void Execute(GameBoard gameBoard)
    {
        base.Execute(gameBoard);

        controlledUnits.Clear();

        BuildDefense();
        MoveIntoFreeFieldForward();
        //TODO: keep an eye on overall Mattle to not get overrun
        //TODO: improve flanking right now its not useable because of backwalk

        DecideAction();
        if (command == "")
            Console.WriteLine(ActionsBuilder.Wait());
        else
            Console.WriteLine(command);
    }

    void MoveIntoFreeFieldForward()
    {
        foreach (Field field in gameBoard.MyUnits)
        {
            foreach (Field moveField in field.GetPossibleMoveDirection(gameBoard))
            {
                bool BackWards = field.X == moveField.X + Player.PlayDirection;
                if (!BackWards && !moveField.mine && !moveField.enemies)
                {
                    if (!controlledUnits.ContainsKey(moveField))
                        controlledUnits.Add(moveField, 0);
                    controlledUnits[moveField] = field.units;
                    command += ActionsBuilder.Move(field, moveField, field.units);
                    Console.Error.WriteLine("Flank");
                    continue;
                }
            }
        }
    }

    void BuildDefense()
    {
        List<Field> defenceBuildSpawn = new List<Field>();
        List<Field> offenceSpawn = new List<Field>();
        List<Field> spawnAtFree = new();
        foreach (Field buildField in gameBoard.MyFields)
        {
            if (buildField.GoodSpawn|| buildField.OffenceSpawn)
            {
                if (buildField.Pressure < 0)
                    if (buildField.canBuild)
                        defenceBuildSpawn.Add(buildField);
                    else
                        offenceSpawn.Add(buildField);
                else if (buildField.Pressure == 0)
                {
                    if(buildField.OffenceSpawn)
                        offenceSpawn.Add(buildField);
                    if(buildField.GoodSpawn)
                        spawnAtFree.Add(buildField);
                }
                else
                {
                    offenceSpawn.Add(buildField);
                }

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
            Console.Error.WriteLine("OffenceSpawn" + offence.PositionLog());
        }
        
        if(gameBoard.MyMatter >= Consts.BuildCost && offenceSpawn.Count != 0)
        {
            int spawnableUnits = Math.Max((gameBoard.MyMatter/10) / offenceSpawn.Count,1);
            Console.Error.WriteLine(spawnableUnits + " " );
            foreach (Field offence in offenceSpawn)
            {
                if (gameBoard.MyMatter < Consts.BuildCost)
                    break;
                Console.Error.WriteLine("OffenceSpawn At" + offence.PositionLog());
                command += ActionsBuilder.Spawn(offence, spawnableUnits);
                gameBoard.MyMatter -= Consts.BuildCost;
            }
        }

        spawnAtFree.Sort(Field.SortByGameDirection);
        foreach(Field field in spawnAtFree)
        {
            if (gameBoard.MyMatter < Consts.BuildCost)
                break;
            Console.Error.WriteLine("Free Spawn At" + field.PositionLog());
                
            command += ActionsBuilder.Spawn(field, 1);
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
                if (myRowMappedUnits.ContainsKey(i))
                {
                    Field closestUnit = myRowMappedUnits[i][0];
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
                if (myRowMappedUnits.ContainsKey(i))
                {
                    Field closestUnit = myRowMappedUnits[i][0];
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
        if (Consts.BuildCost * mustSpawn <= gameBoard.MyMatter)
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
        int unitsLeft = (unit.units - alreadyUsed);
        if (unitsLeft <= 0)
            return;
        //Attack Nearest Enemy Field
        foreach (Field checkField in unit.GetPossibleMoveDirection(gameBoard))
        {
            disscoverdFields.Add(checkField);
            visistedFields.Add(checkField);
            if (checkField.enemies && unit.Pressure > 0)
            {
                unitsLeft -= unit.Pressure;
                command += ActionsBuilder.Move(unit, checkField, unit.Pressure);
                
                return;
            }
        }

        foreach (Field f in disscoverdFields)
            currentFields.Add(f); 

        //Search for next Enemy field
        while (currentFields.Count > 0)
        {
            if (unitsLeft <= 0)
                    return;
            foreach (Field f in currentFields)
            {
                visistedFields.Add(f);
                foreach (Field checkField in f.GetPossibleMoveDirection(gameBoard))
                {
                    //TODO: If two enemy fields are possible try to splitt up
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