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
   /// The CropZoneMapper class specifically relates information pertaining to CropZones in an ADAPT Data Model 
   /// to the ManagementZone objects im my data store.
   /// 
   /// This demonstration uses a singleton pattern for all mapper classes. This pattern makes code easier to read and avoids
   /// constructing the object multiple times within the application. This is by choice and not a requirement.
   /// 
   /// This mapper class is responsible for 
   ///   -  Finding matching ADAPT CropZones and my ManagementZones
   ///   -  Inserting new ManagementZones when a match is not found
   ///   -  Maintaining the referenced uniqueIds from the ADAPT CropZones model to my ManagementZones object
   /// </summary>
   public sealed class CropZoneMapper
   {
      private static readonly Lazy<CropZoneMapper> _instance = new Lazy<CropZoneMapper>(() => new CropZoneMapper());
      private CropZoneMapper()
      {
      }

      /// <summary>
      /// Returns the current instance of the CropZoneMapper object.
      /// </summary>
      public static CropZoneMapper Instance => _instance.Value;

      /// <summary>
      /// Determines whether a CropZone from the ADAPT data model has an equivalent ManagementZone within my data store.
      /// If one exists perhaps there is new information that needs to be updated, if it does not it will be imported.
      /// Data referenced by the Crop Zone will also be imported if it does not exist.
      /// </summary>
      /// <param name="model">The entire ADAPT data model.</param>
      /// <param name="admCropZone">The particular ADAPT cropZone object</param>
      public void ImportCropZone(ApplicationDataModel model, CropZone admCropZone)
      {
         ManagementZone myMgmtZone = null;
         if (DoesCropZoneExistInMyData(admCropZone, out myMgmtZone))
            UpdateManagementZone(model, admCropZone, myMgmtZone);
         else
            InsertNewManagementZone(model, admCropZone);
      }

      /// <summary>
      /// Determines whether or not our data store contains a ManagementZone object that corresponds to the ADAPT CropZone
      /// </summary>
      /// <param name="admCropZone">The ADAPT CropZone model</param>
      /// <param name="myMgmtZone">Filled with the matching ManagementZone if found otherwise it is null.</param>
      /// <returns>True if a matching element was found, false if not.</returns>
      private bool DoesCropZoneExistInMyData(CropZone admCropZone, out ManagementZone myMgmtZone)
      {
         var exists = false;
         myMgmtZone = null;

         //First we will simply look in the ADAPT CompoundIdentifier for a UniqueId with our source.
         var myId = ADAPTDataManager.FindMyId(admCropZone.Id);
         if (myId != null)
         {
            myMgmtZone = MyDataManager.Instance.GetManagementZone(myId);
            exists = true;
         }

         //If the ADADP CompundIdentifier did not contain a uniqueId with our source, perhaps we've added that CropZone before
         //and we can look for ManagementZones that match the source from one of the other entities.
         else if (myId == null)
         {
            var entities = ADAPTDataManager.GetOtherUniqueIds(admCropZone.Id);
            myMgmtZone = MyDataManager.Instance.GetManagementZone(entities);
            if( myMgmtZone != null )
               exists = true;
         }

         //An FMIS may want to employ additional strategies to try to equate a CropZone in the model to an existing object
         //in their database.  Perhaps that would entail trying to equate the spatial boundaries. 
         //Each FMIS will need to determine its own strategy.
         else
         {
         }
         return exists;
      }

      private void UpdateManagementZone(ApplicationDataModel model, CropZone admCropZone, ManagementZone myMgmtZone)
      {
         //If a matching catalog item is found in an ADAPT data model, the application will need to determine a strategy 
         //for updating its data.  Does it depend on the source? Does it always update with any changed information.  
         //Does it never update catalog data?
      }

      /// <summary>
      /// Inserts a new ManagementZone based on information from the ADAPT CropZone
      /// </summary>
      /// <param name="model">The entire ADAPT model</param>
      /// <param name="admCropZone">The specific ADAPT CropZone being inserted</param>
      /// <returns>The new OperatingUnit</returns>
      private void InsertNewManagementZone(ApplicationDataModel model, CropZone admCropZone)
      {
         //Create a new "OperatingUnit" object in my data store to contain the Grower information from the ADAPT model
         var myMgmtZone = new ManagementZone();

         //Map the matching elements of my "Operating Unit" to the Grower model
         myMgmtZone.Name = admCropZone.Description;

         if( admCropZone.Area!=null )
            myMgmtZone.Acres = (decimal)admCropZone.Area.Value.Value; 

         var time = admCropZone.TimeScopes.Where(t => t.DateContext == DateContextEnum.CropSeason)
                                          .FirstOrDefault();
         if (time != null)
            myMgmtZone.ProductionYear = Convert.ToInt32(time.Description);

         //Find my uniqueIds from the referenced Crop and Field objects in the ADAPT CropZone
         myMgmtZone.CropId = CropMapper.Instance.GetMyCropId(model, admCropZone.CropId);
         myMgmtZone.FieldId = FieldMapper.Instance.GetMyFieldId(model, admCropZone.FieldId);

         //Insert the new OperatingUnit
         MyDataManager.Instance.InsertManagementZone(myMgmtZone);

         //Create the references of my new ManagementZone to other uniqueIds found in the CropZone CompoundIdentifier
         var otherEntities = ADAPTDataManager.GetOtherUniqueIds(admCropZone.Id);
         MyDataManager.Instance.InsertExternalEntities("ManagemeZone", myMgmtZone.ID, otherEntities);
      }



   }
}
