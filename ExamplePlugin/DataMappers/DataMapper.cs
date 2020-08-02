using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using ExamplePlugin.PublisherDataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExamplePlugin.DataMappers
{
    public static class DataMapper
    {
        public static void MapData(Data myDataModel, Catalog catalog)
        {
            //Import the clients, farms and fields
            foreach (Client myClient in myDataModel.Clients)
            {
                ClientFarmFieldMapper.MapGrower(myClient, catalog);
            }

            //Import the crops
            foreach (Crop myCrop in myDataModel.CropData.Crops)
            {
                CropMapper.MapCrop(myCrop, catalog);
            }

            //Import the crop assignments
            foreach (CropAssignment assignment in myDataModel.CropData.CropAssignments)
            {
                CropMapper.MapCropAssignment(assignment, catalog);
            }
        }

        public static UniqueId GetNativeID(BaseObject obj)
        {
            UniqueId id = new UniqueId();
            id.Id = obj.ID.ToString();
            id.IdType = IdTypeEnum.UUID;
            id.Source = "PublisherName.example";
            id.SourceType = IdSourceTypeEnum.URI;
            return id;
        }
    }
}
