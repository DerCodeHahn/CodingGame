class EarlyGame : GamePhase
{
    public override void Execute(GameBoard gameBoard)
    {
        base.Execute(gameBoard);
        


        BuildSpeedUps();
        FlankDetection(true);
        FlankDetection(false);

        MoveFastestAttackPoint();

        Console.WriteLine(command);
    }



    void BuildSpeedUps()
    {
        if (gameBoard.MyMatter >= Consts.BuildCost)
        {
            List<Field> higherSurroundingFields = gameBoard.GetHigherSurroundingsFields();
            foreach (Field field in higherSurroundingFields)
            {
                if (field.canBuild && gameBoard.MyMatter >= Consts.BuildCost && field.TotalCollectableScrap >= 20)
                {
                    gameBoard.MyMatter -= 10;
                    command += ActionsBuilder.Build(field);
                }
            }
        }
    }
    
    void BuildArmee()
    {
        if (gameBoard.MyMatter >= Consts.BuildCost)
        {
            int max = rowMappedFields.Keys.Max();
            int min = rowMappedFields.Keys.Min();

            int bestIndexMax = Player.PlayDirection == 1 ? rowMappedFields[max].Count - 1 : 0;
            int bestIndexMin = Player.PlayDirection == 1 ? rowMappedFields[min].Count - 1 : 0;

            bool canSpawnOnBothSides = gameBoard.MyMatter >= Consts.BuildCost * 2;
            bool maxRowFree = (max != GameBoard.height - 1);
            bool minRowFree = (min != 0);

            Console.Error.WriteLine($"in {max} von {GameBoard.height - 1} = free {maxRowFree}");
            if (maxRowFree)
                if (canSpawnOnBothSides || !canSpawnOnBothSides && Player.GameStep % 2 == 0)
                    //if (!gameBoard.MyUnits.Contains (rowMappedFields[max][bestIndexMax]))
                    command += ActionsBuilder.Spawn(rowMappedFields[max][bestIndexMax], 1);

            if (minRowFree)
                if (canSpawnOnBothSides || !canSpawnOnBothSides && Player.GameStep % 2 == 1 && minRowFree)
                    //if (!gameBoard.MyUnits.Contains (rowMappedFields[min][bestIndexMin]))
                    command += ActionsBuilder.Spawn(rowMappedFields[min][bestIndexMin], 1);

        }
    }

    void MoveFastestAttackPoint()
    {

        //Console.Error.WriteLine("Update AttackLine");
        EasyUpdateAttackLine();

        //DynamicAttackLine();


        // toDO send all other to the next field / front

        // Find Point Symetry Attack Line
        // Update Frontline by unit movement
        //
        foreach (Field attackPoint in Player.AttackLine)
        {
            bool canSpawn = gameBoard.MyMatter >= Consts.BuildCost;
            Field bestAttackField = base.FindBestMoveOrSpawn(attackPoint, AlreadySelectedUnits, out Field moveTarget, canSpawn);
            if (attackPoint == bestAttackField)
                continue;
            bool found = AlreadySelectedUnits.TryGetValue(bestAttackField, out int sofarControlled);
            Console.Error.WriteLine($" Best Field {bestAttackField.PositionLog()} to {attackPoint.PositionLog()} {sofarControlled}");
            if (bestAttackField.units >= sofarControlled)
            {
                if (!AlreadySelectedUnits.Keys.Contains(bestAttackField))
                    AlreadySelectedUnits.Add(bestAttackField, 0);
                AlreadySelectedUnits[bestAttackField]++;

                UpdateNotSelected(NotSelectedUnits, bestAttackField);

                command += ActionsBuilder.Move(bestAttackField, moveTarget, 1);
            }
            else
            {
                gameBoard.MyMatter -= Consts.BuildCost;
                command += ActionsBuilder.Spawn(bestAttackField, 1);
            }
        }

        foreach (var field in NotSelectedUnits)
        {
            if (field.Value >= 1)
            {
                Console.Error.WriteLine("Sending" + field.Key.PositionLog() + "to the front");
                foreach (Field f in Player.AttackLine)
                {
                    Console.Error.WriteLine("AttackLine" + f.PositionLog());
                    if (f.Y == field.Key.Y)
                        command += ActionsBuilder.Move(field.Key, f, field.Value);

                }
                if (enemyRowMappedUnits.ContainsKey(field.Key.Y))
                {
                    command += ActionsBuilder.Move(field.Key, enemyRowMappedUnits[field.Key.Y][0], field.Value);
                }
            }
        }
    }

    private static void UpdateNotSelected(Dictionary<Field, int> NotSelectedUnits, Field bestAttackField)
    {
        bool foundNotSelected = NotSelectedUnits.TryGetValue(bestAttackField, out int count);
        if (foundNotSelected)
            NotSelectedUnits[bestAttackField]--;
        else
        {
            foreach (var field in NotSelectedUnits.Keys)
                if (field == bestAttackField)
                    NotSelectedUnits[field]--;
        }
    }

    int L2Distance(Field field, Field other)
    {
        return Math.Abs(field.X - other.X) + Math.Abs(field.Y - other.Y);
    }

    void EasyUpdateAttackLine()
    {
        if (OneUnitReachedAttackLine())
        {
            //Console.Error.WriteLine("Move AttackLine");
            List<Field> newAttackLine = new List<Field>();
            foreach (Field item in Player.AttackLine)
            {
                Field newField = gameBoard[item.X + Player.PlayDirection, item.Y];
                newAttackLine.Add(newField);
            }
            Player.AttackLine = newAttackLine;
        }
    }

    bool OneUnitReachedAttackLine()
    {
        foreach (Field f in gameBoard.MyUnits)
        {
            if (Player.AttackLine.Contains(f))
            {
                Console.Error.WriteLine("Found");
                return true;
            }
        }
        Console.Error.WriteLine("Not Found");
        return false;
    }
    Dictionary<Field, (Field source, int steps, int newFields)> MovementMap = new();
    void DynamicAttackLine()
    {
        MovementMap.Clear();
        HashSet<Field> VisitedFields = new();
        HashSet<Field> CurrentFields = new(gameBoard.EnemyUnits.Concat(gameBoard.MyUnits));
        HashSet<Field> InspectList = new();
        Player.AttackLine.Clear();
        int Step = 1;
        while (CurrentFields.Count != 0)
        {
            foreach (Field currentField in CurrentFields)
            {
                foreach (Field checkField in currentField.GetPossibleMoveDirection(gameBoard, currentField.enemies))
                {
                    //Console.Error.Write($" {checkField.PositionLog()}, ");
                    bool alreadyDiscoverd = MovementMap.ContainsKey(checkField);
                    Field discoverdField = checkField;//copy field
                    discoverdField.mine = currentField.mine;
                    discoverdField.enemies = currentField.enemies;

                    if (alreadyDiscoverd)
                    {
                        Field AlreadyDiscoverdField = MovementMap.Keys.First<Field>((f) => { return f == discoverdField; });
                        if (discoverdField == AlreadyDiscoverdField)
                        {
                            Console.Error.Write("-");
                            if (discoverdField.mine && AlreadyDiscoverdField.enemies)
                            {
                                Console.Error.WriteLine("found collision" + discoverdField.PositionLog());
                                Player.AttackLine.Add(discoverdField);
                            }
                        }
                    }
                    else
                    {
                        InspectList.Add(discoverdField);
                        MovementMap.Add(discoverdField, (currentField, Step, 0));
                    }
                }
                VisitedFields.Add(currentField);
            }
            CurrentFields.Clear();
            foreach (Field item in InspectList)
            {
                CurrentFields.Add(item);
            }
            InspectList.Clear();
            Console.Error.WriteLine($"Step {Step}");
            Step++;
        }
        //TODO: Implement Step like progress for the whole map

    }

    void Move()
    {
        foreach (List<Field> fields in myRowMappedUnits.Values)
        {
            bool first = true;
            if (Player.PlayDirection == 1)
                fields.Reverse();

            foreach (Field field in fields)
            {
                int AwayCenterDirection = Math.Clamp(field.Y.CompareTo(Player.MyBasePosition.Y), -1, 1);
                Console.Error.WriteLine($"base at {Player.MyBasePosition.Y}: {field.PositionLog()} {AwayCenterDirection}");
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
                        first = true;
                        //Assume the way is blocked so search
                        Field TargetField = gameBoard[field.X, field.Y + AwayCenterDirection];
                        bool found = FindNextFreeFieldInRow(TargetField, out Field newTarget);
                        if (found)
                            command += ActionsBuilder.Move(field, field.X, field.Y + AwayCenterDirection, 1);
                        else
                            Console.Error.WriteLine("EndReached");

                    }
                }
            }
        }

        //moveCommands += ActionsBuilder.Move (myUnit.x, myUnit.y, (byte) (myUnit.x + direction.x), (byte) (myUnit.y + direction.y), myUnit.count);
    }

    bool FindNextFreeFieldInRow(Field field, out Field newTarget)
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

    public override bool CheckTransition(GameBoard gameBoard)
    {
        List<Field> myBorderFields = new List<Field>(25);
        foreach (Field field in gameBoard.MyFields)
        {
            foreach (Field borderField in field.GetPossibleMoveDirection(gameBoard))
            {
                if (borderField.enemies)
                    return true;
            }
        }
        return false;
    }

    public override GamePhase Transition()
    {
        MidGame midGame = new MidGame();
        return midGame;
    }
}