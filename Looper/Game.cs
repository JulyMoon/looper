using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Looper
{
    public class Game
    {
        private class LevelUtil
        {
            private int width;
            private int height;
            private bool[,][] level;
            private bool[,] populated;

            private int maxFill => width * height * 4 - width * 2 - height * 2;

            public static bool[] GetItemArray(Shape shape = Shape.None, Rotation rotation = Rotation.Up)
            {
                bool[] array;

                switch (shape)
                {
                    case Shape.Q: array = new[] { true, false, false, false }; break;
                    case Shape.I: array = new[] { true, false, true, false }; break;
                    case Shape.L: array = new[] { true, true, false, false }; break;
                    case Shape.T: array = new[] { true, true, true, false }; break;
                    case Shape.X: return new[] { true, true, true, true };
                    default: return new[] { false, false, false, false };
                }

                return array.Shift((int)rotation);
            }

            public static int[] GetDirectionPoint(Rotation direction)
            {
                switch (direction)
                {
                    case Rotation.Up: return new[] { 0, -1 };
                    case Rotation.Right: return new[] { 1, 0 };
                    case Rotation.Down: return new[] { 0, 1 };
                    case Rotation.Left: return new[] { -1, 0 };
                    default: throw new Exception();
                }
            }

            private double GetFillPercentage()
            {
                int count = 0;
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        count += level[x, y].Count(stick => stick);

                return (double)count / maxFill;
            }

            public static Rotation GetShiftedRotation(Rotation r, int shift)
            {
                return (Rotation)(((int)r + shift) % 4);
            }

            private void GetRandomBorderCoordinates(out int x, out int y)
            {
                if (random.Next(2) == 0)
                {
                    x = random.Next(width);
                    y = random.Next(2) == 0 ? 0 : height - 1;
                }
                else
                {
                    x = random.Next(2) == 0 ? 0 : width - 1;
                    y = random.Next(height);
                }
            }

            private void ArrayLevelToSolvedShapes(out Shape[,] shapes, out Rotation[,] solution)
            {
                shapes = new Shape[width, height];
                solution = new Rotation[width, height];

                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        ArrayToSolvedShape(level[x, y], out shapes[x, y], out solution[x, y]);
            }

            private static void ArrayToSolvedShape(bool[] array, out Shape shape, out Rotation solution)
            {
                shape = Shape.T;
                solution = Rotation.Down;

                foreach (var shape_ in MyExtensions.GetValues<Shape>())
                    foreach (var rotation_ in MyExtensions.GetValues<Rotation>())
                        if (GetItemArray(shape_, rotation_).SequenceEqual(array))
                        {
                            shape = shape_;
                            solution = rotation_;
                            return;
                        }

                throw new Exception();
            }

            private void Populate(int startx, int starty, double limit)
            {
                int count;
                var beforePopulating = (bool[,])populated.Clone();
                do
                {
                    populated = (bool[,])beforePopulating.Clone();
                    var currentGen = new List<int[]> { new[] { startx, starty } };
                    var nextGen = new List<int[]>();
                    count = 0;

                    while (currentGen.Count > 0)
                    {
                        foreach (var point in currentGen)
                        {
                            if (populated[point[0], point[1]])
                                continue;

                            populated[point[0], point[1]] = true;

                            foreach (var direction in GetValidDirections(point[0], point[1]))
                            {
                                if (level[point[0], point[1]][(int)direction])
                                    continue;

                                if ((double)count / maxFill < limit && Random(0.5))
                                {
                                    var directionPoint = GetDirectionPoint(direction);
                                    int nx = point[0] + directionPoint[0];
                                    int ny = point[1] + directionPoint[1];

                                    if (!level[point[0], point[1]][(int)direction])
                                        count++;
                                    level[point[0], point[1]][(int)direction] = true;

                                    if (!level[nx, ny][(int)GetShiftedRotation(direction, 2)])
                                        count++;
                                    level[nx, ny][(int)GetShiftedRotation(direction, 2)] = true;

                                    nextGen.Add(new[] { nx, ny });
                                }
                            }
                        }

                        currentGen = nextGen.Clone();
                        nextGen.Clear();
                    }
                } while (count == 0);
            }

            public void GetRandomLevel(out int width_, out int height_, out Shape[,] shapes, out Rotation[,] solution)
            {
                width = 7;//random.Next(4, 6);
                height = 13;//random.Next(5, 8);
                
                populated = new bool[width, height];
                level = new bool[width, height][];
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        level[x, y] = GetItemArray();

                int bx, by;
                GetRandomBorderCoordinates(out bx, out by);

                Populate(bx, by, 1);

                ArrayLevelToSolvedShapes(out shapes, out solution);
                width_ = width;
                height_ = height;
            }

            private List<Rotation> GetEmptySides()
            {
                var emptySides = new List<Rotation>();
                bool up = false;
                bool down = false;
                bool left = false;
                bool right = false;

                for (int x = 0; x < width; x++)
                {
                    if (level[x, 0].Any(stick => stick))
                        up = true;

                    if (level[x, height - 1].Any(stick => stick))
                        down = true;
                }

                for (int y = 0; y < height; y++)
                {
                    if (level[0, y].Any(stick => stick))
                        left = true;

                    if (level[width - 1, y].Any(stick => stick))
                        right = true;
                }

                if (!up)
                    emptySides.Add(Rotation.Up);

                if (!down)
                    emptySides.Add(Rotation.Down);

                if (!left)
                    emptySides.Add(Rotation.Left);

                if (!right)
                    emptySides.Add(Rotation.Right);

                return emptySides;
            } 

            private static bool Random(double chance)
            {
                return random.NextDouble() < chance;
            }

            private List<Rotation> GetValidDirections(int x, int y)
            {
                var validDirections = new List<Rotation>();
                foreach (var direction in MyExtensions.GetValues<Rotation>())
                {
                    int[] point = GetDirectionPoint(direction);

                    if (IsWithinLevel(x + point[0], y + point[1]))
                        validDirections.Add(direction);
                }

                return validDirections;
            }

            private bool IsWithinLevel(int x, int y)
            {
                return (x >= 0 && x < width && y >= 0 && y < height);
            }

            public static bool Equals(Shape shape, Rotation a, Rotation b)
            {
                return GetItemArray(shape, a).SequenceEqual(GetItemArray(shape, b));
            }
        }

        public enum Shape
        {
            None, Q, I, L, T, X
        }

        public enum Rotation
        {
            Up, Right, Down, Left
        }

        private Shape[,] shapes;
        private Rotation[,] rotations;
        private Rotation[,] solution;

        private static readonly LevelUtil levelUtil = new LevelUtil();
        private static readonly Random random = new Random();

        public int Width;
        public int Height;

        public Shape GetShape(int x, int y)
        {
            return shapes[x, y];
        }

        public Rotation GetRotation(int x, int y)
        {
            return rotations[x, y];
        }

        public void Rotate(int x, int y)
        {
            if (shapes[x, y] != Shape.None)
                rotations[x, y] = LevelUtil.GetShiftedRotation(rotations[x, y], 1);
        }

        public bool IsSolved()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (!IsSmooth(x, y))
                        return false;

            return true;
        }

        private bool IsSmooth(int x, int y)
        {
            foreach (var direction in MyExtensions.GetValues<Rotation>())
            {
                int[] coordShift = LevelUtil.GetDirectionPoint(direction);

                int nx = x + coordShift[0];
                int ny = y + coordShift[1];

                bool otherStickingOut;
                if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    otherStickingOut = IsStickingOut(nx, ny, LevelUtil.GetShiftedRotation(direction, 2));
                else
                    otherStickingOut = false;

                if (IsStickingOut(x, y, direction) != otherStickingOut)
                    return false;
            }

            return true;
        }

        private bool IsStickingOut(int x, int y, Rotation direction)
        {
            return LevelUtil.GetItemArray(shapes[x, y], rotations[x, y])[(int)direction];
        }

        public void NewLevel()
        {
            levelUtil.GetRandomLevel(out Width, out Height, out shapes, out solution);
            rotations = (Rotation[,])solution.Clone();
            //Shuffle();
        }

        private void Shuffle()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    switch (shapes[x, y])
                    {
                        case Shape.None:
                        case Shape.X:
                            continue;
                        case Shape.I:
                            rotations[x, y] = (Rotation)random.Next(2);
                            continue;
                    }

                    var rnd = (Rotation)random.Next(3);

                    while (LevelUtil.Equals(shapes[x, y], rnd, solution[x, y]))
                        rnd = LevelUtil.GetShiftedRotation(rnd, 1);

                    rotations[x, y] = rnd;
                }
        }

        public Game(string path)
        {
            var lines = File.ReadAllLines(path);

            Width = lines[0].Split('|').Length;
            Height = lines.Length;

            shapes = new Shape[Width, Height];
            solution = new Rotation[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                var row = lines[y].Split('|');
                for (int x = 0; x < Width; x++)
                {
                    var split = row[x].Split(' ');
                    shapes[x, y] = (Shape)Int32.Parse(split[0]);
                    solution[x, y] = (Rotation)Int32.Parse(split[1]);
                }
            }

            rotations = (Rotation[,])solution.Clone();
            Shuffle();
        }

        public Game()
        {
            NewLevel();
        }
    }

    public static class MyExtensions
    {
        public static T[] Shift<T>(this T[] array, int shift)
        {
            if (shift == 0)
                return array;

            var newArray = new T[array.Length];
            for (int i = 0; i < newArray.Length; i++)
                newArray[(i + shift) % array.Length] = array[i];

            return newArray;
        }

        public static void Print<T>(this List<T> list)
        {
            Console.Write("[");

            if (list.Count > 0)
            {
                foreach (var element in list.Take(list.Count - 1))
                    Console.Write(element + ", ");
                Console.Write(list[list.Count - 1]);
            }

            Console.WriteLine("]");
        }

        public static bool ContainsArray<T>(this List<T[]> list, T[] array)
        {
            return list.Any(arr => arr.SequenceEqual(array));
        }

        public static List<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static IEnumerable<T> GetValues<T>()
        {
            return (T[])Enum.GetValues(typeof(T));
        }
    }
}
