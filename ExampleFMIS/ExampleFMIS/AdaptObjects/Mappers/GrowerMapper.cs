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
   /// The GrowerMapper class specifically relates information pertaining to Growers in an ADAPT Data Model 
   /// to the OperatingUnit objects im my data store.
   /// 
   /// This demonstration uses a singleton pattern for all mapper classes. This pattern makes code easier to read and avoids
   /// constructing the object multiple times within the application. This is by choice and not a requirement.
   /// 
   /// This mapper class is responsible for 
   ///   -  Finding matching ADAPT Growers and my OperatingUnits
   ///   -  Inserting new OperatingUnits when a match is not found
   ///   -  Maintaining the referenced uniqueIds from the ADAPT Grower model to my OperatingUnit object
   /// </summary>
   public sealed class GrowerMapper
   {

      private static readonly Lazy<GrowerMapper> _instance = new Lazy<GrowerMapper>(() => new GrowerMapper());
      private GrowerMapper()
      {
      }

      /// <summary>
      /// Returns the current instance of the GrowerMapper object.
      /// </summary>
      public static GrowerMapper Instance => _instance.Value;

      /// <summary>
      /// Attempts to find the equivalent OperatingUnit in my data store to a growerId referenced in the ADAPT data model
      /// If one cannot be found, then the ADAPT grower will be added to my data store.
      /// </summary>
      /// <param name="model">The entire ADAPT data model</param>
      /// <param name="admGrowerId">an integer referenceId in the ADAPT data model associated with a grower.</param>
      /// <returns>The uniqueId as a Guid from my data store equivalent to the ADAPT grower.</returns>
      public Guid GetMyOperatingUnitId(ApplicationDataModel model, int? admGrowerId)
      {
         //First get the ADADT grower associated with the referenceId
         var admGrower = GetADMGrower(model, admGrowerId);
         if (admGrower == null)
            return Guid.Empty;

         //Next see if there is an existing element in my data store matching the grower.
         Guid myId;
         ExampleFMIS.MyDataLayer.Models.OperatingUnit myOperatingUnit = null;
         if (DoesOperatingUnitExistInMyData(admGrower, out myOperatingUnit))
         {
            myId = myOperatingUnit.ID;
         }
         else //Insert a new OperatingUnit if a match was not found.
         {
            myOperatingUnit = InsertNewOperatingUnit(model, admGrower);
            myId = myOperatingUnit.ID;
         }
         return myId;
      }

      /// <summary>
      /// Find the ADAPT Grower model referenced by the ADAPT GrowerId
      /// </summary>
      /// <param name="model">The entire ADAPT data model</param>
      /// <param name="growerId">The integer referenceId associated with a particular grower.</param>
      /// <returns>An ADAPT grower model if found, otherwise null.</returns>
      public AgGateway.ADAPT.ApplicationDataModel.Logistics.Grower GetADMGrower(ApplicationDataModel model, int? growerId)
      {
         return model.Catalog.Growers.Where(g => g.Id.ReferenceId == growerId)
                                    .FirstOrDefault();
      }

      /// <summary>
      /// Determines whether or not our data store contains an OperatingUnit that corresponds to the ADAPT Grower
      /// </summary>
      /// <param name="admGrower">The ADAPT Grower model</param>
      /// <param name="myOperatingUnit">Filled with the matching OperatingUnit if found otherwise it is null.</param>
      /// <returns>True if a matching element was found, false if not.</returns>
      private bool DoesOperatingUnitExistInMyData(AgGateway.ADAPT.ApplicationDataModel.Logistics.Grower admGrower, out ExampleFMIS.MyDataLayer.Models.OperatingUnit myOperatingUnit)
      {
         var exists = false;
         myOperatingUnit = null;

         //First iterate the UniqueId collection with the ADAPT CompoundIdentifier to see if it as a uniqueId where the Source is 
         //my application.  If so the ID property in that uniqueId will be my Id associated with that Grower.
         var myId = ADAPTDataManager.FindMyId(admGrower.Id);
         if (myId != null)
         {
            myOperatingUnit = MyDataManager.Instance.GetOperatingUnit(myId);
            if (myOperatingUnit != null)
               exists = true;
         }
         //If the ADAPT CompundIdentifier did not contain a uniqueId with our source, perhaps we've added that OperatingUnit before
         //from a partner entity that is also in this ADAPT model.
         //We can look for OperatingUnits that match the source from one of the other entities.
         else if (myId == null)
         {
            var entities = ADAPTDataManager.GetOtherUniqueIds(admGrower.Id);
            myOperatingUnit = MyDataManager.Instance.GetOperatingUnit(entities);
            if (myOperatingUnit != null)
               exists = true;
         }
         else
         {
            //Just because the ADM model did not contain my uniqueId and I've not perviously added it from another source
            //doesn't mean my data store does not conatin the equivant object to that ADAPT Grower.
            //Perhaps you want to try to match by name or some other proerties that will equate the two objects.  
            //Each FMIS will need to determine its strategy.
            myOperatingUnit = MyDataManager.Instance.GetOperatingUnit(admGrower.Name);
            if (myOperatingUnit != null)
               exists = true;
         }
         return exists;
      }

      /// <summary>
      /// Inserts a new OperatingUnit based on information from the ADAPT Grower
      /// </summary>
      /// <param name="model">The entire ADAPT model</param>
      /// <param name="admGrower">The specific ADAPT Grower being inserted</param>
      /// <returns>The new OperatingUnit</returns>
      private MyDataLayer.Models.OperatingUnit InsertNewOperatingUnit(ApplicationDataModel model, AgGateway.ADAPT.ApplicationDataModel.Logistics.Grower admGrower)
      {
         //Create a new "OperatingUnit" object in my data store to contain the Grower information from the ADAPT model
         var myOperatinUnit = new MyDataLayer.Models.OperatingUnit();

         //Map the matching elements of my "Operating Unit" to the Grower model
         myOperatinUnit.Name = admGrower.Name;

         //Insert the new OperatingUnit
         MyDataManager.Instance.InsertOperatingUnit(myOperatinUnit);

         //Create the references of my new operating unit to other uniqueIds found in the Grower CompoundIdentifier
         var otherEntities = ADAPTDataManager.GetOtherUniqueIds(admGrower.Id);
         MyDataManager.Instance.InsertExternalEntities("OperatingUnit", myOperatinUnit.ID, otherEntities);
         return myOperatinUnit;
      }

   }
}
