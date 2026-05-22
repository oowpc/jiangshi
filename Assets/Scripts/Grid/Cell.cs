namespace Jiangshi.Grid
{
    public enum CellContent { None, Forest, IronOre, CopperOre }

    public sealed class Cell
    {
        public GridPosition Position { get; }
        public bool IsWalkable { get; set; } = true;
        public bool IsBuildable { get; set; } = true;
        public bool IsOccupied { get; set; }
        public CellContent Content { get; set; }

        public Cell(GridPosition position)
        {
            Position = position;
        }
    }
}

