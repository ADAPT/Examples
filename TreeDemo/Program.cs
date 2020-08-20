using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using AgGateway.ADAPT.ApplicationDataModel.FieldBoundaries;
using AgGateway.ADAPT.ApplicationDataModel.Logistics;
using AgGateway.ADAPT.ApplicationDataModel.Products;
using AgGateway.ADAPT.ApplicationDataModel.Representations;
using AgGateway.ADAPT.PluginManager;
using AgGateway.ADAPT.Representation.RepresentationSystem;
using AgGateway.ADAPT.Representation.RepresentationSystem.ExtensionMethods;
using AgGateway.ADAPT.Representation.UnitSystem;
using NetTopologySuite.Geometries;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeDemo
{
    class Program
    {
        static void Main(string[] args)
        {
        // Load up the sample farm/field/cropzone information
            var treeData = (JArray)(JObject.Parse(File.ReadAllText("tree.json"))["Results"]);

        // Load up the field/cropzone boundaries
            var boundaryData = (JArray)(JObject.Parse(File.ReadAllText("boundaries.json"))["Results"]);

        // Initialize a Well-Known-Text (WKT) reader for handling the sample boundary data
            GeometryFactory geometryFactory = new GeometryFactory();
            NetTopologySuite.IO.WKTReader wktReader = new NetTopologySuite.IO.WKTReader(geometryFactory);
        
        // In this console app the ADMPlugin is included as a NuGet package so the ADMPlugin.dll is always
        // copied directly in to the executable directory. That's why we tell the PluginFactory to look there
        // for the ADMPlugin.
            string applicationPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

        // The PluginFactory looks at all the DLLs in the target directory to find any that implement the IPlugin interface.
            var pluginFactory = new PluginFactory(applicationPath);

        // We're only interested in the ADMPlugin here, so I address it directly instead of looking through all the
        // available plugins that the PluginFactory found.
            var admPlugin = pluginFactory.GetPlugin("ADMPlugin");

        // The ADMPlugin doesn't require any initialization parameters.
            admPlugin.Initialize();

        // The ApplicationDataModel is the root object in ADAPT
            ApplicationDataModel export = new ApplicationDataModel();

        // The Catalog object (inside the ApplicationDataModel) holds all the items you would expect to find in a "pick list".
        // Alternatively, you could think of it as the place you put everything used "by reference" in any of the Documents
        // you are trying to send.
            export.Catalog = new Catalog();

        // The Documents object (inside the ApplicationDataModel) holds all the Plans, Recommendations, WorkOrders, and
        // WorkRecords (and their component parts). We won't be using this in this example.
            export.Documents = new Documents();

        // Create a "crop year" TimeScope to tag objects with.
            TimeScope cropYear = new TimeScope();
            cropYear.Description = "2017";
            cropYear.DateContext = DateContextEnum.CropSeason;
            export.Catalog.TimeScopes.Add(cropYear);

        // Create the Grower object. The constructor will automatically create the Id property and assign the 
        // next available ReferenceId integer.
            Grower adaptGrower = new Grower();

        // Associate your internal, unique identifier to the Grower object by creating a UniqueId object
        // and adding it to the Grower object's CompoundIdentifier.
            UniqueId ourId = new UniqueId();
            ourId.Id = "7d2253f0-fce6-4740-b3c3-f9c8ab92bfaa";

        // Notice the available IdTypeEnum choices. Not everybody uses the same way of identifying things in their
        // system. As a result, we must support a number of identification schemes.
            ourId.IdType = IdTypeEnum.UUID;

        // Almost as important as the identifier is knowing who created it (or where it came from).
            ourId.Source = "www.agconnections.com";
            ourId.SourceType = IdSourceTypeEnum.URI;

        // Each CompoundIdentifier that is used in ADAPT can have multiple unique identifiers associated with it.
        // Consider the possibilites here, not only can your identifier for something be peristed but also the
        // identifiers that your trading partner assigns to the same object. PLEASE CONSIDER PERSISTING AND RETURNING 
        // IDENTIFIERS PASSED TO YOU IN THIS FASHION. This has the potential to result in a "frictionless" conversation 
        // once the initial mapping is done, buy this benefit will only emerge if we all are "good neighbors".
            adaptGrower.Id.UniqueIds.Add(ourId);
        
        // You may notice that many of the objects in ADAPT have a minimal number of properties. Don't panic if you
        // can't find a place to put all your data. It may be in an associated object or intended to be expressed
        // as a ContextItem.
            adaptGrower.Name = "Ponderosa Farms";
        // Add the Grower object to the Catalog.
            export.Catalog.Growers.Add(adaptGrower);

        // Pull the farm objects out of the sample JSON test data
            var farms = (from c in treeData where ((string)c["type"] == "farm") select c).ToList();
        // Iterate over each farm
            foreach (var farm in farms)
            {
            // Create the Farm object. The constructor will automatically create the Id property and assign the 
            // next available ReferenceId integer.
                Farm adaptFarm = new Farm();
                ourId = new UniqueId();
                ourId.Id = (string)farm["id"];
                ourId.IdType = IdTypeEnum.UUID;
                ourId.Source = "www.agconnections.com";
                ourId.SourceType = IdSourceTypeEnum.URI;
                adaptFarm.Id.UniqueIds.Add(ourId);
                adaptFarm.Description = (string)farm["text"];
            // Here we link this farm object to the grower. Note that this is the integer (ReferenceId) in the 
            // Grower's CompountIdentifier object.
                adaptFarm.GrowerId = adaptGrower.Id.ReferenceId;
            // Add the Farm object to the Catalog.
                export.Catalog.Farms.Add(adaptFarm);
            // Pull the field objects out of the sample JSON test data that are part of this iteration's farm
                var fields = (from c in treeData where (((string)c["type"] == "field") && ((string)c["parent"] == (string)farm["id"])) select c).ToList();
            // Iterate over each field
                foreach (var field in fields)
                {
                // Create the Field object. The constructor will automatically create the Id property and assign the 
                // next available ReferenceId integer.
                    Field adaptField = new Field();
                    ourId = new UniqueId();
                    ourId.Id = (string)field["id"];
                    ourId.IdType = IdTypeEnum.UUID;
                    ourId.Source = "www.agconnections.com";
                    ourId.SourceType = IdSourceTypeEnum.URI;
                    adaptField.Id.UniqueIds.Add(ourId);
                    adaptField.Description = (string)field["text"];
                // Here we link this field object to the farm. Note that this is the integer (ReferenceId) in the 
                // Farm's CompountIdentifier object.
                    adaptField.FarmId = adaptFarm.Id.ReferenceId;
                // Pull the boundary object out of the sample JSON test data (if it exists)
                    var fieldBoundary = (from c in boundaryData where (((string)c["FieldId"] == (string)field["id"]) && ((string)c["CropZoneId"] == null)) select c).FirstOrDefault();
                    if (fieldBoundary != null)
                    {
                    // This sample data has boundaries expressed as MultiPolygons in WKT so we need to transform that into the correlary ADAPT objects.
                    // Your data may use a different geometry (instead of MultiPolygon) to describe your boundaries so your code may differ at this point.
                        var boundary = wktReader.Read((string)fieldBoundary["MapData"]) as NetTopologySuite.Geometries.MultiPolygon;
                        AgGateway.ADAPT.ApplicationDataModel.Shapes.MultiPolygon adaptMultiPolygon = new AgGateway.ADAPT.ApplicationDataModel.Shapes.MultiPolygon();
                        adaptMultiPolygon.Polygons = new List<AgGateway.ADAPT.ApplicationDataModel.Shapes.Polygon>();
                        foreach (var geometry in boundary.Geometries)
                        {
                            var polygon = geometry as NetTopologySuite.Geometries.Polygon;
                            AgGateway.ADAPT.ApplicationDataModel.Shapes.Polygon adaptPolygon = new AgGateway.ADAPT.ApplicationDataModel.Shapes.Polygon();
                            adaptPolygon.ExteriorRing = new AgGateway.ADAPT.ApplicationDataModel.Shapes.LinearRing();
                            adaptPolygon.InteriorRings = new List<AgGateway.ADAPT.ApplicationDataModel.Shapes.LinearRing>();
                            foreach (var coordinate in polygon.ExteriorRing.Coordinates)
                            {
                                var adaptPoint = new AgGateway.ADAPT.ApplicationDataModel.Shapes.Point();
                                adaptPoint.X = coordinate.X;
                                adaptPoint.Y = coordinate.Y;
                                adaptPolygon.ExteriorRing.Points.Add(adaptPoint);
                            }
                            foreach (var ring in polygon.InteriorRings)
                            {
                                var adaptRing = new AgGateway.ADAPT.ApplicationDataModel.Shapes.LinearRing();
                                adaptRing.Points = new List<AgGateway.ADAPT.ApplicationDataModel.Shapes.Point>();
                                foreach (var coordinate in ring.Coordinates)
                                {
                                    var adaptPoint = new AgGateway.ADAPT.ApplicationDataModel.Shapes.Point();
                                    adaptPoint.X = coordinate.X;
                                    adaptPoint.Y = coordinate.Y;
                                    adaptRing.Points.Add(adaptPoint);
                                }
                                adaptPolygon.InteriorRings.Add(adaptRing);
                            }
                            adaptMultiPolygon.Polygons.Add(adaptPolygon);
                        }
                    // Unlike the CropZone object (which holds its geomertry internally) a Field's boundary is held in a separate FieldBoundary object.
                    // Create the FieldBoundary object. The constructor will automatically create the Id property and assign the 
                    // next available ReferenceId integer.
                        FieldBoundary adaptBoundary = new FieldBoundary();
                    // The FieldBoundary.SpatialData property is an ADAPT Shape object (which is an abastract class). What you actually attach here
                    // is one of the child classes of Shape (Polygon, MultiPolygon, etc.).  
                        adaptBoundary.SpatialData = adaptMultiPolygon;
                    // Here we link this field boundary object to the field. Note that this is the integer (ReferenceId) in the 
                    // Field's CompountIdentifier object.
                        adaptBoundary.FieldId = adaptField.Id.ReferenceId;
                    // Add the FieldBoundary object to the Catalog.
                        export.Catalog.FieldBoundaries.Add(adaptBoundary);
                    // It is possible for a given Field to have multiple FieldBounday objects associated with it, but we need to be able
                    // to indicate which one should be used by "default". 
                        adaptField.ActiveBoundaryId = adaptBoundary.Id.ReferenceId;
                    }
                // Add the Field object to the Catalog. *Note: We are adding this to the Catalog here so that we don't have to go
                // back and fetch the object to set the ActiveBoundaryId property. Not required, just convenient.
                    export.Catalog.Fields.Add(adaptField);

                // We're defining a CropZone as a spatial area within a field grown to a crop during a specific window of time. 
                // This is fundamentally different from the concept of a management zone (that might vary by plant population or soil type).
                // Pull the cropzone objects out of the sample JSON test data that are part of this iteration's field
                    var cropzones = (from c in treeData where (((string)c["type"] == "cropzone") && ((string)c["parent"] == (string)field["id"])) select c).ToList();
                // Iterate over each cropzone
                    foreach (var cropzone in cropzones)
                    {
                    // It's entirely possible that we have already added this Crop to the Catalog during a previous iteration. We need to check
                    // the Crop list in Catalog first and reuse that object if it exists.
                        Crop adaptCrop = null;
                        var crops = export.Catalog.Crops.Where(x => (x.Name == (string)cropzone["li_attr"]["CropName"])).ToList();
                        if (crops.Count > 0)
                            adaptCrop = crops[0];
                        else
                        {
                        // Create the Crop object. The constructor will automatically create the Id property and assign the 
                        // next available ReferenceId integer.
                            adaptCrop = new Crop();
                            ourId = new UniqueId();
                            ourId.Id = (string)cropzone["li_attr"]["CropId"];
                            ourId.IdType = IdTypeEnum.UUID;
                            ourId.Source = "www.agconnections.com";
                            ourId.SourceType = IdSourceTypeEnum.URI;
                            adaptCrop.Id.UniqueIds.Add(ourId);
                            adaptCrop.Name = (string)cropzone["li_attr"]["CropName"];

                        // Add EPPO code as ContextItem at some point in the future

                        // Add the Crop object to the Catalog.
                            export.Catalog.Crops.Add(adaptCrop);
                        }
                    // Create the CropZone object. The constructor will automatically create the Id property and assign the 
                    // next available ReferenceId integer.
                        CropZone adaptCropZone = new CropZone();
                        ourId = new UniqueId();
                        ourId.Id = (string)cropzone["id"];
                        ourId.IdType = IdTypeEnum.UUID;
                        ourId.Source = "www.agconnections.com";
                        ourId.SourceType = IdSourceTypeEnum.URI;
                        adaptCropZone.Id.UniqueIds.Add(ourId);
                        adaptCropZone.Description = (string)cropzone["text"];
                    // Here we link this cropzone object to the field. Note that this is the integer (ReferenceId) in the 
                    // Field's CompountIdentifier object.
                        adaptCropZone.FieldId = adaptField.Id.ReferenceId;
                    // Here we link this cropzone object to the crop. Note that this is the integer (ReferenceId) in the 
                    // Crop's CompountIdentifier object.
                        adaptCropZone.CropId = adaptCrop.Id.ReferenceId;
                    // Here we link this cropzone object to the crop year TimeScope. Note that the TimeScope is used BY VALUE
                    // instead of BY REFERENCE (like the field and crop above).
                        adaptCropZone.TimeScopes.Add(cropYear);
                        string areaString = (string)cropzone["li_attr"]["AreaValue"];
                        if (!string.IsNullOrEmpty(areaString))
                        {
                            double area = Convert.ToDouble(areaString);
                            adaptCropZone.Area = new NumericRepresentationValue(RepresentationInstanceList.vrReportedFieldArea.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("ac1"), area));
                        }
                    // As mentioned before, the CropZone (unlike Field) holds its boundary internally. Also unlike field, a CropZone is only expected
                    // to have a single boundary due to its scope in crop & time.
                        var czBoundary = (from c in boundaryData where ((string)c["CropZoneId"] == (string)cropzone["id"]) select c).FirstOrDefault();
                        if (czBoundary != null)
                        {
                            var boundary = wktReader.Read((string)czBoundary["MapData"]) as NetTopologySuite.Geometries.MultiPolygon;
                            AgGateway.ADAPT.ApplicationDataModel.Shapes.MultiPolygon adaptMultiPolygon = new AgGateway.ADAPT.ApplicationDataModel.Shapes.MultiPolygon();
                            adaptMultiPolygon.Polygons = new List<AgGateway.ADAPT.ApplicationDataModel.Shapes.Polygon>();
                            foreach (var geometry in boundary.Geometries)
                            {
                                var polygon = geometry as NetTopologySuite.Geometries.Polygon;
                                AgGateway.ADAPT.ApplicationDataModel.Shapes.Polygon adaptPolygon = new AgGateway.ADAPT.ApplicationDataModel.Shapes.Polygon();
                                adaptPolygon.ExteriorRing = new AgGateway.ADAPT.ApplicationDataModel.Shapes.LinearRing();
                                adaptPolygon.InteriorRings = new List<AgGateway.ADAPT.ApplicationDataModel.Shapes.LinearRing>();
                                foreach (var coordinate in polygon.ExteriorRing.Coordinates)
                                {
                                    var adaptPoint = new AgGateway.ADAPT.ApplicationDataModel.Shapes.Point();
                                    adaptPoint.X = coordinate.X;
                                    adaptPoint.Y = coordinate.Y;
                                    adaptPolygon.ExteriorRing.Points.Add(adaptPoint);
                                }
                                foreach (var ring in polygon.InteriorRings)
                                {
                                    var adaptRing = new AgGateway.ADAPT.ApplicationDataModel.Shapes.LinearRing();
                                    adaptRing.Points = new List<AgGateway.ADAPT.ApplicationDataModel.Shapes.Point>();
                                    foreach (var coordinate in ring.Coordinates)
                                    {
                                        var adaptPoint = new AgGateway.ADAPT.ApplicationDataModel.Shapes.Point();
                                        adaptPoint.X = coordinate.X;
                                        adaptPoint.Y = coordinate.Y;
                                        adaptRing.Points.Add(adaptPoint);
                                    }
                                    adaptPolygon.InteriorRings.Add(adaptRing);
                                }
                                adaptMultiPolygon.Polygons.Add(adaptPolygon);
                            }
                            adaptCropZone.BoundingRegion = adaptMultiPolygon;
                        }
                    // Add the CropZone object to the Catalog.
                        export.Catalog.CropZones.Add(adaptCropZone);
                    }

                }
            }
        // At this point we have added all the Grower/Farm/Field objects to the Catalog and are ready to export.
        // Create an output path
            string outputPath = applicationPath + @"\Output";
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
        // Export to a local directory using the ADMPlugin
            admPlugin.Export(export, outputPath);
        
        // The ADMPlugin creates an "adm" subdirectory in the indicated local directory that contains the following items:
        //      An additional "documents" subdirectory that contains the protobuf-encoded document files.
        //      An AdmVersion.info file that contains version information.
        //      A ProprietaryValues.adm file 
        //      A Catalog.adm file that contains the zipped JSON serialization of the ApplicationDataModel.Catalog object.

        // We've added logic here to zip that "adm" subdirectory into a single file, in case you want to email it to someone.
            string zipPath = applicationPath + @"\Zip";
            if (Directory.Exists(zipPath))
                Directory.Delete(zipPath, true);
            if (!Directory.Exists(zipPath))
                Directory.CreateDirectory(zipPath);
        // Delete the file if it already exists
            string zipFile = zipPath + @"\tree.zip";
            if (File.Exists(zipFile))
                File.Delete(zipFile);
            ZipFile.CreateFromDirectory(outputPath, zipFile);
        
        // This is logic to import the same data from the "adm" subdirectory we just created so you can compare it
        // in the debugger if you want.
            var pluginFactory2 = new PluginFactory(applicationPath);
            var admPlugin2 = pluginFactory.GetPlugin("ADMPlugin");
            admPlugin2.Initialize();
        // Note that when a plugin imports, the returned object is a list of ApplicationDataModel objects.
            var imports = admPlugin2.Import(outputPath);

        }
    }
}
