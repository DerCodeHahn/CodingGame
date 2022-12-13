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
    static void Main (string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine ().Split (' ');
        int width = int.Parse (inputs[0]);
        int height = int.Parse (inputs[1]);
        GameBoard.height = height;
        GameBoard.width = width;

        Field[, ] fields;

        // game loop
        while (true)
        {
            inputs = Console.ReadLine ().Split (' ');
            int myMatter = int.Parse (inputs[0]);
            int oppMatter = int.Parse (inputs[1]);
            fields = new Field[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 position = new Vector2 (x, y);
                    inputs = Console.ReadLine ().Split (' ');
                    int scrapAmount = int.Parse (inputs[0]);
                    int owner = int.Parse (inputs[1]); // 1 = me, 0 = foe, -1 = neutral
                    int units = int.Parse (inputs[2]);
                    bool recycler = int.Parse (inputs[3]) == 1;
                    bool canBuild = int.Parse (inputs[4]) == 1;
                    bool canSpawn = int.Parse (inputs[5]) == 1;
                    bool inRangeOfRecycler = int.Parse (inputs[6]) == 1;
                    fields[x, y] = new Field (position, scrapAmount, owner, units, recycler, canBuild, canSpawn, inRangeOfRecycler);
                }
            }
            GameBoard gameBoard = new GameBoard (fields);
            gameBoard.Analize ();

            // Write an action using Console.WriteLine()
            List<Field> best = gameBoard.GetBestUnOwnedFields ();
            string command = "";
            HashSet<Field> alreadyTargets = new HashSet<Field> ();
            if (myMatter >= Consts.BuildCost)
            {
                int count = myMatter / Consts.BuildCost;
                for (var i = 0; i < count; i++)
                {
                    command += Actions.Spawn (gameBoard.GetRandomMyField (), 1);
                }
            }

            // foreach (Field field in best)
            // {
            //     //Console.Error.WriteLine(field.Info());
            //     if (!alreadyTargets.Contains (field))
            //     {
            //         Console.Error.WriteLine (myUnit);
            //         command += Actions.Move (myUnit, field.position, 1);

            //         alreadyTargets.Add (field);
            //         break;
            //     }
            // }

            foreach (Vector2 myUnit in gameBoard.MyUnits)
            {
                Field nextFree = gameBoard.NextFree(myUnit);
                command += Actions.Move (myUnit, nextFree.position, 1);
            }

            Console.WriteLine (command);
        }
    }
}