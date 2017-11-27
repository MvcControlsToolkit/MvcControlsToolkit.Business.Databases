using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MvcControlsToolkit.Business.DocumentDB.Test.Models;
using MvcControlsToolkit.Business.DocumentDB.Test.DTOs;
using MvcControlsToolkit.Core.Business.Utilities;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace MvcControlsToolkit.Business.DocumentDB.Test.Data
{
    public class DBInitializer: IDisposable
    {
        protected static readonly string Endpoint = "https://mvcct.documents.azure.com:443/";
        protected static readonly string Key = "...";
        protected static readonly string DatabaseId = "ToDoList";
        protected virtual string CollectionId { get { return "ItemsTest"; } }
        

        public ICRUDRepository Repository { get; protected set; }
        public IDocumentDBConnection Connection { get; protected set; }
        static DBInitializer() 
        {
            DocumentDBCRUDRepository<Item>
                .DeclareProjection
                (m => 
                    new MainItemDTO
                    {
                        
                        SubItems=  m.SubItems.Select(l => new SubItemDTO { })
                    }, m => m.Id
                );
            DocumentDBCRUDRepository<Item>
               .DeclareUpdateProjection<MainItemDTO>
               (m =>
                   new Item
                   {
                       SubItems = m.SubItems.Select(l => new Item { }).ToList(),
                       AssignedTo = m.AssignedToId == null ? null : new Person { }
                   }
               );
            DocumentDBCRUDRepository<PItem>
                .DeclareProjection
                (m =>
                    new MainItemDTOPartition
                    {
                        
                        SubItems = m.SubItems.Select(l => new SubItemDTOPartition { })
                    }, m => m.Id
                );
            DocumentDBCRUDRepository<PItem>
               .DeclareUpdateProjection<MainItemDTOPartition>
               (m =>
                   new PItem
                   {
                       SubItems = m.SubItems.Select(l => new Item { }).ToList(),
                       AssignedTo = m.AssignedToId == null ? null : new Person { }
                   }
               );
        }
        protected virtual bool CreateCollection()
        {
            try
            {
                var res = Connection.Client.DeleteDocumentCollectionAsync(CollectionId);
                res.Wait();
            }
            catch
            {

            }
            var cres = Connection.Client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId });
            cres.Wait();
            return true;
        }
        protected virtual void Init()
        {
            Connection = new DefaultDocumentDBConnection(Endpoint, Key, DatabaseId);
            Repository = new DocumentDBCRUDRepository<Item>(Connection, CollectionId);
            
            if(CreateCollection() ) InitData();

        }
        protected virtual void InitData()
        {
            List<Item> allItems = new List<Item>();
            for (int i = 0; i<4; i++)
            {
                var curr = new Item();
                allItems.Add(curr);
                curr.Name = "Name" + i;
                curr.Description = "Description" + i;
                curr.Completed = i % 2 == 0;
                curr.Id = Guid.NewGuid().ToString();
                
                if (i > 1)
                    curr.AssignedTo = new Person
                    {
                        Name = "Francesco",
                        Surname = "Abbruzzese",
                        Id = Guid.NewGuid().ToString()
                    };
                else
                    curr.AssignedTo = new Person
                    {
                        Name = "John",
                        Surname = "Black",
                        Id = Guid.NewGuid().ToString()
                    };
                var innerlList = new List<Item>();
                for (var j=0; j<4; j++)
                {
                    innerlList.Add(new Item
                    {
                        Name = "ChildrenName" + i,
                        Description="ChildrenDescription"+i,
                        Id=Guid.NewGuid().ToString()
                    });
                }
                curr.SubItems = innerlList;
            }
            foreach(var item in allItems)
            {
                var cres = Connection.Client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
                cres.Wait();
            }
        }
        public DBInitializer()
        {
            Init();
        }

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
    
    public class DBInitializerPartition : DBInitializer
    {
        public DBInitializerPartition(): base()
        {
            Repository = new DocumentDBCRUDRepository<PItem>(Connection, CollectionId);
        }
        protected override bool CreateCollection()
        {
            try
            {
                var res = Connection.Client.DeleteDocumentCollectionAsync(CollectionId);
                res.Wait();
            }
            catch
            {

            }
            var collection = new DocumentCollection { Id = CollectionId };
            collection.PartitionKey.Paths.Add("/Name");
            var cres = Connection.Client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        collection);
            cres.Wait();
            return true;
        }

    }
    public class DBInitializerQuery : DBInitializer
    {
        protected override string CollectionId { get { return "ItemsQuery"; } }
        public ICRUDRepository RepositoryFiltered { get; set; }
        public DBInitializerQuery() : base()
        {
            Repository = new DocumentDBCRUDRepository<Item>(Connection, CollectionId, simulateOperations: SimulateOperations.All);
            RepositoryFiltered = new DocumentDBCRUDRepository<Item>(Connection, CollectionId, m => ! m.Completed, m => !m.Completed, SimulateOperations.All);
        }
        protected override bool CreateCollection()
        {
            try
            {
                var res= Connection.Client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
                res.Wait();
                return false;
            }
            catch (Exception ex)
            {
                DocumentClientException e = (ex as DocumentClientException)??(ex.InnerException as DocumentClientException);
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var collection = new DocumentCollection { Id = CollectionId };
                    collection.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
                    collection.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
                    var fres= Connection.Client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        collection);
                    fres.Wait();
                }
                else
                {
                    throw;
                }
                return true;
            }
           
            
        }
        

        }
    public class DBInitializerPartitionQuery : DBInitializer
    {
        protected override string CollectionId { get { return "ItemsPartitionQuery"; } }
        public ICRUDRepository RepositoryFiltered { get; set; }
        public DBInitializerPartitionQuery() : base()
        {

            Repository = new DocumentDBCRUDRepository<PItem>(Connection, CollectionId, simulateOperations: SimulateOperations.All);
            RepositoryFiltered = new DocumentDBCRUDRepository<PItem>(Connection, CollectionId, m => !m.Completed, m => !m.Completed, SimulateOperations.All);
        }
        protected override bool CreateCollection()
        {
            try
            {
                var res = Connection.Client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
                res.Wait();
                return false;
            }
            catch (Exception ex)
            {
                DocumentClientException e = (ex as DocumentClientException) ?? (ex.InnerException as DocumentClientException);
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var collection = new DocumentCollection { Id = CollectionId };
                    collection.PartitionKey.Paths.Add("/Name");
                    collection.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
                    collection.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
                    var cres = Connection.Client.CreateDocumentCollectionAsync(
                                UriFactory.CreateDatabaseUri(DatabaseId),
                                collection);
                    cres.Wait();

                }
                else
                {
                    throw;
                }
                return true;
            }

        }
    }
}
