using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IceAndFire {
	public class PriorityQueue<T> {
		List<(T elem, double priority)> Queue;

		public PriorityQueue() {
			Queue = new List<(T elem, double priority)>();
		}

		public bool IsEmpty() {
			return Queue.Count == 0;
		}

		public void Add(T elem, double priority) {
			Queue.Add((elem, priority));
		}

		public T Get() {
			var res = Queue.OrderBy(x => x.priority).FirstOrDefault();
			Queue.Remove(res);
			if (!res.Equals(default((T elem, double priority))))
				return res.elem;
			else
				return default(T);
		}
	}

	public class Grapth {
		public Dictionary<Point, List<Point>> Points { get; }

		public Grapth(string[] input) {
			Points = new Dictionary<Point, List<Point>>();

			for (int i = 0; i < input.Length; i++) {
				for (int j = 0; j < input[i].Length; j++) {
					if (input[i][j] == '1')
						continue;
					var point = new Point() {
						X = j,
						Y = i
					};
					List<Point> res = new List<Point>();
					if (i > 0 && input[i - 1][j] != '1') {
						res.Add(new Point() {
							X = j,
							Y = i - 1
						});
					}
					if (j > 0 && input[i][j - 1] != '1') {
						res.Add(new Point() {
							X = j - 1,
							Y = i
						});
					}
					if (i < input.Length - 1 && input[i + 1][j] != '1') {
						res.Add(new Point() {
							X = j,
							Y = i + 1
						});
					}
					if (j < input[i].Length - 1 && input[i][j + 1] != '1') {
						res.Add(new Point() {
							X = j + 1,
							Y = i
						});
					}
					Points.Add(point, res);
				}
			}
		}

		private double Heuristic(Point a, Point b) {
			return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
		}

		private int CostFunction(Point a, Point b) {
			return 1;
		}

		public (int quantity, List<Point> route, Dictionary<Point, int> cost) GetLessDistance(int startX, int startY, int toX, int toY) {
			var frontier = new PriorityQueue<Point>();
			var start = new Point() {
				X = startX,
				Y = startY
			};
			var goal = new Point() {
				X = toX,
				Y = toY
			};

			var nullRef = new Point() {
				X = -1,
				Y = -1
			};
			frontier.Add(start, 0);

			Dictionary<Point, int> CostSoFar = new Dictionary<Point, int>();
			Dictionary<Point, Point> CameFrom = new Dictionary<Point, Point>();
			CameFrom.Add(start, nullRef);
			CostSoFar.Add(start, 0);

			while (!frontier.IsEmpty()) {
				var current = frontier.Get();

				//if (current == goal) {
				//	break;
				//}

				foreach (var child in Points[current]) {
					var newcost = CostSoFar[current] + CostFunction(current, child);
					if (!CostSoFar.ContainsKey(child) || newcost < CostSoFar[child]) {
						CostSoFar[child] = newcost;
						double priority = newcost + Heuristic(goal, child);
						frontier.Add(child, priority);
						CameFrom[child] = current;
					}
				}
			}

			var res = new List<Point>() {
				goal
			};
			var cur = CameFrom[goal];
			while (cur != start) {
				res.Add(cur);
				cur = CameFrom[cur];
			}
			res.Add(start);
			res.Reverse();
			return (CostSoFar[goal], res, CostSoFar);
		}
	}

	public struct Point {
		public int X { get; set; }
		public int Y { get; set; }

		public static bool operator ==(Point a, Point b) {
			return a.X == b.X && a.Y == b.Y;
		}

		public static bool operator !=(Point a, Point b) {
			return a.X != b.X || a.Y != b.Y;
		}
	}


	class Program {

		static void Main(string[] args) {
			string[] input = new string[6] {
				"00010010100",
				"00011000010",
				"00010010000",
				"00000010010",
				"00000010011",
				"00000000000",
			};

			var gr = new Grapth(input);
			var watch = System.Diagnostics.Stopwatch.StartNew();
			// the code that you want to measure comes here
			var a = gr.GetLessDistance(0, 0, 5, 0);
			watch.Stop();
			Console.WriteLine(watch.ElapsedMilliseconds);
			Console.WriteLine(a.Item1);
			Console.WriteLine(String.Join("\n", a.Item2.Select(x => $"{x.X}_{x.Y}")));
			for (int i = 0; i < input.Length; i++) {
				for (int j = 0; j < input[i].Length; j++) {
					Console.Write(String.Format("{0:D2} {1},{2}\t", input[i][j] != '1'
						? a.cost[new Point() {
							X = j,
							Y = i
						}]
						: 0, j, i));
				}
				Console.Write("\n");
			}


			Console.ReadKey();
		}
	}
}
