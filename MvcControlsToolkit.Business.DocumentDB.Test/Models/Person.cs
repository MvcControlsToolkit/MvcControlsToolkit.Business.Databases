using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MvcControlsToolkit.Business.DocumentDB.Test.Models
{
    public class Person
    {
        [JsonProperty(PropertyName = "name")]
        public string Name{ get; set; }
        [JsonProperty(PropertyName = "surname")]
        public string Surname { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
