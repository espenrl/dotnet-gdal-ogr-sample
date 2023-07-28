/* This example will use GDAL to create a ESRI File Geodatabase (FileGDB)
 * 
 * Terminology
 * - layer: a dataset containing multiple features
 * - feature: object containing multiple data fields + geometry field
 * 
 * Notes
 * - layer is very much akin to a table
 * - feature is very much akin to a row in a table (data fields and geometry field being columns)
 * - a single ESRI File Geodatabase can have multiple layers
 * - a single layer has only one type of geometries (points, lines, polygons)
 * - a single layer has only one geometry field
 */

// Works on Windows, Linux and macOS (both x64 and ARM64) - please see https://github.com/MaxRev-Dev/gdal.netcore

// GDAL C# documentation: https://gdal.org/api/csharp.html

// GDAL C/C++ documentation: https://gdal.org/api/index.html (more detailed than the C# documentation - and the C# API is a wrapper over the C/C++ API)

// See available GDAL vector drivers at https://gdal.org/drivers/vector/index.html (also check which drivers supports creation)

using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

GdalBase.ConfigureAll(); // MaxRev.Gdal helper method to configure GDAL data catalogs and drivers

// exceptions as a means of error handling can be enabled/disabled (if disabled then use return codes for error handling)
Gdal.UseExceptions();
Ogr.UseExceptions();
Osr.UseExceptions();

// NOTE: use Gdal class for raster data, Ogr class for vector data, Osr class for spatial reference systems

var applicationPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
ArgumentException.ThrowIfNullOrEmpty(applicationPath);
var fileGdbPath = Path.Combine(applicationPath, "sample.gdb");

if (Directory.Exists(fileGdbPath))
{
    Directory.Delete(fileGdbPath, recursive: true);
}

var spatialReference = new SpatialReference(null);
spatialReference.ImportFromEPSG(4326);

using var driver = Ogr.GetDriverByName("OpenFileGDB"); // OpenFileGDB driver or FileGDB driver (legacy, ESRI SDK dependent)
using var dataSource = driver.CreateDataSource(
    utf8_path: fileGdbPath,
    options: Array.Empty<string>()); // available dataset options at https://gdal.org/drivers/vector/openfilegdb.html

using var layer = dataSource.CreateLayer(
    name: "Employee",
    srs: spatialReference,
    geom_type: wkbGeometryType.wkbPoint,
    options: Array.Empty<string>()); // available layer options at https://gdal.org/drivers/vector/openfilegdb.html

using var nameField = new FieldDefn("Name", FieldType.OFTString);
using var emailField = new FieldDefn("Email", FieldType.OFTString);

layer.CreateField(nameField, approx_ok: 0); // approx_ok is a boolean 0/1
layer.CreateField(emailField, approx_ok: 0);

using var featureDefinition = layer.GetLayerDefn();

/* wkbPoint: 2D
 * wkbPoint25D: 3D
 * wkbPointM: 2D + associated measure
 * wkbPointZM: 3D + associated measure
 *
 * NOTE: 25D and Z are used interchangeably
 * 2.5D hints about the fact that the geometry is 3D, and that the Z coordinate is not used for many calculations
 * - it's the difference between geographic surfaces and geographic volumes
 * - please see https://gis.stackexchange.com/a/99996
 */

var geometry = new Geometry(wkbGeometryType.wkbPoint); // 2D point
geometry.AssignSpatialReference(spatialReference);
geometry.SetPoint_2D(
    point: 0,
    x: 25.7837,
    y: 71.1710); // North Cape 71.1710° N, 25.7837° E

/* NOTE: it's also possible to create a geometry from WKT, WKB, GML
 * Geometry.CreateFromGML()
 * Geometry.CreateFromWkb()
 * Geometry.CreateFromWkt()
 */

using var feature = new Feature(featureDefinition);
feature.SetField("Name", "John");
feature.SetField("Email", "john@google.com");
feature.SetGeometry(geometry);

layer.CreateFeature(feature);

layer.SyncToDisk();
dataSource.SyncToDisk();
dataSource.FlushCache();

Console.WriteLine($"Sample database written to {fileGdbPath}");




// example how to transform geometry between different coordinate systems
static void TransformGeometryInPlaceSample(Geometry geom)
{
    // Transform geometry from WGS84 to British National Grid
    var from_crs = new SpatialReference(null);
    from_crs.SetWellKnownGeogCS("EPSG:4326");

    var to_crs = new SpatialReference(null);
    to_crs.ImportFromEPSG(27700);

    var ct = new CoordinateTransformation(from_crs, to_crs, new CoordinateTransformationOptions());
    // You can use the CoordinateTransformationOptions to set the operation or area of interet etc

    if (geom.Transform(ct) != 0)
        throw new NotSupportedException("projection failed");
}
