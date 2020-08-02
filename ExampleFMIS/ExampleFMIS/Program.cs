using ExampleFMIS.AdaptObjects;
using ExampleFMIS.MyDataLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS
{
   class Program
   {
      static void Main(string[] args)
      {
         Console.WriteLine("This code will attempt to demonstrate some of the core concepts an FMIS application");
         Console.WriteLine("would need to be aware of when importing data from an ADAPT plugin.\r\n");
         Console.WriteLine("The plugin will translate its proprietary data into the ADAPT Data Model using its Import method.");
         Console.WriteLine("The FMIS application can then use the ADAPT data by mapping it to its own proprietary format.\r\n");
         Console.WriteLine("Care should be taken to preserve the UniqueIds contained within the CompoundIdentifier of each");
         Console.WriteLine("ADAPT object for two important reasons.  ");
         Console.WriteLine("\t1. You should always assign other applications UniqueIds if your application exports data");
         Console.WriteLine("\t   in an ADAPT format.");
         Console.WriteLine("\t2. The UniqueIds from other applications can prove very useful in identifying objects you ");
         Console.WriteLine("\t   have already imported in future imports of ADAPT data.");
         Console.WriteLine("\r\nPress any key to continue.\r\n");
         Console.ReadKey();
         Console.Clear();

         Console.WriteLine("First lets discover the ADAPT Plugin included in the folder supplied by this demo.");
         var appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
         var adaptMgr = new ADAPTDataManager();
         adaptMgr.PluginPath = Path.Combine(appPath, @"ADAPT\Plugins");
         var availablePLugins = adaptMgr.GetAvailablePlugins();

         Console.WriteLine("\r\nThe plugin found is listed below.");
         for (int i = 0; i < availablePLugins.Count; i++)
            Console.WriteLine($"{i + 1}. {availablePLugins[i]}");

         Console.WriteLine("\r\nThis is the ExamplePlugin that we have used in our previous video with the same data as well.");
         Console.WriteLine("\r\nPress any key to continue.\r\n");
         Console.ReadKey();
         Console.Clear();

         Console.WriteLine("Next we will assume our application has no existing data and ask it to import CropZone objects");
         Console.WriteLine("from the ADAPT data model supplied by the plugin.  We will expect that all related objects ");
         Console.WriteLine("(crops, fields, farms and growers) would also be imported since we know our data store is empty. ");
         Console.WriteLine("\r\nPress any key to continue.\r\n");
         Console.ReadKey();
         var adaptDataPath = Path.Combine(appPath, @"ADAPT\Data\ExamplePlugin");
         adaptMgr.ImportCropZones("ExamplePlugin", adaptDataPath);

         Console.WriteLine("Here are the results.  The following objects have been inserted into my data from the ");
         Console.WriteLine("ADAPT data model.\r\n");
         var groups = MyDataManager.Instance.InsertedObects.GroupBy(i => i.Class);
         foreach(var grp in groups)
         {
            Console.WriteLine($"Inserted Data Type = '{grp.Key}'");
            foreach(var obj in grp)
               Console.WriteLine($"\t{obj.Name}");
            Console.WriteLine("\r\n");
         }

         Console.WriteLine("\r\nPress any key to continue.\r\n");
         Console.ReadKey();
         Console.Clear();

         Console.WriteLine("\r\nFinally, we will delete the ManagementZones from our data store and then re-import the");
         Console.WriteLine("CropZones from the ADAPT data model.  Since the related crops, fields, farms, and growers");
         Console.WriteLine("already exist and we can identify them by the uniqueIds asscociated with the data imported");
         Console.WriteLine("there will be no need to import them again.");
         Console.WriteLine("Here are the results.  The following objects have been inserted into my data from the ");
         Console.WriteLine("ADAPT data model.\r\n");
         Console.WriteLine("\r\nPress any key to continue.\r\n");
         Console.ReadKey();
         MyDataManager.Instance.RemoveAllManagementZones();
         adaptMgr.ImportCropZones("ExamplePlugin", adaptDataPath);
         groups = MyDataManager.Instance.InsertedObects.GroupBy(i => i.Class);
         foreach (var grp in groups)
         {
            Console.WriteLine($"Inserted Data Type = '{grp.Key}'");
            foreach (var obj in grp)
               Console.WriteLine($"\t{obj.Name}");
            Console.WriteLine("\r\n");
         }
         Console.WriteLine("\r\nPress any key to continue.\r\n");
         Console.ReadKey();
         Console.Clear();
      }
   }
}
