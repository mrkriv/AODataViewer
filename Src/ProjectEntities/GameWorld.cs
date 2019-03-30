using Engine.EntitySystem;

namespace ProjectEntities
{
	public class GameWorldType : WorldType
	{
	}

	public class GameWorld : World
	{
		static GameWorld instance;
		GameWorldType _type = null; public new GameWorldType Type { get { return _type; } }

		public GameWorld()
		{
			instance = this;
		}

		public static new GameWorld Instance
		{
			get { return instance; }
		}
	}
}