using System;
using System.Collections.Generic;
using System.Text;
using MvcControlsToolkit.Core.DataAnnotations;
using Newtonsoft.Json;

namespace MvcControlsToolkit.Business.DocumentDB.Test.Models
{
    public class Item
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "subItems"), CollectionKey("Id")]
        public ICollection<Item> SubItems { get; set; }

        [JsonProperty(PropertyName = "assignedTo")]
        public Person AssignedTo { get; set; }

        [JsonProperty(PropertyName = "isComplete")]
        public bool Completed { get; set; }
    }

    public class PItem
    {
        [CombinedKey, JsonIgnore]
        public string Id {
            get {
                return
                DocumentDBCRUDRepository<PItem>.DefaultCombinedKey(Id, Name);   
            }
            set {
                string id, p;
                DocumentDBCRUDRepository<PItem>.DefaultSplitCombinedKey(value, out id, out p);
                Key = id; Name = p;
            } }

        [JsonProperty(PropertyName = "id")]
        public string Key { get; set; }
        [JsonProperty(PropertyName = "name"), PartitionKey]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "subItems"), CollectionKey("Id")]
        public ICollection<Item> SubItems { get; set; }

        [JsonProperty(PropertyName = "assignedTo")]
        public Person AssignedTo { get; set; }

        [JsonProperty(PropertyName = "isComplete")]
        public bool Completed { get; set; }
    }
}
