using MapInfo.RasterEngine.IO;
using System;
using System.Collections.Generic;


namespace TileGen.Generators
{
    public interface IGenerator
    {

        void GetJobSize(IRasterDataset dataset, int maxDesiredLevel, out int maxActualLevel, out int numTiles);
        bool CreateTile(int tx, int ty, int zoom, IRasterDataset dataset, byte[] tile);
        void CreateAllTiles( IRasterDataset dataset, int maxZoom, IOutput output, System.IProgress<int> progress = null);
    }
}
