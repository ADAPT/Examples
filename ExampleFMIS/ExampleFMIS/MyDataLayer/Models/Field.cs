using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.MyDataLayer.Models
{
   public class Field : BaseModel
   {
      public string Name { get; set; } = string.Empty;
      public Guid FarmID { get; set; } = Guid.Empty;
   }
}
