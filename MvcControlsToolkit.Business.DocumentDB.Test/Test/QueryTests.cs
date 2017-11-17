using System;
using System.Collections.Generic;
using System.Text;
using MvcControlsToolkit.Business.DocumentDB.Test.Data;
using MvcControlsToolkit.Core.Business.Utilities;
using Xunit;

namespace MvcControlsToolkit.Business.DocumentDB.Test
{
    [CollectionDefinition("QueryCollection")]
    public class QueryCollection : ICollectionFixture<DBInitializerQuery>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
    [Collection("QueryCollection")]
    public class QueryTests
    {
        ICRUDRepository Repository;
        IDocumentDBConnection Connection;
        public QueryTests(DBInitializerQuery init)
        {
            Repository = init.Repository;
            Connection = init.Connection;
        }
    }
}
