using AgGateway.ADAPT.ApplicationDataModel.ADM;
using AgGateway.ADAPT.ApplicationDataModel.Common;
using AgGateway.ADAPT.ApplicationDataModel.Documents;
using AgGateway.ADAPT.ApplicationDataModel.Logistics;
using AgGateway.ADAPT.ApplicationDataModel.Products;
using AgGateway.ADAPT.ApplicationDataModel.Representations;
using AgGateway.ADAPT.PluginManager;
using AgGateway.ADAPT.Representation.RepresentationSystem;
using AgGateway.ADAPT.Representation.RepresentationSystem.ExtensionMethods;
using AgGateway.ADAPT.Representation.UnitSystem;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTotalsDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var testData = JArray.Parse(File.ReadAllText("data.json"));

            string applicationPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var pluginFactory = new PluginFactory(applicationPath);

            var admPlugin = pluginFactory.GetPlugin("ADMPlugin");
            admPlugin.Initialize();

            ApplicationDataModel export = new ApplicationDataModel();
            export.Catalog = new Catalog();
            export.Documents = new Documents();
            List<WorkRecord> workRecords = new List<WorkRecord>();
            List<Summary> summaries = new List<Summary>();

            // All of these records are for the same Grower/Farm/Field/CropZone/CropYear so I'm just
            // pulling the common info from the first record.

            #region Create a "crop year" TimeScope to tag each of the WorkRecords with.
            TimeScope cropYear = new TimeScope();
            UniqueId ourId = new UniqueId();
            ourId.Id = testData[0]["CropYear"].ToString();
            ourId.IdType = IdTypeEnum.String;
            ourId.Source = "www.somecompany.com";
            ourId.SourceType = IdSourceTypeEnum.URI;
            cropYear.Id.UniqueIds.Add(ourId);
            cropYear.Description = testData[0]["CropYear"].ToString();
            cropYear.DateContext = DateContextEnum.CropSeason;
            export.Catalog.TimeScopes.Add(cropYear);
            #endregion

            #region Create the Grower/Farm/Field/CropZone objects for this group of applications
            Grower grower = new Grower();
            ourId = new UniqueId();
            ourId.Id = testData[0]["ApplicationId"]["DataSourceId"].ToString();
            ourId.IdType = IdTypeEnum.UUID;
            ourId.Source = "www.somecompany.com";
            ourId.SourceType = IdSourceTypeEnum.URI;
            grower.Id.UniqueIds.Add(ourId);
            grower.Name = testData[0]["GrowerName"].ToString();
            export.Catalog.Growers.Add(grower);

            Farm farm = new Farm();
            ourId = new UniqueId();
            ourId.Id = testData[0]["FarmId"]["Id"].ToString();
            ourId.IdType = IdTypeEnum.UUID;
            ourId.Source = "www.somecompany.com";
            ourId.SourceType = IdSourceTypeEnum.URI;
            farm.Id.UniqueIds.Add(ourId);
            farm.Description = testData[0]["FarmName"].ToString();
            farm.GrowerId = grower.Id.ReferenceId;
            export.Catalog.Farms.Add(farm);

            Field field = new Field();
            ourId = new UniqueId();
            ourId.Id = testData[0]["FieldId"].ToString();
            ourId.IdType = IdTypeEnum.UUID;
            ourId.Source = "www.somecompany.com";
            ourId.SourceType = IdSourceTypeEnum.URI;
            field.Id.UniqueIds.Add(ourId);
            field.Description = testData[0]["FieldName"].ToString();
            field.FarmId = farm.Id.ReferenceId;
            export.Catalog.Fields.Add(field);

            Crop crop = new Crop();
            ourId = new UniqueId();
            ourId.Id = testData[0]["CropId"]["Id"].ToString();
            ourId.IdType = IdTypeEnum.UUID;
            ourId.Source = "www.somecompany.com";
            ourId.SourceType = IdSourceTypeEnum.URI;
            crop.Id.UniqueIds.Add(ourId);
            crop.Name = testData[0]["Crop"].ToString();
            // Add EPPO code as ContextItem at some point in the future
            export.Catalog.Crops.Add(crop);

            CropZone cropZone = new CropZone();
            ourId = new UniqueId();
            ourId.Id = testData[0]["CropZoneId"].ToString();
            ourId.IdType = IdTypeEnum.UUID;
            ourId.Source = "www.somecompany.com";
            ourId.SourceType = IdSourceTypeEnum.URI;
            cropZone.Id.UniqueIds.Add(ourId);
            cropZone.Description = testData[0]["CropZoneName"].ToString();
            cropZone.FieldId = field.Id.ReferenceId;
            cropZone.CropId = crop.Id.ReferenceId;
            cropZone.TimeScopes.Add(cropYear);
            string areaString = testData[0]["AreaApplied"].ToString();
            double area = Convert.ToDouble(areaString);
            cropZone.Area = new NumericRepresentationValue(RepresentationInstanceList.vrReportedFieldArea.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("ac1"), area));
            export.Catalog.CropZones.Add(cropZone);
            #endregion

            // Foreach Application
            var applicationIds = ((from c in testData select c["ApplicationId"]["Id"]).Distinct()).ToList();
            foreach (var applicationId in applicationIds)
            {
                var appliedProducts = (from c in testData where (string)c["ApplicationId"]["Id"] == applicationId.ToString() select c).ToList();

                // Create a WorkRecord and Summary (ADAPT's version of an Application)
                WorkRecord workRecord = new WorkRecord();
                ourId = new UniqueId();
                ourId.Id = appliedProducts[0]["ApplicationId"]["Id"].ToString();
                ourId.IdType = IdTypeEnum.UUID;
                ourId.Source = "www.somecompany.com";
                ourId.SourceType = IdSourceTypeEnum.URI;
                workRecord.Id.UniqueIds.Add(ourId);
                workRecord.Description = appliedProducts[0]["ApplicationName"].ToString();
                workRecord.TimeScopes.Add(cropYear);

                TimeScope timingEvent = new TimeScope();
                timingEvent.DateContext = DateContextEnum.TimingEvent;
                timingEvent.Description = appliedProducts[0]["TimingEvent"].ToString();
                workRecord.TimeScopes.Add(timingEvent);

                TimeScope startDate = new TimeScope();
                startDate.DateContext = DateContextEnum.ActualStart;
                startDate.TimeStamp1 = DateTime.Parse(appliedProducts[0]["StartDate"].ToString());
                workRecord.TimeScopes.Add(startDate);

                TimeScope endDate = new TimeScope();
                endDate.DateContext = DateContextEnum.ActualEnd;
                endDate.TimeStamp1 = DateTime.Parse(appliedProducts[0]["EndDate"].ToString());
                workRecord.TimeScopes.Add(endDate);

                Summary summary = new Summary();
                summary.WorkRecordId = workRecord.Id.ReferenceId;
                summary.GrowerId = grower.Id.ReferenceId;
                summary.FarmId = farm.Id.ReferenceId;
                summary.FieldId = field.Id.ReferenceId;
                summary.CropZoneId = cropZone.Id.ReferenceId;

                // Foreach Product
                foreach (var appliedProduct in appliedProducts)
                {
                    //Note that Manufacturer is not a required property for a given product.
                    Manufacturer manufacturer = null;
                    var manufacturers = export.Catalog.Manufacturers.Where(x => (x.Description == appliedProduct["Manufacturer"].ToString())).ToList();
                    if (manufacturers.Count > 0)
                        manufacturer = manufacturers[0];
                    else
                    {
                        manufacturer = new Manufacturer();
                        ourId = new UniqueId();
                        // Couldn't find Manufacturer id in your data
                        ourId.Id = "00000000-0000-0000-0000-000000000000";
                        ourId.IdType = IdTypeEnum.UUID;
                        ourId.Source = "www.somecompany.com";
                        ourId.SourceType = IdSourceTypeEnum.URI;
                        manufacturer.Id.UniqueIds.Add(ourId);
                        manufacturer.Description = appliedProduct["Manufacturer"].ToString();
                        export.Catalog.Manufacturers.Add(manufacturer);
                    }

                    // This is sub-optimal, but it is what we have to work with at the moment.
                    // We're creating the use of each product as its own "operation"
                    OperationSummary operation = new OperationSummary();
                    operation.Data = new List<StampedMeteredValues>();
                    if (appliedProduct["Type"].ToString() == "Seed")
                    {
                        #region Handle Seed
                        CropVarietyProduct cropVariety = null;
                        var products = export.Catalog.Products.Where(x => (x.Description == appliedProduct["Product"].ToString())).ToList();
                        if (products.Count > 0)
                            cropVariety = products[0] as CropVarietyProduct;
                        else
                        {
                            cropVariety = new CropVarietyProduct();
                            ourId = new UniqueId();
                            ourId.Id = appliedProduct["ProductId"]["Id"].ToString();
                            ourId.IdType = IdTypeEnum.UUID;
                            ourId.Source = "www.somecompany.com";
                            ourId.SourceType = IdSourceTypeEnum.URI;
                            cropVariety.Id.UniqueIds.Add(ourId);
                            cropVariety.Description = appliedProduct["Product"].ToString();
                            cropVariety.CropId = crop.Id.ReferenceId;
                            if (manufacturer != null)
                                cropVariety.ManufacturerId = manufacturer.Id.ReferenceId;
                            export.Catalog.Products.Add(cropVariety);
                        }
                        operation.ProductId = cropVariety.Id.ReferenceId;
                        operation.OperationType = OperationTypeEnum.SowingAndPlanting;
                        #endregion
                    }
                    else if (appliedProduct["Type"].ToString() == "CropProtection")
                    {
                        #region Handle CropProtection
                        CropProtectionProduct cropProtection = null;
                        var products = export.Catalog.Products.Where(x => (x.Description == appliedProduct["Product"].ToString())).ToList();
                        if (products.Count > 0)
                            cropProtection = products[0] as CropProtectionProduct;
                        else
                        {
                            cropProtection = new CropProtectionProduct();
                            ourId = new UniqueId();
                            ourId.Id = appliedProduct["ProductId"]["Id"].ToString();
                            ourId.IdType = IdTypeEnum.UUID;
                            ourId.Source = "www.somecompany.com";
                            ourId.SourceType = IdSourceTypeEnum.URI;
                            cropProtection.Id.UniqueIds.Add(ourId);
                            cropProtection.Description = appliedProduct["Product"].ToString();
                            if (manufacturer != null)
                                cropProtection.ManufacturerId = manufacturer.Id.ReferenceId;
                            if (!string.IsNullOrEmpty(appliedProduct["RegNo"].ToString()))
                            {
                                ContextItem epaNumber = new ContextItem();
                                epaNumber.Code = "US-EPA-N";
                                epaNumber.Value = ConditionEPA(appliedProduct["RegNo"].ToString(), true, true);
                                cropProtection.ContextItems.Add(epaNumber);
                            }
                            export.Catalog.Products.Add(cropProtection);
                        }
                        operation.ProductId = cropProtection.Id.ReferenceId;
                        operation.OperationType = OperationTypeEnum.CropProtection;
                        #endregion
                    }
                    else if (appliedProduct["Type"].ToString() == "Fertilizer")
                    {
                        #region Handle Fertilizer
                        CropNutritionProduct cropNutrition = null;
                        var products = export.Catalog.Products.Where(x => (x.Description == appliedProduct["Product"].ToString())).ToList();
                        if (products.Count > 0)
                            cropNutrition = products[0] as CropNutritionProduct;
                        else
                        {
                            cropNutrition = new CropNutritionProduct();
                            ourId = new UniqueId();
                            ourId.Id = appliedProduct["ProductId"]["Id"].ToString();
                            ourId.IdType = IdTypeEnum.UUID;
                            ourId.Source = "www.somecompany.com";
                            ourId.SourceType = IdSourceTypeEnum.URI;
                            cropNutrition.Id.UniqueIds.Add(ourId);
                            cropNutrition.Description = appliedProduct["Product"].ToString();
                            if (manufacturer != null)
                                cropNutrition.ManufacturerId = manufacturer.Id.ReferenceId;
                            export.Catalog.Products.Add(cropNutrition);
                        }
                        operation.ProductId = cropNutrition.Id.ReferenceId;
                        operation.OperationType = OperationTypeEnum.Fertilizing;
                        #endregion
                    }

                    StampedMeteredValues smv = new StampedMeteredValues();
                    MeteredValue mv = null;

                    NumericRepresentationValue rateValue = null;
                    #region Set the product rate (currently hardcoded to be per acre)
                    switch (appliedProduct["RateUnit"].ToString())
                    {
                        case ("seed"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrSeedRateSeedsActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("seeds1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("kernel"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrSeedRateSeedsActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("seeds1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("short ton"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateMassActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("ton1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("metric ton"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateMassActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("t1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("pound"):
                            if (appliedProduct["Type"].ToString() == "Seed")
                                rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrSeedRateMassActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("lb1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            else
                                rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateMassActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("lb1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("ounce"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateMassActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("oz1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("kilogram"):
                            if (appliedProduct["Type"].ToString() == "Seed")
                                rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrSeedRateMassActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("kg1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            else
                                rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateMassActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("kg1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("gram"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateMassActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("g1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("fluid ounce"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateVolumeActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("floz1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("quart"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateVolumeActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("qt1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("pint"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateVolumeActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("pt1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("milliliter"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateVolumeActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("ml1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("liter"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateVolumeActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("l1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("gallon"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateVolumeActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("gal1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("centiliter"):
                            rateValue = new NumericRepresentationValue(RepresentationInstanceList.vrAppRateVolumeActual.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("cl1ac-1"), Convert.ToDouble(appliedProduct["RateValue"].ToString())));
                            break;
                        case ("acre"):
                            break;
                        default:
                            break;
                    }
                    if (rateValue != null)
                    {
                        mv = new MeteredValue();
                        mv.Value = rateValue;
                        smv.Values.Add(mv);
                    }
                    #endregion

                    // Set the "applied area" for this use of the product (currently hardcoded to be in acres)
                    NumericRepresentationValue areaValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalAreaCovered.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("ac1"), Convert.ToDouble(appliedProduct["AreaApplied"].ToString())));
                    mv = new MeteredValue();
                    mv.Value = areaValue;
                    smv.Values.Add(mv);

                    if (!string.IsNullOrEmpty(appliedProduct["ApplicationMethod"].ToString()))
                    {
                        EnumeratedValue applicationMethod = null;
                        #region Set the product application method
                        switch (appliedProduct["ApplicationMethod"].ToString())
                        {
                            case ("Aerial"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiAerial.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Air Blast"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiAirBlast.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Chemigation"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiChemigation.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Fertigation"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiFertigation.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Ground - Banded"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiBand.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Ground - Broadcast"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiBroadcast.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Ground - Hooded"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiHoodedSprayer.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Ground - In Furrow"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiInFurrow.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Ground Application"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiInGround.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Planting"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiPlanter.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Re-Planting"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiPlanter.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Sidedress"):
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiSideDress.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                            case ("Fumigation"):
                            case ("Ground - Incorporated"):
                            case ("Ground - Seed Treatment"):
                            case ("Ground - Variable Rate"):
                            case ("Storage"):
                            case ("Topdress"):
                            case ("Tree Injection"):
                            case ("Water Run"):
                            default:
                                applicationMethod = new EnumeratedValue { Value = DefinedTypeEnumerationInstanceList.dtiInGround.ToModelEnumMember() };
                                applicationMethod.Representation = RepresentationInstanceList.dtApplicationMethod.ToModelRepresentation();
                                break;
                        }
                        if (applicationMethod != null)
                        {
                            mv = new MeteredValue();
                            mv.Value = applicationMethod;
                            smv.Values.Add(mv);
                        }
                        #endregion
                    }

                    // There is a problem here handling seed totals by bag....will have to come back to this at some point
                    NumericRepresentationValue totalProductValue = null;
                    #region Set the total product
                    switch (appliedProduct["TotalProductUnit"].ToString())
                    {
                        case ("seed"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalSeedQuantityAppliedSeed.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("seeds"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("kernel"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalSeedQuantityAppliedSeed.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("seeds"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("short ton"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedMass.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("ton"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("metric ton"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedMass.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("t"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("pound"):
                            if (appliedProduct["Type"].ToString() == "Seed")
                                totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalSeedQuantityAppliedMass.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("lb"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            else
                                totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedMass.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("lb"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("ounce"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedMass.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("oz"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("kilogram"):
                            if (appliedProduct["Type"].ToString() == "Seed")
                                totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalSeedQuantityAppliedMass.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("kg"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            else
                                totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedMass.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("kg"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("gram"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedMass.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("g1ac-1"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("fluid ounce"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedVolume.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("floz"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("quart"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedVolume.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("qt"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("pint"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedVolume.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("pt"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("milliliter"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedVolume.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("ml"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("liter"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedVolume.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("l"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("gallon"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedVolume.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("gal"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("centiliter"):
                            totalProductValue = new NumericRepresentationValue(RepresentationInstanceList.vrTotalQuantityAppliedVolume.ToModelRepresentation(), new NumericValue(UnitSystemManager.GetUnitOfMeasure("cl"), Convert.ToDouble(appliedProduct["TotalProductQty"].ToString())));
                            break;
                        case ("acre"):
                            break;
                        default:
                            break;
                    }
                    if (totalProductValue != null)
                    {
                        mv = new MeteredValue();
                        mv.Value = totalProductValue;
                        smv.Values.Add(mv);
                    }
                    #endregion

                    operation.Data.Add(smv);

                    // Add the OperationSummary to the collection in Summary
                    summary.OperationSummaries.Add(operation);
                    // End - Foreach Product
                }
            // Add this Summary to the list
                summaries.Add(summary);

            // Add the WorkRecord to the list
                workRecords.Add(workRecord);
                // End - Foreach Application
            }

            // This property is an IEnumerable so we had to build up the collection in a local list then assign it 
            export.Documents.Summaries = summaries;
            // This property is an IEnumerable so we had to build up the collection in a local list then assign it 
            export.Documents.WorkRecords = workRecords;
            // Make sure the target directory exits
            string outputPath = applicationPath + @"\Output";
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
            admPlugin.Export(export, outputPath);
         }

        static public string ConditionEPA(string source, bool strip, bool truncate)
        {
            if (string.IsNullOrEmpty(source))
                return (source);
            string newSource = "";
            string[] sourceParts = source.Split(new Char[] { '-' });
            if (sourceParts.Length < 2)
                return (null);
            else
            {
                if (strip)
                {
                    for (int i = 0; i < sourceParts.Length; i++)
                    {
                        bool nonZeroFound = false;
                        string segment = sourceParts[i];
                        string buff = string.Empty;
                        foreach (char c in sourceParts[i])
                        {
                            if (!((Char.IsLetterOrDigit(c)) || (c == '#')))
                                continue;
                            if ((!nonZeroFound) && (c == '0'))
                                continue;
                            else
                            {
                                nonZeroFound = true;
                                buff += c;
                            }
                        }
                        sourceParts[i] = buff;
                    }
                }
            }
            // Reassemble
            if (truncate)
                newSource = sourceParts[0] + "-" + sourceParts[1];
            else
            {
                for (int i = 0; i < sourceParts.Length; i++)
                {
                    if (i != (sourceParts.Length - 1))
                        newSource += sourceParts[i] + "-";
                    else
                        newSource += sourceParts[i];
                }
            }
            return (newSource);
        }

    }
}
