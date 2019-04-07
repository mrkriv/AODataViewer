using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine.MathEx;
using Engine.UISystem;
using Game.Structures;

namespace Game.Windows
{
    class TextFind : Window
    {
        private List<VFile> _index;
        private readonly BackgroundWorker _bw;
        private readonly float _pgBaseScaleW;

        public TextFind()
            : base("TextFind")
        {
            ((Button) window.Controls["start"]).Click += Start_Click;
            ((Button) window.Controls["stop"]).Click += Stop_Click;
            ((ListBox) window.Controls["list"]).ItemMouseDoubleClick += TextFind_MouseDoubleClick;
            _bw = new BackgroundWorker();

            _bw.WorkerReportsProgress = true;
            _bw.WorkerSupportsCancellation = true;

            _bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            _bw.ProgressChanged += bw_ProgressChanged;
            _bw.DoWork += Find;

            _pgBaseScaleW = window.Controls["bar"].Size.Value.X;
        }

        void TextFind_MouseDoubleClick(object sender, ListBox.ItemMouseEventArgs e)
        {
            var lb = ((ListBox) window.Controls["list"]);
            if (lb.SelectedIndex != -1)
                new TextViewWindow(_index[lb.SelectedIndex], Encoding.Unicode);
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //float p = e.ProgressPercentage / PakViewWindow.Data.Files.Count * 100;
            //window.Controls["count"].Text = $"{e.ProgressPercentage}/{PakViewWindow.Data.Files.Count} ({p}%)";
            // window.Controls["bar_s"].Size = new ScaleValue(ScaleType.Parent, new Vec2(_pgBaseScaleW * p, window.Controls["bar"].Size.Value.Y));

            window.Controls["count"].Text = "processing...";

            if (e.UserState != null)
            {
                ((ListBox) window.Controls["list"]).Items.Add(((VFile) e.UserState).Name);
                _index.Add((VFile) e.UserState);

                if (((CheckBox) window.Controls["isOnlyFind"]).Checked)
                {
                    window.Controls["count"].Text = "";
                    _bw.CancelAsync();
                }
            }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            window.Controls["start"].Enable = true;
            window.Controls["stop"].Enable = false;

            window.Controls["count"].Text = "";
        }

        void Start_Click(Button sender)
        {
            window.Controls["stop"].Enable = true;
            window.Controls["start"].Enable = false;

            _index = new List<VFile>();
            ((ListBox) window.Controls["list"]).Items.Clear();

            _bw.RunWorkerAsync(new object[]
            {
                window.Controls["text"].Text,
                window.Controls["mask"].Text,
                PakViewWindow.Data.RootDirectory
            });
        }

        void Stop_Click(Button sender)
        {
            window.Controls["stop"].Enable = false;
            window.Controls["start"].Enable = true;
            _bw.CancelAsync();
        }

        void Find(object sender, DoWorkEventArgs e)
        {
            var bw = sender as BackgroundWorker;
            var obj = e.Argument as object[];

            if (bw == null || obj == null)
                return;

            var text = obj[0] as string;
            var mask = obj[1] as string;
            var dir = obj[2] as VDirectory;

            if (text == null || mask == null || dir == null)
                return;

            //var progressReported = -1;

            var files = dir.FindFile(mask);
            foreach (var file in files)
            {
                try
                {
                    if (bw.CancellationPending)
                        return;

                    /*var progress = i / (files.Count * 100);
                    if (progress != progressReported)
                    {
                        bw.ReportProgress(i);
                        progressReported = progress;
                    }*/
                    
                    var t = Encoding.Unicode.GetString(file.Data.ToArray());
                    if (t.Contains(text))
                        bw.ReportProgress(0, file);
                }
                catch
                {
                    // ignored
                }
            }
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            _bw?.CancelAsync();
        }
    }
}