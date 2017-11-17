using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace MvcControlsToolkit.Business.DocumentDB
{
    public class DefaultDocumentDBConnection: IDocumentDBConnection
    {
        public DocumentClient Client
        {
            private set; get;
        }
        public string DatabaseId
        {
            private set; get;
        }
        public DefaultDocumentDBConnection(string endpoint, string key, string databaseId,
            ConnectionPolicy connectionPolicy = null,
            ConsistencyLevel? consistencyLevel = null)
        {
            if (string.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(databaseId)) throw new ArgumentNullException(nameof(databaseId));
            DatabaseId = databaseId;
            Client = new DocumentClient(new Uri(endpoint), key, connectionPolicy, consistencyLevel);
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
