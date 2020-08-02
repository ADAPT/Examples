using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Logistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExamplePlugin.DataMappers
{
    public static class ClientFarmFieldMapper
    {
        public static Grower MapGrower(PublisherDataModel.Client myClient, Catalog catalog)
        {
            //Transform the native object into the ADAPT object
            Grower adaptGrower = new Grower();
            adaptGrower.Id.UniqueIds.Add(DataMapper.GetNativeID(myClient));
            adaptGrower.Name = myClient.Name;

            //Add the ADAPT object to its container
            catalog.Growers.Add(adaptGrower);

            foreach (PublisherDataModel.Farm myFarm in myClient.Farms)
            {
                Farm adaptFarm = MapFarm(myFarm, adaptGrower, catalog);
            }
            return adaptGrower;
        }

        public static Farm MapFarm(PublisherDataModel.Farm myFarm, Grower adaptGrower, Catalog catalog)
        {
            //Tranform
            Farm adaptFarm = new Farm();
            adaptFarm.Id.UniqueIds.Add(DataMapper.GetNativeID(myFarm));
            adaptFarm.Description = myFarm.Name;

            //Set any Reference IDs
            adaptFarm.GrowerId = adaptGrower.Id.ReferenceId;

            //Add the ADAPT object to its container
            catalog.Farms.Add(adaptFarm);


            foreach (PublisherDataModel.Field myField in myFarm.Fields)
            {
                Field adaptField = MapField(myField, adaptFarm, catalog);
            }
            return adaptFarm;
        }

        public static Field MapField(PublisherDataModel.Field myField, Farm adaptFarm, Catalog catalog)
        {
            //Tranform
            Field adaptField = new Field();
            adaptField.Id.UniqueIds.Add(DataMapper.GetNativeID(myField));
            adaptField.Description = myField.Name;

            //Set any Reference IDs
            adaptField.FarmId = adaptFarm.Id.ReferenceId;

            //Add the ADAPT object to its container
            catalog.Fields.Add(adaptField);

            return adaptField;
        }
    }
}
