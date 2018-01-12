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
using MvcControlsToolkit.Core.Business.Transformations;

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
        ICRUDRepository repository, repositoryFiltered;
        IDocumentDBConnection connection;
        public QueryTests(DBInitializerQuery init)
        {
            repository = init.Repository;
            connection = init.Connection;
            repositoryFiltered = init.RepositoryFiltered;
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
        public async Task SimpleSelectFiltered()
        {
            var res = await repositoryFiltered.GetPage<MainItemDTO>(null, m => m.OrderByDescending(l => l.Name), 1, 3);

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
        public async Task WhereFiltered()
        {
            var res = await repositoryFiltered.GetPage<MainItemDTO>(m => m.AssignedToSurname == "Abbruzzese", m => m.OrderByDescending(l => l.Name), 1, 3);

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
        [Fact]
        public async Task SimpleGetByIdVM()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));

            var wrep = new TransformationRepositoryFarm()
                .Add<MainItemVM, MainItemDTO>()
                .Create(repository);

            var first = await wrep.GetById<MainItemVM, string>(res.Id);

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
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));


            var first = await repositoryFiltered.GetById<MainItemDTO, string>(res.Id);

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
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderBy(m => m.Name));


            var first = await repositoryFiltered.GetById<MainItemDTO, string>(res.Id);

            Assert.Null(first);


        }
        [Fact]
        public async Task SimpleDelete()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));

            repository.Delete<string>(res.Id);
            await repository.SaveChanges();
            var dels = rep.SimulationResult?.Deletes;
            Assert.NotNull(dels);
            Assert.Equal(dels.FirstOrDefault().Item1,res.Id);
            


        }
        [Fact]
        public async Task SimpleDeleteVM()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            var wrep = new TransformationRepositoryFarm()
                .Add<MainItemVM, MainItemDTO>()
                .Create(repository);
            wrep.Delete<string>(res.Id);
            await repository.SaveChanges();
            var dels = rep.SimulationResult?.Deletes;
            Assert.NotNull(dels);
            Assert.Equal(dels.FirstOrDefault().Item1, res.Id);



        }
        [Fact]
        public async Task FilteredDelete()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));

            repositoryFiltered.Delete<string>(res.Id);
            await repositoryFiltered.SaveChanges();
            var dels = (repositoryFiltered as DocumentDBCRUDRepository<Item>).SimulationResult?.Deletes;
            Assert.NotNull(dels);
            Assert.Equal(dels.FirstOrDefault().Item1, res.Id);



        }

        [Fact]
        public async Task FilteredDeleteOut()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;

            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderBy(m => m.Name));

            repositoryFiltered.Delete<string>(res.Id);
            await repositoryFiltered.SaveChanges();
            var dels = (repositoryFiltered as DocumentDBCRUDRepository<Item>).SimulationResult?.Deletes;
            Assert.Equal(dels.Count, 0);
        }

        [Fact]
        public async Task SimpleAdd()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;
            MainItemDTO toAdd;
            repository.Add<MainItemDTO>(true, toAdd=new MainItemDTO
            {
                Id=Guid.NewGuid().ToString(),
                Name ="AddedName",
                Description="AddedDescription",
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
        public async Task SimpleAddVM()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;
            MainItemVM toAdd;
            var wrep = new TransformationRepositoryFarm()
                .Add<MainItemVM, MainItemDTO>()
                .Create(repository);
            wrep.Add<MainItemVM>(true, toAdd = new MainItemVM
            {
                Id = Guid.NewGuid().ToString(),
                Name = "AddedName",
                Description = "AddedDescription",
                AssignedToId = "AddedOwner",
                AssignedToSurname = "AddedOwnerSurname",
                SubItems = new SubItemVM[1]
                {
                    new SubItemVM
                    {
                        Name="AddedChildName",
                        Id = Guid.NewGuid().ToString(),
                        Description="AddedChildDescription"
                    }
                }
            });
            await wrep.SaveChanges();
            var adds = rep.SimulationResult?.Additions;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 1);
            var item = adds.FirstOrDefault();
            Assert.Equal(item.Name, toAdd.Name);
            Assert.Equal(item.Description, toAdd.Description);
            Assert.Equal(item.Id, toAdd.Id);

            

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
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
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
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTO toAdd;
            repositoryFiltered.Update<MainItemDTO>(true, toAdd = new MainItemDTO
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
            await repositoryFiltered.SaveChanges();
            var adds = (repositoryFiltered as DocumentDBCRUDRepository<Item>).SimulationResult?.Updates;
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
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderBy(m => m.Name));
            MainItemDTO toAdd;
            repositoryFiltered.Update<MainItemDTO>(true, toAdd = new MainItemDTO
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
            await repositoryFiltered.SaveChanges();
            var adds = (repositoryFiltered as DocumentDBCRUDRepository<Item>).SimulationResult?.Updates;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 0);
            
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
            fc= toAdd.SubItems.FirstOrDefault();
            fc.Name = "NameModified";
            var tmp = toAdd.SubItems.ToList();
            tmp.Add(new SubItemDTO
            {
                Name="AddedChildrenName",
                Description= "AddedChildrenDescription",
                Id= "AddedChildrenId"
            });
            toAdd.SubItems = tmp;
            repository.Update<MainItemDTO>(false, toAdd);
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
        public async Task SimplePartialUpdateVM()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var pres = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            var res = pres.Map().To<MainItemVM>();
            var wrep = new TransformationRepositoryFarm()
                .Add<MainItemVM, MainItemDTO>()
                .Create(repository);
            MainItemVM toAdd = res;

            toAdd.Name = "AddedName";
            toAdd.Description = "AddedDescription";
            toAdd.AssignedToSurname = "AddedSurname";
            var fc = toAdd.SubItems.FirstOrDefault();
            toAdd.SubItems = toAdd.SubItems.Where(m => m.Id != fc.Id).ToList();
            fc = toAdd.SubItems.FirstOrDefault();
            fc.Name = "NameModified";
            var tmp = toAdd.SubItems.ToList();
            tmp.Add(new SubItemVM
            {
                Name = "AddedChildrenName",
                Description = "AddedChildrenDescription",
                Id = "AddedChildrenId"
            });
            toAdd.SubItems = tmp;
            wrep.Update<MainItemVM>(false, toAdd);
            await wrep.SaveChanges();
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
            repositoryFiltered.Update<MainItemDTO>(false, toAdd);
            await repositoryFiltered.SaveChanges();
            var adds = (repositoryFiltered as DocumentDBCRUDRepository<Item>).SimulationResult?.Updates;
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
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var res = await rep.FirstOrDefault<MainItemDTO>(rep.Table(3).OrderBy(m => m.Name));
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
            repositoryFiltered.Update<MainItemDTO>(false, toAdd);
            await repositoryFiltered.SaveChanges();
            var adds = (repositoryFiltered as DocumentDBCRUDRepository<Item>).SimulationResult?.Updates;
            Assert.NotNull(adds);
            Assert.Equal(adds.Count, 0);
            
        }
        [Fact]
        public async Task SimpleUpdateList()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var old = await rep.ToList<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            var newList = await rep.ToList<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            MainItemDTO toAdd = toAdd = new MainItemDTO
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
            };
            var toUpdate = newList.Where(m => m.Name == "Name3").FirstOrDefault();
            toUpdate.Description = "UpdatedDescription";
            
            newList = newList.Where(m => m.Name != "Name0").ToList();
            newList.Add(toAdd);
            repository.UpdateList<MainItemDTO>(false, old, newList);
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

        [Fact]
        public async Task SimpleUpdateListVM()
        {
            var rep = repository as DocumentDBCRUDRepository<Item>;
            var pold = await rep.ToList<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            var wrep = new TransformationRepositoryFarm()
                .Add<MainItemVM, MainItemDTO>()
                .Create(repository);
            var pnewList = await rep.ToList<MainItemDTO>(rep.Table(3).OrderByDescending(m => m.Name));
            var old = pold.MapIEnumerable().To<MainItemVM>().ToList();
            var newList = pnewList.MapIEnumerable().To<MainItemVM>().ToList();
            MainItemVM toAdd = toAdd = new MainItemVM
            {
                Id = Guid.NewGuid().ToString(),
                Name = "AddedName",
                Description = "AddedDescription",
                AssignedToId = "AddedOwner",
                AssignedToSurname = "AddedOwnerSurname",
                SubItems = new SubItemVM[1]
                {
                    new SubItemVM
                    {
                        Name="AddedChildName",
                        Id = Guid.NewGuid().ToString(),
                        Description="AddedChildDescription"
                    }
                }
            };
            var toUpdate = newList.Where(m => m.Name == "Name3").FirstOrDefault();
            toUpdate.Description = "UpdatedDescription";

            newList = newList.Where(m => m.Name != "Name0").ToList();
            newList.Add(toAdd);
            wrep.UpdateList<MainItemVM>(false, old, newList);
            await wrep.SaveChanges();
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
