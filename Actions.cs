using System.Numerics;

static class Actions
{
    const string BUILD = "BUILD ";
    const string MOVE = "MOVE ";
    const string SPAWN = "SPAWN ";
    const string WAIT = "WAIT ";
    const string MESSAGE = "MESSAGE ";

    public static string Build (Vector2 position)
    {
        return BUILD + Print (position) + ";";
    }

    public static string Spawn (Field field, int amount)
    {
        return Spawn (field.position, amount);
    }

    public static string Spawn (Vector2 position, int amount)
    {
        return SPAWN + amount + " " + Print (position) + ";";
    }

    public static string Move (Vector2 from, Vector2 to, int amount)
    {
        return MOVE + amount + " " + Print (from) + " " + Print (to) + ";";
    }

    public static string Print (Vector2 target)
    {
        return $"{(int)target.X} {(int) target.Y}";
    }

    public static string Wait ()
    {
        return WAIT;
    }

}