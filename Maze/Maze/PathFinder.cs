using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maze
{
    internal class PathFinder
    {
        public Stack<CellCoords> Way { get; } = new Stack<CellCoords>();
        private State[,] maze;
        private CellCoords finish;

        public PathFinder(State[,] maze, CellCoords start, CellCoords finish)
        {
            this.maze = maze;
            this.finish = finish;
            FindPath(start, new CellCoords());
        }

        private bool FindPath(CellCoords currentCell, CellCoords previousCell)
        {
            Way.Push(currentCell);
            if (currentCell.Equals(finish))
            {
                return true;
            }
            var ways = GetWays(currentCell).Where(cell => !cell.Equals(previousCell)).ToList();
            while (ways.Count != 0)
            {
                var way = ways.First();
                if (FindPath(way, currentCell))
                {
                    return true;
                }
                ways.Remove(way);
            }
            Way.Pop();
            return false;
        }
        private List<CellCoords> GetWays(CellCoords cell)
        {
            var list = Program.GetNeighbours(cell);
            for (int i = 0; i < list.Count; i++)
            {
                var way = list[i];
                bool wayBlocked = false;
                int deltaX = cell.x - way.x;
                CellCoords min;
                CellCoords max;
                if (deltaX == 0)
                {
                    if (cell.y < way.y)
                    {
                        min = cell;
                        max = way;
                    }
                    else
                    {
                        min = way;
                        max = cell;
                    }
                    for (int y = min.y; y <= max.y; y++)
                    {
                        if (maze[y, cell.x] == State.NotVisited)
                        {
                            wayBlocked = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (cell.x < way.x)
                    {
                        min = cell;
                        max = way;
                    }
                    else
                    {
                        min = way;
                        max = cell;
                    }
                    for (int x = min.x; x <= max.x; x++)
                    {
                        if (maze[cell.y, x] == State.NotVisited)
                        {
                            wayBlocked = true;
                            break;
                        }
                    }
                }
                if (wayBlocked)
                {
                    list.Remove(way);
                    i--;
                }
            }
            return list;
        }
    }
}
