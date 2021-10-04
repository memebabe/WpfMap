using MapWpf.Google;
using MapWpf.Spatial.Indexer;
using MapWpf.Spatial.Types;

namespace MapWpf.Spatial
{
    internal class SpatialSheetBase<TNode> where TNode : ISpatialTreeNode
    {
        public int Level { get; private set; }
        public int GoogleLevel { get; private set; }

        //Daughter index sheets
        public readonly SpatialSheetIndexer<TNode> Sheets;

        //Content array of index elemets on bottom index sheets
        public readonly SpatialContentIndexer<TNode> Content;

        public bool IsEmpty
        {
            get
            {
                return !Sheets.HasChilds && !Content.HasChilds;
            }
        }

        public bool IsBottomSheet
        {
            get { return Level >= Sheets.PowerLength; }
        }

        public SpatialSheetBase(SpatialTree<TNode> tree, int level, int googleLevel)
        {
            Level = level;
            GoogleLevel = googleLevel;

            Sheets = new SpatialSheetIndexer<TNode>(this, tree);
            Content = new SpatialContentIndexer<TNode>();
        }

        private int GoogleNumLevel(int level)
        {
            return (int)GoogleMapUtilities.NumLevel((int)Sheets.Power(level));
        }

        private int GoogleNextLevelAddon()
        {
            return GoogleNumLevel(Level) - 1;
        }

        public int NextGoogleLevel
        {
            get { return GoogleLevel + GoogleNextLevelAddon(); }
        }

        public void Clear()
        {
            lock (this)
            {
                if (!IsBottomSheet)
                {
                    if (Sheets.HasChilds)
                    {
                        foreach (var sheet in Sheets.Values)
                        {
                            sheet.Clear();
                        }
                        Sheets.Destroy();
                    }
                }
                else
                {
                    Content.Destroy();
                }
            }
        }
    }
}