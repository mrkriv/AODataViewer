using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.Networking;
using ProjectCommon;

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
