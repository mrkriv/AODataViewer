// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.EntitySystem;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;
using ProjectCommon;

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
