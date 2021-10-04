using System.Collections.Generic;
using MapWpf.Spatial.Indexer;
using MapWpf.Spatial.Types;
using MapWpf.Types;

namespace MapWpf.Spatial
{
    public class SpatialTree<TNode> where TNode : ISpatialTreeNode
    {
        //Spatial index depth count, can be > 4
        internal readonly int SpatialDepth = 4;

        //Root spatial index sheet includes all daughter indexed sheets
        private SpatialSheet<TNode> _root;
        public int NodeCount { get; private set; }
        public readonly SpatialLevelPowerIndexer<TNode> Power;

        internal int[] NodeDimension = null;

        public SpatialTree()
        {
            NodeCount = 0;
            Power = new SpatialLevelPowerIndexer<TNode>(this);

            SetLevels(
                SpatialSheetPowerTypes.Medium,
                SpatialSheetPowerTypes.Medium,
                SpatialSheetPowerTypes.Medium,
                SpatialSheetPowerTypes.Medium);
        }

        public SpatialTree(SpatialSheetPowerTypes level1, SpatialSheetPowerTypes level2, SpatialSheetPowerTypes level3, SpatialSheetPowerTypes level4)
        {
            NodeCount = 0;
            Power = new SpatialLevelPowerIndexer<TNode>(this);

            SetLevels(level1, level2, level3, level4);
        }

        protected void SetLevels(SpatialSheetPowerTypes level1, SpatialSheetPowerTypes level2, SpatialSheetPowerTypes level3, SpatialSheetPowerTypes level4)
        {
            if (NodeCount > 0) return;

            Power[1] = level1;
            if (Power.Length > 2) Power[2] = level2;
            if (Power.Length > 3) Power[3] = level3;
            if (Power.Length > 4) Power[4] = level4;

            for (var i = 5; i < Power.Length; i++)
            {
                Power[i] = SpatialSheetPowerTypes.Low;
            }
            //Root spatial index sheet includes whole world
            _root = new SpatialSheet<TNode>(this, 1, 1, CoordinateRectangle.Empty);

            //Array to grab index stats for tuning
            NodeDimension = new int[Power.Length - 1];
        }

        protected void Insert(TNode value)
        {
            lock (this)
            {
                NodeCount++;
            }

            _root.SheetAction(value, SpatialSheet<TNode>.SheetActionType.Insert);
        }

        protected void Delete(TNode value)
        {
            lock (this)
            {
                NodeCount--;
            }

            _root.SheetAction(value, SpatialSheet<TNode>.SheetActionType.Delete);
        }

        public HashSet<ISpatialTreeNode> Query(CoordinateRectangle rectangle)
        {
            //for debug to see how many iterations used for a search
            var i = SpatialQueryIterator.Start();
            
            var res = new HashSet<ISpatialTreeNode>();
            _root.Query(res, rectangle, InterseptResult.None, i);
            
            //index turning
            System.Diagnostics.Trace.WriteLine(string.Format("{5} Level nodes {1} {2} {3} {4}, Query iterations - {0:d}",
                i.Value, NodeDimension[0], NodeDimension[1], NodeDimension[2], NodeDimension[3], typeof(TNode).Name));

            return res;
        }

        public HashSet<ISpatialTreeNode> Distance(Coordinate coordinate, double variance)
        {
            //for debug to see how many iterations used for a search
            var i = SpatialQueryIterator.Start();

            var res = new HashSet<ISpatialTreeNode>();
            _root.Distance(res, coordinate, variance, i);

            //index turning
            System.Diagnostics.Trace.WriteLine(string.Format("{5} Level nodes {1} {2} {3} {4}, Distance iterations - {0:d}",
                i.Value, NodeDimension[0], NodeDimension[1], NodeDimension[2], NodeDimension[3], typeof(TNode).Name));

            return res;
        }

        public void Clear()
        {
            NodeCount = 0;
            _root.Clear();
            
            NodeDimension = new int[Power.Length - 1];
        }
    }
}
