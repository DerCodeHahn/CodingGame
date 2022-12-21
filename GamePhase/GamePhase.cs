class GamePhase
{
    protected GameBoard gameBoard;
    

    protected Dictionary<int, List<Field>> rowMappedUnits;
    protected Dictionary<int, List<Field>> rowMappedFields;


    virtual public void Execute (GameBoard board)
    {
        this.gameBoard = board;
        
        rowMappedUnits = RowMapMyUnits();
        rowMappedFields = RowMapMyFields();
    }

    virtual protected bool CheckTransition ()
    {
        return false;
    }

    virtual protected GamePhase Transition ()
    {
        return this;
    }

    protected Dictionary<int, List<Field>> RowMapMyUnits()
    {
        Dictionary<int, List<Field>> rowMappedUnits = new Dictionary<int, List<Field>>();

        foreach (Field myUnit in gameBoard.MyUnits)
        {
            if (!rowMappedUnits.ContainsKey(myUnit.Y))
                rowMappedUnits.Add(myUnit.Y, new List<Field>());
            rowMappedUnits[myUnit.Y].Add(myUnit);
        }

        return rowMappedUnits;
    }

    protected Dictionary<int, List<Field>> RowMapMyFields()
    {
        Dictionary<int, List<Field>> rowMappedUnits = new Dictionary<int, List<Field>>();

        foreach (Field field in gameBoard.MyFields)
        {
            if (!rowMappedUnits.ContainsKey(field.Y))
                rowMappedUnits.Add(field.Y, new List<Field>());
            rowMappedUnits[field.Y].Add(field);
        }

        return rowMappedUnits;
    }
}