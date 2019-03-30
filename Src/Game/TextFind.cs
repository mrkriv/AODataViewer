using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.UISystem;

namespace Game
{
    class TextFind : Window
    {
        List<Data.File> Index;
        BackgroundWorker bw;

        public TextFind()
            : base("TextFind")
        {
            ((Button)window.Controls["start"]).Click += Start_Click;
            ((Button)window.Controls["stop"]).Click += Stop_Click;
            ((ListBox)window.Controls["list"]).ItemMouseDoubleClick += TextFind_MouseDoubleClick;
            bw = new BackgroundWorker();

            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.DoWork += Find;
        }

        void TextFind_MouseDoubleClick(object sender, ListBox.ItemMouseEventArgs e)
        {
            ListBox lb = ((ListBox)window.Controls["list"]);
            if (lb.SelectedIndex != -1)
                new LocView(Index[lb.SelectedIndex]);
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            float p = e.ProgressPercentage / PakView.data.Files.Count * 100;
            window.Controls["count"].Text = string.Format("{0}/{1} ({2}%)", e.ProgressPercentage, PakView.data.Files.Count, p);
            window.Controls["bar_s"].Size = new ScaleValue(ScaleType.ScaleByResolution, new Vec2(window.Controls["bar"].Size.Value.X * p, window.Controls["bar"].Size.Value.Y));
                
            if(e.UserState != null)
            {
                ((ListBox)window.Controls["list"]).Items.Add(((Data.File)e.UserState).Name);
                Index.Add((Data.File)e.UserState);

                if (((CheckBox)window.Controls["isOnlyFind"]).Checked)
                    bw.CancelAsync();
            }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            window.Controls["start"].Enable = true;
            window.Controls["stop"].Enable = false;
        }

        void Start_Click(Button sender)
        {
            window.Controls["stop"].Enable = true;
            window.Controls["start"].Enable = false;

            Index = new List<Data.File>();
            ((ListBox)window.Controls["list"]).Items.Clear();

            bw.RunWorkerAsync(new object[] { window.Controls["text"].Text, window.Controls["mask"].Text, PakView.data.Files });
        }

        void Stop_Click(Button sender)
        {
            window.Controls["stop"].Enable = false;
            window.Controls["start"].Enable = true;
            bw.CancelAsync();
        }

        void Find(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            object[] obj = e.Argument as object[];
            string text = obj[0] as string;
            string mask = obj[1] as string;
            List<Data.File> files = obj[2] as List<Data.File>;

            int i = 0;
            int p_l = -1;

            foreach (Data.File file in files)
            {
                try
                {
                    int p = i / files.Count * 100;
                    if (p != p_l)
                        bw.ReportProgress(i);

                    if (file.Name.IndexOf(mask) != -1)
                    {
                        if (bw.CancellationPending)
                            return;

                        string t = Encoding.Unicode.GetString(file.Data.ToArray());
                        if (t.IndexOf(text) != -1)
                            bw.ReportProgress(i, file);
                    }
                }
                catch { }
                finally
                {
                    i++;
                }
            }
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            if (bw != null)
                bw.CancelAsync();
        }
    }
}