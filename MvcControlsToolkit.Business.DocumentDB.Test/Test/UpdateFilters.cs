using System;
using System.Collections.Generic;
using System.Text;
using MvcControlsToolkit.Business.DocumentDB.Test.Data;
using MvcControlsToolkit.Core.Business.Utilities;
using Xunit;

namespace MvcControlsToolkit.Business.DocumentDB.Test
{
    [CollectionDefinition("UpdateFiltersCollection")]
    public class UpdateFiltersCollection : ICollectionFixture<DBInitializerFiltered>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
    [Collection("UpdateFiltersCollection")]
    public class UpdateFilters
    {
        ICRUDRepository Repository;
        IDocumentDBConnection Connection;
        public UpdateFilters(DBInitializerFiltered init)
        {
            Repository = init.Repository;
            Connection = init.Connection;
        }
    }
}
