using Engine.EntitySystem;
using Engine.MapSystem;


namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="GameMap"/> entity type.
	/// </summary>
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class GameMapType : MapType
	{
	}

	public class GameMap : Map
	{
		GameMapType _type = null; public new GameMapType Type { get { return _type; } }
	}
}