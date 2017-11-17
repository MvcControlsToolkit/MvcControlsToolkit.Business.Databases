using System;
using System.Collections.Generic;
using System.Text;
using MvcControlsToolkit.Business.DocumentDB.Test.Data;
using MvcControlsToolkit.Core.Business.Utilities;
using Xunit;
namespace MvcControlsToolkit.Business.DocumentDB.Test
{
    [CollectionDefinition("UpdateCollection")]
    public class UpdateCollection : ICollectionFixture<DBInitializer>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
    [Collection("UpdateCollection")]
    public class UpdateTests
    {
        ICRUDRepository Repository;
        IDocumentDBConnection Connection;
        public UpdateTests(DBInitializerQuery init)
        {
            Repository = init.Repository;
            Connection = init.Connection;
        }
    }
}
