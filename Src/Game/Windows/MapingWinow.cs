namespace Game.Windows
{
    class MapingWinow : Window
    {
        public MapingWinow()
            : base("MapingWindows")
        {
            var p = ModelViewWindow.Instance.Position;
            Position = new ScaleValue(p.Type, p.Value - Size.Value);
        }
    }
}