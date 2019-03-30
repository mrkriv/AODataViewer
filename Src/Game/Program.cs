// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Engine;
using Engine.MathEx;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.Utils;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
    public static class Program
    {
        public static bool needRestartApplication;
        [STAThread]
        static void Main()
        {
            if (Debugger.IsAttached)
            {
                Main2();
            }
            else
            {
                try
                {
                    Main2();
                }
                catch (Exception e)
                {
                    Log.FatalAsException(e.ToString());
                }
            }
        }

        static void Main2()
        {
            string date = DateTime.Now.ToString("HH.mm.dd.MM.yy");
            if (!VirtualFileSystem.Init("user:Logs/" + date + ".log", true, null, null, null, null))
                return;

            EngineApp.ConfigName = "user:Config.config";
            EngineApp.UseDirectInputForMouseRelativeMode = true;

            EngineApp.AllowJoysticksAndCustomInputDevices = true;
            EngineApp.AllowWriteEngineConfigFile = true;
            EngineApp.AllowChangeVideoMode = true;
            EngineApp.Init(new GameEngineApp());
            EngineApp.Instance.Config.RegisterClassParameters(typeof(GameEngineApp));

            EngineApp.Instance.WindowTitle = "Allods Online Model Viewer";
            EngineApp.Instance.WindowState = EngineApp.WindowStates.Maximized;
            EngineApp.Instance.ShowFPS = false;
            EngineApp.Instance.Icon = Game.Properties.Resources.Logo;

            EngineConsole.Init();
            if (EngineApp.Instance.Create())
                EngineApp.Instance.Run();

            EngineApp.Shutdown();

            Log.DumpToFile("Program END\r\n");

            VirtualFileSystem.Shutdown();

            if (needRestartApplication)
                Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location, "");
        }
    }
}