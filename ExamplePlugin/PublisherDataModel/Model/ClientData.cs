using System;
using System.Collections.Generic;
using System.Text;

namespace ExamplePlugin.PublisherDataModel
{
    /// <summary>
    /// An individual customer of the data publisher
    /// </summary>
    public class Client : BaseObject
    {
        public Client() { Farms = new List<Farm>(); }
        public List<Farm> Farms { get; set; }
    }

    /// <summary>
    /// An organizational unit of the customer's operation
    /// </summary>
    public class Farm : BaseObject
    {
        public Farm() { Fields = new List<Field>(); }
        public List<Field> Fields { get; set; }
    }

    /// <summary>
    /// An individual field within the operation
    /// </summary>
    public class Field : BaseObject
    {
    }
}
