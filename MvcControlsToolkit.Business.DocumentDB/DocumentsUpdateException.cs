using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MvcControlsToolkit.Business.DocumentDB
{
    public class UpdateOperationsStatus<M>
    {
        public ConcurrentBag<Tuple<string, object>> Deletes { get; set; }
            = new ConcurrentBag<Tuple<string, object>>();
        public ConcurrentBag<M> Additions { get; set; }
            = new ConcurrentBag<M>();
        public ConcurrentBag<M> FullUpdates { get; set; }
            = new ConcurrentBag<M>();
        public ConcurrentBag<Tuple<Action<M>, string, object>> PartialUpdates { get; set; }
            = new ConcurrentBag<Tuple<Action<M>, string, object>>();
    }
    public class DocumentsUpdateException<M>: AggregateException
    {
        public UpdateOperationsStatus<M> Failed { get; private set; }
        public UpdateOperationsStatus<M> Succeeded { get; private set; }

        public DocumentsUpdateException(ConcurrentBag<Exception> innerExceptions,
            UpdateOperationsStatus<M> failed,
            UpdateOperationsStatus<M> succeeded):base(innerExceptions)
        {
            Succeeded = succeeded;
            Failed = failed; 
        }
    }
}
