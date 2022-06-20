using System.Collections.ObjectModel;
using System.Numerics;

namespace SimpleHitbox2D
{
	/// <summary>
	/// This class is used to make transformations in 2D space and to store their results.
	/// These results may be used by a graphics API to put objects in different positions, rotations and scalings on the screen
	/// (multiple objects may act as the same object in a parent/children behavior). They may also be used by a <see cref="Hitbox"/> for
	/// collision checking inbetween those objects.
	/// </summary>
	public class Transform
	{
		private readonly List<Transform> children = new();
		private Transform? parent;
		private Vector2 localPos;
		private float localAng, localSc;
		private Matrix3x2 global;

		/// <summary>
		/// The parent would transform this <see cref="Transform"/> upon moving, rotating or scaling as if they were the same object
		/// (or attached to one another).<br></br>
		/// - Note: An <see cref="ArgumentException"/> is thrown if the provided <see cref="Transform"/> a child of this <see cref="Transform"/> or itself.
		/// </summary>
		public Transform? Parent
		{
			get => parent;
			set
			{
				if(parent == value)
					return;

				if(this == value)
					throw new ArgumentException("Parenting a transform to itself is invalid.");

				for(int i = 0; i < children.Count; i++)
					if(children[i] == value)
						throw new ArgumentException("A transform cannot be both a child and a parent.");

				if(parent != null && children != null)
					parent.children.Remove(this);

				var prevPos = Position;
				var prevAng = Angle;
				var prevSc = Scale;

				parent = value;

				if(parent != null && children != null)
					parent.children.Add(this);

				Position = prevPos;
				Angle = prevAng;
				Scale = prevSc;
			}
		}
		/// <summary>
		/// The children are transformed according to this <see cref="Transform"/> upon moving, rotating or scaling as if they are the same object
		/// (or attached to one another).
		/// </summary>
		public ReadOnlyCollection<Transform> Children => children.AsReadOnly();

		/// <summary>
		/// The position relative to the <see cref="Parent"/>.
		/// </summary>
		public Vector2 LocalPosition
		{
			get => localPos;
			set { localPos = value; UpdateSelfAndChildren(); }
		}
		/// <summary>
		/// The scale relative to the <see cref="Parent"/>.
		/// </summary>
		public float LocalScale
		{
			get => localSc;
			set { localSc = value; UpdateSelfAndChildren(); }
		}
		/// <summary>
		/// The angle relative to the <see cref="Parent"/>.
		/// </summary>
		public float LocalAngle
		{
			get => localAng;
			set { localAng = value; UpdateSelfAndChildren(); }
		}
		/// <summary>
		/// The direction relative to the <see cref="Parent"/>.
		/// </summary>
		public Vector2 LocalDirection
		{
			get => Vector2.Normalize(AngleToDirection(LocalAngle));
			set => LocalAngle = DirectionToAngle(Vector2.Normalize(value));
		}

		/// <summary>
		/// The position in the world.
		/// </summary>
		public Vector2 Position
		{
			get => GetPosition(global);
			set => LocalPosition = GetLocalPositionFromParent(value);
		}
		/// <summary>
		/// The scale in the world.
		/// </summary>
		public float Scale
		{
			get => GetScale(global);
			set => LocalScale = GetScale(GlobalToLocal(value, Angle, Position));
		}
		/// <summary>
		/// The angle in the world.
		/// </summary>
		public float Angle
		{
			get => GetAngle(global);
			set => LocalAngle = GetAngle(GlobalToLocal(Scale, value, Position));
		}
		/// <summary>
		/// The direction in the world.
		/// </summary>
		public Vector2 Direction
		{
			get => Vector2.Normalize(AngleToDirection(Angle));
			set => Angle = DirectionToAngle(Vector2.Normalize(value));
		}

		/// <summary>
		/// Translates a <paramref name="position"/> in the world into a local position (relative to the <see cref="Parent"/>).
		/// </summary>
		public Vector2 GetLocalPositionFromParent(Vector2 position)
		{
			return GetPosition(GlobalToLocal(Scale, Angle, position));
		}
		/// <summary>
		/// Translates a <paramref name="localPosition"/> (relative to the <see cref="Parent"/>) into a world position.
		/// </summary>
		public Vector2 GetPositionFromParent(Vector2 localPosition)
		{
			return GetPosition(LocalToGlobal(LocalScale, LocalAngle, localPosition));
		}
		/// <summary>
		/// Translates a <paramref name="position"/> in the world into a local position (relative to this <see cref="Transform"/>).
		/// </summary>
		public Vector2 GetLocalPositionFromSelf(Vector2 position)
		{
			var m = Matrix3x2.Identity;
			m *= Matrix3x2.CreateTranslation(position);
			m *= Matrix3x2.CreateTranslation(Position);

			return GetPosition(m);
		}
		/// <summary>
		/// Translates a <paramref name="localPosition"/> (relative to this <see cref="Transform"/>) into a world position.
		/// </summary>
		public Vector2 GetPositionFromSelf(Vector2 localPosition)
		{
			var m = Matrix3x2.Identity;
			m *= Matrix3x2.CreateTranslation(localPosition);
			m *= Matrix3x2.CreateRotation(DegreesToRadians(LocalAngle));
			m *= Matrix3x2.CreateScale(LocalScale);
			m *= Matrix3x2.CreateTranslation(LocalPosition);

			return GetPosition(m * GetParentMatrix());
		}

		/// <summary>
		/// Create the <see cref="Transform"/> by specifying all of its various components.
		/// </summary>
		public Transform(Vector2 localPosition = default, float localAngle = default, float localScale = 1, Transform? parent = default, params Transform[] children)
		{
			Parent = parent;
			LocalPosition = localPosition;
			LocalAngle = localAngle;
			LocalScale = localScale;

			for(int i = 0; i < children?.Length; i++)
				if(children[i] != null)
					children[i].Parent = this;
		}

		/// <summary>
		/// Converts a 360 degrees <paramref name="angle"/> into a normalized direction <see cref="Vector2"/> then returns the result.
		/// 0 translates to <see cref="Vector2.UnitX"/>.
		/// </summary>
		public static Vector2 AngleToDirection(float angle)
		{
			//Angle to Radians : (Math.PI / 180) * angle
			//Radians to Vector2 : Vector2.x = cos(angle) ; Vector2.y = sin(angle)

			var rad = MathF.PI / 180 * angle;
			var dir = new Vector2(MathF.Cos(rad), MathF.Sin(rad));

			return new(dir.X, dir.Y);
		}
		/// <summary>
		/// Calculates the direction between <paramref name="point"/> and <paramref name="targetPoint"/>. The result may be
		/// <paramref name="normalized"/>. Then it is returned.
		/// </summary>
		public static Vector2 DirectionBetweenPoints(Vector2 point, Vector2 targetPoint, bool normalized = true)
		{
			return normalized ? Vector2.Normalize(targetPoint - point) : targetPoint - point;
		}

		/// <summary>
		/// Converts a <paramref name="direction"/> into a 360 degrees angle and returns the result.
		/// </summary>
		public static float DirectionToAngle(Vector2 direction)
		{
			//Vector2 to Radians: atan2(Vector2.y, Vector2.x)
			//Radians to Angle: radians * (180 / Math.PI)

			var rad = MathF.Atan2(direction.Y, direction.X);
			var result = rad * (180f / MathF.PI);
			return result;
		}
		/// <summary>
		/// Wraps a <paramref name="number"/> around the range 0-360 and returns it.
		/// </summary>
		public static float AngleTo360(float number)
		{
			return ((number % 360) + 360) % 360;
		}
		/// <summary>
		/// Calculates the 360 degrees angle between <paramref name="point"/> and <paramref name="targetPoint"/> then returns it.
		/// </summary>
		public static float AngleBetweenPoints(Vector2 point, Vector2 targetPoint)
		{
			return AngleTo360(DirectionToAngle(targetPoint - point));
		}

		private static float DegreesToRadians(float degrees)
		{
			return (MathF.PI / 180f) * degrees;
		}
		private static float RadiansToDegrees(float radians)
		{
			return radians * (180f / MathF.PI);
		}

		private Matrix3x2 LocalToGlobal(float localScale, float localAngle, Vector2 localPosition)
		{
			var c = Matrix3x2.Identity;
			c *= Matrix3x2.CreateScale(localScale);
			c *= Matrix3x2.CreateRotation(DegreesToRadians(localAngle));
			c *= Matrix3x2.CreateTranslation(localPosition);

			return c * GetParentMatrix();
		}
		private Matrix3x2 GlobalToLocal(float scale, float angle, Vector2 position)
		{
			var c = Matrix3x2.Identity;
			c *= Matrix3x2.CreateScale(scale);
			c *= Matrix3x2.CreateRotation(DegreesToRadians(angle));
			c *= Matrix3x2.CreateTranslation(position);

			return c * GetInverseParentMatrix();
		}
		private Matrix3x2 GetParentMatrix()
		{
			var p = Matrix3x2.Identity;
			if(parent != null)
			{
				p *= Matrix3x2.CreateScale(parent.Scale);
				p *= Matrix3x2.CreateRotation(DegreesToRadians(parent.Angle));
				p *= Matrix3x2.CreateTranslation(parent.Position);
			}
			return p;
		}
		private Matrix3x2 GetInverseParentMatrix()
		{
			var inverseParent = Matrix3x2.Identity;
			if(parent != null)
			{
				Matrix3x2.Invert(Matrix3x2.CreateScale(parent.Scale), out var s);
				Matrix3x2.Invert(Matrix3x2.CreateRotation(DegreesToRadians(parent.Angle)), out var r);
				Matrix3x2.Invert(Matrix3x2.CreateTranslation(parent.Position), out var t);

				inverseParent *= t;
				inverseParent *= r;
				inverseParent *= s;
			}

			return inverseParent;
		}

		private void UpdateSelfAndChildren()
		{
			UpdateGlobalMatrix();

			for(int i = 0; i < children.Count; i++)
				children[i].UpdateSelfAndChildren();
		}
		private void UpdateGlobalMatrix()
		{
			global = LocalToGlobal(LocalScale, LocalAngle, LocalPosition);
		}

		private static float GetAngle(Matrix3x2 matrix)
		{
			return RadiansToDegrees(MathF.Atan2(matrix.M12, matrix.M11));
		}
		private static Vector2 GetPosition(Matrix3x2 matrix)
		{
			return new(matrix.M31, matrix.M32);
		}
		private static float GetScale(Matrix3x2 matrix)
		{
			return MathF.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);
		}
	}
}
