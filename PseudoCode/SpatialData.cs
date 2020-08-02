using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpatialDataPseudoCode
{
    /// <summary>
    /// This pseudo code demonstrates how to obtain point-by-point spatial sensors values in the ADAPT LoggedData object
    /// </summary>
    public class SpatialData
    {
        public void PseudoCode()
        {
            AgGateway.ADAPT.ApplicationDataModel.ADM.ApplicationDataModel adm = null;  //A given Application Data Model
            foreach (AgGateway.ADAPT.ApplicationDataModel.LoggedData.LoggedData loggedData in adm.Documents.LoggedData)
            {
                //A logged data record with associated values
                AgGateway.ADAPT.ApplicationDataModel.Logistics.Grower grower = adm.Catalog.Growers.FirstOrDefault(g => g.Id.ReferenceId == loggedData.GrowerId.Value);
                AgGateway.ADAPT.ApplicationDataModel.Logistics.Farm farm = adm.Catalog.Farms.FirstOrDefault(f => f.Id.ReferenceId == loggedData.FarmId.Value);
                AgGateway.ADAPT.ApplicationDataModel.Logistics.Field field = adm.Catalog.Fields.FirstOrDefault(f => f.Id.ReferenceId == loggedData.FieldId.Value);

                foreach (AgGateway.ADAPT.ApplicationDataModel.LoggedData.OperationData operationData in loggedData.OperationData)
                {
                    //An individual operation for the logged data
                    foreach (int productId in operationData.ProductIds)
                    {
                        AgGateway.ADAPT.ApplicationDataModel.Products.Product product = adm.Catalog.Products.FirstOrDefault(p => p.Id.ReferenceId == productId);
                        if (product is AgGateway.ADAPT.ApplicationDataModel.Products.CropVarietyProduct)
                        {
                            AgGateway.ADAPT.ApplicationDataModel.Products.CropVarietyProduct variety = product as AgGateway.ADAPT.ApplicationDataModel.Products.CropVarietyProduct;
                            AgGateway.ADAPT.ApplicationDataModel.Products.Crop crop = adm.Catalog.Crops.FirstOrDefault(c => c.Id.ReferenceId == variety.CropId);
                        }
                    }

                    foreach (AgGateway.ADAPT.ApplicationDataModel.LoggedData.SpatialRecord record in operationData.GetSpatialRecords())
                    {
                        //Each record in the dataset
                        DateTime timestamp = record.Timestamp;
                        AgGateway.ADAPT.ApplicationDataModel.Shapes.Point point = record.Geometry as AgGateway.ADAPT.ApplicationDataModel.Shapes.Point;
                        if (point != null)
                        {
                            double latitude = point.Y;
                            double longitude = point.X;
                            if (point.Z.HasValue)
                            {
                                double elevation = point.Z.Value;
                            }
                        }

                        for (int depth = 0; depth <= operationData.MaxDepth; depth++)
                        {
                            //Each hierarchical implement recording level

                            foreach (AgGateway.ADAPT.ApplicationDataModel.Equipment.DeviceElementUse deviceElementUse in operationData.GetDeviceElementUses(depth).OrderBy(s => s.Order))
                            {
                                //Each implement/section at the given level.    Width details for the implement/section can be retrieved from the Catalog via a lookup with the deviceElementUse.DeviceConfigurationId

                                foreach (AgGateway.ADAPT.ApplicationDataModel.LoggedData.WorkingData workingData in deviceElementUse.GetWorkingDatas())
                                {
                                    //Each recorded value for the section
                                    if (workingData.Representation.Code == "vrYieldWetMass")
                                    {
                                        AgGateway.ADAPT.ApplicationDataModel.Representations.NumericRepresentationValue dataValue = record.GetMeterValue(workingData) as AgGateway.ADAPT.ApplicationDataModel.Representations.NumericRepresentationValue;
                                        AgGateway.ADAPT.ApplicationDataModel.Common.UnitOfMeasure wetMassUnits = dataValue.Value.UnitOfMeasure;
                                        double wetMass = dataValue.Value.Value;
                                    }
                                    else if (workingData.Representation.Code == "vrHarvestMoisture")
                                    {
                                        AgGateway.ADAPT.ApplicationDataModel.Representations.NumericRepresentationValue dataValue = record.GetMeterValue(workingData) as AgGateway.ADAPT.ApplicationDataModel.Representations.NumericRepresentationValue;
                                        AgGateway.ADAPT.ApplicationDataModel.Common.UnitOfMeasure moistureUnits = dataValue.Value.UnitOfMeasure;
                                        double moisture = dataValue.Value.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
