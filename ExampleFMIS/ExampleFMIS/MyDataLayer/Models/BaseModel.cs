using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleFMIS.MyDataLayer.Models
{
   public class BaseModel
   {
      public Guid ID { get; set; } = Guid.NewGuid();
   }
}
