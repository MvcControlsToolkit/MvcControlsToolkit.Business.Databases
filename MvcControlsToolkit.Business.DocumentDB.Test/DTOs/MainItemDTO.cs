using System;
using System.Collections.Generic;
using System.Text;

namespace MvcControlsToolkit.Business.DocumentDB.Test.DTOs
{
    public class MainItemDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<SubItemDTO> SubItems { get; set; }

        

        public string  AssignedToSurname { get; set; }

        public string AssignedToId { get; set; }

    }
    public class MainItemDTOPartition: MainItemDTO
    {
        public string CombinedId { get; set; }

    }
}
