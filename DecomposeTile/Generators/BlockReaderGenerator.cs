using MapInfo.RasterEngine.Common;
using MapInfo.RasterEngine.IO;
using System;
using Tiler;

namespace TileGen.Generators
{
    public class BlockReaderGenerator:AbstractGenerator
    {
        public override bool CreateTile(int tx, int ty, int zoom, IRasterDataset dataset, byte[] tile)
        {
            int accum = 0;
            var boundingBox = PointToTile.TileBounds(zoom, tx, ty);
            var seaLevel = BitConverter.GetBytes(5000);
            var iterator = dataset.GetBlockIterator(0, IteratorMode.Read);
            iterator.Begin();

            //get the distance between samples
            var dx = (boundingBox.Max.Lon - boundingBox.Min.Lon) / 64;
            var dy = (boundingBox.Max.Lat - boundingBox.Min.Lat) / 64;

            var cellSize = dataset.Info.FieldInfo[0].CellSizeX.Decimal;
            var rat = dx / cellSize;
            int resolution = 0;

            //when sample size is less than cell size use an underview
            while (rat < 1.0)
            {
                resolution--;
                cellSize = cellSize / 2.0;
                rat = rat * 2.0;
            }
            //when sample size is greater than cell size use an overview
            while (rat > 2.0)
            {
                resolution++;
                cellSize = cellSize * 2.0;
                rat = rat / 2.0;
            }
            //We've added an optional resolution parameter to our cell position calculation
            int westmost, southmost;
            GetCellPosition(boundingBox.Min.Lon, boundingBox.Min.Lat, dataset, out westmost, out southmost, resolution);


            //64 samples with one row of overlap with the next tile.
            //Hence desired width is 65 tile samples, converted into cell units
            var width = (int)Math.Ceiling((dx * 65) / cellSize);
            double cellToSampleRatio = width / 65.0;
            double[] actualData = null;
            bool[] actualValid = null;

            try
            {
                GetGridBlock(westmost, southmost, width, width, resolution, dataset, out actualData, out actualValid);
            }
            catch 
            {

                Console.WriteLine();
            }


            bool hasData = false;

            //cesium expects the northwest position first, but the raster block
            //gives the southwest as the first element so remember to transform
            for (int lat = 64; lat >= 0; lat--)
            {
                //     var Y = dy * lat;
                var Y = (int)((cellToSampleRatio * lat) + 0.5);
                for (int lon = 0; lon < 65; lon++)
                {
                    //transform lat and lon into block units

                    var X = (int)((cellToSampleRatio * lon) + 0.5);
                    // var X = (int)((dx * lon)+0.5);
                    var place = (width * Y) + X;

                    bool isValid = actualValid[place];

                    if (isValid)
                    {
                        var value = actualData[place];
                        hasData = true;
                        var sValue = Convert.ToUInt16((value + 1000) * 5);
                        var conv = BitConverter.GetBytes(sValue);
                        tile[accum] = conv[0];
                        tile[accum + 1] = conv[1];
                    }
                    else
                    {

                        tile[accum] = seaLevel[0];
                        tile[accum + 1] = seaLevel[1];
                    }
                    accum += 2;
                }
            }
            iterator.End();
            return hasData;

        }

        private void GetGridBlock(int col, int row, int width, int height, int res, IRasterDataset dataset, out double[] odata, out bool[] ovalid)
        {
            var iterator = dataset.GetBlockIterator(res, IteratorMode.Read);
            iterator.Begin();
            var bnd = iterator[0];
            byte[] data;
            byte[] valid;
            bnd.GetBlock(col, row, (uint)width, (uint)height, RasterBandDataType.RealDouble, out data, out valid);
            int numCells = data.Length / sizeof(double);
            double[] actualData = new double[numCells];
            bool[] actualValid = new bool[numCells];
            // iterate over each cell in block
            for (int cellIndex = 0; cellIndex < numCells; cellIndex++)
            {
                // convert to required types
                actualData[cellIndex] = BitConverter.ToDouble(data, cellIndex * sizeof(double));
                actualValid[cellIndex] = BitConverter.ToBoolean(valid, cellIndex);
            }
            odata = actualData;
            ovalid = actualValid;
            iterator.End();
        }
    }
}
