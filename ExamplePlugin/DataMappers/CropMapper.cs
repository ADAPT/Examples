using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Logistics;
using AgGateway.ADAPT.ApplicationDataModel.Products;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ExamplePlugin.DataMappers
{
    public static class CropMapper
    {
        public static Crop MapCrop(PublisherDataModel.Crop myCrop, Catalog catalog)
        {
            //Transform the native object into the ADAPT object
            Crop adaptCrop = new Crop();
            adaptCrop.Name = myCrop.Name;

            //Publish any persistent publisher ID to the list of IDs
            adaptCrop.Id.UniqueIds.Add(DataMapper.GetNativeID(myCrop));

            //Add the ADAPT object to its container
            catalog.Crops.Add(adaptCrop);

            return adaptCrop;
        }

        public static CropZone MapCropAssignment(PublisherDataModel.CropAssignment assignment, Catalog catalog)
        {
            //Get a reference to the field for the ADAPT cropzone via the ID mapping
            Field adaptField = catalog.Fields.FirstOrDefault(f => f.Id.UniqueIds.Any(i => i.Id == assignment.FieldID.ToString()));

            //Transform the native object into the ADAPT object
            CropZone adaptCropzone = new CropZone();
            adaptCropzone.Description = $"{adaptField.Description} {assignment.GrowingSeason}";

            //Set any Reference IDs
            adaptCropzone.FieldId = adaptField.Id.ReferenceId;

            //Add the ADAPT object to its container
            catalog.CropZones.Add(adaptCropzone);

            return adaptCropzone;
        }
    }
}
