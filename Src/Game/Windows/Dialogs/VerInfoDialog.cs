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

        public VerInfoDialog(string path) : base("VerInfo")
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

            window.Controls["ver"].Text += Ver;
            window.Controls["hd"].Text += Head;
            window.Controls["done"].Visible = false;
        }

        protected override bool OnKeyDown(KeyEvent e)
        {
            if (e.Key == EKeys.Enter || e.Key == EKeys.Escape)
                SetShouldDetach();

            return base.OnKeyDown(e);
        }

        public void Report(object state)
        {
            window.Controls["count_files"].Text = "Кличество файлов: " + TotalFile;
            window.Controls["count_loc_files"].Text = "Файлов локанизации: " + TotalVFile;
            window.Controls["size"].Text = "Общий размер: " + TotalSize;

            if (state == null)
            {
                window.Controls["time"].Text = "Время загрузки: " + LoadTime.ToString("0.00") + "с";
                window.Controls["status"].Visible = false;
                window.Controls["done"].Visible = true;
            }
            else
            {
                window.Controls["status"].Text = state.ToString();
            }
        }
    }
}