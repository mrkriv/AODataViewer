using Engine.EntitySystem;

namespace ProjectEntities
{
	public class GameWorldType : WorldType
	{
	}

	public class GameWorld : World
	{
		// ReSharper disable once ArrangeAccessorOwnerBody
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		// ReSharper disable once ArrangeTypeMemberModifiers
		// ReSharper disable once ConvertToAutoProperty
		GameWorldType _type = null; public new GameWorldType Type => _type;

		public GameWorld()
		{
			Instance = this;
		}

		public static new GameWorld Instance { get; private set; }
	}
}