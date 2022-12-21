using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

class Player
{
    public static sbyte PlayDirection = 1; // 1 to right , -1 to left
    public static Field MyBasePosition;
    public static Field EnemyBasePosition;
    public static byte middle;
    public static bool Init = false;
    public static int GameStep = 0;

    static (sbyte, sbyte) [] MoveDirections = new (sbyte, sbyte) []
    {
        (0, -1), (0, 1), (-1, 0), (1, 0)
    }; //up,down,left,right
    public static Random random = new Random ();
    static void Main (string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine ().Split (' ');
        int width = int.Parse (inputs[0]);
        int height = int.Parse (inputs[1]);
        GameBoard.height = height;
        GameBoard.width = width;

        middle = (byte) (width / 2);
        Field[, ] fields = new Field[width, height];
        int SurroundingCounter = 10;
        GamePhase gamePhase = new EarlyGame ();

        // game loop
        while (true)
        {
            GameStep++;
            inputs = Console.ReadLine ().Split (' ');
            int myMatter = int.Parse (inputs[0]);
            int oppMatter = int.Parse (inputs[1]);
            fields = new Field[width, height];
            int complexity = 1;
            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    inputs = Console.ReadLine ().Split (' ');
                    byte scrapAmount = byte.Parse (inputs[0]);
                    SByte owner = SByte.Parse (inputs[1]); // 1 = me, 0 = foe, -1 = neutral
                    bool mine = owner == 1;
                    bool enemies = owner == 0;
                    byte units = byte.Parse (inputs[2]);
                    bool recycler = int.Parse (inputs[3]) == 1;
                    bool canBuild = int.Parse (inputs[4]) == 1;
                    bool canSpawn = int.Parse (inputs[5]) == 1;
                    bool inRangeOfRecycler = int.Parse (inputs[6]) == 1;
                    fields[x, y] = new Field (x, y, scrapAmount, mine, enemies, units, recycler, canBuild, canSpawn, inRangeOfRecycler);
                }
            }

            GameBoard gameBoard = new GameBoard (fields);
            gameBoard.MyMatter = myMatter;
            gameBoard.Analize ();

            if (!Init)
                FindMatchData (gameBoard);

            gamePhase.Execute (gameBoard);

            //nextGameBoards.Clear();
            //gameBoard.CommandGettingHere = command;

            //Console.WriteLine (nextGameBoards[0].GetBuildString ());

        }
    }

    static List<GameBoard> nextGameBoards = new ();

    static void PopulateGameBoards (GameBoard board, int depth)
    {
        for (int i = 0; i <= 100; i++)
        {
            GameBoard next = new GameBoard (board);
            List<Action> moveCommands = new ();
            foreach (Field myUnit in board.MyUnits)
            {
                List < (sbyte, sbyte) > possibleDirection = board[myUnit.X, myUnit.Y].GetPossibleMoveDirection (board);
                if (possibleDirection.Count == 0)
                    continue;
                (sbyte x, sbyte y) direction = possibleDirection[random.Next (possibleDirection.Count)];

                moveCommands.Add (new Move (myUnit.X, myUnit.Y, (byte) (myUnit.X + direction.x), (byte) (myUnit.Y + direction.y), myUnit.units));

                //moveCommands += ActionsBuilder.Move (myUnit.x, myUnit.y, (byte) (myUnit.x + direction.x), (byte) (myUnit.y + direction.y), myUnit.count);
            }

            //next.CurrentCommands = moveCommands;
            next.ExecuteCommands (moveCommands);
            nextGameBoards.Add (next);
        }
        nextGameBoards.Sort (GameBoard.SortByScore); // Maybe use directly sorted Structure ?

        for (int i = 0; i < 10; i++)
        {
            Console.Error.WriteLine ($"{nextGameBoards[i].score} {nextGameBoards[i].GetBuildString()}");
        }

    }

    private static void FindMatchData (GameBoard board)
    {
        Init = true;
        foreach (Field field in board.EnemieFields)
            if (field.units == 0)
                EnemyBasePosition = field;

        foreach (Field field in board.MyFields)
            if (field.units == 0)
                MyBasePosition = field;

        if (MyBasePosition.X >= middle)
            PlayDirection = -1;
        else
            PlayDirection = 1;
    }
}