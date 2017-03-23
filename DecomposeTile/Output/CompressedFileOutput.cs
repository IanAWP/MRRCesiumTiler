using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileGen
{
    public class CompressedFileOutput:IOutput
    {
        private string OutDir { get; set; }
        public CompressedFileOutput(string outputFolder) {
            OutDir = outputFolder;
        }
       public void WriteTile(int zoom, int tX, int tY, byte[] terrainTile)
        {
            var s = ($"{zoom}/{tX}/{tY}.terrain");
            var terrainPath = Path.Combine(OutDir, s);
            Directory.CreateDirectory(Path.GetDirectoryName(terrainPath));
            CompressBytes(terrainPath, terrainTile);
        }

        public static void CompressBytes(string fileName, byte[] b)
        {
            // Use GZipStream to write compressed bytes to target file.
            using (FileStream f2 = new FileStream(fileName, FileMode.Create))
            using (GZipStream gz = new GZipStream(f2, CompressionMode.Compress, false))
            {
                gz.Write(b, 0, b.Length);
            }
        }
    }
}
