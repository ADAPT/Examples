using AgGateway.ADAPT.ApplicationDataModel.ADM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;  

namespace ExamplePlugin
{
    public class Plugin : IPlugin
    {
        public string Name => "Example Plugin";

        public string Version => "0.1";

        public string Owner => "AgGateway";

        /// <summary>
        /// This code illustrates a simple import use case of a proprietary OEM data file into the ADAPT data model
        /// </summary>
        /// <param name="dataPath">Path containing 0 or or more proprietary data files</param>
        /// <param name="properties">Key/Value collection of any custom instructions for the Plugin</param>
        /// <returns>One or more ADAPT ApplicationDataModel objects</returns>
        public IList<ApplicationDataModel> Import(string dataPath, Properties properties)
        {
            IList<ApplicationDataModel> models = new List<ApplicationDataModel>();

            //Find any data files in the defined path
            //The .myjson file is a trivial example illustrating what a proprietary data file might be.
            //In practice, the data file is probably not human readable and only decipherable by the OEM itself.
            string[] myDataFiles = Directory.GetFiles(dataPath, "*.myjson", SearchOption.AllDirectories);
            if (myDataFiles.Any())
            {
                //A plugin publisher can choose to create one or multiple application data models as appropriate for the data
                ApplicationDataModel adm = new ApplicationDataModel();
                adm.Catalog = new Catalog() { Description = $"ADAPT data transformation of Publisher data {DateTime.Now.ToShortDateString()} {dataPath}" };
                models.Add(adm);

                foreach (string myDataFile in myDataFiles)
                {
                    //Import each file.   This simply loads the proprietary file into the proprietary data model.
                    string myJson = File.ReadAllText(myDataFile, System.Text.Encoding.Default);
                    PublisherDataModel.Data myData = JsonConvert.DeserializeObject<PublisherDataModel.Data>(myJson);

                    //This completes the transformation of the proprietary data model into the ADAPT data model.
                    //Each property is mapped appropriately from one to the other.
                    DataMappers.DataMapper.MapData(myData, adm.Catalog);
                }
            }

            return models;
        }

        /// <summary>
        /// Export works just like import, except the Mappers should work in the reverse direction
        /// </summary>
        /// <param name="dataModel">The ADAPT adm to export to the plugin's format</param>
        /// <param name="exportPath">Path to publish to</param>
        /// <param name="properties">Any proprietary values the user may pass in customizing the export</param>
        public void Export(ApplicationDataModel dataModel, string exportPath, Properties properties)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// GetProperties is an optional feature to return 
        /// basic key/value information about a data set without
        /// completing the complete transformation
        /// </summary>
        /// <param name="dataPath"></param>
        /// <returns></returns>
        public Properties GetProperties(string dataPath)
        {

            throw new NotImplementedException();
        }


        /// <summary>
        ///Initialize is an optional feature if a publisher wishes
        ///to secure use of the plugin with specific arguments  
        ///or otherwise customize the behavior with a particular set of parameters
        /// <param name="args"></param>
        public void Initialize(string args)
        {

        }

        /// <summary>
        /// Determines whether the folder contains data that this plugin can import
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public bool IsDataCardSupported(string dataPath, Properties properties)
        {
            //In this simple example, we are simply looking for the myjson extension to identify data in our format
            if (Directory.GetFiles(dataPath, "*.myjson", SearchOption.AllDirectories).Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Errors provides a place for the Import & Export methods to return runtime errors encountered during their respective processing.
        /// </summary>
        public IList<IError> Errors
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Examines the contents of a data file for formatting errors
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public IList<IError> ValidateDataOnCard(string dataPath, Properties properties)
        {
            throw new NotImplementedException();
        }
    }
}
