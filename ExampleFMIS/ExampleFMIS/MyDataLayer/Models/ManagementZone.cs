using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.MyDataLayer.Models
{
   public class ManagementZone: BaseModel
   {
      public string Name { get; set; } = string.Empty;
      public decimal Acres { get; set; } = 0M;
      public Guid CropId { get; set; } = Guid.Empty;
      public Guid FieldId { get; set; } = Guid.Empty;
      public int ProductionYear { get; set; } = 0;
   }
}
