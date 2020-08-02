using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.MyDataLayer.Models
{
   public class InsertedObject: BaseModel
   {
      public string Class { get; set; } = string.Empty;
      public string Name { get; set; } = string.Empty;
   }
}
