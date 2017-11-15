using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace MvcControlsToolkit.Business.DocumentDB
{
    public interface IDocumentDBConnection
    {
        DocumentClient Client
        {
              get;
        }
        string DatabaseId
        {
              get;
        }
    }
}
