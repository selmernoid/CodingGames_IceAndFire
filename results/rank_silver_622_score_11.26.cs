//#define SHOW_DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

public class ACodeOfIceAndFire
{
    public enum BuildingType
    {
        Hq,
        Mine,
        Tower
    }

    public enum Team
    {
        Fire = 1,
        Ice = -1
    }

    private const int WIDTH = 12;
    private const int HEIGHT = 12;

    private const int LAST_IDX = 11;

    private const int ME = 0;
    private const int OPPONENT = 1;
    private const int NEUTRAL = -1;


    private const int TRAIN_COST_LEVEL_1 = 10;
    private const int TRAIN_COST_LEVEL_2 = 20;
    private const int TRAIN_COST_LEVEL_3 = 30;

    private static void Main()
    {
        var game = new Game();
        game.Init();

        // game loop
        while (true)
        {
            game.Update();
            game.Solve();
            Console.WriteLine(game.Output.ToString());
        }
    }

    public class Game
    {
        public readonly List<Building> Buildings = new List<Building>();

        public readonly Tile[,] Map = new Tile[WIDTH, HEIGHT];
        public readonly StringBuilder Output = new StringBuilder();


        private int MINE_COST => Buildings.Count(x => x.IsOwned && x.IsMine) * 4 + 20; // TODO: Check this formula

        public List<Position> MineSpots = new List<Position>();

        public int MyGold;
        public int MyIncome;
        public Team MyTeam;

        public int OpponentGold;
        public int OpponentIncome;
        public int Turn;
        public List<Unit> Units = new List<Unit>();

        public List<Unit> MyUnits => Units.Where(u => u.IsOwned).ToList();
        public List<Unit> OpponentUnits => Units.Where(u => u.IsOpponent).ToList();

        public Position MyHq => MyTeam == Team.Fire ? (0, 0) : (11, 11);
        public Position OpponentHq => MyTeam == Team.Fire ? (11, 11) : (0, 0);

        public List<Position> MyPositions = new List<Position>();
        public List<Position> OpponentPositions = new List<Position>();
        public List<Position> NeutralPositions = new List<Position>();

        public void Init()
        {
            for (var y = 0; y < HEIGHT; y++)
                for (var x = 0; x < WIDTH; x++)
                {
                    Map[x, y] = new Tile
                    {
                        Position = (x, y)
                    };
                }

            var numberMineSpots = int.Parse(Console.ReadLine());
            for (var i = 0; i < numberMineSpots; i++)
            {
                var inputs = Console.ReadLine().Split(' ');
                MineSpots.Add((int.Parse(inputs[0]), int.Parse(inputs[1])));
            }
        }

        public void Update() {
            Units.Clear();
            Buildings.Clear();

            MyPositions.Clear();
            OpponentPositions.Clear();
            NeutralPositions.Clear();

            Output.Clear();

            // --------------------------------------

            MyGold = int.Parse(Console.ReadLine());
            MyIncome = int.Parse(Console.ReadLine());
            OpponentGold = int.Parse(Console.ReadLine());
            OpponentIncome = int.Parse(Console.ReadLine());

            // Read Map
            for (var y = 0; y < HEIGHT; y++) {
                var line = Console.ReadLine();
                for (var x = 0; x < WIDTH; x++) {
                    var c = line[x] + "";
                    Map[x, y].IsWall = c == "#";
                    Map[x, y].Active = "OX".Contains(c);
                    Map[x, y].Owner = c.ToLower() == "o" ? ME : c.ToLower() == "x" ? OPPONENT : NEUTRAL;
                    Map[x, y].HasMineSpot = MineSpots.Count(spot => spot == (x, y)) > 0;

                    Position p = (x, y);
                    if (Map[x, y].IsOwned)
                        MyPositions.Add(p);
                    else if (Map[x, y].IsOpponent)
                        OpponentPositions.Add(p);
                    else if (!Map[x, y].IsWall) {
                        NeutralPositions.Add(p);
                    }
                }
            }

            // Read Buildings
            var buildingCount = int.Parse(Console.ReadLine());
            for (var i = 0; i < buildingCount; i++) {
                var inputs = Console.ReadLine().Split(' ');
                Buildings.Add(new Building {
                    Owner = int.Parse(inputs[0]),
                    Type = (BuildingType)int.Parse(inputs[1]),
                    Position = (int.Parse(inputs[2]), int.Parse(inputs[3]))
                });
            }

            // Read Units
            var unitCount = int.Parse(Console.ReadLine());
            for (var i = 0; i < unitCount; i++) {
                var inputs = Console.ReadLine().Split(' ');
                Units.Add(new Unit {
                    Owner = int.Parse(inputs[0]),
                    Id = int.Parse(inputs[1]),
                    Level = int.Parse(inputs[2]),
                    Position = (int.Parse(inputs[3]), int.Parse(inputs[4]))
                });
            }

            // --------------------------------

            // Get Team
            MyTeam = Buildings.Find(b => b.IsHq && b.IsOwned).Position == (0, 0) ? Team.Fire : Team.Ice;

            //Search Path algo


            for (int i = 0; i < WIDTH; i++)
                for (int j = 0; j < HEIGHT; j++) {
                    Map[i, j].DistanceToHq = 0;
                    Map[i, j].DistanceToOpponentHq = 0;
                }
            SearchPathLength(0, 0);
            SearchPathLength(WIDTH - 1, HEIGHT - 1);

            // Usefull for symmetric AI
            if (MyTeam == Team.Ice) {
                MyPositions.Reverse();
                OpponentPositions.Reverse();
                NeutralPositions.Reverse();
            }

            // --------------------------------

            // Debug
            Debug();
        }

        byte GetTileValue(bool isEnemy, Position pos) {
            return isEnemy ? Map[pos.X, pos.Y].DistanceToOpponentHq : Map[pos.X, pos.Y].DistanceToHq;
        }
        void SetTileValue(bool isEnemy, Position pos, byte value) {
            if (isEnemy)
                Map[pos.X, pos.Y].DistanceToOpponentHq = value;
            else
                Map[pos.X, pos.Y].DistanceToHq = value;
        } 
        
        private void SearchPathLength(int startX, int startY) {
            var frontier = new Queue<Position>();
            Position start = (startX, startY);
            var isEnemy = start == OpponentHq;


            SetTileValue(isEnemy, start, 0);
            frontier.Enqueue(start);

            while (frontier.Count > 0) {
                var current = frontier.Dequeue();
                var currentLength = GetTileValue(isEnemy, current);
                foreach (var point in current.GetSiblings()) {
                    if (GetTileValue(isEnemy, point) == 0 && point != start) {
                        frontier.Enqueue(point);
                        SetTileValue(isEnemy, point, (byte)(currentLength + 1));
                    }
                }
            };
        }

        [Conditional("SHOW_DEBUG")]
        public void Debug()
        {
            Console.Error.WriteLine($"Turn: {Turn}");
            Console.Error.WriteLine($"My team: {MyTeam}");
            Console.Error.WriteLine($"My gold: {MyGold} (+{MyIncome})");
            Console.Error.WriteLine($"Opponent gold: {OpponentGold} (+{OpponentIncome})");

            Console.Error.WriteLine("=====");
            foreach (var b in Buildings) Console.Error.WriteLine(b);
            foreach (var u in Units) Console.Error.WriteLine(u);
        }

        /***
            * -----------------------------------------------------------
            * TODO Solve
            * -----------------------------------------------------------
            */
        public void Solve()
        {
            // Make sur the AI doesn't timeout
            Wait();

            MoveUnits();

            TrainUnits();

            BuildMines();

            Turn++;
        }

        public void MoveUnits()
        {
            // Rush center
            Position target = OpponentHq;// MyTeam == Team.Fire ? (5, 5) : (6, 6);
            if (Map[target.X, target.Y].IsOwned) return;

            var mines = MineSpots.Where(x => !Map[x.X, x.Y].IsOwned).ToList();//.GroupBy(x => x.Owner).OrderBy(x=>x.Key == NEUTRAL);
            var selectedMines = new List<Entity>();


            foreach (var unit in MyUnits.OrderBy(x=> x.Level)
                .OrderByDescending(x=>
                x.Position.GetSiblings().Where(cell => !Map[cell.X, cell.Y].IsWall)
                .Sum(cell =>                    
                    Map[cell.X,cell.Y].IsOpponent
                        ? 2 
                        : (Map[cell.X, cell.Y].IsOwned ? 0 : 1)))) // сначала 1 лвл, и те у которых рядом мало свободных клеток
            {
                if (unit.Level == 1) { //TODO: they stucks (
                    target =
                        unit.Position.GetSiblings().Where(cell => !Map[cell.X, cell.Y].IsWall)
                        .FirstOrDefault(cell =>
                            Map[cell.X, cell.Y].Owner == OPPONENT &&
                            OpponentUnits.All(ou => ou.Position != cell) &&
                            Buildings.All(build => build.Position != cell)
                            ) ??
                        unit.Position.GetSiblings().Where(cell => !Map[cell.X, cell.Y].IsWall).FirstOrDefault(cell => !Map[cell.X, cell.Y].IsOwned);
                    if (target == null) {
                        if (mines.Any()) {
                            var closestMine = mines.OrderBy(x => unit.Position.Dist(x)).FirstOrDefault();
                            target = closestMine;
                            mines.Remove(closestMine);
                        } else target = OpponentHq;
                    }
                } else {
                    //unit.Position.GetSiblings().Where(cell => !Map[cell.X, cell.Y].IsWall &&
                    //        OpponentUnits.All(ou => ou.Position != cell) &&
                    //        Buildings.All(build => build.Position != cell))
                    target = OpponentHq;
                }

                Move(unit.Id, target);
            }
        }

        public void TrainUnits()
        {
            if (MyGold >= TRAIN_COST_LEVEL_1 && MyUnits.Count < 16) {
                //Position target;// = MyTeam == Team.Fire ? (1, 0) : (10, 11);
                int start = MyTeam == Team.Fire ? 0 : 11;

                while ((MyGold >= TRAIN_COST_LEVEL_1 && MyUnits.Count < 14)) {
                    Position minPos = (0, 0);
                    int minValue = int.MaxValue;
                    for (var i = 0; i < 12; i++) {
                        for (var j = 0; j < 12; j++) {
                            Position thisPos = (i, j);
                            if (Map[i,j].DistanceToOpponentHq < minValue
                                    && Buildings.All(b => b.Position != (i, j)) 
                                    && Units.All(b => b.Position != (i, j))
                                    && !Map[thisPos.X, thisPos.Y].IsWall
                                    && thisPos.GetSiblings().Any(s => Map[s.X, s.Y].IsOwned)) {
                                minPos = (i, j);
                                minValue = Map[i, j].DistanceToOpponentHq;
                            }
                        }
                    }
                    if (minValue != int.MaxValue) {
                        Train(1, minPos);
                    }
                }
            }
            if (Turn > 15 && MyGold >= TRAIN_COST_LEVEL_3 && MyUnits.Count(x => x.Level == 3) < 4)
            {
                var target = Map.ToEnumerable().Where(x =>
                        !x.IsWall
                        && Units.All(unit => unit.Position != x.Position) && Buildings.All(b => b.Position != x.Position)
                        && x.Position.GetSiblings().Any(sib => Map[sib.X, sib.Y].IsOwned && Map[sib.X, sib.Y].Active)
                    ).OrderBy(x => x.Position.Dist(OpponentHq));
                if (target.Any())
                    Train(2, target.First().Position);
            }

            //DEFENSE TRAIN:
            var oponentUnits = OpponentUnits;

            var danger = oponentUnits.Where(x =>
                Map.ToEnumerable().Any(t => t.IsOwned && t.Active && t.Position.Dist(x.Position) <= 2) &&
                MyUnits.All(my => my.Level < x.Level || my.Position.Dist(x.Position) > 2)
            ).OrderBy(x => x.Position.Dist(MyHq));
            foreach (var enemyUnit in danger)
            {
                int requiredLvl = Math.Min(3, enemyUnit.Level + 1);
                int requireMoney = requiredLvl * TRAIN_COST_LEVEL_1;
                if (MyGold < requireMoney)
                    break;

                //TODO: make possible to train mask.
                var tile = Map.ToEnumerable().Where(t => t.IsOwned && t.Position.Dist(enemyUnit.Position) <= 2 && Units.All(unit => unit.Position != t.Position) && Buildings.All(b => b.Position != t.Position));
                if (!tile.Any())
                    continue;

                Train(requiredLvl, tile.First().Position);
            }
        }

        public void BuildMines()
        {
            if (MyGold < MINE_COST)
                return;
            var mines =
                    MineSpots
                    .Where(x =>
                    {
                        var cell = Map[x.X, x.Y];
                        return cell.IsOwned && cell.Active && Buildings.All(b => b.Position != cell.Position);
                    }).ToList();

            foreach (var mine in mines)
            {
                if (MyGold < MINE_COST)
                    return;

                Build(BuildingType.Mine, mine);
                Buildings.Add(new Building
                {
                    Owner = ME,
                    Type = BuildingType.Mine,
                    Position = (mine.X, mine.Y)
                });

            }
        }

        public void Wait()
        {
            Output.Append("WAIT;");
        }

        public void Train(int level, Position position)
        {
            // TODO: Handle upkeep
            Units.Add(new Unit { Id = -1, Owner = ME, Position = (position.X, position.Y), Level = level });
            int cost = 0;
            switch (level)
            {
                case 1: cost = TRAIN_COST_LEVEL_1; break;
                case 2: cost = TRAIN_COST_LEVEL_2; break;
                case 3: cost = TRAIN_COST_LEVEL_3; break;
            }

            MyGold -= cost;
            Output.Append($"TRAIN {level} {position.X} {position.Y};");
        }

        public void Move(int id, Position position)
        {
            var unit = Units.First(x => x.Id == id);
            unit.Position = (position.X, position.Y);
            Output.Append($"MOVE {id} {position.X} {position.Y};");
        }
        public void Build(BuildingType building, Position position)
        {
            // TODO: Handle map change
            Output.Append($"BUILD {building.ToString().ToUpper()} {position.X} {position.Y};");
            MyGold -= MINE_COST;
        }
    }


    public class Unit : Entity
    {
        public int Id;
        public int Level;

        public override string ToString() => $"Unit => {base.ToString()} Id: {Id} Level: {Level}";
    }

    public class Building : Entity
    {
        public BuildingType Type;

        public bool IsHq => Type == BuildingType.Hq;
        public bool IsTower => Type == BuildingType.Tower;
        public bool IsMine => Type == BuildingType.Mine;

        public override string ToString() => $"Building => {base.ToString()} Type: {Type}";
    }

    public class Entity
    {
        public int Owner;
        public Position Position;

        public bool IsOwned => Owner == ME;
        public bool IsOpponent => Owner == OPPONENT;

        public int X => Position.X;
        public int Y => Position.Y;

        public override string ToString() => $"Owner: {Owner} Position: {Position}";
    }

    public class Tile
    {
        public bool Active;
        public bool HasMineSpot;
        public bool IsWall;

        public int Owner = NEUTRAL;

        public Position Position;
        public int X => Position.X;
        public int Y => Position.Y;

        public byte DistanceToHq;
        public byte DistanceToOpponentHq;

        public bool IsOwned => Owner == ME;
        public bool IsOpponent => Owner == OPPONENT;
        public bool IsNeutral => Owner == NEUTRAL;
    }

    public class Position {
        public int X;
        public int Y;

        public IEnumerable<Position> GetSiblings() {
            if (X != 0) yield return (X - 1, Y);
            if (Y != 0) yield return (X, Y - 1);
            if (X != WIDTH - 1) yield return (X + 1, Y);
            if (Y != HEIGHT - 1) yield return (X, Y + 1);
        }

        public static implicit operator Position(ValueTuple<int, int> cell) => new Position {
            X = cell.Item1,
            Y = cell.Item2
        };

        public override string ToString() => $"({X},{Y})";

        public static bool operator ==(Position obj1, Position obj2) => Equals2(obj1, obj2);

        public static bool operator !=(Position obj1, Position obj2) => !Equals2(obj1, obj2);

        public override bool Equals(object obj) => Equals2(this, (Position)obj);

        protected static bool Equals2(Position obj1, Position obj2) {
            if (Object.ReferenceEquals(obj1, obj2))
                return true;
            if (Object.ReferenceEquals(null, obj2) || Object.ReferenceEquals(obj1, null))
                return false;
            return obj1.X == obj2.X && obj1.Y == obj2.Y;
        }

        protected bool Equals(Position other) => Equals2(this, other);

        public int Dist(Position p) => Math.Abs(X - p.X) + Math.Abs(Y - p.Y);
    }
}


public static class ArrayExtensions
{
    public static IEnumerable<T> ToEnumerable<T>(this T[,] target)
    {
        foreach (var item in target)
            yield return item;
    }
}