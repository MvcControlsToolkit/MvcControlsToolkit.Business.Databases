using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MvcControlsToolkit.Business.DocumentDB
{
    public class DocumentsUpdateSimulationResult<M>
    {
        public ConcurrentBag<M> Updates { get; set; } = new ConcurrentBag<M>();
        public ConcurrentBag<M> Additions { get; set; } = new ConcurrentBag<M>();
        public ConcurrentBag<Tuple<string, object>> Deletes { get; set; } = new ConcurrentBag<Tuple<string, object>>();
    }
}
