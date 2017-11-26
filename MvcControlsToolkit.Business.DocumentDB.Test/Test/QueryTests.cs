using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using MvcControlsToolkit.Business.DocumentDB.Test.Data;
using MvcControlsToolkit.Business.DocumentDB.Test.DTOs;
using MvcControlsToolkit.Core.Business.Utilities;
using Xunit;
using MvcControlsToolkit.Business.DocumentDB.Test.Models;

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
        ICRUDRepository repository;
        IDocumentDBConnection connection;
        public QueryTests(DBInitializerQuery init)
        {
            repository = init.Repository;
            connection = init.Connection;
        }
        [Fact]
        public async Task SimpleSelect()
        {
            var res = await repository.GetPage<MainItemDTO>(null ,m => m.OrderByDescending( l => l.Name), 1, 3);

            Assert.NotNull(res);
            Assert.Equal(res.TotalCount, 4);
            Assert.Equal(res.TotalPages, 2);
            Assert.Equal(res.Data.Count(), 3);

            var first = res.Data.FirstOrDefault();

            Assert.Equal(first.Name, "Name3");
            Assert.Equal(first.Description, "Description3");
            Assert.Equal(first.AssignedToSurname, "Abbruzzese");

            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName3");
            Assert.Equal(firstChild.Description, "ChildrenDescription3");
            

        }
        [Fact]
        public async Task PagedSelect()
        {
            var res = await repository.GetPage<MainItemDTO>(null, m => m.OrderByDescending(l => l.Name), 2, 3);

            Assert.NotNull(res);
            Assert.Equal(res.TotalCount, 4);
            Assert.Equal(res.TotalPages, 2);
            Assert.Equal(res.Data.Count(), 1);

            var first = res.Data.FirstOrDefault();

            Assert.Equal(first.Name, "Name0");
            Assert.Equal(first.Description, "Description0");
            

            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName0");
            Assert.Equal(firstChild.Description, "ChildrenDescription0");


        }

        [Fact]
        public async Task Where()
        {
            var res = await repository.GetPage<MainItemDTO>(m => m.Name == "Name0", m => m.OrderByDescending(l => l.Name), 1, 3);

            Assert.NotNull(res);
            Assert.Equal(res.TotalCount, 1);
            Assert.Equal(res.TotalPages, 1);
            Assert.Equal(res.Data.Count(), 1);

            var first = res.Data.FirstOrDefault();

            Assert.Equal(first.Name, "Name0");
            Assert.Equal(first.Description, "Description0");


            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName0");
            Assert.Equal(firstChild.Description, "ChildrenDescription0");


        }
        [Fact]
        public async Task TableAndList()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.ToList<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name).Take(3));
            Assert.NotNull(res);

            Assert.Equal(res.Count(), 3);

            var first = res.FirstOrDefault();

            Assert.Equal(first.Name, "Name3");
            Assert.Equal(first.Description, "Description3");
            Assert.Equal(first.AssignedToSurname, "Abbruzzese");

            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName3");
            Assert.Equal(firstChild.Description, "ChildrenDescription3");


        }

        [Fact]
        public async Task TableAndSequence()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.ToSequence<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            Assert.NotNull(res);

            Assert.Equal(res.Data.Count(), 3);

            var first = res.Data.FirstOrDefault();

            Assert.Equal(first.Name, "Name3");
            Assert.Equal(first.Description, "Description3");
            Assert.Equal(first.AssignedToSurname, "Abbruzzese");

            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName3");
            Assert.Equal(firstChild.Description, "ChildrenDescription3");

            res = await rep.ToSequence<MainItemDTO>(rep.Table(3, null, res.Continuation).OrderByDescending(m => m.Name));

            Assert.NotNull(res);

            Assert.Equal(res.Data.Count(), 1);

            first = res.Data.FirstOrDefault();

            Assert.Equal(first.Name, "Name0");
            Assert.Equal(first.Description, "Description0");
            

            Assert.Equal(first.SubItems.Count(), 4);
            firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName0");
            Assert.Equal(firstChild.Description, "ChildrenDescription0");
        }

        [Fact]
        public async Task TableFirstOrDefault()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            Assert.NotNull(res);

           

            var first = res;

            Assert.Equal(first.Name, "Name3");
            Assert.Equal(first.Description, "Description3");
            Assert.Equal(first.AssignedToSurname, "Abbruzzese");

            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName3");
            Assert.Equal(firstChild.Description, "ChildrenDescription3");

            
        }
        [Fact]
        public async Task SimpleGetById()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));


            var first = await repository.GetById<MainItemDTO, string>(res.Id);

            Assert.Equal(first.Name, "Name3");
            Assert.Equal(first.Description, "Description3");
            Assert.Equal(first.AssignedToSurname, "Abbruzzese");

            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName3");
            Assert.Equal(firstChild.Description, "ChildrenDescription3");


        }
    }
}
