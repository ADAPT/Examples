using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using AgGateway.ADAPT.ApplicationDataModel.Logistics;
using ExampleFMIS.MyDataLayer;
using ExampleFMIS.MyDataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.AdaptObjects.Mappers
{
   /// <summary>
   /// The FarmMapper class specifically relates information pertaining to Farms in an ADAPT Data Model 
   /// to the Farm objects im my data store.
   /// 
   /// This demonstration uses a singleton pattern for all mapper classes. This pattern makes code easier to read and avoids
   /// constructing the object multiple times within the application. This is by choice and not a requirement.
   /// 
   /// This mapper class is responsible for 
   ///   -  Finding matching ADAPT Farms and my Farm objects
   ///   -  Inserting new Farms when a match is not found
   ///   -  Maintaining the referenced uniqueIds from the ADAPT Farm model to my Farm object
   /// </summary>
   public sealed class FarmMapper
   {
      private static readonly Lazy<FarmMapper> _instance = new Lazy<FarmMapper>(() => new FarmMapper());
      private FarmMapper()
      {
      }
      /// <summary>
      /// Returns the current instance of the FarmMapper object.
      /// </summary>
      public static FarmMapper Instance => _instance.Value;

      /// <summary>
      /// Attempts to find the equivalent Farm object in my data store to a farmId referenced in the ADAPT data model.
      /// If one cannot be found, then the ADAPT farm will be added to my data store.
      /// </summary>
      /// <param name="model">The entire ADAPT data model</param>
      /// <param name="admFarmId">an integer referenceId in the ADAPT data model associated with a farm.</param>
      /// <returns>The uniqueId as a Guid from my data store equivalent to the ADAPT farm</returns>
      public Guid GetMyFarmId(ApplicationDataModel model, int? admFarmId)
      {
         //First get the ADADT farm associated with the referenceId
         var admFarm = GetADMFarm(model, admFarmId);
         if (admFarm == null)
            return Guid.Empty;

         //Next see if there is an existing element in my data store matching the farm.
         Guid myId;
         ExampleFMIS.MyDataLayer.Models.Farm myFarm = null;
         if (DoesFarmExistInMyData(admFarm, out myFarm))
         {
            myId = myFarm.ID;
         }
         else  //Insert a new Farm if a match was not found.
         {
            myFarm = InsertNewFarm(model, admFarm);
            myId = myFarm.ID;
         }
         return myId;
      }

      /// <summary>
      /// Find the ADAPT Farm model referenced by the ADAPT FarmId
      /// </summary>
      /// <param name="model">The entire ADAPT data model</param>
      /// <param name="farmId">The integer referenceId associated with a particular farm.</param>
      /// <returns>An ADAPT Farm model if found, otherwise null.</returns>
      public AgGateway.ADAPT.ApplicationDataModel.Logistics.Farm GetADMFarm(ApplicationDataModel model, int? farmId)
      {
         return model.Catalog.Farms.Where(c => c.Id.ReferenceId == farmId)
                                    .FirstOrDefault();
      }

      /// <summary>
      /// Determines whether or not our data store contains a Farm that corresponds to the ADAPT Farm
      /// </summary>
      /// <param name="admFarm">The ADAPT Farm model</param>
      /// <param name="myFarm">Filled with the matching farm from my data store if found otherwise it is null.</param>
      /// <returns>True if a matching element was found, false if not.</returns>
      private bool DoesFarmExistInMyData(AgGateway.ADAPT.ApplicationDataModel.Logistics.Farm admFarm, out ExampleFMIS.MyDataLayer.Models.Farm myFarm)
      {
         var exists = false;
         myFarm = null;

         //First iterate the UniqueId collection with the ADAPT CompoundIdentifier to see if it as a uniqueId where the Source is 
         //my application.  If so the ID property in that uniqueId will be my Id associated with that farm.
         var myId = ADAPTDataManager.FindMyId(admFarm.Id);
         if (myId != null)
         {
            //Get my Farm object based on my uniqueID.
            myFarm = MyDataManager.Instance.GetFarm(myId);
            if (myFarm != null)
               exists = true;
         }
         //If the ADADP CompundIdentifier did not contain a uniqueId with our source, perhaps we've added that Farm before
         //from a partner entity that is also in this ADAPT model.
         //We can look for Farms that match the source from one of the other entities.
         else if (myId == null)
         {
            var entities = ADAPTDataManager.GetOtherUniqueIds(admFarm.Id);
            myFarm = MyDataManager.Instance.GetFarm(entities);
            if (myFarm != null)
               exists = true;
         }
         else
         {
            //Just because the ADM model did not contain my uniqueId and I've not perviously added it from another source
            //doesn't mean my data store does not conatin the equivant object to that ADAPT Farm.
            //Perhaps you want to try to match by name or some other proerties that will equate the two objects.  
            //Each FMIS will need to determine its strategy.
            myFarm = MyDataManager.Instance.GetFarm(admFarm.Description);
            if (myFarm != null)
               exists = true;
         }
         return exists;
      }

      /// <summary>
      /// Inserts a new Farm based on information from the ADAPT Farm
      /// </summary>
      /// <param name="model">The entire ADAPT model</param>
      /// <param name="admFarm">The specific ADAPT Farm being inserted</param>
      /// <returns>The new OperatingUnit</returns>
      private MyDataLayer.Models.Farm InsertNewFarm(ApplicationDataModel model, AgGateway.ADAPT.ApplicationDataModel.Logistics.Farm admFarm)
      {
         //Create a new Farm object in my data store to contain the Farm information from the ADAPT model
         var myFarm = new MyDataLayer.Models.Farm();

         //Map the matching elements of my Farm to the ADAPT Farm model
         myFarm.Name = admFarm.Description;
         myFarm.OperatingUnitID = GrowerMapper.Instance.GetMyOperatingUnitId(model, admFarm.GrowerId);

         //Insert the new Farm
         MyDataManager.Instance.InsertFarm(myFarm);

         //Create the references of my new farm to other uniqueIds found in the ADAPT Farm CompoundIdentifier
         var otherEntities = ADAPTDataManager.GetOtherUniqueIds(admFarm.Id);
         MyDataManager.Instance.InsertExternalEntities("Farm", myFarm.ID, otherEntities);
         return myFarm;
      }

   }
}
