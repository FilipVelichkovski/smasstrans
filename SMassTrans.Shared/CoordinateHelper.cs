namespace SMassTrans.Shared;

public static class CoordinateHelper
{
    //https://github.com/microsoft/referencesource/blob/master/System.Device/GeoCoordinate.cs#L190-L239
    public static Double GetDistanceTo(double latitude, double longitude, double otherLatitude, double otherLongitude)
    {
        if(double.IsNaN(latitude)) throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude is NaN");
        if(double.IsNaN(longitude)) throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude is NaN");
        if(double.IsNaN(otherLatitude)) throw new ArgumentOutOfRangeException(nameof(otherLatitude), "OtherLatitude is NaN");
        if(double.IsNaN(otherLongitude)) throw new ArgumentOutOfRangeException(nameof(otherLongitude), "OtherLongitude is NaN");
        //  The Haversine formula according to Dr. Math.
        //  http://mathforum.org/library/drmath/view/51879.html
                
        //  dlon = lon2 - lon1
        //  dlat = lat2 - lat1
        //  a = (sin(dlat/2))^2 + cos(lat1) * cos(lat2) * (sin(dlon/2))^2
        //  c = 2 * atan2(sqrt(a), sqrt(1-a)) 
        //  d = R * c
                
        //  Where
        //    * dlon is the change in longitude
        //    * dlat is the change in latitude
        //    * c is the great circle distance in Radians.
        //    * R is the radius of a spherical Earth.
        //    * The locations of the two points in 
        //        spherical coordinates (longitude and 
        //        latitude) are lon1,lat1 and lon2, lat2.
      
        var dDistance = double.NaN;

        var dLat1 = latitude * (Math.PI / 180.0);
        var dLon1 = longitude * (Math.PI / 180.0);
        var dLat2 = otherLatitude * (Math.PI / 180.0);
        var dLon2 = otherLongitude * (Math.PI / 180.0);

        var dLon = dLon2 - dLon1;
        var dLat = dLat2 - dLat1;

        // Intermediate result a.
        var a = Math.Pow(Math.Sin(dLat / 2.0), 2.0) + 
                Math.Cos(dLat1) * Math.Cos(dLat2) * 
                Math.Pow(Math.Sin(dLon / 2.0), 2.0);

        // Intermediate result c (great circle distance in Radians).
        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        // Distance.
        const Double kEarthRadiusMs = 6376500;
        dDistance = kEarthRadiusMs * c;

        return dDistance;
    }
}