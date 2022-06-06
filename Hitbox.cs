using System.Numerics;

namespace SimpleHitbox2D
{
	/// <summary>
	/// A <see cref="Line"/> collection used to determine whether it interacts in any way with other hitboxes/points.
	/// </summary>
	public class Hitbox
	{
		/// <summary>
		/// This list is used by <see cref="TransformLocalLines"/> which transforms and moves its contents into <see cref="Lines"/>.
		/// </summary>
		public List<Line> LocalLines { get; } = new();
		/// <summary>
		/// The resulting list of lines of all transformations after <see cref="TransformLocalLines"/> (if any).
		/// </summary>
		public List<Line> Lines { get; } = new();

		/// <summary>
		/// Constructs both <see cref="Lines"/> and <see cref="LocalLines"/> from between a set of <paramref name="points"/>.
		/// </summary>
		public Hitbox(params Vector2[] points)
		{
			for(int i = 1; i < points?.Length; i++)
			{
				var line = new Line(points[i - 1], points[i]);
				LocalLines.Add(line);
				Lines.Add(line);
			}
		}

		/// <summary>
		/// Takes <see cref="LocalLines"/>, applies a <paramref name="transform"/> on them and puts the result into <see cref="Lines"/>
		/// for the rest of the methods to use. Any previous changes to the <see cref="Lines"/> list will be erased.
		/// </summary>
		public void TransformLocalLines(Transform transform)
		{
			Lines.Clear();

			for(int i = 0; i < LocalLines.Count; i++)
			{
				var a = transform.GetPositionFromSelf(LocalLines[i].A);
				var b = transform.GetPositionFromSelf(LocalLines[i].B);
				Lines.Add(new(a, b));
			}
		}
		/// <summary>
		/// Calculates and then returns all the cross points (if any) produced between this and and another <paramref name="hitbox"/>.
		/// </summary>
		public List<Vector2> GetCrossPoints(Hitbox hitbox)
		{
			var result = new List<Vector2>();
			for(int i = 0; i < Lines.Count; i++)
				for(int j = 0; j < hitbox.Lines.Count; j++)
				{
					var p = Lines[i].GetCrossPoint(hitbox.Lines[j]);
					if(float.IsNaN(p.X) == false && float.IsNaN(p.Y) == false)
						result.Add(p);
				}
			return result;
		}
		/// <summary>
		/// A shortcut for
		/// <code>var overlaps = Crosses(hitbox) || ConvexContains(hitbox);</code>
		/// </summary>
		public bool ConvexOverlaps(Hitbox hitbox)
		{
			return Crosses(hitbox) || ConvexContains(hitbox);
		}
		/// <summary>
		/// Whether <see cref="Lines"/> surround a <paramref name="point"/>.
		/// Or in other words: whether this <see cref="Hitbox"/> contains a <paramref name="point"/>.<br></br>
		/// - Note: Some of the results will be wrong if <see cref="Lines"/> are forming a concave shape.
		/// </summary>
		public bool ConvexContains(Vector2 point)
		{
			if(Lines == null || Lines.Count < 3)
				return false;

			var crosses = 0;
			var outsidePoint = Vector2.Lerp(Lines[0].A, Lines[0].B, -50);

			for(int i = 0; i < Lines.Count; i++)
				if(Lines[i].Crosses(new(point, outsidePoint)))
					crosses++;

			return crosses % 2 == 1;
		}
		/// <summary>
		/// Whether <see cref="Lines"/> cross <paramref name="hitbox"/>'s lines.
		/// </summary>
		public bool Crosses(Hitbox hitbox)
		{
			for(int i = 0; i < Lines.Count; i++)
				for(int j = 0; j < hitbox.Lines.Count; j++)
					if(Lines[i].Crosses(hitbox.Lines[j]))
						return true;

			return false;
		}
		/// <summary>
		/// Whether <see cref="Lines"/> completely surround <paramref name="hitbox"/>'s lines.
		/// Or in other words: whether this <see cref="Hitbox"/> contains <paramref name="hitbox"/>.<br></br>
		/// - Note: Some of the results will be wrong if any of the hitboxes' lines are forming a concave shape.
		/// </summary>
		public bool ConvexContains(Hitbox hitbox)
		{
			for(int i = 0; i < hitbox.Lines.Count; i++)
				for(int j = 0; j < Lines.Count; j++)
					if((ConvexContains(hitbox.Lines[i].A) == false || ConvexContains(hitbox.Lines[i].B) == false) &&
						 (hitbox.ConvexContains(Lines[j].A) == false || hitbox.ConvexContains(Lines[j].B) == false))
						return false;
			return true;
		}
		/// <summary>
		/// Returns the closest point on this hitbox's lines to a certain point.
		/// </summary>
		public Vector2 GetClosestPoint(Vector2 point)
		{
			var points = new List<Vector2>();
			var result = new Vector2();
			var bestDist = float.MaxValue;

			for(int i = 0; i < Lines.Count; i++)
				points.Add(Lines[i].GetClosestPoint(point));
			for(int i = 0; i < points.Count; i++)
			{
				var dist = Vector2.Distance(points[i], point);
				if(dist < bestDist)
				{
					bestDist = dist;
					result = points[i];
				}
			}
			return result;
		}
	}
}