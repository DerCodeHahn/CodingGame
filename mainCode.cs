using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

public enum GamePhase
{
    Early,
    Mid,
    Late
}

class Player
{
    public static sbyte PlayDirection = 1; // 1 to right , -1 to left
    public static byte middle;
    public static bool Init = false;
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
        // game loop
        while (true)
        {
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
            gameBoard.Analize ();

            if (!Init)
                FindMatchData (gameBoard);

            string command = "";
            HashSet<Field> alreadyTargets = new HashSet<Field> ();

            if (myMatter >= Consts.BuildCost)
            {
                List<Field> higherSurroundingFields = gameBoard.GetHigherSurroundingsFields ();
                foreach (Field field in higherSurroundingFields)
                {
                    if (field.canBuild && SurroundingCounter > 0 && myMatter >= Consts.BuildCost && field.TotalCollectableScrap >= 20)
                    {
                        myMatter -= 10;
                        SurroundingCounter--;
                        command += ActionsBuilder.Build (field);
                    }
                }
                int count = myMatter / Consts.BuildCost;
                for (var i = 0; i < count; i++)
                {
                    List<Field> spawns = gameBoard.GetSpawnFields ();

                    //command += Actions.Spawn (spawns[rnd.Next (spawns.Count)], 1);
                    //complexity *= spawns.Count;
                }

            }

            //gameBoard.CommandGettingHere = command;
            PopulateGameBoards(gameBoard, 0);
            
            Console.WriteLine (nextGameBoards[0].GetBuildString());

        }
    }

    static List<GameBoard> nextGameBoards = new ();

    static void PopulateGameBoards (GameBoard board, int depth)
    {
        for (int i = 0; i <= 100; i++)
        {
            GameBoard next = new GameBoard (board);
            List<Action> moveCommands = new();

            foreach ((byte x, byte y, byte count) myUnit in board.MyUnits)
            {
                (sbyte x, sbyte y) direction = MoveDirections[random.Next (MoveDirections.Length)];
                //TODO Check for correct movement
                moveCommands.Add(new Move(myUnit.x, myUnit.y, (byte) (myUnit.x + direction.x), (byte) (myUnit.y + direction.y), myUnit.count));

                //moveCommands += ActionsBuilder.Move (myUnit.x, myUnit.y, (byte) (myUnit.x + direction.x), (byte) (myUnit.y + direction.y), myUnit.count);
            }
            //next.CurrentCommands = moveCommands;
            next.ExecuteCommands(moveCommands);
            nextGameBoards.Add (next);
        }
        nextGameBoards.Sort(GameBoard.SortByScore); // Maybe use directly sorted Structure ?

        for(int i = 0; i < 10; i++){
            Console.Error.WriteLine(nextGameBoards[i].score);
        }
        
    }

    private static void FindMatchData (GameBoard board)
    {
        Init = true;
        Field someOfMyFields = board.MyFields.First ();
        if (someOfMyFields.X >= middle)
            PlayDirection = -1;
        else
            PlayDirection = 1;

        Console.Error.WriteLine ($"PlayDirection {PlayDirection}");
    }
}