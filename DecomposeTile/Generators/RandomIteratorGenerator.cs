using MapInfo.RasterEngine.Common;
using MapInfo.RasterEngine.IO;
using System;
using Tiler;

namespace TileGen.Generators
{
    public class RandomIteratorGenerator:AbstractGenerator
    {

        public override bool CreateTile(int tx, int ty, int zoom, IRasterDataset dataset, byte[] tile)
        {

            var boundingBox = PointToTile.TileBounds(zoom, tx, ty);
            var iterator = dataset.GetRandomIterator(IteratorMode.Read);

            iterator.Begin();

            //using band 0 in this example.  Be careful with your
            //own data as MRR is multifield, multiband
            IRandomBand rband = iterator[0];

            //get the distance between samples
            var dx = (boundingBox.Max.Lon - boundingBox.Min.Lon) / 64;
            var dy = (boundingBox.Max.Lat - boundingBox.Min.Lat) / 64;
            int accum = 0;
            //64 samples with one row of overlap with the next tile.
            //Hence 65 * (1/64) tile size
            bool hasData = false;
            for (int lat = 0; lat < 65; lat++)
            {
                for (int lon = 0; lon < 65; lon++)
                {
                    //read evenly across the whole tile
                    var wX = boundingBox.Min.Lon + (lon * dx);
                    var wY = boundingBox.Max.Lat - (lat * dy);
                    int cX, cY;
                    double value = 0;
                    bool valid = false;

                    var inside = !GetCellPosition(wX, wY, dataset, out cX, out cY);
                    if (inside)
                    {
                        rband.GetCellValue((long)cX, (long)cY, out value, out valid);
                    }
                    else
                    {
                        valid = false;
                    }

                    if (valid)
                    {
                        hasData = true;
                        var sValue = Convert.ToUInt16((value + 1000) * 5);
                        var conv = BitConverter.GetBytes(sValue);
                        tile[accum] = conv[0];
                        tile[accum + 1] = conv[1];
                    }
                    //otherwise, sea level
                    else
                    {
                        var conv = BitConverter.GetBytes(5000);
                        tile[accum] = conv[0];
                        tile[accum + 1] = conv[1];
                    }
                    accum += 2;
                }
            }
            //don't forget to close the iterator
            iterator.End();
           
            return hasData;

        }
    }
}
