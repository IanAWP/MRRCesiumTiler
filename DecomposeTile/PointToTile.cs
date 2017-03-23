using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiler
{
    public class PointToTile
    {
     

       public struct LatLonCoordinate
        {
            public double Lat;
            public double Lon;
        };

        public struct TileCoordinate
        {
            public int X;
            public int Y;
        };

        public struct LatLonBounds
        {
            public LatLonCoordinate Min;
            public LatLonCoordinate Max;
        };



        public static TileCoordinate LatLonToTile(int zoom, double lat, double lon)
        {

            // Calculate the number of tiles across the map, n, using 2^zoom
            double nY = Math.Pow(2.0, zoom);
            double nX = Math.Pow(2.0, zoom + 1);

            // Multiply x and y by n. Round results down to give tilex and tiley.
            TileCoordinate coord = new TileCoordinate();
            coord.X = (int)(nX * ((lon + 180) / 360));
            coord.Y = (int)(nY * ((lat + 90)/180));
            return coord;
        }

        public static LatLonBounds TileBounds(int zoom, int xtile, int ytile) {
            double ny = Math.Pow(2.0, zoom);
            double nX = Math.Pow(2.0, zoom + 1);

            LatLonCoordinate coordMin = new LatLonCoordinate();
            coordMin.Lon = ((xtile / nX) * 360) - 180;
            coordMin.Lat = ((ytile / ny) * 180) - 90;
            LatLonCoordinate coordMax = new LatLonCoordinate();
            coordMax.Lon = coordMin.Lon+((1 / nX) * 360) ;
            coordMax.Lat = coordMin.Lat + ((1 / ny) * 180);
            var llb = new LatLonBounds();
            llb.Min = coordMin;
            llb.Max = coordMax;
            return llb;
        }


    }
}
