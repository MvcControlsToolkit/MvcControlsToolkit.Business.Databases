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
    [CollectionDefinition("DBInitializerPartitionQuery")]
    public class UpdateFiltersCollection : ICollectionFixture<DBInitializerPartitionQuery>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
    [Collection("DBInitializerPartitionQuery")]
    public class QueryPartition
    {
        ICRUDRepository repository, repositoryFiltered;
        IDocumentDBConnection connection;
        public QueryPartition(DBInitializerPartitionQuery init)
        {
            repository = init.Repository;
            repositoryFiltered = init.RepositoryFiltered;
            connection = init.Connection;
        }

        [Fact]
        public async Task SimpleSelect()
        {
            var res = await repository.GetPage<MainItemDTOPartition>(null, m => m.OrderByDescending(l => l.Name), 1, 3);

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
        public async Task SimpleSelectFiltered()
        {
            var res = await repositoryFiltered.GetPage<MainItemDTOPartition>(null, m => m.OrderByDescending(l => l.Name), 1, 3);

            Assert.NotNull(res);
            Assert.Equal(res.TotalCount, 2);
            Assert.Equal(res.TotalPages, 1);
            Assert.Equal(res.Data.Count(), 2);

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
            var res = await repository.GetPage<MainItemDTOPartition>(null, m => m.OrderByDescending(l => l.Name), 2, 3);

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
            var res = await repository.GetPage<MainItemDTOPartition>(m => m.Name == "Name0", m => m.OrderByDescending(l => l.Name), 1, 3);

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
        public async Task WhereFiltered()
        {
            var res = await repositoryFiltered.GetPage<MainItemDTOPartition>(m => m.AssignedToSurname == "Abbruzzese", m => m.OrderByDescending(l => l.Name), 1, 3);

            Assert.NotNull(res);
            Assert.Equal(res.TotalCount, 1);
            Assert.Equal(res.TotalPages, 1);
            Assert.Equal(res.Data.Count(), 1);

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
        public async Task TableAndList()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.ToList<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name).Take(3));
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
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.ToSequence<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
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

            res = await rep.ToSequence<MainItemDTOPartition>(rep.Table(3, null, res.Continuation).OrderByDescending(m => m.Name));

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
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
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
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));


            var first = await repository.GetById<MainItemDTOPartition, string>(res.CombinedId);

            Assert.Equal(first.Name, "Name3");
            Assert.Equal(first.Description, "Description3");
            Assert.Equal(first.AssignedToSurname, "Abbruzzese");

            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName3");
            Assert.Equal(firstChild.Description, "ChildrenDescription3");


        }
        [Fact]
        public async Task SimpleGetByIdFiltered()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));


            var first = await repositoryFiltered.GetById<MainItemDTOPartition, string>(res.CombinedId);

            Assert.Equal(first.Name, "Name3");
            Assert.Equal(first.Description, "Description3");
            Assert.Equal(first.AssignedToSurname, "Abbruzzese");

            Assert.Equal(first.SubItems.Count(), 4);
            var firstChild = first.SubItems.FirstOrDefault();

            Assert.Equal(firstChild.Name, "ChildrenName3");
            Assert.Equal(firstChild.Description, "ChildrenDescription3");


        }

        [Fact]
        public async Task SimpleGetByIdFilteredOut()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderBy(m => m.Name));


            var first = await repositoryFiltered.GetById<MainItemDTOPartition, string>(res.CombinedId);

            Assert.Null(first);


        }

        [Fact]
        public async Task SimpleDelete()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));

            repository.Delete<string>(res.CombinedId);
            await repository.SaveChanges();
            var dels = rep.SimulationResult?.Deletes;
            Assert.NotNull(dels);
            Assert.Equal(dels.FirstOrDefault().Item1, res.Id);



        }
        [Fact]
        public async Task FilteredDelete()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));

            repositoryFiltered.Delete<string>(res.CombinedId);
            await repositoryFiltered.SaveChanges();
            var dels = (repositoryFiltered as DocumentDBCRUDRepository<PItem>).SimulationResult?.Deletes;
            Assert.NotNull(dels);
            Assert.Equal(dels.FirstOrDefault().Item1, res.Id);
        }

        [Fact]
        public async Task FilteredDeleteOut()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;

            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderBy(m => m.Name));

            repositoryFiltered.Delete<string>(res.CombinedId);
            await repositoryFiltered.SaveChanges();
            var dels = (repositoryFiltered as DocumentDBCRUDRepository<PItem>).SimulationResult?.Deletes;
            Assert.Equal(dels.Count, 0);
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
                SubItems = new SubItemDTO[1]
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
            var adds = rep.SimulationResult?.Additions;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 1);
            var item = adds.FirstOrDefault();
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            Assert.NotNull(item.AssignedTo);
            Assert.Equal(item.AssignedTo.Id, toAdd.AssignedToId);
            Assert.Equal(item.AssignedTo.Surname, toAdd.AssignedToSurname);

            Assert.NotNull(item.SubItems);
            Assert.Equal(item.SubItems.Count, 1);

            var cfirst = toAdd.SubItems.FirstOrDefault();
            var first = item.SubItems.FirstOrDefault();

            Assert.Equal(cfirst.Name, first.Name);
            Assert.Equal(cfirst.Id, first.Id);
            Assert.Equal(cfirst.Description, first.Description);
        }

        [Fact]
        public async Task SimpleFullUpdate()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTOPartition toAdd;
            repository.Update<MainItemDTOPartition>(true, toAdd = new MainItemDTOPartition
            {
                Id = res.Id,
                CombinedId=res.CombinedId,
                Name = "AddedName",
                Description = "AddedDescription",
                AssignedToId = "AddedOwner",
                AssignedToSurname = "AddedOwnerSurname",
                SubItems = new SubItemDTO[1]
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
            var adds = rep.SimulationResult?.Updates;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 1);
            var item = adds.FirstOrDefault();
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            Assert.NotNull(item.AssignedTo);
            Assert.Equal(item.AssignedTo.Id, toAdd.AssignedToId);
            Assert.Equal(item.AssignedTo.Surname, toAdd.AssignedToSurname);

            Assert.NotNull(item.SubItems);
            Assert.Equal(item.SubItems.Count, 1);

            var cfirst = toAdd.SubItems.FirstOrDefault();
            var first = item.SubItems.FirstOrDefault();

            Assert.Equal(cfirst.Name, first.Name);
            Assert.Equal(cfirst.Id, first.Id);
            Assert.Equal(cfirst.Description, first.Description);
        }

        [Fact]
        public async Task FilteredFullUpdate()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTOPartition toAdd;
            repositoryFiltered.Update<MainItemDTOPartition>(true, toAdd = new MainItemDTOPartition
            {
                Id = res.Id,
                CombinedId=res.CombinedId,
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
            await repositoryFiltered.SaveChanges();
            var adds = (repositoryFiltered as DocumentDBCRUDRepository<PItem>).SimulationResult?.Updates;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 1);
            var item = adds.FirstOrDefault();
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            Assert.NotNull(item.AssignedTo);
            Assert.Equal(item.AssignedTo.Id, toAdd.AssignedToId);
            Assert.Equal(item.AssignedTo.Surname, toAdd.AssignedToSurname);

            Assert.NotNull(item.SubItems);
            Assert.Equal(item.SubItems.Count, 1);

            var cfirst = toAdd.SubItems.FirstOrDefault();
            var first = item.SubItems.FirstOrDefault();

            Assert.Equal(cfirst.Name, first.Name);
            Assert.Equal(cfirst.Id, first.Id);
            Assert.Equal(cfirst.Description, first.Description);
        }

        [Fact]
        public async Task FilteredFullUpdateOut()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderBy(m => m.Name));
            MainItemDTOPartition toAdd;
            repositoryFiltered.Update<MainItemDTO>(true, toAdd = new MainItemDTOPartition
            {
                Id = res.Id,
                CombinedId=res.CombinedId,
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
            await repositoryFiltered.SaveChanges();
            var adds = (repositoryFiltered as DocumentDBCRUDRepository<PItem>).SimulationResult?.Updates;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 0);

        }

        [Fact]
        public async Task SimplePartialUpdate()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTOPartition toAdd = res;

            toAdd.Name = "AddedName";
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
            var adds = rep.SimulationResult?.Updates;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 1);
            var item = adds.FirstOrDefault();
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            Assert.NotNull(item.AssignedTo);
            Assert.Equal(item.AssignedTo.Id, toAdd.AssignedToId);
            Assert.Equal(item.AssignedTo.Surname, toAdd.AssignedToSurname);

            Assert.NotNull(item.SubItems);
            Assert.Equal(item.SubItems.Count, 4);

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
        public async Task FilteredPartialUpdate()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTOPartition toAdd = res;

            toAdd.Name = "AddedName";
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
            repositoryFiltered.Update<MainItemDTOPartition>(false, toAdd);
            await repositoryFiltered.SaveChanges();
            var adds = (repositoryFiltered as DocumentDBCRUDRepository<PItem>).SimulationResult?.Updates;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 1);
            var item = adds.FirstOrDefault();
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            Assert.NotNull(item.AssignedTo);
            Assert.Equal(item.AssignedTo.Id, toAdd.AssignedToId);
            Assert.Equal(item.AssignedTo.Surname, toAdd.AssignedToSurname);

            Assert.NotNull(item.SubItems);
            Assert.Equal(item.SubItems.Count, 4);

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
        public async Task FilteredPartialUpdateOut()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var res = await rep.FirstOrDefault<MainItemDTOPartition>(rep.Table(3).OrderBy(m => m.Name));
            MainItemDTOPartition toAdd = res;

            toAdd.Name = "AddedName";
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
            repositoryFiltered.Update<MainItemDTOPartition>(false, toAdd);
            await repositoryFiltered.SaveChanges();
            var adds = (repositoryFiltered as DocumentDBCRUDRepository<PItem>).SimulationResult?.Updates;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 0);

        }

        [Fact]
        public async Task SimpleUpdateList()
        {
            var rep = repository as DocumentDBCRUDRepository<PItem>;
            var old = await rep.ToList<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
            var newList = await rep.ToList<MainItemDTOPartition>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTOPartition toAdd = new MainItemDTOPartition
            {
                Id = Guid.NewGuid().ToString(),
                Name = "AddedName",
                Description = "AddedDescription",
                AssignedToId = "AddedOwner",
                AssignedToSurname = "AddedOwnerSurname",
                SubItems = new SubItemDTO[1]
                {
                    new SubItemDTOPartition
                    {
                        Name="AddedChildName",
                        Id = Guid.NewGuid().ToString(),
                        Description="AddedChildDescription"
                    }
                }
            };
            toAdd.CombinedId = DocumentDBCRUDRepository<PItem>.DefaultCombinedKey(toAdd.Id, toAdd.Name);
            var toUpdate = newList.Where(m => m.Name == "Name3").FirstOrDefault();
            toUpdate.Description = "UpdatedDescription";

            newList = newList.Where(m => m.Name != "Name0").ToList();
            newList.Add(toAdd);
            repository.UpdateList<MainItemDTOPartition>(false, old, newList);
            await repository.SaveChanges();
            var adds = rep.SimulationResult?.Additions;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 1);
            Assert.Equal(adds.FirstOrDefault().Name, "AddedName");

            var updates = rep.SimulationResult?.Updates;
            Assert.NotNull(updates);
            Assert.Equal(updates.Count, 1);
            Assert.Equal(updates.FirstOrDefault().Description, "UpdatedDescription");

            var deletes = rep.SimulationResult?.Deletes;
            Assert.NotNull(deletes);
            Assert.Equal(deletes.Count, 1);



        }
    }
}
