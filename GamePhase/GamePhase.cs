class GamePhase
{
    protected string command = "";
    protected GameBoard gameBoard;

    protected Dictionary<int, List<Field>> rowMappedUnits;
    protected Dictionary<int, List<Field>> rowMappedFields;

    public void Init()
    {
        rowMappedUnits = new Dictionary<int, List<Field>>(GameBoard.height);
        rowMappedFields = new Dictionary<int, List<Field>>(GameBoard.height);
    }

    virtual public void Execute(GameBoard board)
    {
        this.gameBoard = board;

        RowMapMyUnits();
        RowMapMyFields();
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
        rowMappedUnits.Clear();

        foreach (Field myUnit in gameBoard.MyUnits)
        {
            if (!rowMappedUnits.ContainsKey(myUnit.Y))
                rowMappedUnits.Add(myUnit.Y, new List<Field>());
            rowMappedUnits[myUnit.Y].Add(myUnit);
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
                foreach ((sbyte x, sbyte y) direction in field.GetPossibleMoveDirection(gameBoard))
                {
                    Field checkField = gameBoard[field.X + direction.x, field.Y + direction.y];
                    
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