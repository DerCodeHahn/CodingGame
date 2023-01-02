class MidGame : GamePhase
{
    Dictionary<Field, int> controlledUnits = new Dictionary<Field, int>();
    public override void Execute(GameBoard gameBoard)
    {
        base.Execute(gameBoard);

        controlledUnits.Clear();
        CloseBorders(controlledUnits);

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
                    Console.Error.WriteLine($"Flank with {field.PositionLog()} to {moveField.PositionLog()}");
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
            if (buildField.GoodSpawn || buildField.OffenceSpawn)
            {
                if (buildField.Pressure < 0)
                    //if (buildField.canBuild)
                        defenceBuildSpawn.Add(buildField);
                    // else
                    //     offenceSpawn.Add(buildField);
                else if (buildField.Pressure == 0)
                {
                    if (buildField.OffenceSpawn)
                        offenceSpawn.Add(buildField);
                    if (buildField.GoodSpawn)
                        spawnAtFree.Add(buildField);
                }
                else
                {
                    offenceSpawn.Add(buildField);
                }

            }

        }
        //defenceBuildSpawn.Sort(Field.SortByPressure);
        List<(Field, int)> DefenceValues = AnalysePointsOnRisk(defenceBuildSpawn);
        foreach ((Field field, int pointLossScore) defence in DefenceValues)
        {
            Console.Error.WriteLine($"Position {defence.field.PositionLog()} with Score: {defence.pointLossScore}");
        }
        foreach ((Field field, int pointLossScore) defence in DefenceValues)
        {
            if (gameBoard.MyMatter < Consts.BuildCost)
                break;
            if(defence.field.canBuild)
            {
                command += ActionsBuilder.Build(defence.field);
                Field f = gameBoard[defence.field.X, defence.field.Y];
                f.recycler = true;
                gameBoard[defence.field.X, defence.field.Y] = f;
                gameBoard.MyMatter -= Consts.BuildCost;
            }
            else if(defence.field.canSpawn)
            {
                command += ActionsBuilder.Spawn(defence.field, defence.field.Pressure * -1);
                gameBoard.MyMatter -= Consts.BuildCost;
            }
        }

        offenceSpawn.Sort(Field.SortByGameDirection);
        //List<(Field, int)> DefenceUnitSpawnValues = AnalysePointsOnRisk(offenceSpawn,true);
        
        foreach (Field offence in offenceSpawn)
        {
            Console.Error.WriteLine($"OffenceSpawn {offence.PositionLog()}");
        }

        if (gameBoard.MyMatter >= Consts.BuildCost && offenceSpawn.Count != 0)
        {
            int spawnableUnits = Math.Max((gameBoard.MyMatter / Consts.BuildCost) / offenceSpawn.Count, 1);
            Console.Error.WriteLine(spawnableUnits + " ");
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
        foreach (Field field in spawnAtFree)
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

    List<(Field, int)> AnalysePointsOnRisk(List<Field> fields)
    {
        List<(Field, int)> pointsAtRisk = new();
        HashSet<Field> fieldCounted = new();
        foreach (Field defendPoint in fields)
        {
            
            fieldCounted.Clear();
            fieldCounted.Add(defendPoint);
            int points = 1; //the field it self

            bool inbound = defendPoint.GetFieldInDirection(true, gameBoard, out Field fieldInAttackDirection);
            bool horizontalAttack = inbound && fieldInAttackDirection.enemies && fieldInAttackDirection.units >= 1;
            bool inboundDefend = defendPoint.GetFieldInDirection(false, gameBoard, out Field fieldInDefendDirection);
            bool freeAreaBehindAttack = inboundDefend && !fieldInDefendDirection.enemies && !fieldInDefendDirection.mine;

            foreach (Field buildAroundField in defendPoint.GetPossibleMoveDirection(gameBoard))
            {
                if (buildAroundField.mine)
                {
                    points++; // field where i build gets destroyed
                    //check how much gets Destroyed by blocking
                    if (buildAroundField.canBuild || buildAroundField.canSpawn)
                        foreach (Field checkMyField in buildAroundField.GetPossibleMoveDirection(gameBoard))
                        {
                            bool notDefendPoint = checkMyField != defendPoint;
                            bool alreadyCounted = fieldCounted.Contains(checkMyField);
                            //TODO Check if Field is in my half or enemy half, or even a enemy field
                            //TODO Check my unit count may be i dont have to build
                            if (checkMyField.mine && notDefendPoint && !alreadyCounted && buildAroundField.scrapAmount >= checkMyField.scrapAmount)
                            {
                                fieldCounted.Add(checkMyField);
                                points++; // field gets destroyed 
                            }
                        }
                }
                else
                {
                    if (horizontalAttack && buildAroundField == fieldInDefendDirection && freeAreaBehindAttack)
                        points += Player.PlayDirection == 1 ? GameBoard.width - buildAroundField.X : buildAroundField.X;
                }

            }
            if (horizontalAttack)
                points *= 2;
            Console.Error.WriteLine($"DefendPoint {defendPoint.PositionLog()} would loose {points}");
            pointsAtRisk.Add((defendPoint, points));
        }
        pointsAtRisk.Sort((x, y) => { return x.Item2.CompareTo(y.Item2) * -1; });

        return pointsAtRisk;
    }

    private void CloseBorders(Dictionary<Field, int> controlledUnits)
    {
        bool canSpawn = gameBoard.MyMatter >= Consts.BuildCost;
        if (!rowMappedFields.ContainsKey(0))
        {
            for (int i = 0; i < GameBoard.height; i++)
            {
                if (myRowMappedUnits.ContainsKey(i))
                {
                    Field closestUnit = myRowMappedUnits[i][0];
                    Console.Error.WriteLine("Close Top Border With " + closestUnit.PositionLog());
                    Field borderField = gameBoard.fields[closestUnit.X, 0];
                    while(borderField.scrapAmount == 0)
                    {
                        borderField = gameBoard.fields[closestUnit.X, borderField.Y + 1];
                    }
                    controlledUnits.Add(closestUnit, 1);
                    command += ActionsBuilder.Move(closestUnit, borderField, 1);
                    break;
                }
                if (rowMappedFields.ContainsKey(i - 1))
                {
                    Field closestSpawn = UTIL.GetFurthestField(rowMappedFields[i - 1]);
                    Console.Error.WriteLine("Close Top Border With new Spawn" + closestSpawn.PositionLog());
                    command += ActionsBuilder.Spawn(closestSpawn, 1);
                    gameBoard.MyMatter -= Consts.BuildCost;
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
                    Field closestUnit = UTIL.GetFurthestField(myRowMappedUnits[i]);
                    Console.Error.WriteLine("Close Bot Border With " + closestUnit.PositionLog());
                    controlledUnits.Add(closestUnit, 1);
                    Field borderField = gameBoard.fields[closestUnit.X, GameBoard.height - 1];
                    while(borderField.scrapAmount == 0)
                    {
                        borderField = gameBoard.fields[closestUnit.X, borderField.Y - 1];
                    }
                    command += ActionsBuilder.Move(closestUnit,borderField , 1);
                    break;
                }
                if (rowMappedFields.ContainsKey(i + 1))
                {
                    Field closestSpawn = UTIL.GetFurthestField(rowMappedFields[i + 1]);
                    Console.Error.WriteLine("Close Bot Border With new Spawn" + closestSpawn.PositionLog());
                    command += ActionsBuilder.Spawn(closestSpawn, 1);
                    gameBoard.MyMatter -= Consts.BuildCost;
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