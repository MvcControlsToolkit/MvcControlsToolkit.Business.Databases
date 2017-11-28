using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MvcControlsToolkit.Business.DocumentDB.Test.Data;
using MvcControlsToolkit.Business.DocumentDB.Test.DTOs;
using MvcControlsToolkit.Business.DocumentDB.Test.Models;
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
        ICRUDRepository repository;
        IDocumentDBConnection connection;
        public UpdateTests(DBInitializer init)
        {
            repository = init.Repository;
            connection = init.Connection;
        }
        [Fact]
        public async Task SimpleAdd()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;
            MainItemDTO toAdd;
            repository.Add<MainItemDTO>(true, toAdd = new MainItemDTO
            {
                Id = Guid.NewGuid().ToString(),
                Name = "AddedName",
                Description = "AddedDescription",
                AssignedToId = "AddedOwner",
                AssignedToSurname = "AddedOwnerSurname",
                SubItems = new SubItemDTO[1]
                {
                    new SubItemDTO
                    {
                        Name="AddedChildName",
                        Id = Guid.NewGuid().ToString(),
                        Description="AddedChildDescription"
                    }
                }
            });
            await repository.SaveChanges();
            var item = await repository.GetById<MainItemDTO, string>(toAdd.Id);
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            
            Assert.Equal(item.AssignedToId, toAdd.AssignedToId);
            Assert.Equal(item.AssignedToSurname, toAdd.AssignedToSurname);

            Assert.NotNull(item.SubItems);
            Assert.Equal(item.SubItems.Count(), 1);

            var cfirst = toAdd.SubItems.FirstOrDefault();
            var first = item.SubItems.FirstOrDefault();

            Assert.Equal(cfirst.Name, first.Name);
            Assert.Equal(cfirst.Id, first.Id);
            Assert.Equal(cfirst.Description, first.Description);
        }

        [Fact]
        public async Task SimplePartialUpdate()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTO toAdd = res;

            toAdd.Name = "AddedName";
            toAdd.Description = "AddedDescription";
            toAdd.AssignedToSurname = "AddedSurname";
            var fc = toAdd.SubItems.FirstOrDefault();
            toAdd.SubItems = toAdd.SubItems.Where(m => m.Id != fc.Id).ToList();
            fc = toAdd.SubItems.FirstOrDefault();
            fc.Name = "NameModified";
            var tmp = toAdd.SubItems.ToList();
            tmp.Add(new SubItemDTO
            {
                Name = "AddedChildrenName",
                Description = "AddedChildrenDescription",
                Id = "AddedChildrenId"
            });
            toAdd.SubItems = tmp;
            repository.Update<MainItemDTO>(false, toAdd);
            await repository.SaveChanges();

            var item = await repository.GetById<MainItemDTO, string>(toAdd.Id);
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            Assert.Equal(item.AssignedToId, toAdd.AssignedToId);
            Assert.Equal(item.AssignedToSurname, toAdd.AssignedToSurname);

            Assert.NotNull(item.SubItems);
            Assert.Equal(item.SubItems.Count(), 4);

            var cfirst = toAdd.SubItems.FirstOrDefault();
            var mod = item.SubItems.Where(m => m.Name == "NameModified").FirstOrDefault();
            Assert.NotNull(mod);
            Assert.Equal(cfirst.Description, mod.Description);

            var add = item.SubItems.Where(m => m.Name == "AddedChildrenName").FirstOrDefault();
            Assert.NotNull(add);
            Assert.Equal(add.Description, "AddedChildrenDescription");
            Assert.Equal(add.Id, "AddedChildrenId");
        }
        [Fact]
        public async Task SimpleFullUpdate()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderBy(m => m.Name));
            MainItemDTO toAdd;
            repository.Update<MainItemDTO>(true, toAdd = new MainItemDTO
            {
                Id = res.Id,
                Name = "AddedName",
                Description = "AddedDescription",
                AssignedToId = "AddedOwner",
                AssignedToSurname = "AddedOwnerSurname",
                SubItems = new SubItemDTO[1]
                {
                    new SubItemDTO
                    {
                        Name="AddedChildName",
                        Id = Guid.NewGuid().ToString(),
                        Description="AddedChildDescription"
                    }
                }
            });
            await repository.SaveChanges();

            var item = await repository.GetById<MainItemDTO, string>(toAdd.Id);
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            
            Assert.Equal(item.AssignedToId, toAdd.AssignedToId);
            Assert.Equal(item.AssignedToSurname, toAdd.AssignedToSurname);

            Assert.NotNull(item.SubItems);
            Assert.Equal(item.SubItems.Count(), 1);

            var cfirst = toAdd.SubItems.FirstOrDefault();
            var first = item.SubItems.FirstOrDefault();

            Assert.Equal(cfirst.Name, first.Name);
            Assert.Equal(cfirst.Id, first.Id);
            Assert.Equal(cfirst.Description, first.Description);
        }
        [Fact]
        public async Task SimpleDelete()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));

            repository.Delete<string>(res.Id);
            await repository.SaveChanges();

            var item = await repository.GetById<MainItemDTO, string>(res.Id);
            Assert.Null(item);


        }
    }
}
