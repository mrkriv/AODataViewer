using Engine.EntitySystem;
using Engine.MapSystem;


namespace ProjectEntities
{
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class GameMapType : MapType
	{
	}

	public class GameMap : Map
	{
		// ReSharper disable once ArrangeAccessorOwnerBody
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		// ReSharper disable once ArrangeTypeMemberModifiers
		// ReSharper disable once ConvertToAutoProperty
		GameMapType _type = null; public new GameMapType Type => _type;
	}
}