using System;
using System.Collections.Generic;
using System.Text;

namespace ExamplePlugin.PublisherDataModel
{

    public class CropData
    {
        public CropData()
        {
            Crops = new List<Crop>();
            CropAssignments = new List<CropAssignment>();
        }

        public List<Crop> Crops { get; set; }
        public List<CropAssignment> CropAssignments { get; set; }
    }

    public class Crop : BaseObject
    {
    }

    public class CropAssignment
    {
        public Guid CropID { get; set; }
        public Guid FieldID { get; set; }
        public int GrowingSeason { get; set; }
    }
}
