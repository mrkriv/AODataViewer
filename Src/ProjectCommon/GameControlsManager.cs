// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Reflection;
using Engine;
using Engine.MathEx;

namespace ProjectCommon
{
	[AttributeUsageAttribute( AttributeTargets.Field, AllowMultiple = true )]
	public class DefaultKeyboardMouseValueAttribute : Attribute
	{
		GameControlsManager.SystemKeyboardMouseValue value;

		//

		public DefaultKeyboardMouseValueAttribute( EKeys key )
		{
			value = new GameControlsManager.SystemKeyboardMouseValue( key );
		}

		public DefaultKeyboardMouseValueAttribute( EMouseButtons mouseButton )
		{
			value = new GameControlsManager.SystemKeyboardMouseValue( mouseButton );
		}

		public GameControlsManager.SystemKeyboardMouseValue Value
		{
			get { return value; }
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public enum JoystickAxisFilters
	{
		//NotZero,
		GreaterZero,
		LessZero,
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[AttributeUsageAttribute( AttributeTargets.Field, AllowMultiple = true )]
	public class DefaultJoystickValueAttribute : Attribute
	{
		GameControlsManager.SystemJoystickValue value;

		//

		public DefaultJoystickValueAttribute( JoystickButtons button )
		{
			value = new GameControlsManager.SystemJoystickValue( button );
		}

		public DefaultJoystickValueAttribute( JoystickAxes axis, JoystickAxisFilters filter )
		{
			value = new GameControlsManager.SystemJoystickValue( axis, filter );
		}

		public DefaultJoystickValueAttribute( JoystickPOVs pov, JoystickPOVDirections direction )
		{
			value = new GameControlsManager.SystemJoystickValue( pov, direction );
		}

		public GameControlsManager.SystemJoystickValue Value
		{
			get { return value; }
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public abstract class GameControlsEventData
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public abstract class GameControlsKeyEventData : GameControlsEventData
	{
		GameControlKeys controlKey;

		//

		public GameControlsKeyEventData( GameControlKeys controlKey )
		{
			this.controlKey = controlKey;
		}

		public GameControlKeys ControlKey
		{
			get { return controlKey; }
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class GameControlsKeyDownEventData : GameControlsKeyEventData
	{
		float strength;

		public GameControlsKeyDownEventData( GameControlKeys controlKey, float strength )
			: base( controlKey )
		{
			this.strength = strength;
		}

		public float Strength
		{
			get { return strength; }
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class GameControlsKeyUpEventData : GameControlsKeyEventData
	{
		public GameControlsKeyUpEventData( GameControlKeys controlKey )
			: base( controlKey )
		{
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class GameControlsMouseMoveEventData : GameControlsEventData
	{
		Vec2 mouseOffset;

		public GameControlsMouseMoveEventData( Vec2 mouseOffset )
		{
			this.mouseOffset = mouseOffset;
		}

		public Vec2 MouseOffset
		{
			get { return mouseOffset; }
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class GameControlsTickEventData : GameControlsEventData
	{
		float delta;

		public GameControlsTickEventData( float delta )
		{
			this.delta = delta;
		}

		public float Delta
		{
			get { return delta; }
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public delegate void GameControlsEventDelegate( GameControlsEventData e );

	////////////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Represents the player control management.
	/// </summary>
	public sealed class GameControlsManager
	{
		static GameControlsManager instance;

		GameControlItem[] items;
		Dictionary<GameControlKeys, GameControlItem> itemsControlKeysDictionary;

		[Config( "GameControls", "mouseSensitivity" )]
		public static Vec2 mouseSensitivity = new Vec2( 1, 1 );

		[Config( "GameControls", "joystickAxesSensitivity" )]
		public static Vec2 joystickAxesSensitivity = new Vec2( 1, 1 );

		[Config( "GameControls", "alwaysRun" )]
		public static bool alwaysRun = true;

		///////////////////////////////////////////

		public event GameControlsEventDelegate GameControlsEvent;

		///////////////////////////////////////////

		public class SystemKeyboardMouseValue
		{
			public enum Types
			{
				Key,
				MouseButton,
			}
			Types type;
			EKeys key;
			EMouseButtons mouseButton;

			//

			public SystemKeyboardMouseValue( EKeys key )
			{
				type = Types.Key;
				this.key = key;
			}

			public SystemKeyboardMouseValue( EMouseButtons mouseButton )
			{
				type = Types.MouseButton;
				this.mouseButton = mouseButton;
			}

			public Types Type
			{
				get { return type; }
			}

			public EKeys Key
			{
				get { return key; }
			}

			public EMouseButtons MouseButton
			{
				get { return mouseButton; }
			}

			public override string ToString()
			{
				if( type == Types.Key )
					return $"Key \"{key}\"";
				else
					return $"Mouse {mouseButton.ToString().ToLower()} button";
			}
		}

		///////////////////////////////////////////

		public class SystemJoystickValue
		{
			public enum Types
			{
				Button,
				Axis,
				POV,
			}
			Types type;
			JoystickButtons button;
			JoystickAxes axis;
			JoystickAxisFilters axisFilter;
			JoystickPOVs pov;
			JoystickPOVDirections povDirection;

			//

			public SystemJoystickValue( JoystickButtons button )
			{
				type = Types.Button;
				this.button = button;
			}

			public SystemJoystickValue( JoystickAxes axis, JoystickAxisFilters axisFilter )
			{
				type = Types.Axis;
				this.axis = axis;
				this.axisFilter = axisFilter;
			}

			public SystemJoystickValue( JoystickPOVs pov, JoystickPOVDirections povDirection )
			{
				type = Types.POV;
				this.pov = pov;
				this.povDirection = povDirection;
			}

			public Types Type
			{
				get { return type; }
			}

			public JoystickButtons Button
			{
				get { return button; }
			}

			public JoystickAxes Axis
			{
				get { return axis; }
			}

			public JoystickAxisFilters AxisFilter
			{
				get { return axisFilter; }
			}

			public JoystickPOVs POV
			{
				get { return pov; }
			}

			public JoystickPOVDirections POVDirection
			{
				get { return povDirection; }
			}
		}

		///////////////////////////////////////////

		public class GameControlItem
		{
			GameControlKeys controlKey;

			SystemKeyboardMouseValue[] defaultKeyboardMouseValues;
			SystemJoystickValue[] defaultJoystickValues;

			//!!!!!!not implemented
			//List<SystemKeyboardMouseValue> bindedKeyboardMouseValues = 
			//   new List<SystemKeyboardMouseValue>();
			//List<SystemJoystickValue> bindedDefaultJoystickValues = 
			//   new List<SystemJoystickValue>();

			//

			public GameControlItem( GameControlKeys controlKey )
			{
				this.controlKey = controlKey;

				//defaultKeyboardMouseValue
				{
					var field = typeof( GameControlKeys ).GetField(
						Enum.GetName( typeof( GameControlKeys ), controlKey ) );
					var attributes =
						(DefaultKeyboardMouseValueAttribute[])Attribute.GetCustomAttributes(
						field, typeof( DefaultKeyboardMouseValueAttribute ) );

					defaultKeyboardMouseValues = new SystemKeyboardMouseValue[ attributes.Length ];
					for( var n = 0; n < attributes.Length; n++ )
						defaultKeyboardMouseValues[ n ] = attributes[ n ].Value;
				}

				//defaultJoystickValue
				{
					var field = typeof( GameControlKeys ).GetField(
						Enum.GetName( typeof( GameControlKeys ), controlKey ) );
					var attributes = (DefaultJoystickValueAttribute[])
						Attribute.GetCustomAttributes( field, typeof( DefaultJoystickValueAttribute ) );

					defaultJoystickValues = new SystemJoystickValue[ attributes.Length ];
					for( var n = 0; n < attributes.Length; n++ )
						defaultJoystickValues[ n ] = attributes[ n ].Value;
				}
			}

			public GameControlKeys ControlKey
			{
				get { return controlKey; }
			}

			/// <summary>
			/// <b>Don't modify</b>.
			/// </summary>
			public SystemKeyboardMouseValue[] DefaultKeyboardMouseValues
			{
				get { return defaultKeyboardMouseValues; }
			}

			/// <summary>
			/// <b>Don't modify</b>.
			/// </summary>
			public SystemJoystickValue[] DefaultJoystickValues
			{
				get { return defaultJoystickValues; }
			}
		}

		///////////////////////////////////////////

		/// <summary>
		/// Initialization the class.
		/// </summary>
		/// <returns><b>true</b> if the object successfully initialized; otherwise, <b>false</b>.</returns>
		public static bool Init()
		{
			if( instance != null )
				Log.Fatal( "GameControlsManager class is already initialized." );

			instance = new GameControlsManager();
			var ret = instance.InitInternal();
			if( !ret )
				Shutdown();
			return ret;
		}

		/// <summary>
		/// Shutdown the class.
		/// </summary>
		public static void Shutdown()
		{
			if( instance != null )
			{
				instance.ShutdownInternal();
				instance = null;
			}
		}

		/// <summary>
		/// Gets an instance of the <see cref="ProjectCommon.GameControlsManager"/>.
		/// </summary>
		public static GameControlsManager Instance
		{
			get { return instance; }
		}

		bool InitInternal()
		{
			//register config settings
			EngineApp.Instance.Config.RegisterClassParameters( typeof( GameControlsManager ) );

			//create items
			{
				var controlKeyCount = 0;
				{
					foreach( var value in Enum.GetValues( typeof( GameControlKeys ) ) )
					{
						var controlKey = (GameControlKeys)value;
						if( (int)controlKey >= controlKeyCount )
							controlKeyCount = (int)controlKey + 1;
					}
				}

				items = new GameControlItem[ controlKeyCount ];
				for( var n = 0; n < controlKeyCount; n++ )
				{
					if( !Enum.IsDefined( typeof( GameControlKeys ), n ) )
					{
						Log.Fatal( "GameControlsManager: Init: Invalid \"GameControlKeys\" enumeration." );
						return false;
					}
					var controlKey = (GameControlKeys)n;
					items[ n ] = new GameControlItem( controlKey );
				}
			}

			//itemsControlKeysDictionary
			{
				itemsControlKeysDictionary = new Dictionary<GameControlKeys, GameControlItem>();
				foreach( var item in items )
					itemsControlKeysDictionary.Add( item.ControlKey, item );
			}

			return true;
		}

		void ShutdownInternal()
		{
		}

		/// <summary>
		/// Sends the notice on pressing a system key.
		/// </summary>
		/// <param name="e">Key event arguments.</param>
		/// <returns><b>true</b> if such system key is used; otherwise, <b>false</b>.</returns>
		public bool DoKeyDown( KeyEvent e )
		{
			var handled = false;
			//!!!!!slowly
			foreach( var item in items )
			{
				//!!!!!need use binded values here
				foreach( var value in item.DefaultKeyboardMouseValues )
				{
					if( value.Type == SystemKeyboardMouseValue.Types.Key && value.Key == e.Key )
					{
						GameControlsEvent?.Invoke( new GameControlsKeyDownEventData( item.ControlKey, 1 ) );
						handled = true;
					}
				}
			}
			return handled;
		}

		/// <summary>
		/// Sends the notice on releasing a system key.
		/// </summary>
		/// <param name="e">Key event arguments.</param>
		/// <returns><b>true</b> if such system key is used; otherwise, <b>false</b>.</returns>
		public bool DoKeyUp( KeyEvent e )
		{
			var handled = false;
			//!!!!!slowly
			foreach( var item in items )
			{
				//!!!!!need use binded values here
				foreach( var value in item.DefaultKeyboardMouseValues )
				{
					if( value.Type == SystemKeyboardMouseValue.Types.Key && value.Key == e.Key )
					{
						GameControlsEvent?.Invoke( new GameControlsKeyUpEventData( item.ControlKey ) );
						handled = true;
					}
				}
			}
			return handled;
		}

		/// <summary>
		/// Sends the notice on pressing a mouse button.
		/// </summary>
		/// <param name="button">A value indicating which button was clicked.</param>
		/// <returns><b>true</b> if such system key is used; otherwise, <b>false</b>.</returns>
		public bool DoMouseDown( EMouseButtons button )
		{
			var handled = false;
			//!!!!!slowly
			foreach( var item in items )
			{
				//!!!!!need use binded values here
				foreach( var value in item.DefaultKeyboardMouseValues )
				{
					if( value.Type == SystemKeyboardMouseValue.Types.MouseButton &&
						value.MouseButton == button )
					{
						GameControlsEvent?.Invoke( new GameControlsKeyDownEventData( item.ControlKey, 1 ) );
						handled = true;
					}
				}
			}
			return handled;
		}

		/// <summary>
		/// Sends the notice on releasing a mouse button.
		/// </summary>
		/// <param name="button">A value indicating which button was clicked.</param>
		/// <returns><b>true</b> if such system key is used; otherwise, <b>false</b>.</returns>
		public bool DoMouseUp( EMouseButtons button )
		{
			var handled = false;
			//!!!!!slowly
			foreach( var item in items )
			{
				//!!!!!need use binded values here
				foreach( var value in item.DefaultKeyboardMouseValues )
				{
					if( value.Type == SystemKeyboardMouseValue.Types.MouseButton &&
						value.MouseButton == button )
					{
						GameControlsEvent?.Invoke( new GameControlsKeyUpEventData( item.ControlKey ) );
						handled = true;
					}
				}
			}
			return handled;
		}

		/// <summary>
		/// Sends the notice on cursor moved.
		/// </summary>
		/// <param name="mouseOffset">Current mouse position.</param>
		public void DoMouseMoveRelative( Vec2 mouseOffset )
		{
			GameControlsEvent?.Invoke( new GameControlsMouseMoveEventData( mouseOffset ) );
		}

		public bool DoJoystickEvent( JoystickInputEvent e )
		{
			//JoystickButtonDownEvent
			{
				var evt = e as JoystickButtonDownEvent;
				if( evt != null )
				{
					var handled = false;
					//!!!!!slowly
					foreach( var item in items )
					{
						//!!!!!need use binded values here
						foreach( var value in item.DefaultJoystickValues )
						{
							if( value.Type == SystemJoystickValue.Types.Button &&
								value.Button == evt.Button.Name )
							{
								GameControlsEvent?.Invoke( new GameControlsKeyDownEventData(
									item.ControlKey, 1 ) );
								handled = true;
							}
						}
					}
					return handled;
				}
			}

			//JoystickButtonUpEvent
			{
				var evt = e as JoystickButtonUpEvent;
				if( evt != null )
				{
					var handled = false;
					//!!!!!slowly
					foreach( var item in items )
					{
						//!!!!!need use binded values here
						foreach( var value in item.DefaultJoystickValues )
						{
							if( value.Type == SystemJoystickValue.Types.Button &&
								value.Button == evt.Button.Name )
							{
								GameControlsEvent?.Invoke( new GameControlsKeyUpEventData( item.ControlKey ) );
								handled = true;
							}
						}
					}
					return handled;
				}
			}

			//JoystickAxisChangedEvent
			{
				var evt = e as JoystickAxisChangedEvent;
				if( evt != null )
				{
					var handled = false;
					//!!!!!slowly
					foreach( var item in items )
					{
						//!!!!!need use binded values here
						foreach( var value in item.DefaultJoystickValues )
						{
							if( value.Type == SystemJoystickValue.Types.Axis &&
								value.Axis == evt.Axis.Name )
							{
								float strength = 0;

								//!!!!need change in the options
								const float deadZone = .2f;// 20%

								switch( value.AxisFilter )
								{
								case JoystickAxisFilters.LessZero:
									if( evt.Axis.Value < -deadZone )
										strength = -evt.Axis.Value;
									break;

								case JoystickAxisFilters.GreaterZero:
									if( evt.Axis.Value > deadZone )
										strength = evt.Axis.Value;
									break;
								}

								if( strength != 0 )
								{
									GameControlsEvent?.Invoke( new GameControlsKeyDownEventData(
										item.ControlKey, strength ) );
								}
								else
								{
									GameControlsEvent?.Invoke( new GameControlsKeyUpEventData(
										item.ControlKey ) );
								}

								handled = true;
							}
						}
					}

					return handled;
				}
			}

			//JoystickPOVChangedEvent
			{
				var evt = e as JoystickPOVChangedEvent;
				if( evt != null )
				{
					var handled = false;
					//!!!!!slowly
					foreach( var item in items )
					{
						//!!!!!need use binded values here
						foreach( var value in item.DefaultJoystickValues )
						{
							if( value.Type == SystemJoystickValue.Types.POV &&
								value.POV == evt.POV.Name )
							{
								if( ( value.POVDirection & evt.POV.Value ) != 0 )
								{
									GameControlsEvent?.Invoke( new GameControlsKeyDownEventData(
										item.ControlKey, 1 ) );
								}
								else
								{
									GameControlsEvent?.Invoke( new GameControlsKeyUpEventData(
										item.ControlKey ) );
								}
								handled = true;
							}
						}
					}
					return handled;
				}
			}

			//JoystickSliderChangedEvent
			{
				var evt = e as JoystickSliderChangedEvent;
				if( evt != null )
				{
					//..
				}
			}

			return false;
		}

		public void DoTick( float delta )
		{
			GameControlsEvent?.Invoke( new GameControlsTickEventData( delta ) );
		}

		public void DoKeyUpAll()
		{
			foreach( var item in items )
			{
				var eventData =
					new GameControlsKeyUpEventData( item.ControlKey );

				GameControlsEvent?.Invoke( eventData );
			}
		}

		public Vec2 MouseSensitivity
		{
			get { return mouseSensitivity; }
			set { mouseSensitivity = value; }
		}

		public Vec2 JoystickAxesSensitivity
		{
			get { return joystickAxesSensitivity; }
			set { joystickAxesSensitivity = value; }
		}

		public bool AlwaysRun
		{
			get { return alwaysRun; }
			set { alwaysRun = value; }
		}

		/// <summary>
		/// Gets the key information collection. <b>Don't modify</b>.
		/// </summary>
		public GameControlItem[] Items
		{
			get { return items; }
		}

		public GameControlItem GetItemByControlKey( GameControlKeys controlKey )
		{
			GameControlItem item;
			if( !itemsControlKeysDictionary.TryGetValue( controlKey, out item ) )
				return null;
			return item;
		}
	}
}
