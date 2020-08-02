using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.MyDataLayer.Models
{
   public class ExternalData : BaseModel
   {
      public string ModelName { get; set; } = string.Empty;
      public Guid MyId { get; set; } = Guid.Empty;
      public List<ExternalEntity> ExternalEntities { get; set; } = new List<ExternalEntity>();
   }

   public class ExternalEntity
   {
      public string Id { get; set; }

      public IdTypeEnum IdType { get; set; }

      public string Source { get; set; }

      public IdSourceTypeEnum? SourceType { get; set; }

   }

   public enum IdTypeEnum
   {
      UUID,
      String,
      LongInt,
      URI
   }
   public enum IdSourceTypeEnum
   {
      GLN,
      URI
   }
}
