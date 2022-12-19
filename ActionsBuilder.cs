using System.Numerics;

static class ActionsBuilder
{
    const string BUILD = "BUILD";
    const string MOVE = "MOVE";
    const string SPAWN = "SPAWN";
    const string WAIT = "WAIT";
    const string MESSAGE = "MESSAGE";

    public static string Build (Field field)
    {
        return $"{BUILD} {field.PositionLog()};";
    }

    public static string Spawn (Field field, int amount)
    {
        return $"{SPAWN} {amount} {field.PositionLog()};";
    }

    public static string Move (Field from, Field to, int amount)
    {
        return $"{MOVE} {amount} {from.PositionLog()} {to.PositionLog()};";
    }

    public static string Move (Field from, int toX ,int toY , int amount)
    {
        return $"{MOVE} {amount} {from.PositionLog()} {toX} {toY};";
    }

    public static string Move (byte fromX ,byte fromY , byte toX ,byte toY , int amount)
    {
        return $"{MOVE} {amount} {fromX} {fromY} {toX} {toY};";
    }

    public static string Wait ()
    {
        return WAIT;
    }

}