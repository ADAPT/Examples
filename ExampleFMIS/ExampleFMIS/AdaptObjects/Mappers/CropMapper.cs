using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Products;
using ExampleFMIS.MyDataLayer;
using ExampleFMIS.MyDataLayer.Models;

namespace ExampleFMIS.AdaptObjects.Mappers
{
   /// <summary>
   /// The CropMapper class specifically relates information pertaining to Crop in an ADAPT Data Model 
   /// to the Crop objects im my data store.
   /// 
   /// This demonstration uses a singleton pattern for all mapper classes. This pattern makes code easier to read and avoids
   /// constructing the object multiple times within the application. This is by choice and not a requirement.
   /// 
   /// This mapper class is responsible for 
   ///   -  Finding matching ADAPT Crops and my Crops
   ///   -  Inserting new Crop when a match is not found
   ///   -  Maintaining the referenced uniqueIds from the ADAPT Crop model to my Crop object
   /// </summary>
   public sealed class CropMapper
   {
      private static readonly Lazy<CropMapper> _instance = new Lazy<CropMapper>(() => new CropMapper());
      private CropMapper()
      {
      }

      /// <summary>
      /// Returns the current instance of the CropMapper object.
      /// </summary>
      public static CropMapper Instance => _instance.Value;

      /// <summary>
      /// Attempts to find the equivalent Crop in my data store to a cropId referenced in the ADAPT data model
      /// If one cannot be found, then the informatiuon in the ADAPT crop will be added to my data store.
      /// </summary>
      /// <param name="model">The entire ADAPT data model</param>
      /// <param name="admCropId">an integer referenceId in the ADAPT data model associated with a crop.</param>
      /// <returns>The uniqueId as a Guid from my data store equivalent to the ADAPT crop.</returns>
      public Guid GetMyCropId(ApplicationDataModel model, int? admCropId)
      {
         //First get the ADADT crop associated with the referenceId
         var admCrop = GetADMCrop(model, admCropId);
         if (admCrop == null)
            return Guid.Empty;

         //Next see if there is an existing element in my data store matching the crop.
         Guid myId;
         ExampleFMIS.MyDataLayer.Models.Crop myCrop = null;
         if (DoesCropExistInMyData(admCrop, out myCrop))
         {
            myId = myCrop.ID;
         }
         else //Insert a new Crop if a match was not found.
         {
            myCrop = InsertNewCrop(model, admCrop);
            myId = myCrop.ID;
         }
         return myId;
      }


      /// <summary>
      /// Find the ADAPT Crop model referenced by the ADAPT CropId
      /// </summary>
      /// <param name="model">The entire ADAPT data model</param>
      /// <param name="cropId">The integer referenceId associated with a particular crop.</param>
      /// <returns>An ADAPT crop model if found, otherwise null.</returns>
      public AgGateway.ADAPT.ApplicationDataModel.Products.Crop GetADMCrop(ApplicationDataModel model, int? cropId)
      {
         return model.Catalog.Crops.Where(c => c.Id.ReferenceId == cropId)
                                       .FirstOrDefault();
      }

      /// <summary>
      /// Determines whether or not our data store contains a Crop that corresponds to the ADAPT Crop
      /// </summary>
      /// <param name="admCrop">The ADAPT Crop model</param>
      /// <param name="myCrop">Filled with the matching Crop if found otherwise it is null.</param>
      /// <returns>True if a matching element was found, false if not.</returns>
      private bool DoesCropExistInMyData(AgGateway.ADAPT.ApplicationDataModel.Products.Crop admCrop, out ExampleFMIS.MyDataLayer.Models.Crop myCrop)
      {
         var exists = false;
         myCrop = null;

         //First iterate the UniqueId collection with the ADAPT CompoundIdentifier to see if it as a uniqueId where the Source is 
         //my application.  If so the ID property in that uniqueId will be my Id associated with that crop.
         var myId = ADAPTDataManager.FindMyId(admCrop.Id);
         if (myId != null)
         {
            myCrop = MyDataManager.Instance.GetCrop(myId);
            if (myCrop != null)
               exists = true;
         }
         //If the ADAPT CompundIdentifier did not contain a uniqueId with our source, perhaps we've added that Field before
         //from a partner entity that is also in this ADAPT model.
         //We can look for Crops that match the source from one of the other entities.
         else if (myId == null)
         {
            var entities = ADAPTDataManager.GetOtherUniqueIds(admCrop.Id);
            myCrop = MyDataManager.Instance.GetCrop(entities);
            if (myCrop != null)
               exists = true;
         }
         else
         {
            //Just because the ADM model did not contain my uniqueId and I've not perviously added it from another source
            //doesn't mean my data store does not conatin the equivant object to that ADAPT Crop.
            //Perhaps you want to try to match by name or some other proerties that will equate the two objects.
            //Each FMIS will need to determine its strategy.
            myCrop = MyDataManager.Instance.GetCrop(admCrop.Name);
            if (myCrop != null)
               exists = true;
         }
         return exists;
      }

      /// <summary>
      /// Inserts a new Crop based on information from the ADAPT Crop
      /// </summary>
      /// <param name="model">The entire ADAPT model</param>
      /// <param name="admCrop">The specific ADAPT Crop being inserted</param>
      /// <returns>The new Crop</returns>
      private MyDataLayer.Models.Crop InsertNewCrop(ApplicationDataModel model, AgGateway.ADAPT.ApplicationDataModel.Products.Crop admCrop)
      {
         //Create a new Crop object in my data store to contain the Crop information from the ADAPT model
         var myCrop = new MyDataLayer.Models.Crop()
         {
            //Map the matching elements of my Crop to the ADAPT Crop model
            Name = admCrop.Name
         };

         //Insert the new OperatingUnit
         MyDataManager.Instance.InsertCrop(myCrop);

         //Create the references of my new crop to other uniqueIds found in the Crop CompoundIdentifier
         var otherEntities = ADAPTDataManager.GetOtherUniqueIds(admCrop.Id);
         MyDataManager.Instance.InsertExternalEntities("Crop", myCrop.ID, otherEntities);
         return myCrop;
      }

   }
}
