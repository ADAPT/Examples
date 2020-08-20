using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Logistics;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using AgGateway.ADAPT.ApplicationDataModel.Products;
using AgGateway.ADAPT.PluginManager;

/// <summary>
/// USE CASE:
///   This project was created to demonstrate how a service provider might discover the farms, fields and areas
///   within a field that his grower customer wanted him to address. 
///   
///   Presumably, the service provider is not interested in every farm, field, crop, or season within the data
///   provided, so the demo gives you the opportunity to filter based on the specific data contained within the model.
/// 
///   A sample set of ApplicationDataModel is supplied within the project as a zipped file.  An initial question in
///   the demo asks if you want to use that data.  If yes, then the files in the Source Path (from the first command 
///   line argument) will be deleted and the sample data model unzipped in that folder.  Alternatively, you could 
///   specify that the Source Path points to an existing ApplicationDataModel location and not use the sample data.
///   
///   Launch the application from a command prompt as:
///      ActiveCropZones [source path] 
///         where [source path] points to the folder where output either exist or sample data to be unzipped in.
/// 
/// BUILDING A PROJECT USING THE ADMPlugin:
///   This code references the ADMPlugin available as a NuGet package.
///   That NuGet package in turn adds references to: 
///        AgGateway.ADAPT.ApplicationDataModel
///        AgGateway.ADAPT.PluginManager
///        AgGateway.ADAPT.Representation
///        Newtonsoft.Json
///        protobuf-net
///      
/// NOTE:
///   Care must be taken to manually update the protobuf-net package to version 2.1 once the 
///   original package is installed with the ADMPlugin package.  An earlier version is installed
///   which will cause Runtime errors.
/// 
/// NOTE:
///   Two resources are added by the ADNPlugin package:
///        RepresentationSystem.xml
///        UnitSystem.xml
///   Their properties should be altered to "Copy Always" to the output directory.   
/// </summary>
/// 
namespace ActiveCropZones
{
   class Program
   {
      /// <summary>
      /// Generates a list of the Growers and Farms within a single ADAPT.ApplicationDataModel previously exported
      /// Within each grower and farm a list of Field, Crop, Season and area are also displayed.
      /// </summary>
      /// <param name="args"> The first command line argument needs to identify the path to the root folder
      /// of the exported ApplicationDataModel</param>
      static void Main( string[] args )
      {
         try
         {
            GetCommandLineParameters( args );
            AdmPlugin = InitializeAdaptPlugin();
            GetUserResponses();
            ListActiveCropZones();
         }
         catch (Exception exp)
         {
            Console.WriteLine( exp.Message );
            Console.ReadKey();
         }
      }
      /// <summary>
      /// The initialized AdmPlugin
      /// </summary>
      private static IPlugin AdmPlugin{get; set;}
      /// <summary>
      /// The path to the root folder where the ApplicationDataModel exists.
      /// </summary>
      private static string SourcePath { get; set; }
      /// <summary>
      /// The instaniated ApplicationDataModel
      /// </summary>
      private static ApplicationDataModel DataModel { get; set; }
      /// <summary>
      /// A flattened collection of information from the data model used to supply data to this demo. This tree is
      /// built once the ApplicationDataModel has been loaded.
      /// </summary>
      private static GrowerTree GrowerTree { get; set; }
      /// <summary>
      /// Linit the GrowIds output to this value unless it is zero.
      /// </summary>
      private static int UseGrowerId { get; set; } = 0;
      /// <summary>
      /// Linit the FarmIds output to this value unless it is zero.
      /// </summary>
      private static int UseFarmId { get; set; } = 0;
      /// <summary>
      /// Linit the FieldIds output to this value unless it is zero.
      /// </summary>
      private static int UseFieldId { get; set; } = 0;
      /// <summary>
      /// Linit the CropIds output to this value unless it is zero.
      /// </summary>
      private static int UseCropId { get; set; } = 0;
      /// <summary>
      /// Linit the CropSeasons output to this value unless it is empty.
      /// </summary>
      private static string UseCropSeason { get; set; } = string.Empty;

      /// <summary>
      /// This demo uses the ADMPlugin to manage the contents of the ADAPT Application Data Model.  This method will 
      /// initialize the plugin.
      /// </summary>
      private static IPlugin InitializeAdaptPlugin()
      {
         IPlugin iPlugin = null;
         try
         {
            var appPath = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location );
            var pluginFactory = new PluginFactory( appPath );
            iPlugin = pluginFactory.GetPlugin( "ADMPlugin" );
            iPlugin.Initialize();
         }
         catch (Exception exp)
         {
            throw exp;
         }
         return iPlugin;
      }

      /// <summary>
      /// Retrieves the path to the root folder of the ApplicationDataModel. 
      /// </summary>
      /// <param name="args">An array of strings passed to this application the first of which is expected
      /// to be the path to the root folder of the ApplicationDataModel.</param>
      private static void GetCommandLineParameters( string[] args )
      {
         if (args.Length > 0 && !string.IsNullOrEmpty( args[0] ))
         {
            SourcePath = args[0];
            if (!Directory.Exists( SourcePath ))
            {
               Console.WriteLine( $"The path {SourcePath} does not exist.  Would you like to create it? (Y/N)" );
               var keyChar = Console.ReadKey().KeyChar;
               if (keyChar == 'Y' || keyChar == 'y')
               {
                  Directory.CreateDirectory( SourcePath );
               }
               Console.WriteLine();
               Console.WriteLine();
               if (!Directory.Exists( SourcePath ))
                  throw new Exception( $"The folder {SourcePath} must exist to continue." );
            }
         }
         else
            throw new ArgumentException( "The first argument must contain the path to the adm model folder." );
      }

      /// <summary>
      /// Ask the user intial questions to determine which data to use and how they would like it limited.
      /// </summary>
      private static void GetUserResponses()
      {
         Console.WriteLine( "Would you like to use the supplied sample data? (Y/N)" );
         var keyChar = Console.ReadKey().KeyChar;
         Console.WriteLine();
         Console.WriteLine();
         if (keyChar == 'Y' || keyChar == 'y')
         {
            UnzipSampleData();
         }
         DataModel = LoadAdaptDataModel();

         UseGrowerId = Convert.ToInt32( ChooseLimitingId( "Grower" ) );
         UseFarmId = Convert.ToInt32( ChooseLimitingId( "Farm" ) );
         UseFieldId = Convert.ToInt32( ChooseLimitingId( "Field" ) );
         UseCropId = Convert.ToInt32( ChooseLimitingId( "Crop" ) );
         UseCropSeason = ChooseLimitingId( "CropSeason" );
      }
      /// <summary>
      /// Ask if the user is interested in limiting the output to this item and if so, display
      /// the distinct possibilities and ask him to choose.
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      private static string ChooseLimitingId( string item )
      {
         string result = (item == "CropSeason")? string.Empty: "0";
         var list = new List<Tuple<int, int?, string>>();
         int count = 0;

         Console.WriteLine( $"Would you like to limit the output by a specific {item}? (Y/N)" );
         var keyChar = Console.ReadKey().KeyChar;
         Console.WriteLine();
         Console.WriteLine();
         if (keyChar != 'Y' && keyChar != 'y')
            return result;

         switch (item)
         {
            case "Grower":
               GrowerTree.Select( g => new { Id = g.GrowerId, Name = g.GrowerName } )
                         .Distinct()
                         .ToList()
                         .ForEach( d => { list.Add( new Tuple<int, int?, string>( ++count, d.Id, d.Name ) ); } );
               break;
            case "Farm":
               GrowerTree.Select( g => new { Id = g.FarmId, Name = g.FarmName } )
                         .Distinct()
                         .ToList()
                         .ForEach( d => { list.Add( new Tuple<int, int?, string>( ++count, d.Id, d.Name ) ); } );
               break;
            case "Field":
               GrowerTree.Select( g => new { Id = g.FieldId, Name = g.FieldName } )
                         .Distinct()
                         .ToList()
                         .ForEach( d => { list.Add( new Tuple<int, int?, string>( ++count, d.Id, d.Name ) ); } );
               break;
            case "Crop":
               GrowerTree.Select( g => new { Id = g.CropId, Name = g.CropName } )
                         .Distinct()
                         .ToList()
                         .ForEach( d => { list.Add( new Tuple<int, int?, string>( ++count, d.Id, d.Name ) ); } );
               break;
            case "CropSeason":
               GrowerTree.Select( g => new { Id = 0, Name = g.CropSeason.Description } )
                         .Distinct()
                         .ToList()
                         .ForEach( d => { list.Add( new Tuple<int, int?, string>( ++count, d.Id, d.Name ) ); } );
               break;
         }
         foreach (var l in list)
            Console.WriteLine( $"{l.Item1}.\t{l.Item3}" );
         Console.WriteLine( $"Select the number of the {item} to limit the output." );
         int line = Convert.ToInt32( Console.ReadLine() );
         Console.WriteLine();
         Console.WriteLine();

         if (item != "CropSeason")
         {
            result = list.Where( l => l.Item1 == line )
                        .Select( l => l.Item2 )
                        .DefaultIfEmpty( 0 )
                        .FirstOrDefault()
                        .ToString();
         }
         else
         {
            result = list.Where( l => l.Item1 == line )
                        .Select( l => l.Item3 )
                        .DefaultIfEmpty( string.Empty )
                        .FirstOrDefault()
                        .ToString();
         }
         return result;
      }
      /// <summary>
      /// The user has indicated that they would like to use the provided sample data.  Clean the SourcePath
      /// folder of any files and folders and then unzip the provided sample to that location.
      /// </summary>
      private static void UnzipSampleData()
      {
         var dirInfo = new DirectoryInfo( SourcePath );
         foreach( FileInfo file in dirInfo.GetFiles())
            file.Delete();
         foreach (DirectoryInfo dir in dirInfo.GetDirectories())
            dir.Delete(true);

         var appPath = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location );
         var source = Path.Combine( appPath, @"Resources\TreeDemoADM.zip" );
         ZipFile.ExtractToDirectory( source, SourcePath );
         

      }

      /// <summary>
      /// Retrieve the first ApplicationDataModel from the list of discovered data models at the source location.
      /// Set the DataModel property with this value. If successful, build the GrowerTree collection from the 
      /// information in the DataModel.
      /// </summary>
      /// <returns></returns>
      private static ApplicationDataModel LoadAdaptDataModel()
      {
         var models = AdmPlugin.Import( SourcePath );
         if (models.Count > 0)
         {
            DataModel = models[0];
            GrowerTree = new GrowerTree( DataModel );
            GrowerTree.BuildFromCropZones();
         }
         else
            throw new Exception( $"There were no ADAPT Application Data Models found at {SourcePath}." );

         return DataModel;
      }
      /// <summary>
      /// Displays a list of Growers and Farms within the DataModel.
      /// For each Grower and Farm a list of Fields, Crops, Seasons and Area within the field is also listed.
      /// </summary>
      private static void ListActiveCropZones()
      {
         var list = GrowerTree.Select(g => g);
         if (UseGrowerId != 0)
            list = list.Where( g => g.GrowerId == UseGrowerId );
         if (UseFarmId != 0)
            list = list.Where( g => g.FarmId == UseFarmId );
         if (UseFieldId != 0)
            list = list.Where( g => g.FieldId == UseFieldId );
         if (UseCropId != 0)
            list = list.Where( g => g.CropId == UseCropId );
         if( !string.IsNullOrEmpty( UseCropSeason ) )
            list = list.Where( g => g.CropSeason.Description == UseCropSeason );

         var group = list.OrderBy( g => g.GrowerName )
                         .ThenBy( g => g.FarmName )
                         .ThenBy( g => g.FieldName )
                         .ThenBy( g => g.CropName )
                         .GroupBy( g => new { g.GrowerName, g.FarmName } );
         foreach (var g in group)
         {
            Console.WriteLine( $"GROWER: {g.Key.GrowerName}\tFARM: {g.Key.FarmName}" );
            foreach (var n in g)
            {
               Console.WriteLine( $"\tFIELD: {n.FieldName}\tCROP: {n.CropName}\tSEASON: {n.CropSeason?.Description}\tAREA: {Math.Round(n.ZoneArea,2)}" );
            }
            Console.WriteLine();
         }
         Console.ReadKey();
      }
   }

   /// <summary>
   /// This class builds up a collection of the Grower,Farm, Field, Crop and CropZone information so they can be 
   /// sorted and grouped by the appropriate values.
   /// </summary>
   internal class GrowerTree: List<GrowerFarmFieldZone>
   {
      /// <summary>
      /// The collection is based on a single ApplicationDataModel.  This is an important concept.  The reference
      /// ids used within the model to identify Grower, Farm, Field, Crop, CropZone etc. are valid only within the 
      /// contect of the current ApplicationDataModel.  The same item added to another model would likely have a 
      /// different value.  Therefore all values must be taken from the same ApplicationDataModel.
      /// </summary>
      /// <param name="model"></param>
      internal GrowerTree( ApplicationDataModel model )
         :base()
      {
         DataModel = model;
      }
      /// <summary>
      /// The current ApplicationDataModel used to create this collection.
      /// </summary>
      internal ApplicationDataModel DataModel { get; set; }

      /// <summary>
      /// This method iterates through the CropZones within the ApplicationDataModel and builds a new member of the
      /// collection for each CropZone found.  The related Crop and Field within the CropZone are found and populated.
      /// The Field in turn points to a Farm and the Farm to the Grower.
      /// </summary>
      /// <returns></returns>
      internal GrowerTree BuildFromCropZones()
      {
         Clear();

         foreach (var zone in DataModel.Catalog.CropZones)
         {
            var item = new GrowerFarmFieldZone();
            item.CropZoneId = zone.Id.ReferenceId;
            item.CropId = zone.CropId;
            var crop = GetCrop( item.CropId );
            item.CropName = crop?.Name;

            item.CropSeason = zone.TimeScopes.Where( t => t.DateContext == DateContextEnum.CropSeason )
                                             .FirstOrDefault();
            item.ZoneArea = zone.Area.Value.Value;
            item.ZoneDescription = zone.Description;

            item.FieldId = zone.FieldId;
            var field = GetField( item.FieldId );

            item.FarmId = field?.FarmId;
            item.FieldName = field?.Description;
            item.FieldArea = (field!=null && field.Area != null) ? field.Area.Value.Value: 0;

            var farm = GetFarm( item.FarmId );
            item.GrowerId = farm?.GrowerId;
            item.FarmName = farm?.Description;

            item.GrowerName = GetGrower( item.GrowerId )?.Name;
            Add( item );
         }
         return this;
      }
      /// <summary>
      /// Find a Grower using the referenced growerId.
      /// </summary>
      /// <param name="growerId"></param>
      /// <returns></returns>
      internal Grower GetGrower( int? growerId )
      {
         Grower grower = null;
         if (growerId.HasValue)
            grower = DataModel.Catalog.Growers.Where( g => g.Id.ReferenceId == growerId )
                                      .FirstOrDefault();
         return grower;
      }
      /// <summary>
      /// Find a Farm using the referenced farmId.
      /// </summary>
      /// <param name="farmId"></param>
      /// <returns></returns>
      internal Farm GetFarm( int? farmId )
      {
         Farm farm = null;
         if (farmId.HasValue)
            farm = DataModel.Catalog.Farms.Where( f => f.Id.ReferenceId == farmId )
                                          .FirstOrDefault();
         return farm;
      }
      /// <summary>
      /// Find a Field using the referenced fieldId.
      /// </summary>
      /// <param name="fieldId"></param>
      /// <returns></returns>
      internal Field GetField( int? fieldId )
      {
         Field field = null;
         if (fieldId.HasValue)
            field = DataModel.Catalog.Fields.Where( f => f.Id.ReferenceId == fieldId )
                                            .FirstOrDefault();
         return field;
      }
      /// <summary>
      /// Find a Crop using the referenced cropId.
      /// </summary>
      /// <param name="cropId"></param>
      /// <returns></returns>
      internal Crop GetCrop( int? cropId )
      {
         Crop crop = null;
         if (cropId.HasValue)
            crop = DataModel.Catalog.Crops.Where( c => c.Id.ReferenceId == cropId )
                                          .FirstOrDefault();
         return crop;
      }
   }

   /// <summary>
   /// This class represents the properties associated with each CropZone found within an ApplicationDataModel.
   /// </summary>
   internal class GrowerFarmFieldZone
   {
      internal int? GrowerId { get; set; }
      internal int? FarmId { get; set; }
      internal int? FieldId { get; set; }
      internal int? CropZoneId { get; set; }
      internal int? CropId { get; set; }
      internal TimeScope CropSeason {get; set;}

      internal string GrowerName { get; set; } = string.Empty;
      internal string FarmName { get; set; } = string.Empty;
      internal string FieldName { get; set; } = string.Empty;
      internal string CropName { get; set; } = string.Empty;
      internal string ZoneDescription { get; set; } = string.Empty;
      internal double ZoneArea { get; set; } = 0.0;
      internal double FieldArea { get; set; } = 0.0;
   }
}
