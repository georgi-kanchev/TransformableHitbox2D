using System.Numerics;

namespace SimpleHitbox2D
{
	/// <summary>
	/// Lines are useful for collision detection, debugging, raycasting and much more.
	/// </summary>
	public struct Line
	{
		/// <summary>
		/// The first (starting) point of the line.
		/// </summary>
		public Vector2 A { get; set; }
		/// <summary>
		/// The second (ending) point of the line.
		/// </summary>
		public Vector2 B { get; set; }
		/// <summary>
		/// The distance between <see cref="A"/> and <see cref="B"/>.
		/// </summary>
		public float Length => Vector2.Distance(A, B);
		/// <summary>
		/// The angle between <see cref="A"/> and <see cref="B"/>.
		/// </summary>
		public float Angle => Transform.AngleBetweenPoints(A, B);
		/// <summary>
		/// The direction between <see cref="A"/> and <see cref="B"/>.
		/// </summary>
		public Vector2 Direction => Transform.AngleToDirection(Angle);

		/// <summary>
		/// Creates the line from two points: <paramref name="a"/> and  <paramref name="b"/>.
		/// </summary>
		public Line(Vector2 a, Vector2 b)
		{
			A = a;
			B = b;
		}

		/// <summary>
		/// Returns the point where this line and another line cross. Returns an invalid vector if there is no such point.
		/// </summary>
		public Vector2 GetCrossPoint(Line line)
		{
			var p = CrossPoint(A, B, line.A, line.B);
			return Contains(p) && line.Contains(p) ? p : new(float.NaN, float.NaN);
		}
		/// <summary>
		/// Returns whether this line and another cross.
		/// </summary>
		public bool Crosses(Line line)
		{
			var result = GetCrossPoint(line);
			return float.IsNaN(result.X) == false && float.IsNaN(result.Y) == false;
		}
		/// <summary>
		/// Returns whether a point is on top of this line within a certain <paramref name="errorMargin"/>.
		/// </summary>
		public bool Contains(Vector2 point, float errorMargin = 0.01f)
		{
			var length = Vector2.Distance(A, B);
			var sum = Vector2.Distance(A, point) + Vector2.Distance(B, point);

			return IsBetween(sum, length - errorMargin, length + errorMargin);

			bool IsBetween(float number, float rangeA, float rangeB, bool inclusiveA = false, bool inclusiveB = false)
			{
				if(rangeA > rangeB)
					(rangeA, rangeB) = (rangeB, rangeA);
				var l = inclusiveA ? rangeA <= number : rangeA < number;
				var u = inclusiveB ? rangeB >= number : rangeB > number;
				return l && u;
			}
		}
		/// <summary>
		/// Returns the closest point on the line to a <paramref name="point"/>.
		/// </summary>
		public Vector2 GetClosestPoint(Vector2 point)
		{
			var AP = point - A;
			var AB = B - A;

			var magnitudeAB = AB.LengthSquared();
			var ABAPproduct = Vector2.Dot(AP, AB);
			var distance = ABAPproduct / magnitudeAB;

			return distance < 0 ?
				 A : distance > 1 ?
				 B : A + AB * distance;
		}

		private static Vector2 CrossPoint(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
		{
			var a1 = B.Y - A.Y;
			var b1 = A.X - B.X;
			var c1 = a1 * (A.X) + b1 * (A.Y);
			var a2 = D.Y - C.Y;
			var b2 = C.X - D.X;
			var c2 = a2 * (C.X) + b2 * (C.Y);
			var determinant = a1 * b2 - a2 * b1;

			if(determinant == 0)
				return new Vector2(float.NaN, float.NaN);

			var x = (b2 * c1 - b1 * c2) / determinant;
			var y = (a1 * c2 - a2 * c1) / determinant;
			return new Vector2(x, y);
		}
	}
}
