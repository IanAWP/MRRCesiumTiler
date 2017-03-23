using MapInfo.RasterEngine.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tiler;

namespace TileGen.Generators
{
    public abstract class AbstractGenerator : IGenerator
    {

        public void GetJobSize(IRasterDataset dataset, int maxDesiredLevel, out int maxActualLevel, out int numTiles)
        {
            double xMin, yMin, xMax, yMax;
            maxActualLevel = 0;

            RasterInfo r = dataset.Info;
            xMin = r.FieldInfo[0].Envelope.XMin;
            xMax = r.FieldInfo[0].Envelope.XMax;
            yMin = r.FieldInfo[0].Envelope.YMin;
            yMax = r.FieldInfo[0].Envelope.YMax;
            var sourceSample = dataset.Info.FieldInfo[0].CellSizeX.Decimal;

            int total = 0;
            bool atMax = false;
            int zoom = 0;
            while (!atMax)
            {
                var southWestTile = PointToTile.LatLonToTile(zoom, yMin, xMin);
                var northEastTile = PointToTile.LatLonToTile(zoom, yMax, xMax);
                var dx = northEastTile.X - southWestTile.X;
                var dy = northEastTile.Y - southWestTile.Y;

                //copy paste quick+ dirty code to determine whether we are creating this zoom level
                var boundingBox = PointToTile.TileBounds(zoom, northEastTile.X, northEastTile.Y);
                var tileSample = (boundingBox.Max.Lon - boundingBox.Min.Lon) / 64;

                if (tileSample < (sourceSample / 2.0) || zoom > maxDesiredLevel)
                {
                    atMax = true;
                }
                else
                {
                    maxActualLevel = zoom;
                    zoom++;
                    total += ((dx + 1) * (dy + 1));
                }

            }

            numTiles = total;
        }

        protected byte GetChildQuads(int zoom, int tX, int tY, Dictionary<string, string> existingTiles)
        {
            byte childQuadSwitch = 0;
            var ct = GetChildTiles(tX, tY, zoom);
            if (existingTiles.ContainsKey(ct[0])) childQuadSwitch += 1;
            if (existingTiles.ContainsKey(ct[1])) childQuadSwitch += 2;
            if (existingTiles.ContainsKey(ct[2])) childQuadSwitch += 4;
            if (existingTiles.ContainsKey(ct[3])) childQuadSwitch += 8;
            // b = 15;

            return childQuadSwitch;
        }

        protected List<string> GetChildTiles(int tx, int ty, int zoom)
        {
            var boundingBox = PointToTile.TileBounds(zoom, tx, ty);
            var s = new List<string>();
            var v = PointToTile.LatLonToTile(zoom + 1, boundingBox.Min.Lat, boundingBox.Min.Lon);
            var x = PointToTile.LatLonToTile(zoom + 1, boundingBox.Max.Lat, boundingBox.Max.Lon);
            s.Add($"{zoom + 1}/{v.X + 0}/{v.Y + 0}");
            s.Add($"{zoom + 1}/{v.X + 1}/{v.Y + 0}");
            s.Add($"{zoom + 1}/{v.X + 0}/{v.Y + 1}");
            s.Add($"{zoom + 1}/{v.X + 1}/{v.Y + 1}");

            return s;
        }

        /// <summary>
        /// Get zoom dependent position of WGS84 coords in grid cell coordinates.  May be negative if outside the grid.
        /// </summary>
        /// <param name="dWorldX"></param>
        /// <param name="dWorldY"></param>
        /// <param name="dataset"></param>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        protected bool GetCellPosition(double dWorldX, double dWorldY, IRasterDataset dataset, out int nX, out int nY, int resolution = 0)
        {

            var fieldInfo = dataset.Info.FieldInfo[0];
            ulong nColumns = fieldInfo.Width;
            ulong nRows = fieldInfo.Height;
            double dCellSizeX = fieldInfo.CellSizeX.Decimal;
            double dCellSizeY = fieldInfo.CellSizeY.Decimal;
            double dOriginX = fieldInfo.OriginX.Decimal;
            double dOriginY = fieldInfo.OriginY.Decimal;


            double multiplyer = Math.Pow(2, resolution);
            dCellSizeX = dCellSizeX * multiplyer;
            dCellSizeY = dCellSizeY * multiplyer;
            // if we are outside the grid border then get virtual coordinates anyway
            bool bExternal = false;
            bExternal |= (dWorldX < dOriginX);
            bExternal |= (dWorldY < dOriginY);
            bExternal |= (dWorldX >= dOriginX + (double)(nColumns * dCellSizeX));
            bExternal |= (dWorldY >= dOriginY + (double)(nRows * dCellSizeY));

            nX = (int)Math.Floor((dWorldX - dOriginX) / dCellSizeX);
            nY = (int)Math.Floor((dWorldY - dOriginY) / dCellSizeY);
            return bExternal;


        }
        abstract public bool CreateTile(int tx, int ty, int zoom, IRasterDataset dataset, byte[] tile);

        public void CreateAllTiles(IRasterDataset dataset, int maxZoom, IOutput output, System.IProgress<int> progress = null)
        {
            double xMin, yMin, xMax, yMax;
            var bytes = 65 * 65 * 2;
            byte zero = 0;
            int count = 0;
            RasterInfo r = dataset.Info;
            xMin = r.FieldInfo[0].Envelope.XMin;
            xMax = r.FieldInfo[0].Envelope.XMax;
            yMin = r.FieldInfo[0].Envelope.YMin;
            yMax = r.FieldInfo[0].Envelope.YMax;


            Dictionary<string, string> existingTiles = new Dictionary<string, string>();

            for (int zoom = maxZoom; zoom >= 0; zoom--)
            {
                //only generate tiles within the extents of the data
                var southWestTile = PointToTile.LatLonToTile(zoom, yMin, xMin);
                var northEastTile = PointToTile.LatLonToTile(zoom, yMax, xMax);
                var dx = northEastTile.X - southWestTile.X;
                var dy = northEastTile.Y - southWestTile.Y;

                var boundingBox = PointToTile.TileBounds(zoom, northEastTile.X, northEastTile.Y);
                var tileSample = (boundingBox.Max.Lon - boundingBox.Min.Lon) / 64;
                var sourceSample = dataset.Info.FieldInfo[0].CellSizeX.Decimal;


                ThreadLocal<byte[]> terrainTileLocal = new ThreadLocal<byte[]>(() => new byte[(bytes + 2)]);
                Console.WriteLine($"{(dx+1)*dy}");
                Parallel.For(0, dx + 1, (easterlyOffset) =>
                {
                    
                    Parallel.For(0, dy+1, (northerlyOffset) =>
                       {

                           int tX = southWestTile.X + easterlyOffset;
                           int tY = southWestTile.Y + northerlyOffset;
                           var terrainTile = terrainTileLocal.Value;
                           byte childQuadSwitch = zero;
                           if (zoom != maxZoom) { childQuadSwitch = GetChildQuads(zoom, tX, tY, existingTiles); }
                        //always create tiles with child tiles
                        bool create = childQuadSwitch > 0 || (zoom == maxZoom);
                           if (create)
                           {
                               bool created = false;

                               created = CreateTile(tX, tY, zoom, dataset, terrainTile);


                               if (zoom == maxZoom)
                               {
                                   create = created;
                               }
                           }


                           if (create)
                           {


                               terrainTile[bytes] = childQuadSwitch;
                               terrainTile[bytes + 1] = 0;//We'll say it's all land for now

                            output.WriteTile(zoom, tX, tY, terrainTile);
                               existingTiles.Add($"{zoom}/{tX}/{tY}", "");
                           }
                           progress?.Report(++count);

                       });

                });








            }
        }

    }
}
