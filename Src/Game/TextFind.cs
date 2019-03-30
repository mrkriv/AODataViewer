using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine.MathEx;
using Engine.UISystem;

namespace Game
{
    class TextFind : Window
    {
        private List<Data.File> _index;
        private readonly BackgroundWorker _bw;
        private readonly float _pgBaseScaleW;

        public TextFind()
            : base("TextFind")
        {
            ((Button)window.Controls["start"]).Click += Start_Click;
            ((Button)window.Controls["stop"]).Click += Stop_Click;
            ((ListBox)window.Controls["list"]).ItemMouseDoubleClick += TextFind_MouseDoubleClick;
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
            var lb = ((ListBox)window.Controls["list"]);
            if (lb.SelectedIndex != -1)
                new LocView(_index[lb.SelectedIndex]);
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            float p = e.ProgressPercentage / PakView.Data.Files.Count * 100;
            window.Controls["count"].Text = $"{e.ProgressPercentage}/{PakView.Data.Files.Count} ({p}%)";
            window.Controls["bar_s"].Size = new ScaleValue(ScaleType.Parent, new Vec2(_pgBaseScaleW * p, window.Controls["bar"].Size.Value.Y));
                
            if(e.UserState != null)
            {
                ((ListBox)window.Controls["list"]).Items.Add(((Data.File)e.UserState).Name);
                _index.Add((Data.File)e.UserState);

                if (((CheckBox)window.Controls["isOnlyFind"]).Checked)
                    _bw.CancelAsync();
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

            _index = new List<Data.File>();
            ((ListBox)window.Controls["list"]).Items.Clear();

            _bw.RunWorkerAsync(new object[] { window.Controls["text"].Text, window.Controls["mask"].Text, PakView.Data.Files });
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
            var files = obj[2] as List<Data.File>;

            if (text == null || mask == null || files == null)
                return;
            
            var progressReported = -1;

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                try
                {
                    if (bw.CancellationPending)
                        return;

                    var progress = i / (files.Count * 100);
                    if (progress != progressReported)
                    {
                        bw.ReportProgress(i);
                        progressReported = progress;
                    }

                    if (file.Name.Contains(mask))
                    {
                        var t = Encoding.Unicode.GetString(file.Data.ToArray());
                        if (t.Contains(text))
                            bw.ReportProgress(i, file);
                    }
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