using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using AgGateway.ADAPT.PluginManager;
using ExampleFMIS.AdaptObjects.Mappers;
using ExampleFMIS.MyDataLayer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.AdaptObjects
{
   /// <summary>
   /// This class is responsible managing the plugins available for working with the ADAPT data and also providing methods of
   /// basic manipulations of data within the ADAPT Data Model.  Specifically it will
   ///   -  Create a PluginFactory Object 
   ///   -  Provide a list of available plugins within the supplied directory path
   ///   -  Read data from that plugin from a supplied directory path
   ///   -  Import CropZones and their referenced data within the ADPAT Data Model
   ///   -  Examine a CompoundIdentifier and return the unique id from this application associated with the ADAPT object
   ///   -  Examine a CompoundIdentifier and return a collection of UniqueId objects from other applications
   /// </summary>
   public class ADAPTDataManager
   {
      //This is the Source string that identifies uniqueIds from this application.
      const string MySourceURL = "http://ExampleFMIS.source";

      public ADAPTDataManager()
      {
      }

      public ADAPTDataManager(string pluginPath)
      {
         PluginPath = pluginPath;
      }

      /// <summary>
      /// Gets the plugin object currently in use
      /// </summary>
      public IPlugin CurrentPlugin { get; private set; } = null;

      private PluginFactory _factory = null;
      /// <summary>
      /// This is an ADM PluginFactory object created the first time it is used.
      /// </summary>
      private PluginFactory PluginFactory 
      {
         get 
         {
            if (_factory == null)
            {
               if (string.IsNullOrEmpty(PluginPath) || !Directory.Exists(PluginPath) )
                  throw new ApplicationException("A valid path to the ADAPT Plugins must be set.");
               _factory = new PluginFactory(PluginPath);
            }
            return _factory;
         }
         set { _factory = value; }
      }

      private string _pluginPath = string.Empty;
      //This is path where plugin objects can be found
      public string PluginPath {
         get { return _pluginPath; }
         set 
         {
            PluginFactory = null;
            CurrentPlugin = null;
            _pluginPath = value;
         }
      }

      /// <summary>
      /// Returns a list of plugin names that are currently in the PluginPath directory.
      /// </summary>
      /// <returns></returns>
      public List<string> GetAvailablePlugins()
      {
         return PluginFactory.AvailablePlugins;
      }

      /// <summary>
      /// Using one of the available plugins, read ADAPT data from the supplied data path.
      /// </summary>
      /// <param name="pluginName">one of the available plugins</param>
      /// <param name="dataPath">the directory where data exists for the specified plugin.</param>
      /// <returns></returns>
      private ApplicationDataModel ReadPluginData(string pluginName, string dataPath)
      {
         ApplicationDataModel model = null;
         CurrentPlugin = PluginFactory.GetPlugin(pluginName);
         if( CurrentPlugin != null)
         {
            InitializeCurrentPlugin();
            var admModels = CurrentPlugin.Import(dataPath);
            if( admModels != null && admModels.Count > 0 )
               model = admModels[0];
         }
         return model;
      }

      /// <summary>
      /// Each plugin may require its own unique initialization parameters. They will need to be provided for the
      /// specific plugin
      /// </summary>
      private void InitializeCurrentPlugin()
      {
         switch( CurrentPlugin.Owner )
         {
            default:
            case "AgGateway":
               CurrentPlugin.Initialize();
               break;
         }
      }

      /// <summary>
      /// Import Crop Zones from ADAPT data provided by a specified plugin and specified directory path
      /// </summary>
      /// <param name="pluginName">the plugin used to read the data</param>
      /// <param name="dataPath">the directory where the data exists</param>
      public void ImportCropZones(string pluginName, string dataPath)
      {
         var model = ReadPluginData(pluginName, dataPath);
         if( model != null )
         {
            foreach(AgGateway.ADAPT.ApplicationDataModel.Logistics.CropZone cropZone in model.Catalog.CropZones)
            {
               CropZoneMapper.Instance.ImportCropZone(model, cropZone);
            }
         }
      }

      /// <summary>
      /// Finds the specific ADAPT uniqueId that matches my source name and returns the value from the ID property as a Guid
      /// since I know that my application uses Guid values as uniqueIds.
      /// </summary>
      /// <param name="compoundIdentifier"></param>
      /// <returns></returns>
      public static Guid? FindMyId(CompoundIdentifier compoundIdentifier)
      {
         Guid? id = null;
         var result = compoundIdentifier.UniqueIds.Where(u => u.Source == MySourceURL)
                                        .Select(u => u.Id)
                                        .FirstOrDefault();
         if (result != null)
            id = new Guid(result);

         return id;
      }

      /// <summary>
      /// Returns a collection of ExternalEntity objects created from the ADAPT UniqueId objects in the compound identifier 
      /// that do not my applications source name.
      /// </summary>
      /// <param name="compoundIdentifier"></param>
      /// <returns></returns>
      public static List<ExternalEntity> GetOtherUniqueIds(CompoundIdentifier compoundIdentifier)
      {
         var entityList = new List<ExternalEntity>();
         var list = compoundIdentifier.UniqueIds.Where(u => u.Source != MySourceURL)
                                                .ToList();
         foreach( var uniqueId in list)
         {
            var entity = new ExternalEntity();
            entity.Id = uniqueId.Id;
            entity.Source = uniqueId.Source;
            switch( uniqueId.IdType )
            {
               case AgGateway.ADAPT.ApplicationDataModel.Common.IdTypeEnum.LongInt:
                  entity.IdType = MyDataLayer.Models.IdTypeEnum.LongInt;
                  break;
               case AgGateway.ADAPT.ApplicationDataModel.Common.IdTypeEnum.String:
                  entity.IdType = MyDataLayer.Models.IdTypeEnum.String;
                  break;
               case AgGateway.ADAPT.ApplicationDataModel.Common.IdTypeEnum.URI:
                  entity.IdType = MyDataLayer.Models.IdTypeEnum.URI;
                  break;
               case AgGateway.ADAPT.ApplicationDataModel.Common.IdTypeEnum.UUID:
                  entity.IdType = MyDataLayer.Models.IdTypeEnum.UUID;
                  break;
            }

            switch( uniqueId.SourceType)
            {
               case AgGateway.ADAPT.ApplicationDataModel.Common.IdSourceTypeEnum.GLN:
                  entity.SourceType = MyDataLayer.Models.IdSourceTypeEnum.GLN;
                  break;
               case AgGateway.ADAPT.ApplicationDataModel.Common.IdSourceTypeEnum.URI:
                  entity.SourceType = MyDataLayer.Models.IdSourceTypeEnum.URI;
                  break;
            }
            entityList.Add(entity);
         }
         return entityList;
      }
   }



}
