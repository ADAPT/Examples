using AgGateway.ADAPT.ApplicationDataModel.ADM;
using ExampleFMIS.MyDataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.AdaptObjects.Mappers
{
   /// <summary>
   /// The FieldMapper class specifically relates information pertaining to Field in an ADAPT Data Model 
   /// to the Field objects im my data store.
   /// 
   /// This demonstration uses a singleton pattern for all mapper classes. This pattern makes code easier to read and avoids
   /// constructing the object multiple times within the application. This is by choice and not a requirement.
   /// 
   /// This mapper class is responsible for 
   ///   -  Finding matching ADAPT Fields and my Fields
   ///   -  Inserting new Field when a match is not found
   ///   -  Maintaining the referenced uniqueIds from the ADAPT Field model to my Field object
   /// </summary>
   public sealed class FieldMapper
   {
      private static readonly Lazy<FieldMapper> _instance = new Lazy<FieldMapper>(() => new FieldMapper());
      private FieldMapper()
      {
      }

      /// <summary>
      /// Returns the current instance of the FieldMapper object.
      /// </summary>
      public static FieldMapper Instance => _instance.Value;

      /// <summary>
      /// Attempts to find the equivalent Field in my data store to a fieldId referenced in the ADAPT data model
      /// If one cannot be found, then the informatiuon in the ADAPT field will be added to my data store.
      /// </summary>
      /// <param name="model">The entire ADAPT data model</param>
      /// <param name="admFieldId">an integer referenceId in the ADAPT data model associated with a field.</param>
      /// <returns>The uniqueId as a Guid from my data store equivalent to the ADAPT field.</returns>
      public Guid GetMyFieldId(ApplicationDataModel model, int? admFieldId)
      {
         //First get the ADADT field associated with the referenceId
         var admField = GetADMField(model, admFieldId);
         if (admField == null)
            return Guid.Empty;

         //Next see if there is an existing element in my data store matching the field.
         Guid myId;
         ExampleFMIS.MyDataLayer.Models.Field myField = null;
         if (DoesFieldExistInMyData(admField, out myField))
         {
            myId = myField.ID;
         }
         else //Insert a new Field if a match was not found.
         {
            myField = InsertNewField(model, admField);
            myId = myField.ID;
         }
         return myId;
      }


      /// <summary>
      /// Find the ADAPT Field model referenced by the ADAPT FieldId
      /// </summary>
      /// <param name="model">The entire ADAPT data model</param>
      /// <param name="fieldId">The integer referenceId associated with a particular field.</param>
      /// <returns>An ADAPT field model if found, otherwise null.</returns>
      public AgGateway.ADAPT.ApplicationDataModel.Logistics.Field GetADMField(ApplicationDataModel model, int? fieldId)
      {
         return model.Catalog.Fields.Where(c => c.Id.ReferenceId == fieldId)
                                    .FirstOrDefault();
      }


      /// <summary>
      /// Determines whether or not our data store contains a Field that corresponds to the ADAPT Field
      /// </summary>
      /// <param name="admField">The ADAPT Field model</param>
      /// <param name="myField">Filled with the matching Field if found otherwise it is null.</param>
      /// <returns>True if a matching element was found, false if not.</returns>
      private bool DoesFieldExistInMyData(AgGateway.ADAPT.ApplicationDataModel.Logistics.Field admField, out ExampleFMIS.MyDataLayer.Models.Field myField)
      {
         var exists = false;
         myField = null;

         //First iterate the UniqueId collection with the ADAPT CompoundIdentifier to see if it as a uniqueId where the Source is 
         //my application.  If so the ID property in that uniqueId will be my Id associated with that field.
         var myId = ADAPTDataManager.FindMyId(admField.Id);
         if (myId != null)
         {
            myField = MyDataManager.Instance.GetField(myId);
            if (myField != null)
               exists = true;
         }
         //If the ADAPT CompundIdentifier did not contain a uniqueId with our source, perhaps we've added that Field before
         //from a partner entity that is also in this ADAPT model.
         //We can look for Fields that match the source from one of the other entities.
         else if (myId == null)
         {
            var entities = ADAPTDataManager.GetOtherUniqueIds(admField.Id);
            myField = MyDataManager.Instance.GetField(entities);
            if (myField != null)
               exists = true;
         }
         else
         {
            //Just because the ADM model did not contain my uniqueId and I've not perviously added it from another source
            //doesn't mean my data store does not conatin the equivant object to that ADAPT Field.
            //Perhaps you want to try to match by name or some other proerties that will equate the two objects. Since fields
            //have spatial boundaries perhaps you want to match based on that spatial data.
            //Each FMIS will need to determine its strategy.
            myField = MyDataManager.Instance.GetField(admField.Description);
            if (myField != null)
               exists = true;
         }
         return exists;
      }

      /// <summary>
      /// Inserts a new Field based on information from the ADAPT Field
      /// </summary>
      /// <param name="model">The entire ADAPT model</param>
      /// <param name="admField">The specific ADAPT Field being inserted</param>
      /// <returns>The new Field</returns>
      private MyDataLayer.Models.Field InsertNewField(ApplicationDataModel model, AgGateway.ADAPT.ApplicationDataModel.Logistics.Field admField)
      {
         //Create a new Field object in my data store to contain the Field information from the ADAPT model
         var myField = new MyDataLayer.Models.Field()
         {
            //Map the matching elements of my Field to the ADAPT Field model
            Name = admField.Description,
            FarmID = FarmMapper.Instance.GetMyFarmId(model, admField.FarmId)
         };

         //Insert the new OperatingUnit
         MyDataManager.Instance.InsertField(myField);

         //Create the references of my new field to other uniqueIds found in the Field CompoundIdentifier
         var otherEntities = ADAPTDataManager.GetOtherUniqueIds(admField.Id);
         MyDataManager.Instance.InsertExternalEntities("Field", myField.ID, otherEntities);
         return myField;
      }

   }
}
