using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileGen
{
    public interface IOutput
    {
        void WriteTile(int zoom, int tX, int tY, byte[] terrainTile);
    }
}
