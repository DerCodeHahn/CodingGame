class MidGame : GamePhase
{
    int nothingChangedCounter = 0;
    int oldPoints = 0;
    Field conquereUnit;
    bool Conquering = false;
    private bool topClosed = false;
    private bool bottomClosed = false;

    public override void Execute (GameBoard gameBoard)
    {
        base.Execute (gameBoard);

        AlreadySelectedUnits.Clear ();

        //DontCountFlankingUnitsForDefence ();

        ConquereMapOnStuck ();
        SaveUnits ();
        //CloseBorders (AlreadySelectedUnits);
        FlankDetection(true);
        FlankDetection(false);
        BuildDefense ();
        MoveIntoFreeFieldForward ();
        //TODO: keep an eye on overall Mattle to not get overrun

        DecideAction ();
        if (command == "")
            Console.WriteLine (ActionsBuilder.Wait ());
        else
            Console.WriteLine (command);
    }

    void DontCountFlankingUnitsForDefence ()
    {
        foreach (var row in myRowMappedUnits)
        {
            Field field = UTIL.GetFurthestField (row.Value);

            foreach (Field moveField in field.GetPossibleMoveDirection (gameBoard))
            {
                bool BackWards = field.X == moveField.X + Player.PlayDirection;
                if (!BackWards && !moveField.mine && !moveField.enemies)
                { 
                    Console.Error.WriteLine($"Mark {field.PositionLog()} as Flank");
                    IncreasePressure (field, field.units);
                    break;
                }
            }
        }
    }

    private void SaveUnits ()
    {
        foreach (Field unit in gameBoard.MyUnits)
        {
            if (unit.scrapAmount == 1 && unit.inRangeOfRecycler)
                MoveAway (unit);
        }
    }

    private void MoveAway (Field unit)
    {
        List < (Field, int) > PrioList = new (4);
        foreach (Field f in unit.GetPossibleMoveDirection (gameBoard))
        {
            if (f.enemies && !f.recycler) // attack
            {
                PrioList.Add ((f, 1));
            }
            else if (!f.enemies && !f.mine) // move to free
            {
                PrioList.Add ((f, 2));
            }
            else //justsave
            {
                int prio = f.X == unit.X ? 3 : 4;
                PrioList.Add ((f, prio));
            }
        }
        PrioList.Sort ((x1, x2) => { return x1.Item2.CompareTo (x2.Item2); });

        foreach ((Field, int) f in PrioList)
        {
            Console.Error.WriteLine ($"Save {unit.PositionLog()} to {f.Item1.PositionLog()} with Prio {f.Item2} ");
        }
        if (PrioList.Count >= 1)
        {
            AlreadySelectedUnits.Add (unit, unit.units);
            command += ActionsBuilder.Move (unit, PrioList[0].Item1, unit.units);
        }

    }

    void MoveIntoFreeFieldForward ()
    {
        foreach (var row in myRowMappedUnits)
        {
            Field field = UTIL.GetFurthestField (row.Value);

            foreach (Field moveField in field.GetPossibleMoveDirection (gameBoard))
            {
                bool BackWards = field.X == moveField.X + Player.PlayDirection;
                if (!BackWards && !moveField.mine && !moveField.enemies)
                {
                    if (!AlreadySelectedUnits.ContainsKey (moveField))
                        AlreadySelectedUnits.Add (moveField, 0);
                    AlreadySelectedUnits[moveField] = field.units;
                    IncreasePressure (field, field.units);
                    command += ActionsBuilder.Move (field, moveField, field.units);
                    Console.Error.WriteLine ($"Flank with {field.PositionLog()} to {moveField.PositionLog()}");
                    continue;
                }
            }
        }
    }

    void BuildDefense ()
    {
        List<Field> defenceBuildSpawn = new List<Field> ();
        List<Field> offenceSpawn = new List<Field> ();
        List<Field> spawnAtFree = new ();
        foreach (Field buildField in gameBoard.MyFields)
        {
            if (buildField.GoodSpawn || buildField.OffenceSpawn)
            {
                Console.Error.WriteLine ($"At Field {buildField.PositionLog()} {buildField.Pressure + buildField.PressureChangeForecast } =  {buildField.Pressure}+{buildField.PressureChangeForecast}");
                if (buildField.Pressure + buildField.PressureChangeForecast < 0)
                    //if (buildField.canBuild)
                    defenceBuildSpawn.Add (buildField);
                // else
                //     offenceSpawn.Add(buildField);
                else if (buildField.Pressure == 0)
                {
                    if (buildField.OffenceSpawn)
                        offenceSpawn.Add (buildField);
                    if (buildField.GoodSpawn)
                        spawnAtFree.Add (buildField);
                }
                else
                {
                    offenceSpawn.Add (buildField);
                }

            }

        }
        //defenceBuildSpawn.Sort(Field.SortByPressure);
        List < (Field, int) > DefenceValues = AnalysePointsOnRisk (defenceBuildSpawn);
        foreach ((Field field, int pointLossScore) defence in DefenceValues)
        {
            Console.Error.WriteLine ($"Position {defence.field.PositionLog()} with Score: {defence.pointLossScore}");
        }
        foreach ((Field field, int pointLossScore) defence in DefenceValues)
        {
            if (gameBoard.MyMatter < Consts.BuildCost)
                break;
            if (defence.field.inRangeOfRecycler && defence.field.scrapAmount == 1)
                continue;
            if (defence.field.canBuild)
            {
                command += ActionsBuilder.Build (defence.field);
                Field f = gameBoard[defence.field.X, defence.field.Y];
                f.recycler = true;
                gameBoard[defence.field.X, defence.field.Y] = f;
                gameBoard.MyMatter -= Consts.BuildCost;
            }
            else if (defence.field.canSpawn)
            {
                int spawnUnits = Math.Max (defence.field.Pressure * -1, 1);
                Console.Error.WriteLine ($"Deffence Spawn {defence.field.PositionLog()}");
                command += ActionsBuilder.Spawn (defence.field, spawnUnits);
                DecreasePressure (defence.field);

                gameBoard.MyMatter -= Consts.BuildCost * spawnUnits;
            }
        }

        offenceSpawn.Sort (Field.SortByGameDirection);
        //List<(Field, int)> DefenceUnitSpawnValues = AnalysePointsOnRisk(offenceSpawn,true);

        // foreach (Field offence in offenceSpawn)
        // {
        //     Console.Error.WriteLine ($"OffenceSpawn {offence.PositionLog()}");
        // }

        if (gameBoard.MyMatter >= Consts.BuildCost && offenceSpawn.Count != 0)
        {
            int spawnableUnits = Math.Max ((gameBoard.MyMatter / Consts.BuildCost) / offenceSpawn.Count, 1);
            Console.Error.WriteLine (spawnableUnits + " ");
            foreach (Field offence in offenceSpawn)
            {
                if (gameBoard.MyMatter < Consts.BuildCost)
                    break;
                if (offence.inRangeOfRecycler && offence.scrapAmount == 1)
                    continue;
                Console.Error.WriteLine ("OffenceSpawn At" + offence.PositionLog () + " with " + spawnableUnits);
                command += ActionsBuilder.Spawn (offence, spawnableUnits);
                DecreasePressure (offence);
                gameBoard.MyMatter -= Consts.BuildCost * spawnableUnits;
            }
        }

        spawnAtFree.Sort (Field.SortByGameDirection);
        foreach (Field field in spawnAtFree)
        {
            if (gameBoard.MyMatter < Consts.BuildCost)
                break;
            if (field.inRangeOfRecycler && field.scrapAmount == 1)
                continue;
            Console.Error.WriteLine ("Free Spawn At" + field.PositionLog ());

            command += ActionsBuilder.Spawn (field, 1);
            gameBoard.MyMatter -= Consts.BuildCost;
        }
    }

    void IncreasePressure (Field field, byte units)
    {
        int index = gameBoard.MyUnits.FindIndex (0, gameBoard.MyUnits.Count, (x) => { return x == field; });
        if (index == -1) //no units to influence
            return;
        gameBoard.MyFields.Remove (field);
        field.PressureChangeForecast -= units;

        gameBoard.MyFields.Add (field);
        gameBoard.MyUnits[index] = field;
    }
    private void DecreasePressure (Field defence)
    {
        int index = gameBoard.MyUnits.FindIndex (0, gameBoard.MyUnits.Count, (x) => { return x == defence; });
        if (index == -1) //no units to influence
            return;
        Field target = gameBoard.MyUnits[index];
        target.PressureChangeForecast += 1;
        gameBoard.MyUnits[index] = target;
    }

    void DecideAction ()
    {
        foreach (Field unit in gameBoard.MyUnits)
        {
            int Pressure = unit.Pressure + unit.PressureChangeForecast;
            if (Pressure < 0)
            {
                Defence (unit);
            }

            if (Pressure > 0)
            {
                Attack (unit);
            }
            // if (unit.Pressure == 0)
            // {
            //     WaitPrepareAttack(unit);
            // }
        }
    }

    List < (Field, int) > AnalysePointsOnRisk (List<Field> fields)
    {
        List < (Field, int) > pointsAtRisk = new ();
        HashSet<Field> fieldCounted = new ();
        foreach (Field defendPoint in fields)
        {

            fieldCounted.Clear ();
            fieldCounted.Add (defendPoint);
            int points = 1; //the field it self

            bool inbound = defendPoint.GetFieldInDirection (true, gameBoard, out Field fieldInAttackDirection);
            bool horizontalAttack = inbound && fieldInAttackDirection.enemies && fieldInAttackDirection.units >= 1;
            bool inboundDefend = defendPoint.GetFieldInDirection (false, gameBoard, out Field fieldInDefendDirection);
            bool freeAreaBehindAttack = inboundDefend && !fieldInDefendDirection.enemies && !fieldInDefendDirection.mine;

            foreach (Field buildAroundField in defendPoint.GetPossibleMoveDirection (gameBoard))
            {
                if (buildAroundField.mine)
                {
                    points++; // field where i build gets destroyed
                    //check how much gets Destroyed by blocking
                    if (buildAroundField.canBuild || buildAroundField.canSpawn)
                        foreach (Field checkMyField in buildAroundField.GetPossibleMoveDirection (gameBoard))
                        {
                            bool notDefendPoint = checkMyField != defendPoint;
                            bool alreadyCounted = fieldCounted.Contains (checkMyField);
                            //TODO Check if Field is in my half or enemy half, or even a enemy field
                            //TODO Check my unit count may be i dont have to build
                            if (checkMyField.mine && notDefendPoint && !alreadyCounted && buildAroundField.scrapAmount >= checkMyField.scrapAmount)
                            {
                                fieldCounted.Add (checkMyField);
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
            pointsAtRisk.Add ((defendPoint, points));
        }
        pointsAtRisk.Sort ((x, y) => { return x.Item2.CompareTo (y.Item2) * -1; });

        return pointsAtRisk;
    }

    private void CloseBorders (Dictionary<Field, int> controlledUnits)
    {
        bool canSpawn = gameBoard.MyMatter >= Consts.BuildCost;
        if (!rowMappedFields.ContainsKey (0) && !topClosed)
        {
            for (int i = 0; i < GameBoard.height; i++)
            {
                if (myRowMappedUnits.ContainsKey (i))
                {
                    Field closestUnit = myRowMappedUnits[i][0];
                    if (!controlledUnits.ContainsKey (closestUnit))
                    {
                        Console.Error.WriteLine ("Close Top Border With " + closestUnit.PositionLog ());
                        Field borderField = gameBoard.fields[closestUnit.X, 0];
                        while (borderField.scrapAmount == 0)
                        {
                            borderField = gameBoard.fields[closestUnit.X, borderField.Y + 1];
                        }
                        if (closestUnit == borderField)
                            topClosed = true;
                        else
                        {
                            controlledUnits.Add (closestUnit, 1);
                            command += ActionsBuilder.Move (closestUnit, borderField, 1);
                        }
                        break;
                    }
                }
                if (rowMappedFields.ContainsKey (i - 1))
                {
                    Field closestSpawn = UTIL.GetFurthestField (rowMappedFields[i - 1]);
                    Console.Error.WriteLine ("Close Top Border With new Spawn" + closestSpawn.PositionLog ());
                    command += ActionsBuilder.Spawn (closestSpawn, 1);
                    gameBoard.MyMatter -= Consts.BuildCost;
                    break;
                }
            }
        }

        if (!rowMappedFields.ContainsKey (GameBoard.height - 1) && !bottomClosed)
        {
            for (int i = GameBoard.height - 1; i >= 0; i--)
            {
                if (myRowMappedUnits.ContainsKey (i))
                {

                    Field closestUnit = UTIL.GetFurthestField (myRowMappedUnits[i]);
                    if (!controlledUnits.ContainsKey (closestUnit))
                    {
                        Console.Error.WriteLine ("Close Bot Border With " + closestUnit.PositionLog ());
                        controlledUnits.Add (closestUnit, 1);
                        Field borderField = gameBoard.fields[closestUnit.X, GameBoard.height - 1];
                        while (borderField.scrapAmount == 0)
                        {
                            borderField = gameBoard.fields[closestUnit.X, borderField.Y - 1];
                        }
                        if (closestUnit == borderField)
                            bottomClosed = true;
                        else
                        {
                            command += ActionsBuilder.Move (closestUnit, borderField, 1);
                        }
                        break;
                    }
                }
                if (rowMappedFields.ContainsKey (i + 1))
                {
                    Field closestSpawn = UTIL.GetFurthestField (rowMappedFields[i + 1]);
                    Console.Error.WriteLine ("Close Bot Border With new Spawn" + closestSpawn.PositionLog ());
                    command += ActionsBuilder.Spawn (closestSpawn, 1);
                    gameBoard.MyMatter -= Consts.BuildCost;
                    break;
                }
            }
        }
    }

    void Defence (Field unit)
    {
        if (unit.inRangeOfRecycler && unit.scrapAmount == 1)
            return;
        int mustSpawn = (unit.Pressure + unit.PressureChangeForecast) * -1;
        Console.Error.WriteLine($"Defence on {unit.PositionLog()} {unit.Pressure} + {unit.PressureChangeForecast}");
        if (Consts.BuildCost * mustSpawn <= gameBoard.MyMatter)
        {
            gameBoard.MyMatter -= Consts.BuildCost * mustSpawn;
            command += ActionsBuilder.Spawn (unit, mustSpawn);
        }
    }

    void WaitPrepareAttack (Field unit)
    {

    }

    void Attack (Field unit)
    {
        HashSet<Field> disscoverdFields = new ();
        HashSet<Field> currentFields = new ();
        HashSet<Field> visistedFields = new ();
        bool found = AlreadySelectedUnits.TryGetValue (unit, out int alreadyUsed);
        int unitsLeft = (unit.units - alreadyUsed);
        if (unitsLeft <= 0)
            return;
        //Attack Nearest Enemy Field
        List<Field> possibleFields = new ();
        foreach (Field checkField in unit.GetPossibleMoveDirection (gameBoard))
        {
            disscoverdFields.Add (checkField);
            visistedFields.Add (checkField);

            if (checkField.enemies && unit.Pressure + unit.PressureChangeForecast > 0)
            {
                possibleFields.Add (checkField);
            }
        }
        if (possibleFields.Count >= 1)
        {
            //SplittAttack (possibleFields, unit);
            int unitCount = unit.Pressure + unit.PressureChangeForecast;
            command += ActionsBuilder.Move (unit, possibleFields[0], unitCount);

            return;
        }

        foreach (Field f in disscoverdFields)
            currentFields.Add (f);

        //Search for next Enemy field
        while (currentFields.Count > 0)
        {
            if (unitsLeft <= 0)
                return;
            foreach (Field f in currentFields)
            {
                visistedFields.Add (f);
                foreach (Field checkField in f.GetPossibleMoveDirection (gameBoard))
                {
                    //TODO: If two enemy fields are possible try to splitt up
                    if (checkField.enemies)
                    {
                        unitsLeft -= unit.Pressure;
                        Console.Error.WriteLine ($"{unit.PositionLog()} to {checkField.PositionLog()}");
                        command += ActionsBuilder.Move (unit, checkField, unit.Pressure);
                        return;
                    }
                    if (!visistedFields.Contains (checkField) &&
                        !disscoverdFields.Contains (checkField))
                        disscoverdFields.Add (checkField);
                }

            }
            currentFields.Clear ();

            foreach (Field f in disscoverdFields)
                currentFields.Add (f);
            disscoverdFields.Clear ();
        }
        foreach (Field lastFree in visistedFields)
        {
            if (!lastFree.mine)
            {
                command += ActionsBuilder.Move (unit, lastFree, unit.Pressure);
                return;
            }
        }
    }

    void SplittAttack (List<Field> possibleAttackFields, Field targetUnit)
    {
        int unitsPerField;
        int rest = targetUnit.PressureChangeForecast;
        if (possibleAttackFields.Count == 1)
            unitsPerField = targetUnit.Pressure;
        else
        {
            unitsPerField = (targetUnit.Pressure + targetUnit.PressureChangeForecast) / possibleAttackFields.Count;
            rest = (targetUnit.Pressure + targetUnit.PressureChangeForecast) % possibleAttackFields.Count;
        }

        Console.Error.WriteLine ($"SplittAttack at {targetUnit.PositionLog()} to {possibleAttackFields.Count} Fields: {unitsPerField} + rest {rest} | Pressure {targetUnit.Pressure} + cast {targetUnit.PressureChangeForecast}");

        foreach (Field f in possibleAttackFields)
        {
            int unitCount = unitsPerField;
            if (rest > 0)
                unitCount += 1;
            command += ActionsBuilder.Move (targetUnit, f, unitCount);
            rest--;
        }

    }

    void ConquereMapOnStuck ()
    {
        Console.Error.WriteLine ($"ChangeCounter {nothingChangedCounter}");
        if (Conquering)
        {
            HashSet<Field> disscoverdSet = new ();
            HashSet<Field> currentSet = new ();
            HashSet<Field> checkedSet = new ();
            Dictionary<Field, Field> discoverList = new ();
            currentSet.Add (conquereUnit);
            while (currentSet.Count != 0)
            {
                foreach (Field checkField in currentSet)
                {
                    checkedSet.Add (checkField);
                    foreach (Field next in checkField.GetPossibleMoveDirection (gameBoard))
                    {
                        if (next == conquereUnit)
                            continue;
                        Console.Error.Write (next.PositionLog () + ", ");
                        if (!discoverList.ContainsKey (next))
                            discoverList.Add (next, checkField);
                        if (!next.mine && !next.enemies)
                        {
                            Field backtrackingField = next;
                            while (discoverList.ContainsKey (backtrackingField))
                            {
                                if (conquereUnit == discoverList[backtrackingField])
                                    break;
                                backtrackingField = discoverList[backtrackingField];
                            }
                            Console.Error.WriteLine ($"conquereUnit {conquereUnit.PositionLog()} to {next.PositionLog()} over {backtrackingField.PositionLog()}");
                            command += ActionsBuilder.Move (conquereUnit, backtrackingField, 1);
                            conquereUnit = backtrackingField;
                            return;
                        }
                        if (!next.enemies && !checkedSet.Contains (next) && !disscoverdSet.Contains (next))
                        {
                            disscoverdSet.Add (next);
                        }
                    }
                }
                currentSet.Clear ();

                foreach (Field f in disscoverdSet)
                    currentSet.Add (f);
                disscoverdSet.Clear ();
            }
            Conquering = false;
            nothingChangedCounter = 0;
        }

        if (oldPoints == gameBoard.MyFields.Count)
            nothingChangedCounter++;
        else
        {
            nothingChangedCounter = 0;
        }

        if (nothingChangedCounter >= Settings.StartConquereAfterSteps)
        {
            List<Field> spawnAtFree = new ();
            foreach (Field buildField in gameBoard.MyFields)
            {
                if (buildField.GoodSpawn)
                {
                    command += ActionsBuilder.Spawn (buildField, 1);
                    conquereUnit = buildField;
                    Conquering = true;
                    gameBoard.MyMatter -= Consts.BuildCost;
                    Console.Error.WriteLine ("spawning C-Unit " + buildField.PositionLog ());
                    break;
                }
            }
        }
        oldPoints = gameBoard.MyFields.Count;
    }
}