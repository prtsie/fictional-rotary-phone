using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Maze
{
    enum State : byte
    {
        NotVisited,
        Visited
    }
    [Flags]
    enum SurroundingWalls
    {
        None = 0,
        Up = 0x0001,
        Down = 0x0002,
        Left = 0x0004,
        Right = 0x0008
    }

    struct CellCoords
    {
        public int x;
        public int y;
        public CellCoords(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(CellCoords cell)
        {
            return x == cell.x && y == cell.y;
        }
    }

    internal class Program
    {
        static int wallsThickness = 2;
        static int generatorStep = wallsThickness + 1;

        //Size must be at least 2x2
        static int stepsHeight = 4;
        static int stepsWidth = 10;

        static int rows;
        static int columns;

        const int minSize = 2;
        static int minWallThickness = 1;

        const ConsoleColor backgroundColor = ConsoleColor.Gray;
        const ConsoleColor foregroundColor = ConsoleColor.Red;
        const ConsoleColor finishColor = ConsoleColor.Green;

        static CellCoords finishCell;
        static State[,] maze;
        static Random random = new();
        static Dictionary<SurroundingWalls, char> chars = new Dictionary<SurroundingWalls, char>()
        {
            {SurroundingWalls.None, ' ' },
            {SurroundingWalls.Up,  '║'},
            {SurroundingWalls.Down, '║' },
            {SurroundingWalls.Left, '═' },
            {SurroundingWalls.Right, '═' },
            {SurroundingWalls.Left | SurroundingWalls.Up, '╝' },
            {SurroundingWalls.Left | SurroundingWalls.Right, '═' },
            {SurroundingWalls.Left | SurroundingWalls.Down, '╗' },
            {SurroundingWalls.Up | SurroundingWalls.Right, '╚' },
            {SurroundingWalls.Up | SurroundingWalls.Down, '║' },
            {SurroundingWalls.Right | SurroundingWalls.Down, '╔' },
            {SurroundingWalls.Left | SurroundingWalls.Up | SurroundingWalls.Right, '╩' },
            {SurroundingWalls.Left| SurroundingWalls.Up | SurroundingWalls.Down, '╣' },
            {SurroundingWalls.Left | SurroundingWalls.Down | SurroundingWalls.Right, '╦' },
            {SurroundingWalls.Up | SurroundingWalls.Right | SurroundingWalls.Down, '╠' },
            {SurroundingWalls.Up | SurroundingWalls.Down | SurroundingWalls.Left | SurroundingWalls.Right, '╬' }
        };
        const char finishChar = '█';
        const string code = "hesoyam";

        static void Main(string[] args)
        {
            int startConsoleWidth = Console.WindowWidth;
            int startConsoleHeight = Console.WindowHeight;
            var startBackgroundColor = Console.BackgroundColor;
            var startForegroundColor = Console.ForegroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.CursorSize = 100;

            //Maze generation cycle
            bool escapePressed = false;
            while (!escapePressed)
            {
                generatorStep = wallsThickness + 1;
                int cursorStep = generatorStep;
                rows = stepsHeight * generatorStep + 2 * wallsThickness + 1;
                columns = stepsWidth * generatorStep + 2 * wallsThickness + 1;
                maze = new State[rows, columns];

                int rightWindowOffset = 60;
                Console.SetWindowSize(columns + rightWindowOffset, rows + 1);

                GenerateMaze();
                Display();

                int guideLeftOffset = 3;
                int guideTopOffset = 1;
                Console.SetCursorPosition(columns + guideLeftOffset, guideTopOffset++);
                Console.Write("Press \"Escape\"/\"Enter\" to exit");
                Console.SetCursorPosition(columns + guideLeftOffset, guideTopOffset++);
                Console.Write("Press \"4\"/\"6\" to remove/add columns");
                Console.SetCursorPosition(columns + guideLeftOffset, guideTopOffset++);
                Console.Write("Press \"8\"/\"2\" to remove/add rows");
                Console.SetCursorPosition(columns + guideLeftOffset, guideTopOffset++);
                Console.Write("Press \"-\"/\"+\" to increase/decrease wall thickness");

                Console.SetCursorPosition(wallsThickness, wallsThickness);
                bool mazeExitRequested = false;
                string inputCode = "";

                //User-input request cycle
                while (!mazeExitRequested)
                {
                    var (Left, Top) = Console.GetCursorPosition();
                    var key = Console.ReadKey(true);
                    if (code.Contains(inputCode + key.KeyChar))
                    {
                        inputCode += key.KeyChar;
                        if (inputCode.Length == code.Length)
                        {
                            DisplayWithPath(new PathFinder(maze, new CellCoords(wallsThickness, wallsThickness), finishCell));
                            Console.SetCursorPosition(Left, Top);
                            inputCode = "";
                        }
                    }
                    else
                    {
                        inputCode = "";
                    }

                    bool WayBlocked = false;
                    switch (key.Key)
                    {
                        case ConsoleKey.W:
                        case ConsoleKey.UpArrow:
                            if (Top >= cursorStep)
                            {
                                for (var i = Top; i > Top - cursorStep; i--)
                                    if (maze[i, Left] == State.NotVisited)
                                    {
                                        WayBlocked = true;
                                        break;
                                    }
                                if (!WayBlocked)
                                    Console.SetCursorPosition(Left, Top - cursorStep);
                            }
                            break;
                        case ConsoleKey.S:
                        case ConsoleKey.DownArrow:
                            for (var i = Top; i < Top + cursorStep; i++)
                                if (maze[i, Left] == State.NotVisited)
                                {
                                    WayBlocked = true;
                                    break;
                                }
                            if (!WayBlocked)
                                Console.SetCursorPosition(Left, Top + cursorStep);
                            break;
                        case ConsoleKey.A:
                        case ConsoleKey.LeftArrow:
                            if (Left >= cursorStep)
                            {
                                for (var i = Left; i > Left - cursorStep; i--)
                                    if (maze[Top, i] == State.NotVisited)
                                    {
                                        WayBlocked = true;
                                        break;
                                    }
                                if (!WayBlocked)
                                    Console.SetCursorPosition(Left - cursorStep, Top);
                            }
                            break;
                        case ConsoleKey.D:
                        case ConsoleKey.RightArrow:
                            if (Left + cursorStep >= columns - 1 && Top == finishCell.y)
                                mazeExitRequested = true;
                            else
                            {

                                for (var i = Left; i < Left + cursorStep; i++)
                                    if (maze[Top, i] == State.NotVisited)
                                    {
                                        WayBlocked = true;
                                        break;
                                    }
                                if (!WayBlocked)
                                    Console.SetCursorPosition(Left + cursorStep, Top);
                            }
                            break;
                        case ConsoleKey.D4:
                        case ConsoleKey.NumPad4:
                            if (stepsWidth > minSize)
                            {
                                stepsWidth--;
                                mazeExitRequested = true;
                            }
                            break;
                        case ConsoleKey.D6:
                        case ConsoleKey.NumPad6:
                            if (Console.LargestWindowWidth > (stepsWidth + 1) * generatorStep + 2 * wallsThickness + 1 + rightWindowOffset)
                            {
                                stepsWidth++;
                                mazeExitRequested = true;
                            }
                            break;
                        case ConsoleKey.D8:
                        case ConsoleKey.NumPad8:
                            if (stepsHeight > minSize)
                            {
                                stepsHeight--;
                                mazeExitRequested = true;
                            }
                            break;
                        case ConsoleKey.D2:
                        case ConsoleKey.NumPad2:
                            if (Console.LargestWindowHeight > (stepsHeight + 1) * generatorStep + 2 * wallsThickness + 1)
                            {
                                stepsHeight++;
                                mazeExitRequested = true;
                            }
                            break;
                        case ConsoleKey.OemMinus:
                        case ConsoleKey.Subtract:
                            if (wallsThickness > minWallThickness)
                            {
                                wallsThickness--;
                                mazeExitRequested = true;
                            }
                            break;
                        case ConsoleKey.OemPlus:
                        case ConsoleKey.Add:
                            if (Console.LargestWindowWidth > stepsWidth * (generatorStep + 1) + 2 * (wallsThickness + 1) + 1 + rightWindowOffset
                               && Console.LargestWindowHeight > stepsHeight * (generatorStep + 1) + 2 * (wallsThickness + 1) + 1)
                            {
                                wallsThickness++;
                                mazeExitRequested = true;
                            }
                            break;
                        case ConsoleKey.Enter:
                        case ConsoleKey.Escape:
                            mazeExitRequested = true;
                            escapePressed = true;
                            break;
                    }
                }
            }
            Console.SetWindowSize(startConsoleWidth, startConsoleHeight);
            Console.BackgroundColor = startBackgroundColor;
            Console.ForegroundColor = startForegroundColor;
            Console.Clear();
            Console.SetCursorPosition(0, 0);
        }

        static public List<CellCoords> GetNeighbours(CellCoords cell)
        {
            var list = new List<CellCoords>();

            if (cell.y >= wallsThickness + generatorStep)
            {
                list.Add(new CellCoords(cell.x, cell.y - generatorStep));
            }

            if (cell.x <= columns - wallsThickness - generatorStep)
            {
                list.Add(new CellCoords(cell.x + generatorStep, cell.y));
            }

            if (cell.y <= rows - wallsThickness - generatorStep)
            {
                list.Add(new CellCoords(cell.x, cell.y + generatorStep));
            }

            if (cell.x >= wallsThickness + generatorStep)
            {
                list.Add(new CellCoords(cell.x - generatorStep, cell.y));
            }
            return list;
        }

        static void GenerateMaze()
        {
            int generationStartX = wallsThickness;
            int generationStartY = wallsThickness;

            CellCoords generationStartCell = new CellCoords(generationStartX, generationStartY);
            maze[generationStartY, generationStartX] = State.Visited;
            //List with visited cells and surrounding unvisited cells to visit
            List<(List<CellCoords> cells, CellCoords from)> routeTuples = [(GetNeighbours(generationStartCell), generationStartCell)];
            while (routeTuples.Count > 0)
            {
                var randomTuple = routeTuples[random.Next(routeTuples.Count)];
                var unvisitedCells = randomTuple.cells.Where(cell => maze[cell.y, cell.x] == State.NotVisited).ToList();
                if (unvisitedCells.Count > 0)
                {
                    var randomDestination = unvisitedCells[random.Next(unvisitedCells.Count)];
                    MakePath(randomTuple.from, randomDestination);
                    routeTuples.Add((GetNeighbours(randomDestination), randomDestination));
                    randomTuple.cells.Remove(randomDestination);
                }
                else
                {
                    routeTuples.Remove(randomTuple);
                }
            }
            finishCell = GenerateFinish();
        }


        static void MakePath(CellCoords first, CellCoords second)
        {
            int deltaX = first.x - second.x;
            CellCoords min;
            CellCoords max;
            if (deltaX == 0)
            {
                if (first.y < second.y)
                {
                    min = first;
                    max = second;
                }
                else
                {
                    min = second;
                    max = first;
                }
                for (int i = min.y; i <= max.y; i++)
                {
                    maze[i, first.x] = State.Visited;
                }
            }
            else
            {
                if (first.x < second.x)
                {
                    min = first;
                    max = second;
                }
                else
                {
                    min = second;
                    max = first;
                }
                for (int i = min.x; i <= max.x; i++)
                {
                    maze[first.y, i] = State.Visited;
                }
            }
        }

        static CellCoords GenerateFinish()
        {
            CellCoords cell = new CellCoords();
            int y = random.Next(wallsThickness - 1, rows - wallsThickness);
            y -= (y - wallsThickness) % generatorStep;
            cell.x = columns - 1 - wallsThickness;
            cell.y = y;
            MakePath(cell, new CellCoords(columns - 1, cell.y));
            return cell;
        }

        static void Display()
        {
            Console.Clear();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    SurroundingWalls walls = SurroundingWalls.None;
                    if (i == finishCell.y && j > finishCell.x)
                    {
                        var consoleColor = Console.ForegroundColor;
                        Console.ForegroundColor = finishColor;
                        Console.Write(finishChar);
                        Console.ForegroundColor = consoleColor;
                    }
                    else
                    {
                        if (maze[i, j] != State.Visited)
                        {
                            if (i > 0 && maze[i - 1, j] == State.NotVisited) walls |= SurroundingWalls.Up;

                            if (i < rows - 1 && maze[i + 1, j] == State.NotVisited) walls |= SurroundingWalls.Down;

                            if (j > 0 && maze[i, j - 1] == State.NotVisited) walls |= SurroundingWalls.Left;

                            if (j < columns - 1 && maze[i, j + 1] == State.NotVisited) walls |= SurroundingWalls.Right;
                        }
                        Console.Write(chars[walls]);
                    }
                }
                Console.WriteLine();
            }
        }
        static void DisplayWithPath(PathFinder pathFinder)
        {
            Console.Clear();
            var pathColor = ConsoleColor.DarkGreen;
            var pathChar = finishChar;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    SurroundingWalls walls = SurroundingWalls.None;
                    if (i == finishCell.y && j > finishCell.x)
                    {
                        var consoleColor = Console.ForegroundColor;
                        Console.ForegroundColor = finishColor;
                        Console.Write(finishChar);
                        Console.ForegroundColor = consoleColor;
                    }
                    else if (pathFinder.Way.Contains(new CellCoords(j, i)))
                    {
                        var consoleColor = Console.ForegroundColor;
                        Console.ForegroundColor = pathColor;
                        Console.Write(pathChar);
                        Console.ForegroundColor = consoleColor;
                    }
                    else
                    {
                        if (maze[i, j] != State.Visited)
                        {
                            if (i > 0 && maze[i - 1, j] == State.NotVisited)
                            {
                                walls |= SurroundingWalls.Up;
                            }

                            if (i < rows - 1 && maze[i + 1, j] == State.NotVisited)
                            {
                                walls |= SurroundingWalls.Down;
                            }

                            if (j > 0 && maze[i, j - 1] == State.NotVisited)
                            {
                                walls |= SurroundingWalls.Left;
                            }

                            if (j < columns - 1 && maze[i, j + 1] == State.NotVisited)
                            {
                                walls |= SurroundingWalls.Right;
                            }
                        }
                        Console.Write(chars[walls]);
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
