using System;
using System.IO;
using System.Text;
using Engine;

namespace Game.Windows.Dialogs
{
    public class VerInfoDialog : Window
    {
        public static string Ver = "";
        public static string Head = "";
        public static long TotalSize;
        public static int TotalFile;
        public static int TotalVFile;
        public static double LoadTime;
        public static string Path = "";

        public VerInfoDialog(string path)
            : base("VerInfo")
        {

            var versionFilePath = path + "\\Profiles\\game.version";

            if (File.Exists(versionFilePath))
            {
                var gv = File.ReadAllBytes(versionFilePath);

                Head = Encoding.UTF8.GetString(gv, 4, 4);
                Ver = Encoding.UTF8.GetString(gv, 12, BitConverter.ToInt32(gv, 8));
            }
            else
            {
                Head = "raw data";
                Ver = "undefine";
            }

            window.Controls["time"].Text += LoadTime.ToString("0.00") + "с";
            window.Controls["ver"].Text += Ver;
            window.Controls["hd"].Text += Head;
            window.Controls["count"].Text += TotalFile;
            window.Controls["counti"].Text += TotalVFile;
            window.Controls["size"].Text += TotalSize;
        }

        protected override bool OnKeyDown(KeyEvent e)
        {
            if (e.Key == EKeys.Enter || e.Key == EKeys.Escape)
                SetShouldDetach();

            return base.OnKeyDown(e);
        }
    }
}