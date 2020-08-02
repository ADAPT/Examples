using ExampleFMIS.MyDataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.MyDataLayer
{
   public sealed class MyDataManager
   {
      private static readonly Lazy<MyDataManager> _instance = new Lazy<MyDataManager>(() => new MyDataManager());

      private List<Crop> Crops { get; } = new List<Crop>();
      private List<ExternalData> ExternalData { get; } = new List<ExternalData>();
      private List<Farm> Farms { get; } = new List<Farm>();
      private List<Field> Fields { get; } = new List<Field>();
      private List<ManagementZone> ManagementZones { get; } = new List<ManagementZone>();
      private List<OperatingUnit> OperatingUnits { get; } = new List<OperatingUnit>();
      public List<InsertedObject> InsertedObects { get; } = new List<InsertedObject>();

      public static MyDataManager Instance => _instance.Value;
      private MyDataManager()
      {
      }

      public void InsertExternalEntities(string modelName, Guid myId, List<ExternalEntity> entities)
      {
         var data = ExternalData.Where(d => d.MyId == myId && d.ModelName == modelName).FirstOrDefault();
         if (data == null)
         {
            data = new ExternalData();
            data.MyId = myId;
            data.ModelName = modelName;
         }
         for(int i=0; i<entities.Count; i++)
         {
            if( data.ExternalEntities.Where(e => e.Id == entities[i].Id && e.Source == entities[i].Source).Count() == 0 )
            {
               data.ExternalEntities.Add(entities[i]);
            }
         }
         ExternalData.Add(data);
      }

      #region ManagementZones
      public ManagementZone GetManagementZone(Guid? myId)
      {
         return ManagementZones.Where(z => z.ID == myId)
                                       .FirstOrDefault();
      }

      public ManagementZone GetManagementZone(List<ExternalEntity> entities)
      {
         foreach (var entity in entities)
         {
            var mgList = ExternalData.Where(d => d.ModelName =="ManagementZone").ToList();
            var myId = mgList.Where(d => d.ExternalEntities.Where(e => e.Id == entity.Id && 
                                                                       e.Source == entity.Source) 
                                                           .FirstOrDefault() != null)
                             .Select(d => d.MyId)
                             .FirstOrDefault();
            if (myId != null)
               return GetManagementZone(myId);
         }
         return null;
      }

      public void InsertManagementZone( ManagementZone myManagementZone)
      {
         ManagementZones.Add(myManagementZone);
         AddToInsertedObjects("ManagementZones", myManagementZone.Name);
      }

      public void RemoveAllManagementZones()
      {
         ManagementZones.Clear();
         InsertedObects.Clear();
      }

      #endregion ManagementZones

      #region Crops
      public Crop GetCrop(Guid? myId)
      {
         return Crops.Where(c => c.ID == myId).FirstOrDefault();
      }
      public Crop GetCrop(string name)
      {
         return Crops.Where(c => string.Compare(c.Name, name, true) == 0).FirstOrDefault();
      }
      public Crop GetCrop(List<ExternalEntity> entities)
      {
         foreach (var entity in entities)
         {
            var mgList = ExternalData.Where(d => d.ModelName == "Crop").ToList();
            var myId = mgList.Where(d => d.ExternalEntities.Where(e => e.Id == entity.Id && e.Source == entity.Source)
                                                           .FirstOrDefault() != null)
                             .Select(d => d.MyId)
                             .FirstOrDefault();
            if (myId != null)
               return GetCrop(myId);
         }
         return null;
      }

      public void InsertCrop(Crop myCrop)
      {
         Crops.Add(myCrop);
         AddToInsertedObjects("Crops", myCrop.Name);
      }
      #endregion Crops

      #region Fields
      public Field GetField(Guid? myId)
      {
         return Fields.Where(f => f.ID == myId).FirstOrDefault();
      }

      public Field GetField(string name)
      {
         return Fields.Where(f => string.Compare(f.Name,name,true)==0).FirstOrDefault();
      }

      public Field GetField(List<ExternalEntity> entities)
      {
         foreach (var entity in entities)
         {
            var mgList = ExternalData.Where(d => d.ModelName == "Field").ToList();
            var myId = mgList.Where(d => d.ExternalEntities.Where(e => e.Id == entity.Id && e.Source == entity.Source)
                                                .FirstOrDefault() != null)
                             .Select(d => d.MyId)
                             .FirstOrDefault();
            if (myId != null)
               return GetField(myId);
         }
         return null;
      }

      public void InsertField(Field myField)
      {
         Fields.Add(myField);
         AddToInsertedObjects("Fields", myField.Name);
      }
      #endregion Fields

      #region Farms
      public Farm GetFarm(Guid? myId)
      {
         return Farms.Where(f => f.ID == myId).FirstOrDefault();
      }

      public Farm GetFarm(string name)
      {
         return Farms.Where(f => string.Compare(f.Name, name, true) == 0).FirstOrDefault();
      }

      public Farm GetFarm(List<ExternalEntity> entities)
      {
         foreach (var entity in entities)
         {
            var mgList = ExternalData.Where(d => d.ModelName == "Farm").ToList();
            var myId = mgList.Where(d => d.ExternalEntities.Where(e => e.Id == entity.Id && e.Source == entity.Source)
                                                .FirstOrDefault() != null)
                             .Select(d => d.MyId)
                             .FirstOrDefault();
            if (myId != null)
               return GetFarm(myId);
         }
         return null;
      }

      public void InsertFarm(Farm myFarm)
      {
         Farms.Add(myFarm);
         AddToInsertedObjects("Farms", myFarm.Name);
      }
      #endregion Farms

      #region OperatingUnits
      public OperatingUnit GetOperatingUnit(Guid? myId)
      {
         return OperatingUnits.Where(f => f.ID == myId).FirstOrDefault();
      }

      public OperatingUnit GetOperatingUnit(string name)
      {
         return OperatingUnits.Where(f => string.Compare(f.Name, name, true) == 0).FirstOrDefault();
      }

      public OperatingUnit GetOperatingUnit(List<ExternalEntity> entities)
      {
         foreach (var entity in entities)
         {
            var mgList = ExternalData.Where(d => d.ModelName == "OperatingUnit").ToList();
            var myId = mgList.Where(d => d.ExternalEntities.Where(e => e.Id == entity.Id && e.Source == entity.Source)
                                                .FirstOrDefault() != null)
                             .Select(d => d.MyId)
                             .FirstOrDefault();
            if (myId != null)
               return GetOperatingUnit(myId);
         }
         return null;
      }

      public void InsertOperatingUnit(OperatingUnit myOperatingUnit)
      {
         OperatingUnits.Add(myOperatingUnit);
         AddToInsertedObjects("OperatingUnits", myOperatingUnit.Name);
      }
      #endregion OperatingUnits

      private void AddToInsertedObjects(string type, string name)
      {
         var obj = new InsertedObject()
         {
            Class = type,
            Name = name
         };
         InsertedObects.Add(obj);
      }
   }
}
