using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.FileSystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.SoundSystem;
using Engine.Utils;
using Engine.Networking;
using Game.Windows;
using ProjectCommon;

namespace Game
{
	public class GameEngineApp : EngineApp
	{
		public const float FadingTime = 1;
		private float _fadingOutTimer;

		private string _needMapLoadName;
		private bool _needRunExampleOfProceduralMapCreation;
		private string _needWorldLoadName;

		private bool _needFadingOutAndExit;

		private float _fadingInRemainingTime;
		private int _fadingInSkipFirstFrames;

		private static float _gamma = 1;

		[Config("Video", "gamma")]
		public static float Gamma
		{
			get => _gamma;
			set
			{
				_gamma = value;
				EngineApp.Instance.Gamma = _gamma;
			}
		}

		static bool _showSystemCursor = true;

		[Config("Video", "showSystemCursor")]
		public static bool ShowSystemCursor
		{
			get => _showSystemCursor;
			set
			{
				_showSystemCursor = value;

				EngineApp.Instance.ShowSystemCursor = value;

				if (EngineApp.Instance.ShowSystemCursor)
				{
					if (Instance != null && Instance.ControlManager != null)
						Instance.ControlManager.DefaultCursor = null;
				}
				else
				{
					var cursorName = "GUI\\Cursors\\Default.png";
					if (!VirtualFile.Exists(cursorName))
						cursorName = null;
					if (Instance != null && Instance.ControlManager != null)
						Instance.ControlManager.DefaultCursor = cursorName;
					if (cursorName == null)
						EngineApp.Instance.ShowSystemCursor = true;
				}
			}
		}

		static bool _drawFps;

		[Config("Video", "drawFPS")]
		public static bool DrawFps
		{
			get => _drawFps;
			set
			{
				_drawFps = value;
				EngineApp.Instance.ShowFPS = value;
			}
		}

		static MaterialSchemes _materialScheme = MaterialSchemes.Default;

		[Config("Video", "materialScheme")]
		public static MaterialSchemes MaterialScheme
		{
			get => _materialScheme;
			set
			{
				_materialScheme = value;
				if (RendererWorld.Instance != null)
					RendererWorld.Instance.DefaultViewport.MaterialScheme = _materialScheme.ToString();
			}
		}

		[Config("Video", "shadowTechnique")]
		public static ShadowTechniques ShadowTechnique { get; set; } = ShadowTechniques.ShadowmapHigh;

		//this options affect to shadowColor and shadowFarDistance

		[Config("Video", "shadowUseMapSettings")]
		public static bool ShadowUseMapSettings { get; set; } = true;

		[Config("Video", "shadowColor")]
		public static ColorValue ShadowColor { get; set; } = new ColorValue(.75f, .75f, .75f);

		[Config("Video", "shadowFarDistance")] public static float ShadowFarDistance { get; set; } = 50;

		[Config("Video", "shadowPSSMSplitFactors")]
		public static Vec2 ShadowPssmSplitFactors { get; set; } = new Vec2(.1f, .4f);

		[Config("Video", "shadowDirectionalLightTextureSize")]
		public static int ShadowDirectionalLightTextureSize { get; set; } = 2048;

		[Config("Video", "shadowDirectionalLightMaxTextureCount")]
		public static int ShadowDirectionalLightMaxTextureCount { get; set; } = 1;

		[Config("Video", "shadowSpotLightTextureSize")]
		public static int ShadowSpotLightTextureSize { get; set; } = 2048;

		[Config("Video", "shadowSpotLightMaxTextureCount")]
		public static int ShadowSpotLightMaxTextureCount { get; set; } = 2;

		[Config("Video", "shadowPointLightTextureSize")]
		public static int ShadowPointLightTextureSize { get; set; } = 1024;

		[Config("Video", "shadowPointLightMaxTextureCount")]
		public static int ShadowPointLightMaxTextureCount { get; set; } = 2;

		[Config("Video", "showDecorativeObjects")]
		public static bool ShowDecorativeObjects { get; set; } = true;

		[Config("Environment", "autorunMapName")]
		public static string AutorunMapName = "";

		//screenMessages
		class ScreenMessage
		{
			public string Text;
			public float TimeRemaining;
		}

		List<ScreenMessage> _screenMessages = new List<ScreenMessage>();

		//

		public GameEngineApp()
			: base(ApplicationTypes.Simulation)
		{
		}

		public static new GameEngineApp Instance { get; private set; }

		void ChangeToBetterDefaultSettings()
		{
			var shadowTechniqueInitialized = false;
			var shadowTextureSizeInitialized = false;

			if (!string.IsNullOrEmpty(ConfigName))
			{
				string error;
				var block = TextBlockUtils.LoadFromRealFile(
					VirtualFileSystem.GetRealPathByVirtual(ConfigName), out error);
				var blockVideo = block?.FindChild("Video");
				if (blockVideo != null)
				{
					if (blockVideo.IsAttributeExist("shadowTechnique"))
						shadowTechniqueInitialized = true;
					if (blockVideo.IsAttributeExist("shadowDirectionalLightTextureSize"))
						shadowTextureSizeInitialized = true;
				}
			}

			//shadowTechnique
			if (!shadowTechniqueInitialized)
			{
				//configure optimal settings for this computer
				if (RenderSystem.Instance.HasShaderModel3() &&
				    RenderSystem.Instance.Capabilities.HardwareRenderToTexture)
				{
					ShadowTechnique = ShadowTechniques.ShadowmapHigh;

					//if( RenderSystem.Instance.GPUIsGeForce() )
					//{
					//   if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
					//      RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV40 )
					//   {
					//      shadowTechnique = ShadowTechniques.ShadowmapLow;
					//   }
					//   if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.GeForce_G70 )
					//      shadowTechnique = ShadowTechniques.ShadowmapMedium;
					//}

					//if( RenderSystem.Instance.GPUIsRadeon() )
					//{
					//   if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
					//      RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
					//   {
					//      shadowTechnique = ShadowTechniques.ShadowmapLow;
					//   }
					//   if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.Radeon_R500 )
					//      shadowTechnique = ShadowTechniques.ShadowmapMedium;
					//}

					//if( RenderSystem.Instance.GPUIsIntel() )
					//{
					//   if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.Intel_HDGraphics )
					//      shadowTechnique = ShadowTechniques.ShadowmapHigh;
					//}
				}
				else
					ShadowTechnique = ShadowTechniques.None;
			}

			//shadow texture size
			if (!shadowTextureSizeInitialized)
			{
				if (RenderSystem.Instance.GPUIsGeForce())
				{
					if (RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
					    RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_G70)
					{
						ShadowDirectionalLightTextureSize = 1024;
						ShadowSpotLightTextureSize = 1024;
						ShadowPointLightTextureSize = 512;
					}
				}
				else if (RenderSystem.Instance.GPUIsRadeon())
				{
					if (RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
					    RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R500)
					{
						ShadowDirectionalLightTextureSize = 1024;
						ShadowSpotLightTextureSize = 1024;
						ShadowPointLightTextureSize = 512;
					}
				}
				else if (RenderSystem.Instance.GPUIsIntel())
				{
					if (RenderSystem.Instance.GPUCodeName != GPUCodeNames.Intel_HDGraphics)
					{
						ShadowDirectionalLightTextureSize = 1024;
						ShadowSpotLightTextureSize = 1024;
						ShadowPointLightTextureSize = 512;
					}
				}
				else
				{
					ShadowDirectionalLightTextureSize = 1024;
					ShadowSpotLightTextureSize = 1024;
					ShadowPointLightTextureSize = 512;
				}
			}
		}

		float _loadingCallbackLastTimeCall;

		void LongOperationCallbackManager_LoadingCallback(string callerInfo, object userData)
		{
			//How to calculate time for progress bar.
			//It is impossible to make universal solution. It's depending to concrete project.
			//By "callerInfo" data you can collect useful info for calculation total loading time.

			//Limit fps.
			const float maxFpsInv = 1.0f / 15.0f;
			var now = Time;
			if (now - _loadingCallbackLastTimeCall < maxFpsInv)
				return;
			_loadingCallbackLastTimeCall = now;

			//animate "Indicator".
			var loadingWindow = userData as Control;
			var indicator = loadingWindow?.Controls["Indicator"];
			if (indicator != null)
			{
				var frame = (int) ((EngineApp.Instance.Time * 20) % 8);
				indicator.BackTextureCoord =
					new Rect((float) frame / 8, 0, (float) (frame + 1) / 8, 1);
			}

			//Update frame (2D only).
			SceneManager.Instance.Enable3DSceneRendering = false;
			RenderScene();
			SceneManager.Instance.Enable3DSceneRendering = true;
		}

		protected override void OnBeforeRendererWorldInit()
		{
			base.OnBeforeRendererWorldInit();

			//here you can set default settings like:
			//RendererWorld.InitializationOptions.FullSceneAntialiasing.
		}

		protected override void OnAfterRendererWorldInit()
		{
			base.OnAfterRendererWorldInit();

			//We will load materials later (in OnCreate()), during showing "ProgramLoadingWindow.gui".
			HighLevelMaterialManager.Instance.NeedLoadAllMaterialsAtStartup = false;
		}

		protected override bool OnCreate()
		{
			Instance = this;

			ChangeToBetterDefaultSettings();

			if (!base.OnCreate())
				return false;

			ControlManager = new ScreenControlManager(ScreenGuiRenderer);
			if (!ControlsWorld.Init())
				return false;

			ShowSystemCursor = ShowSystemCursor;
			DrawFps = DrawFps;
			MaterialScheme = _materialScheme;

			Log.Handlers.InvisibleInfoHandler += InvisibleLog_Handlers_InfoHandler;
			Log.Handlers.InfoHandler += Log_Handlers_InfoHandler;
			Log.Handlers.WarningHandler += Log_Handlers_WarningHandler;
			Log.Handlers.ErrorHandler += Log_Handlers_ErrorHandler;
			Log.Handlers.FatalHandler += Log_Handlers_FatalHandler;

			//Camera
			var camera = RendererWorld.Instance.DefaultCamera;
			camera.NearClipDistance = .1f;
			camera.FarClipDistance = 1000.0f;
			camera.FixedUp = Vec3.ZAxis;
			camera.Fov = 90;
			camera.Position = new Vec3(-10, -10, 10);
			camera.LookAt(new Vec3(0, 0, 0));

			var programLoadingWindow = ControlDeclarationManager.Instance.CreateControl("Gui\\Loading.gui");
			
			if (programLoadingWindow != null)
				ControlManager.Controls.Add(programLoadingWindow);

			//Subcribe to callbacks during engine loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe(LongOperationCallbackManager_LoadingCallback,
				programLoadingWindow);

			//load materials.
			if (!HighLevelMaterialManager.Instance.NeedLoadAllMaterialsAtStartup)
			{
				//prevent double initialization of materials after startup by means CreateEmptyMaterialsForFasterStartupInitialization = true.
				ShaderBaseMaterial.CreateEmptyMaterialsForFasterStartupInitialization = true;
				if (!HighLevelMaterialManager.Instance.LoadAllMaterials())
				{
					LongOperationCallbackManager.Unsubscribe();
					return true;
				}

				ShaderBaseMaterial.CreateEmptyMaterialsForFasterStartupInitialization = false;
			}

			RenderScene();

			//EntitySystem
			if (!EntitySystemWorld.Init(new EntitySystemWorld()))
			{
				LongOperationCallbackManager.Unsubscribe();
				return true;
			}

			//load autorun map
			var mapName = GetAutorunMapName();
			var mapLoadingFailed = false;
			if (mapName != "")
			{
				//hide loading window.
				LongOperationCallbackManager.Unsubscribe();
				programLoadingWindow?.SetShouldDetach();

				if (!ServerOrSingle_MapLoad(mapName, EntitySystemWorld.Instance.DefaultWorldType, false))
					mapLoadingFailed = true;
			}

			//finish initialization of materials and hide loading window.
			ShaderBaseMaterial.FinishInitializationOfEmptyMaterials();
			LongOperationCallbackManager.Unsubscribe();
			programLoadingWindow?.SetShouldDetach();

			//if no autorun map play music and go to EngineLogoWindow.
			if (Map.Instance == null && !mapLoadingFailed)
			{
				ControlManager.Controls.Add(new MainMenuWindow());
			}

			return true;
		}

		string GetAutorunMapName()
		{
			var mapName = "";

			if (AutorunMapName != "" && AutorunMapName.Length > 2)
			{
				mapName = AutorunMapName;
				if (!mapName.Contains("\\") && !mapName.Contains("/"))
					mapName = "Maps/" + mapName + "/Map.map";
			}

			if (PlatformInfo.Platform != PlatformInfo.Platforms.Android)
			{
				var commandLineArgs = Environment.GetCommandLineArgs();
				if (commandLineArgs.Length > 1)
				{
					var name = commandLineArgs[1];
					if (name[0] == '\"' && name[name.Length - 1] == '\"')
						name = name.Substring(1, name.Length - 2);
					name = name.Replace('/', '\\');

					var dataDirectory = VirtualFileSystem.ResourceDirectoryPath;
					dataDirectory = dataDirectory.Replace('/', '\\');

					if (name.Length > dataDirectory.Length)
						if (string.Compare(name.Substring(0, dataDirectory.Length), dataDirectory, true) == 0)
							name = name.Substring(dataDirectory.Length + 1);

					mapName = name;
				}
			}

			return mapName;
		}

		protected override void OnDestroy()
		{
			MapSystemWorld.MapDestroy();
			if (EntitySystemWorld.Instance != null)
				EntitySystemWorld.Instance.WorldDestroy();

			Server_DestroyServer("The server has been destroyed");
			EntitySystemWorld.Shutdown();

			ControlsWorld.Shutdown();
			ControlManager = null;

			EngineConsole.Shutdown();

			Instance = null;
			base.OnDestroy();
		}

		protected override bool OnKeyDown(KeyEvent e)
		{
			//Engine console
			if (EngineConsole.Instance != null)
				if (EngineConsole.Instance.DoKeyDown(e))
					return true;

			//UI controls
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoKeyDown(e))
					return true;

			return base.OnKeyDown(e);
		}

		protected override bool OnKeyPress(KeyPressEvent e)
		{
			if (EngineConsole.Instance != null)
				if (EngineConsole.Instance.DoKeyPress(e))
					return true;
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoKeyPress(e))
					return true;

			return base.OnKeyPress(e);
		}

		protected override bool OnKeyUp(KeyEvent e)
		{
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoKeyUp(e))
					return true;
			return base.OnKeyUp(e);
		}

		protected override bool OnMouseDown(EMouseButtons button)
		{
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoMouseDown(button))
					return true;
			return base.OnMouseDown(button);
		}

		protected override bool OnMouseUp(EMouseButtons button)
		{
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoMouseUp(button))
					return true;
			return base.OnMouseUp(button);
		}

		protected override bool OnMouseDoubleClick(EMouseButtons button)
		{
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoMouseDoubleClick(button))
					return true;
			return base.OnMouseDoubleClick(button);
		}

		protected override void OnMouseMove(Vec2 mouse)
		{
			base.OnMouseMove(mouse);
			if (ControlManager != null && !IsScreenFadingOut())
				ControlManager.DoMouseMove(mouse);
		}

		protected override bool OnMouseWheel(int delta)
		{
			if (EngineConsole.Instance != null)
				if (EngineConsole.Instance.DoMouseWheel(delta))
					return true;
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoMouseWheel(delta))
					return true;
			return base.OnMouseWheel(delta);
		}

		protected override bool OnJoystickEvent(JoystickInputEvent e)
		{
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoJoystickEvent(e))
					return true;
			return base.OnJoystickEvent(e);
		}

		protected override bool OnCustomInputDeviceEvent(InputEvent e)
		{
			if (ControlManager != null && !IsScreenFadingOut())
				if (ControlManager.DoCustomInputDeviceEvent(e))
					return true;
			return base.OnCustomInputDeviceEvent(e);
		}

		protected override void OnSystemPause(bool pause)
		{
			base.OnSystemPause(pause);

			if (EntitySystemWorld.Instance != null)
				EntitySystemWorld.Instance.SystemPauseOfSimulation = pause;
		}

		bool IsScreenFadingOut()
		{
			if (_needMapLoadName != null || _needRunExampleOfProceduralMapCreation || _needWorldLoadName != null)
				return true;
			if (_needFadingOutAndExit)
				return true;
			return false;
		}

		protected override void OnTick(float delta)
		{
			base.OnTick(delta);

			//need load map or world?
			if (_needMapLoadName != null || _needRunExampleOfProceduralMapCreation || _needWorldLoadName != null)
			{
				if (_fadingOutTimer > 0)
				{
					_fadingOutTimer -= delta;
					if (_fadingOutTimer < 0)
						_fadingOutTimer = 0;
				}

				if (_fadingOutTimer == 0)
				{
					//close all windows
					foreach (var control in ControlManager.Controls)
						control.SetShouldDetach();

					if (_needMapLoadName != null)
					{
						var name = _needMapLoadName;
						_needMapLoadName = null;
						ServerOrSingle_MapLoad(name, EntitySystemWorld.Instance.DefaultWorldType, false);
					}
					else if (_needRunExampleOfProceduralMapCreation)
					{
						_needRunExampleOfProceduralMapCreation = false;
					}
					else if (_needWorldLoadName != null)
					{
						var name = _needWorldLoadName;
						_needWorldLoadName = null;
						WorldLoad(name);
					}
				}
			}

			//exit application fading out
			if (_needFadingOutAndExit)
			{
				if (_fadingOutTimer > 0)
				{
					_fadingOutTimer -= delta;
					if (_fadingOutTimer < 0)
						_fadingOutTimer = 0;
				}

				if (_fadingOutTimer == 0)
				{
					//Now application must be closed.
					SetNeedExit();

					return;
				}
			}

			if (EngineConsole.Instance != null)
				EngineConsole.Instance.DoTick(delta);
			ControlManager.DoTick(delta);

			//screenMessages
			{
				for (var n = 0; n < _screenMessages.Count; n++)
				{
					_screenMessages[n].TimeRemaining -= delta;
					if (_screenMessages[n].TimeRemaining <= 0)
					{
						_screenMessages.RemoveAt(n);
						n--;
					}
				}
			}
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			SystemCursorFileName = "GUI\\Cursors\\DefaultSystem.cur";
			ControlManager.DoRender();
		}

		protected override void OnRenderScreenUI(GuiRenderer renderer)
		{
			base.OnRenderScreenUI(renderer);

			if (Map.Instance != null)
				Map.Instance.DoRenderUI(renderer);

			ControlManager.DoRenderUI(renderer);

			//screenMessages
			{
				var viewport = RendererWorld.Instance.DefaultViewport;
				var shadowOffset = 2.0f / viewport.DimensionsInPixels.Size.ToVec2();

				var pos = new Vec2(.03f, .75f);

				for (var n = _screenMessages.Count - 1; n >= 0; n--)
				{
					var message = _screenMessages[n];

					var alpha = message.TimeRemaining;
					if (alpha > 1)
						alpha = 1;
					renderer.AddText(message.Text, pos + shadowOffset, HorizontalAlign.Left,
						VerticalAlign.Bottom, new ColorValue(0, 0, 0, alpha / 2));
					renderer.AddText(message.Text, pos, HorizontalAlign.Left, VerticalAlign.Bottom,
						new ColorValue(1, 1, 1, alpha));

					pos.Y -= renderer.DefaultFont.Height;
				}
			}

			//fading in, out
			RenderFadingOut(renderer);
			RenderFadingIn(renderer);

			if (EngineConsole.Instance != null)
				EngineConsole.Instance.DoRenderUI();
		}

		void RenderFadingOut(GuiRenderer renderer)
		{
			if (IsScreenFadingOut())
			{
				if (_fadingOutTimer != 0)
				{
					var alpha = 1.0f - _fadingOutTimer / FadingTime;
					MathFunctions.Saturate(ref alpha);
					renderer.AddQuad(new Rect(0, 0, 1, 1), new ColorValue(0, 0, 0, alpha));
				}
			}
		}

		void RenderFadingIn(GuiRenderer renderer)
		{
			if (_fadingInRemainingTime > 0)
			{
				//we are skip some amount of frames because resources can be loaded during it.
				if (_fadingInSkipFirstFrames == 0)
				{
					_fadingInRemainingTime -= RendererWorld.Instance.FrameRenderTimeStep;
					if (_fadingInRemainingTime < 0)
						_fadingInRemainingTime = 0;
				}
				else
					_fadingInSkipFirstFrames--;

				var alpha = _fadingInRemainingTime / 1;
				MathFunctions.Saturate(ref alpha);
				renderer.AddQuad(new Rect(0, 0, 1, 1), new ColorValue(0, 0, 0, alpha));
			}
		}


		public bool ServerOrSingle_MapLoad(string fileName, WorldType worldType,
			bool noChangeWindows)
		{
			Control mapLoadingWindow = null;

			//show map loading window
			if (!noChangeWindows)
			{
				var mapDirectory = Path.GetDirectoryName(fileName);
				var guiPath = Path.Combine(mapDirectory, "Description\\MapLoadingWindow.gui");
				if (!VirtualFile.Exists(guiPath))
					guiPath = "Gui\\MapLoadingWindow.gui";
				mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl(guiPath);
				if (mapLoadingWindow != null)
				{
					mapLoadingWindow.Text = fileName;
					ControlManager.Controls.Add(mapLoadingWindow);
				}
			}

			var mapWasDestroyed = Map.Instance != null;

			MapSystemWorld.MapDestroy();

			//update sound listener
			if (SoundWorld.Instance != null)
				SoundWorld.Instance.SetListener(new Vec3(10000, 10000, 10000), Vec3.Zero, Vec3.XAxis, Vec3.ZAxis);

			if (!noChangeWindows)
				RenderScene();

			//unload all reloadable textures
			TextureManager.Instance.UnloadAll(true, false);

			//create world if need
			if (World.Instance == null || World.Instance.Type != worldType)
			{
				WorldSimulationTypes worldSimulationType;
				EntitySystemWorld.NetworkingInterface networkingInterface = null;

				worldSimulationType = WorldSimulationTypes.Single;

				if (!EntitySystemWorld.Instance.WorldCreate(worldSimulationType, worldType,
					networkingInterface))
				{
					Log.Fatal("GameEngineApp: MapLoad: EntitySystemWorld.WorldCreate failed.");
				}
			}

			//Subcribe to callbacks during map loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe(LongOperationCallbackManager_LoadingCallback,
				mapLoadingWindow);

			//load map
			if (!MapSystemWorld.MapLoad(fileName))
			{
				mapLoadingWindow?.SetShouldDetach();

				LongOperationCallbackManager.Unsubscribe();

				return false;
			}

			//Simulate physics for 5 seconds. That the physics has fallen asleep.
			if (EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle())
				SimulatePhysicsForLoadedMap(5);

			//Update fog and shadow settings. This operation can be slow because need update all 
			//shaders if fog type or shadow technique changed.
			Map.Instance.UpdateSceneManagerFogAndShadowSettings();

			//Ensure that all materials are fully initialized.
			ShaderBaseMaterial.FinishInitializationOfEmptyMaterials();

			ActivateScreenFadingIn();

			LongOperationCallbackManager.Unsubscribe();

			EntitySystemWorld.Instance.ResetExecutedTime();

			return true;
		}

		void SimulatePhysicsForLoadedMap(float seconds)
		{
		}

		public bool WorldLoad(string fileName)
		{
			Control worldLoadingWindow = null;

			//world loading window
			{
				worldLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\WorldLoadingWindow.gui");
				if (worldLoadingWindow != null)
				{
					worldLoadingWindow.Text = fileName;
					ControlManager.Controls.Add(worldLoadingWindow);
				}
			}

			//Subcribe to callbacks during engine loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe(LongOperationCallbackManager_LoadingCallback,
				worldLoadingWindow);

			MapSystemWorld.MapDestroy();
			if (EntitySystemWorld.Instance != null)
				EntitySystemWorld.Instance.WorldDestroy();

			RenderScene();

			//unload all reloadable textures
			TextureManager.Instance.UnloadAll(true, false);

			if (!MapSystemWorld.WorldLoad(WorldSimulationTypes.Single, fileName))
			{
				worldLoadingWindow?.SetShouldDetach();

				LongOperationCallbackManager.Unsubscribe();

				return false;
			}

			//Update fog and shadow settings. This operation can be slow because need update all 
			//shaders if fog type or shadow technique changed.
			Map.Instance.UpdateSceneManagerFogAndShadowSettings();

			ActivateScreenFadingIn();

			LongOperationCallbackManager.Unsubscribe();

			return true;
		}

		public void SetNeedMapLoad(string fileName)
		{
			_needMapLoadName = fileName;
			_fadingOutTimer = FadingTime;
		}

		public void SetNeedRunExampleOfProceduralMapCreation()
		{
			_needRunExampleOfProceduralMapCreation = true;
			_fadingOutTimer = FadingTime;
		}

		public void SetNeedWorldLoad(string fileName)
		{
			_needWorldLoadName = fileName;
			_fadingOutTimer = FadingTime;
		}

		public void SetFadeOutScreenAndExit()
		{
			_needFadingOutAndExit = true;
			_fadingOutTimer = FadingTime;
		}


		public void Server_OnCreateServer()
		{
			SuspendWorkingWhenApplicationIsNotActive = false;
		}

		public void Server_DestroyServer(string reason)
		{
		}

		public bool ClientAllowCheckForDisconnection { get; set; } = true;

		static readonly string[] SkipLogMessages = new[]
		{
			"Initializing high level material:",
			"OGRE: Texture: ",
			"OGRE: D3D9 : Loading ",
			"OGRE: Mesh: Loading ",
		};

		void InvisibleLog_Handlers_InfoHandler(string text, ref bool dumpToLogFile)
		{
			//prevent some messages from writing to log file.
			foreach (var filter in SkipLogMessages)
			{
				if (text.Contains(filter))
					dumpToLogFile = false;
			}

			//if( EngineConsole.Instance != null )
			//   EngineConsole.Instance.Print( text );
		}

		void Log_Handlers_InfoHandler(string text, ref bool dumpToLogFile)
		{
			if (EngineConsole.Instance != null)
				EngineConsole.Instance.Print(text);
		}

		void Log_Handlers_WarningHandler(string text, ref bool handled, ref bool dumpToLogFile)
		{
			if (EngineConsole.Instance != null)
			{
				handled = true;
				EngineConsole.Instance.Print("Warning: " + text, new ColorValue(1, 0, 0));
				if (EngineConsole.Instance.AutoOpening)
					EngineConsole.Instance.Active = true;
			}
		}

		void Log_Handlers_ErrorHandler(string text, ref bool handled, ref bool dumpToLogFile)
		{
		}

		void Log_Handlers_FatalHandler(string text, string createdLogFilePath, ref bool handled)
		{
		}

		public ScreenControlManager ControlManager { get; private set; }

		protected override void OnRegisterConfigParameter(Config.Parameter parameter)
		{
			base.OnRegisterConfigParameter(parameter);

			if (EngineConsole.Instance != null)
				EngineConsole.Instance.RegisterConfigParameter(parameter);
		}

		protected override void OnBeforeUpdateShadowSettings()
		{
			base.OnBeforeUpdateShadowSettings();

			//Override map's shadow settings by game options.
			if (Map.Instance != null)
			{
				var map = Map.Instance;

				map.ShadowTechnique = ShadowTechnique;

				if (ShadowUseMapSettings)
				{
					ShadowPssmSplitFactors = map.InitialShadowPSSMSplitFactors;
					ShadowFarDistance = map.InitialShadowFarDistance;
					ShadowColor = map.InitialShadowColor;
				}

				map.ShadowColor = ShadowColor;
				map.ShadowFarDistance = ShadowFarDistance;
				map.ShadowPSSMSplitFactors = ShadowPssmSplitFactors;

				map.ShadowDirectionalLightTextureSize = Map.GetShadowTextureSize(
					ShadowDirectionalLightTextureSize);
				map.ShadowDirectionalLightMaxTextureCount = ShadowDirectionalLightMaxTextureCount;

				map.ShadowSpotLightTextureSize = Map.GetShadowTextureSize(
					ShadowSpotLightTextureSize);
				map.ShadowSpotLightMaxTextureCount = ShadowSpotLightMaxTextureCount;

				map.ShadowPointLightTextureSize = Map.GetShadowTextureSize(
					ShadowPointLightTextureSize);
				map.ShadowPointLightMaxTextureCount = ShadowPointLightMaxTextureCount;
			}
		}

		public void AddScreenMessage(string text)
		{
			var message = new ScreenMessage();
			message.Text = text;
			message.TimeRemaining = 5;
			_screenMessages.Add(message);

			while (_screenMessages.Count > 70)
				_screenMessages.RemoveAt(0);
		}

		void ActivateScreenFadingIn()
		{
			_fadingInRemainingTime = 1;
			_fadingInSkipFirstFrames = 5;
		}
	}
}