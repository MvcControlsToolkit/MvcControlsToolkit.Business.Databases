using System;
using System.Collections.Generic;
using System.Text;

namespace MvcControlsToolkit.Business.DocumentDB.Test.DTOs
{
    public class SubItemDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class SubItemDTOPartition: SubItemDTO
    {
        
    }
}
