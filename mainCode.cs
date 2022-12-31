using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
//8236584684887423000 wide big nopoint
//4922755871518232000 big point flank

//IMPROVEMENTS: 
//1. When calc Way towards AttackPoint mark fields as steped
//1.1 Compare ways with the same length how many newStep fields its creates 

//Calculate how much points a move of a enemy will cost me and prio spawn
class Player
{
    public static sbyte PlayDirection = 1; // 1 to right , -1 to left
    public static Field MyBasePosition;
    public static Field EnemyBasePosition;
    public static byte middle;
    public static bool Init = false;
    public static int GameStep = 0;
    public static List<Field> AttackLine = new List<Field>(50);

    static (sbyte, sbyte)[] MoveDirections = new (sbyte, sbyte)[]
    {
        (0, -1), (0, 1), (-1, 0), (1, 0)
    }; //up,down,left,right
    public static Random random = new Random();
    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]);
        int height = int.Parse(inputs[1]);
        GameBoard.height = height;
        GameBoard.width = width;

        middle = (byte)(width / 2);
        Field[,] fields = new Field[width, height];
        int SurroundingCounter = 10;
        GamePhase gamePhase = new EarlyGame();

        // game loop
        while (true)
        {
            GameStep++;
            inputs = Console.ReadLine().Split(' ');
            int myMatter = int.Parse(inputs[0]);
            int oppMatter = int.Parse(inputs[1]);
            fields = new Field[width, height];
            int complexity = 1;
            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    inputs = Console.ReadLine().Split(' ');
                    byte scrapAmount = byte.Parse(inputs[0]);
                    SByte owner = SByte.Parse(inputs[1]); // 1 = me, 0 = foe, -1 = neutral
                    bool mine = owner == 1;
                    bool enemies = owner == 0;
                    byte units = byte.Parse(inputs[2]);
                    bool recycler = int.Parse(inputs[3]) == 1;
                    bool canBuild = int.Parse(inputs[4]) == 1;
                    bool canSpawn = int.Parse(inputs[5]) == 1;
                    bool inRangeOfRecycler = int.Parse(inputs[6]) == 1;
                    fields[x, y] = new Field(x, y, scrapAmount, mine, enemies, units, recycler, canBuild, canSpawn, inRangeOfRecycler);
                }
            }

            GameBoard gameBoard = new GameBoard(fields);
            gameBoard.MyMatter = myMatter;
            gameBoard.Analize();
            //DebugPressureSystem(width, height, fields);

            if (!Init)
            {
                FindMatchData(gameBoard);
            }
            //UpdateAttackLine(gameBoard);
            if (gamePhase.CheckTransition(gameBoard))
                gamePhase = gamePhase.Transition();

            // foreach (Field attackPoint in AttackLine)
            //     Console.Error.WriteLine($"AttackPoint {attackPoint.PositionLog()}");
            // if (gamePhase.CheckTransition())
            //     gamePhase = gamePhase.Transition();
            gamePhase.Execute(gameBoard);

            //nextGameBoards.Clear();
            //gameBoard.CommandGettingHere = command;

            //Console.WriteLine (nextGameBoards[0].GetBuildString ());

        }
    }

    private static void DebugPressureSystem(int width, int height, Field[,] fields)
    {
        for (byte y = 0; y < height; y++)
        {
            for (byte x = 0; x < width; x++)
            {
                Field field = fields[x, y];
                if (field.scrapAmount == 0)
                    Console.Error.Write(" -");
                else
                    if (field.recycler)
                {
                    Console.Error.Write($" x");
                }
                else
                    Console.Error.Write($"{field.Pressure}".PadLeft(2));
            }
            Console.Error.WriteLine();
        }
    }

    static void UpdateAttackLine(GameBoard gameBoard)
    {
        AttackLine.Clear();

        HashSet<Field> myVisitedFields = new HashSet<Field>();
        HashSet<Field> myCurrentFields = new HashSet<Field>();
        HashSet<Field> enemyVisitedFields = new HashSet<Field>();
        HashSet<Field> enemyCurrentFields = new HashSet<Field>();
        HashSet<Field> MyDisscoverdField = new();
        HashSet<Field> EnemyDisscoverdField = new();

        foreach (var item in gameBoard.MyBorderFields)
        {
            myCurrentFields.Add(item);
            myVisitedFields.Add(item);
        }
        
        foreach (var item in gameBoard.EnemieBorderFields)
        {
            Console.Error.Write(item.PositionLog()+ ", ");
            enemyCurrentFields.Add(item);
            enemyVisitedFields.Add(item);
        }
        Console.Error.WriteLine("----");

        while (myCurrentFields.Count != 0)
        {
            MyDisscoverdField.Clear();
            EnemyDisscoverdField.Clear();

            foreach (Field item in myCurrentFields)
            {
                foreach(Field newField in item.GetPossibleMoveDirection(gameBoard))
                {
                    if(!myVisitedFields.Contains(newField))
                    {
                        if(newField.enemies )
                            AttackLine.Add(newField);

                        else if(!newField.mine && !MyDisscoverdField.Contains(newField))
                            MyDisscoverdField.Add(newField);

                        myVisitedFields.Add(newField);
                    }
                }
            }
            foreach (Field item in enemyCurrentFields)
            {
                foreach(Field newField in item.GetPossibleMoveDirection(gameBoard))
                {
                    if(!enemyVisitedFields.Contains(newField)&&!myVisitedFields.Contains(newField))
                    {
                        if(newField.mine)
                        {
                        }
                        else if(!newField.enemies && !EnemyDisscoverdField.Contains(newField))
                        {
                            Console.Error.Write(newField.PositionLog()+newField.enemies+ ", ");
                            EnemyDisscoverdField.Add(newField);
                        }
                        enemyVisitedFields.Add(newField);
                    }
                }
            }
            List<Field> intersections = MyDisscoverdField.Intersect(EnemyDisscoverdField).ToList();
            AttackLine.AddRange(intersections);

            MyDisscoverdField.ExceptWith(intersections);
            EnemyDisscoverdField.ExceptWith(intersections);

            myCurrentFields.Clear();
            foreach (var item in MyDisscoverdField)
            {
                myCurrentFields.Add(item);
            }

            enemyCurrentFields.Clear();
            foreach (var item in EnemyDisscoverdField)
            {
                enemyCurrentFields.Add(item);
            }
        }
    }

    static List<GameBoard> nextGameBoards = new();

    static void PopulateGameBoards(GameBoard board, int depth)
    {
        for (int i = 0; i <= 100; i++)
        {
            GameBoard next = new GameBoard(board);
            List<Action> moveCommands = new();
            foreach (Field myUnit in board.MyUnits)
            {
                List<Field> possibleDirection = board[myUnit.X, myUnit.Y].GetPossibleMoveDirection(board);
                if (possibleDirection.Count == 0)
                    continue;
                Field direction = possibleDirection[random.Next(possibleDirection.Count)];

                //moveCommands.Add(new Move(myUnit.X, myUnit.Y, (byte)(myUnit.X + direction.x), (byte)(myUnit.Y + direction.y), myUnit.units));

                //moveCommands += ActionsBuilder.Move (myUnit.x, myUnit.y, (byte) (myUnit.x + direction.x), (byte) (myUnit.y + direction.y), myUnit.count);
            }

            //next.CurrentCommands = moveCommands;
            next.ExecuteCommands(moveCommands);
            nextGameBoards.Add(next);
        }
        nextGameBoards.Sort(GameBoard.SortByScore); // Maybe use directly sorted Structure ?

        for (int i = 0; i < 10; i++)
        {
            Console.Error.WriteLine($"{nextGameBoards[i].score} {nextGameBoards[i].GetBuildString()}");
        }

    }

    private static void FindMatchData(GameBoard board)
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

        AttackLine = GetAttackLine(board);
    }

    static List<Field> GetAttackLine(GameBoard gameBoard)
    {
        List<Field> attackPoints = new List<Field>();
        Field AttackPoint = gameBoard[Player.middle, Player.MyBasePosition.Y];
        for (int i = 0; i < GameBoard.height; i++)
        {
            bool inBoundUpper = UTIL.CheckForInBound(Player.middle, Player.MyBasePosition.Y + i);
            bool inBoundLower = UTIL.CheckForInBound(Player.middle, Player.MyBasePosition.Y - i);
            if (inBoundUpper)
            {
                Field field = gameBoard[Player.middle, Player.MyBasePosition.Y + i];
                if (field.scrapAmount != 0)
                    attackPoints.Add(field);
            }

            if (inBoundLower && i != 0)
            {
                Field field = gameBoard[Player.middle, Player.MyBasePosition.Y - i];
                if (field.scrapAmount != 0)
                    attackPoints.Add(field);
            }

        }

        return attackPoints;
    }


}