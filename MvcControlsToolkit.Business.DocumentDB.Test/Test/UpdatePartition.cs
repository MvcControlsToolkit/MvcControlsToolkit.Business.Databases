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
    [CollectionDefinition("UpdatePartitionCollection")]
    public class UpdatePartitionCollection : ICollectionFixture<DBInitializerPartition>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
    [Collection("UpdatePartitionCollection")]
    public class UpdatePartition
    {
        ICRUDRepository repository;
        IDocumentDBConnection connection;
        public UpdatePartition(DBInitializerPartition init)
        {
            repository = init.Repository;
            connection = init.Connection;
        }

        [Fact]
        public async Task SimpleAdd()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            MainItemDTOPartition toAdd;
            repository.Add<MainItemDTOPartition>(true, toAdd = new MainItemDTOPartition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "AddedName",
                Description = "AddedDescription",
                AssignedToId = "AddedOwner",
                AssignedToSurname = "AddedOwnerSurname",
                SubItems = new SubItemDTOPartition[1]
                {
                    new SubItemDTOPartition
                    {
                        Name="AddedChildName",
                        Id = Guid.NewGuid().ToString(),
                        Description="AddedChildDescription"
                    }
                }
            });
            toAdd.CombinedId= DocumentDBCRUDRepository<PItem>.DefaultCombinedKey(toAdd.Id, toAdd.Name);
            await repository.SaveChanges();
            var item = await repository.GetById<MainItemDTOPartition, string>(toAdd.CombinedId);
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
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTOPartition toAdd = res;

            
            toAdd.Description = "AddedDescription";
            toAdd.AssignedToSurname = "AddedSurname";
            var fc = toAdd.SubItems.FirstOrDefault();
            toAdd.SubItems = toAdd.SubItems.Where(m => m.Id != fc.Id).ToList();
            fc = toAdd.SubItems.FirstOrDefault();
            fc.Name = "NameModified";
            var tmp = toAdd.SubItems.ToList();
            tmp.Add(new SubItemDTOPartition
            {
                Name = "AddedChildrenName",
                Description = "AddedChildrenDescription",
                Id = "AddedChildrenId"
            });
            toAdd.SubItems = tmp;
            
            repository.Update<MainItemDTOPartition>(false, toAdd);
            await repository.SaveChanges();

            var item = await repository.GetById<MainItemDTOPartition, string>(toAdd.CombinedId);
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
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderBy(m => m.Name));
            MainItemDTOPartition toAdd;
            repository.Update<MainItemDTOPartition>(true, toAdd = new MainItemDTOPartition
            {
                CombinedId=res.CombinedId,
                Id = res.Id,
                Name = res.Name,
                Description = "AddedDescription",
                AssignedToId = "AddedOwner",
                AssignedToSurname = "AddedOwnerSurname",
                SubItems = new SubItemDTOPartition[1]
                {
                    new SubItemDTOPartition
                    {
                        Name="AddedChildName",
                        Id = Guid.NewGuid().ToString(),
                        Description="AddedChildDescription"
                    }
                }
            });
            await repository.SaveChanges();

            var item = await repository.GetById<MainItemDTOPartition, string>(toAdd.CombinedId);
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
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));

            repository.Delete<string>(res.CombinedId);
            await repository.SaveChanges();

            var item = await repository.GetById<MainItemDTO, string>(res.CombinedId);
            Assert.Null(item);


        }
    }
}
