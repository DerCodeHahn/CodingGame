class GamePhase
{
    protected string command = "";
    protected GameBoard gameBoard;

    protected Dictionary<int, List<Field>> myRowMappedUnits = new (GameBoard.height);
    protected Dictionary<int, List<Field>> enemyRowMappedUnits = new (GameBoard.height);
    protected Dictionary<int, List<Field>> rowMappedFields = new (GameBoard.height);

    protected Dictionary<Field, int> AlreadySelectedUnits = new Dictionary<Field, int>();
    protected Dictionary<Field, int> NotSelectedUnits = new Dictionary<Field, int>();

    virtual public void Execute (GameBoard board)
    {
        this.gameBoard = board;

        RowMapMyUnits ();
        RowMapMyFields ();
        RowMapEnemyUnits ();

        AlreadySelectedUnits.Clear();
        NotSelectedUnits.Clear();

        foreach (Field field in gameBoard.MyUnits)
            NotSelectedUnits.Add(field, field.units);

        command = "";
    }

    virtual public bool CheckTransition (GameBoard gameBoard)
    {
        return false;
    }

    virtual public GamePhase Transition ()
    {
        return this;
    }

    protected void RowMapMyUnits ()
    {
        myRowMappedUnits.Clear ();

        foreach (Field myUnit in gameBoard.MyUnits)
        {
            if (!myRowMappedUnits.ContainsKey (myUnit.Y))
                myRowMappedUnits.Add (myUnit.Y, new List<Field> ());
            myRowMappedUnits[myUnit.Y].Add (myUnit);
        }
    }
    protected void RowMapEnemyUnits ()
    {
        enemyRowMappedUnits.Clear ();

        foreach (Field enemyUnit in gameBoard.EnemyUnits)
        {
            if (!enemyRowMappedUnits.ContainsKey (enemyUnit.Y))
                enemyRowMappedUnits.Add (enemyUnit.Y, new List<Field> ());
            enemyRowMappedUnits[enemyUnit.Y].Add (enemyUnit);
        }
    }

    protected void RowMapMyFields ()
    {
        rowMappedFields.Clear ();

        foreach (Field field in gameBoard.MyFields)
        {
            if (!rowMappedFields.ContainsKey (field.Y))
                rowMappedFields.Add (field.Y, new List<Field> ());
            rowMappedFields[field.Y].Add (field);
        }
    }

    protected Field FindBestMoveOrSpawn (Field AttackField, Dictionary<Field, int> AlreadySelectedUnits, out Field moveTarget, bool withSpawn = true)
    {
        HashSet<Field> visitedFields = new HashSet<Field> ();
        HashSet<Field> currentFields = new HashSet<Field> ();
        moveTarget = AttackField;
        currentFields.Add (AttackField);
        bool found = false;
        HashSet<Field> inspectList = new ();
        Dictionary<Field, int> spawnList = new ();
        while (!found)
        {
            inspectList.Clear ();

            foreach (Field field in currentFields)
            {
                //Otp could have an Defensive Mode where Playdirection is take into account
                foreach (Field checkField in field.GetPossibleMoveDirection (gameBoard))
                {
                    int openUnitCount = checkField.units;
                    if (AlreadySelectedUnits.Keys.Contains (checkField))
                        openUnitCount -= AlreadySelectedUnits[checkField];

                    if (checkField.mine && openUnitCount >= 1)
                    {
                        moveTarget = field;
                        return checkField;
                    }
                    if (withSpawn && checkField.mine && !spawnList.Keys.Contains (checkField))
                        spawnList.Add (checkField, Settings.OffsetToFindSpawn);
                    if (!visitedFields.Contains (checkField) && !inspectList.Contains (checkField))
                        inspectList.Add (checkField);
                }
                visitedFields.Add (field);
            }

            currentFields.Clear ();
            foreach (Field field in inspectList)
            {
                currentFields.Add (field);
            }

            if (withSpawn)
            {
                foreach (var spawn in spawnList)
                {
                    if (spawn.Value == 0)
                        return spawn.Key;
                    spawnList[spawn.Key] = spawn.Value - 1;
                }
            }

            if (currentFields.Count == 0)
                found = true;
        }
        Console.Error.WriteLine ($"No way found to {AttackField.PositionLog()}");
        return AttackField;
    }

    protected void FlankDetection (bool Top)
    {
        if(enemyRowMappedUnits.Count == 0 || myRowMappedUnits.Count == 0)
            return;
        int mostmyTopRow = Top ? myRowMappedUnits.Keys.Min () : myRowMappedUnits.Keys.Max ();
        int enemieTopUnit = Top ? enemyRowMappedUnits.Keys.Min () : enemyRowMappedUnits.Keys.Max ();
        if (Top)
            Console.Error.WriteLine ($"My Top{mostmyTopRow} other Top{enemieTopUnit}");
        else
            Console.Error.WriteLine ($"My Bottom{mostmyTopRow} other Bottom{enemieTopUnit}");

        if (Top && mostmyTopRow <= enemieTopUnit || !Top && mostmyTopRow >= enemieTopUnit)
            return;


        int myDefendingUnitIndex = Player.PlayDirection == 1 ? myRowMappedUnits[mostmyTopRow].Count - 1 : 0;
        Field DefendingUnit = myRowMappedUnits[mostmyTopRow][myDefendingUnitIndex];

        int AttackingUnitIndex = Player.PlayDirection == 1 ? enemyRowMappedUnits[enemieTopUnit].Count - 1 : 0;
        Field AttackingUnit = enemyRowMappedUnits[enemieTopUnit][AttackingUnitIndex];
        Console.Error.WriteLine ($"Attacking Unit{AttackingUnit.PositionLog()} Def{DefendingUnit.PositionLog()}");

        HashSet<Field> myVisitedFields = new ();
        HashSet<Field> myCurrentFields = new ();
        HashSet<Field> myInspectList = new ();

        HashSet<Field> enemyVisitedFields = new ();
        HashSet<Field> enemyCurrentFields = new ();
        HashSet<Field> enemyInspectList = new ();

        Dictionary<Field, int> EnemyStepCounter = new ();

        myCurrentFields.Add (DefendingUnit);
        foreach (Field enemy in gameBoard.EnemyUnits)
        {
            bool furtherOutThanMe = Top && enemy.Y < mostmyTopRow || !Top && enemy.Y > mostmyTopRow;
            if(furtherOutThanMe)
                enemyCurrentFields.Add (enemy);
        }
        
        int step = 0;

        while (enemyCurrentFields.Count > 0)
        {
            foreach (Field f in enemyCurrentFields)
            {
                if(enemyVisitedFields.Contains(f))
                    break;
                enemyVisitedFields.Add (f);
                EnemyStepCounter.Add (f, step);
                bool hasField = f.GetFieldInDirection (false, gameBoard, out Field moveField);

                if ( !enemyVisitedFields.Contains (moveField)) // !moveField.enemies &&
                    enemyInspectList.Add (moveField);
                if(hasField && moveField.mine)
                    DefendFlankBySpawn (moveField);
            }
            step++;
            enemyCurrentFields.Clear ();

            foreach (Field f in enemyInspectList)
                enemyCurrentFields.Add (f);
            enemyInspectList.Clear ();
        }
        step = 1;
        while (myCurrentFields.Count > 0)
        {
            foreach (Field f in myCurrentFields)
            {
                myVisitedFields.Add (f);
                foreach (Field moveField in f.GetPossibleMoveDirection (gameBoard))
                {
                    if (!moveField.mine && !myVisitedFields.Contains (moveField))
                        myInspectList.Add (moveField);

                    if (EnemyStepCounter.ContainsKey (moveField))
                    {
                        int stepBalance = EnemyStepCounter[moveField] - step;
                        if (stepBalance == 1 || stepBalance == 2)
                        {
                            Console.Error.WriteLine ($"Defending at {moveField.PositionLog()}");
                            DefendFlank (DefendingUnit, moveField);
                            return;
                        }
                    }
                }

            }
            step++;
            myCurrentFields.Clear ();
            foreach (Field f in myInspectList)
                myCurrentFields.Add (f);
            myInspectList.Clear ();
        }
    }

    private void DefendFlankBySpawn(Field moveField)
    {
        command += ActionsBuilder.Spawn (moveField, 1);
        gameBoard.MyMatter -= Consts.BuildCost;
    }

    protected void DefendFlank (Field defendingUnit, Field defendField)
    {
        AlreadySelectedUnits.Add (defendingUnit, 1);
        NotSelectedUnits[defendingUnit]--;
        command += ActionsBuilder.Move (defendingUnit, defendField, 1);
    }
}