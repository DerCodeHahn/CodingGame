class GamePhase
{
    protected string command = "";
    protected GameBoard gameBoard;

    protected Dictionary<int, List<Field>> myRowMappedUnits = new (GameBoard.height);
    protected Dictionary<int, List<Field>> enemyRowMappedUnits = new (GameBoard.height);
    protected Dictionary<int, List<Field>> rowMappedFields = new (GameBoard.height);

    virtual public void Execute(GameBoard board)
    {
        this.gameBoard = board;

        RowMapMyUnits();
        RowMapMyFields();
        RowMapEnemyUnits();
        command = "";
    }

    virtual public bool CheckTransition(GameBoard gameBoard)
    {
        return false;
    }

    virtual public GamePhase Transition()
    {
        return this;
    }

    protected void RowMapMyUnits()
    {
        myRowMappedUnits.Clear();

        foreach (Field myUnit in gameBoard.MyUnits)
        {
            if (!myRowMappedUnits.ContainsKey(myUnit.Y))
                myRowMappedUnits.Add(myUnit.Y, new List<Field>());
            myRowMappedUnits[myUnit.Y].Add(myUnit);
        }
    }
    protected void RowMapEnemyUnits()
    {
        enemyRowMappedUnits.Clear();

        foreach (Field enemyUnit in gameBoard.EnemyUnits)
        {
            if (!enemyRowMappedUnits.ContainsKey(enemyUnit.Y))
                enemyRowMappedUnits.Add(enemyUnit.Y, new List<Field>());
            enemyRowMappedUnits[enemyUnit.Y].Add(enemyUnit);
        }
    }

    protected void RowMapMyFields()
    {
        rowMappedFields.Clear();

        foreach (Field field in gameBoard.MyFields)
        {
            if (!rowMappedFields.ContainsKey(field.Y))
                rowMappedFields.Add(field.Y, new List<Field>());
            rowMappedFields[field.Y].Add(field);
        }
    }

    protected Field FindBestMoveOrSpawn(Field AttackField, Dictionary<Field, int> AlreadySelectedUnits, out Field moveTarget, bool withSpawn = true)
    {
        HashSet<Field> visitedFields = new HashSet<Field>();
        HashSet<Field> currentFields = new HashSet<Field>();
        moveTarget = AttackField;
        currentFields.Add(AttackField);
        bool found = false;
        HashSet<Field> inspectList = new();
        Dictionary<Field, int> spawnList = new();
        while (!found)
        {
            inspectList.Clear();

            foreach (Field field in currentFields)
            {
                //Otp could have an Defensive Mode where Playdirection is take into account
                foreach (Field checkField in field.GetPossibleMoveDirection(gameBoard))
                {
                    
                    int openUnitCount = checkField.units;
                    if (AlreadySelectedUnits.Keys.Contains(checkField))
                        openUnitCount -= AlreadySelectedUnits[checkField];

                    if (checkField.mine && openUnitCount >= 1)
                    {
                        moveTarget = field;
                        return checkField;
                    }
                    if (withSpawn && checkField.mine && !spawnList.Keys.Contains(checkField))
                        spawnList.Add(checkField, Settings.OffsetToFindSpawn);
                    if (!visitedFields.Contains(checkField) && !inspectList.Contains(checkField))
                        inspectList.Add(checkField);
                }
                visitedFields.Add(field);
            }

            currentFields.Clear();
            foreach (Field field in inspectList)
            {
                currentFields.Add(field);
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
        Console.Error.WriteLine($"No way found to {AttackField.PositionLog()}");
        return AttackField;
    }


}