using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using MvcControlsToolkit.Core.Business.Utilities;
using MvcControlsToolkit.Core.Business.Utilities.Internal;
using MvcControlsToolkit.Core.Linq;
using Newtonsoft.Json;
using MvcControlsToolkit.Core.DataAnnotations;
using System.Collections.Concurrent;
using System.Collections;

namespace MvcControlsToolkit.Business.DocumentDB
{
    public class DocumentDBCRUDRepository<M> : ICRUDRepository
        where M: class, new()
    {
        IDocumentDBConnection connection;
        string collectionId;
        List<M> toAdd=null;
        List<Tuple<string, object>> toDelete = null;
        List<M> toModifyFull = null;
        List<Tuple<Action<M>, string, object>> toModifyPartial = null;
        Expression<Func<M, bool>> selectFilter, modificationFilter;
        static PropertyInfo keyProperty, partitionProperty, combinedKeyProperty;
        private static readonly ConcurrentDictionary<Type, PropertyInfo> KeyProperty = new ConcurrentDictionary<Type, PropertyInfo>();
        private static MethodInfo internalUpdateList = typeof(DocumentDBCRUDRepository<M>).GetTypeInfo().GetMethod("updateList", BindingFlags.Instance | BindingFlags.NonPublic);
        PropertyInfo lastKeyProperty = null;
        public static void DeclareProjection<K>(Expression<Func<M, K>> proj)
        {
            DefaultCRUDRepository<Microsoft.EntityFrameworkCore.DbContext, M>.DeclareProjection(proj);

        }
        public static RecursiveObjectCopier<K,M> DeclareUpdateProjection<K>(Expression<Func<K, M>> proj)
        {
            if (proj == null) return null;
            return RecursiveCopiersCache.DeclareCopierSpecifications<K, M>(proj);
        }
        public static void DeclareQueryProjection<K, PK>(Expression<Func<M, K>> proj, Expression<Func<K, PK>> key)
        {
            DefaultCRUDRepository<Microsoft.EntityFrameworkCore.DbContext, M>.DeclareQueryProjection(proj, key);
        }
        public static Func<M, K> GetCompiledExpression<K>()
        {
            return DefaultCRUDRepository<Microsoft.EntityFrameworkCore.DbContext, M>.GetCompiledExpression<K>();
        }
        public static Expression<Func<M, K>> GetExpression<K>()
        {
            return DefaultCRUDRepository<Microsoft.EntityFrameworkCore.DbContext, M>.GetExpression<K>();
        }
        public static Expression<Func<M, K>> GetQueryExpression<K>(out Func<IQueryable, IEnumerable> getKeys)
        {
            return DefaultCRUDRepository<Microsoft.EntityFrameworkCore.DbContext, M>.GetQueryExpression<K>(out getKeys);
        }
        static DocumentDBCRUDRepository()
        {
            
            foreach(var m in typeof(M).GetTypeInfo().GetProperties())
            {
                if (keyProperty == null && (m.Name.ToLowerInvariant() == "id" || m.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName == "id"))
                    keyProperty = m;
                else if (partitionProperty == null && m.GetCustomAttribute<PartitionKeyAttribute>() != null)
                    partitionProperty = m;
                else if (combinedKeyProperty == null && m.GetCustomAttribute<CombinedKeyAttribute>() != null)
                    combinedKeyProperty = m;
            }
        }
        private PropertyInfo getKeyProperty<K>()
        {
            PropertyInfo res;
            var pair = typeof(K);
            if (KeyProperty.TryGetValue(pair, out res))
                return res;
            PropertyInfo key = ((RecursiveCopiersCache.Get<K, M>() as RecursiveObjectCopier<K, M>)?
                .GetMappedProperty(combinedKeyProperty ?? keyProperty)) ??
                typeof(K).GetTypeInfo().GetProperty((combinedKeyProperty??keyProperty)?.Name);
            if (key != null)
                return KeyProperty[pair] = key;
            else return null;
        }
        private static LambdaExpression GetPropertyExpression<T>(PropertyInfo prop)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T), "m");
            Expression body = Expression.MakeMemberAccess(parameter, prop);
            return Expression.Lambda(body, parameter);
        }
        static void getKeys(object inputKey, out string key, out object partitionKey)
        {
            partitionKey = null;
            if (partitionProperty == null || combinedKeyProperty == null) key = inputKey as string;
            else
            {
                var test = new M();
                combinedKeyProperty.SetValue(inputKey, test);
                key = keyProperty.GetValue(test) as string;
                partitionKey = partitionProperty.GetValue(test);
            }
        }

        static Expression<Func<M, bool>> GetKeySelector(string key)
        {
            var par = Expression.Parameter(typeof(M));
            var body = Expression.Equal(Expression.Property(par, keyProperty), Expression.Constant(key));
            return Expression.Lambda(body, par) as Expression<Func<M, bool>>;
        }
        
        public DocumentDBCRUDRepository(
            IDocumentDBConnection connection,
            string collectionId,
            Expression<Func<M, bool>> selectFilter=null,
            Expression<Func<M, bool>> modificationFilter = null)
        {
            if (string.IsNullOrEmpty(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.collectionId = collectionId;
            this.modificationFilter = modificationFilter;
            this.selectFilter = selectFilter;
            
        }
        public Func<object, object> GetKey
        {
            get
            {
                return x => lastKeyProperty.GetValue(x);
            }
        }

        private void add<T>(bool full, ICollection<T> viewModel)
        {
            if (viewModel == null || viewModel.Count == 0) return;
            if (toAdd == null) toAdd = new List<M>();
            var copier=RecursiveCopiersCache.Get<T, M>()??new RecursiveObjectCopier<T,M>();
            lastKeyProperty = getKeyProperty<T>();
            foreach(var m in viewModel)
            {
                var fm = copier.Copy(m, null);
                lastKeyProperty.SetValue(m,(combinedKeyProperty ?? keyProperty).GetValue(fm));
                toAdd.Add(fm);
            }
            
        }
        public void Add<T>(bool full, params T[] viewModel)
        {
            add(full, viewModel);
        }
        private void delete<U>(ICollection<U> key)
        {
            if (key == null || key.Count == 0) return;
            if (toDelete == null) toDelete = new List<Tuple<string, object>>();
            string tkey;
            object partitionKey;
            getKeys(key, out tkey, out partitionKey);
            toDelete.AddRange(key.Select(m => Tuple.Create(tkey, partitionKey)));
        }
        public virtual void Delete<U>(params U[] key)
        {
            delete(key);
        }
        private  async Task<T> getById<T>(string trueKey, object partition, Expression<Func<M, bool>> selectFilter)
        {
            PartitionKey pKey = null;
            if (partition != null) pKey = new PartitionKey(partition);

            if (selectFilter == null)
            {
                try
                {
                    Document document = await connection.Client
                        .ReadDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, trueKey),
                        pKey == null ? null : new RequestOptions { PartitionKey = pKey });
                    if (typeof(M) == typeof(T)) return (T)(dynamic)document;

                    var res = (M)(dynamic)document;
                    return GetCompiledExpression<T>()(res);
                }
                catch (DocumentClientException e)
                {
                    if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return default(T);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                var query = connection.Client.CreateDocumentQuery<M>(
                UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId),
                pKey == null ?
                    new FeedOptions { MaxItemCount = 1, PartitionKey = pKey } :
                    new FeedOptions { MaxItemCount = 1 })
                .Where(selectFilter)
                .Where(GetKeySelector(trueKey));

                
                if (typeof(M) == typeof(T))
                {
                    IDocumentQuery<M> dPQuery =
                        query.AsDocumentQuery();
                    if (dPQuery.HasMoreResults)
                        return (await dPQuery.ExecuteNextAsync<T>()).FirstOrDefault();
                    else return default(T);
                }
                IQueryable<T> fQuery = null;
                var exp = GetExpression<T>();
                if (exp == null) fQuery = query.Project().To<T>();
                else fQuery = query.Select(exp);
                IDocumentQuery<T> dQuery =
                fQuery.AsDocumentQuery();
                if (dQuery.HasMoreResults)
                    return (await dQuery.ExecuteNextAsync<T>()).FirstOrDefault();
                else return default(T);
            }
        }
        
        public virtual async Task<T> GetById<T, U>(U key)
        {
            if (key == null) return default(T);
            string trueKey;
            object partition;
            getKeys(key, out trueKey, out partition);
            return await getById<T>(trueKey, partition, selectFilter);
        }
        private IQueryable<T2> InternalGetPageExtended<T1, T2>(
            Expression<Func<T1, bool>> filter,
            Func<IQueryable<T2>, IOrderedQueryable<T2>> sorting,
            int page,
            int itemsPerPage,
            Func<IQueryable<T1>, IQueryable<T2>> grouping,
            out Func<IQueryable, IEnumerable> getKeys
            )
         {
            if (sorting == null) throw new ArgumentNullException(nameof(sorting));

            IQueryable<M> start = connection.Client.CreateDocumentQuery<M>(
                UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId),
                    new FeedOptions { MaxItemCount = itemsPerPage == 0 ? -1 : itemsPerPage });
            if (selectFilter != null) start = start.Where(selectFilter);
            

            IQueryable<T1> proj = null;
            getKeys=null;
            var projExp = GetQueryExpression<T1>(out getKeys) ??GetExpression<T1>();
            if (projExp != null)
                proj = start.Select(projExp );
            else
                proj = start.Project().To<T1>();
            if (filter != null) proj = proj.Where(filter);
            IQueryable<T2> toGroup;
            if (grouping != null)
            {
                getKeys = null;
                toGroup = grouping(proj);
            }
            else
            {

                toGroup = proj as IQueryable<T2>;
            }
            if (toGroup == null) toGroup = proj.Project().To<T2>();
            return toGroup;
         }
        public virtual async Task<DataPage<T1>> GetPage<T1>(
            Expression<Func<T1, bool>> filter,
            Func<IQueryable<T1>, IOrderedQueryable<T1>> sorting,
            int page,
            int itemsPerPage,
            Func<IQueryable<T1>, IQueryable<T1>> grouping = null
            )
        {

            return await GetPageExtended<T1, T1>(filter, sorting, page, itemsPerPage, grouping);
        }
        protected Expression<Func<M, bool>> BuildKeysFilter(IEnumerable keyVals)
        {
            var prop = keyProperty;
            return new FilterBuilder<M>().Add(FilterCondition.IsContainedIn, prop.Name, keyVals).Get();

        }
        public virtual async Task<DataPage<T1>> GetPageExtended<T1, T2>(Expression<Func<T1, bool>> filter, Func<IQueryable<T2>, IOrderedQueryable<T2>> sorting, int page, int itemsPerPage, Func<IQueryable<T1>, IQueryable<T2>> grouping = null) where T2 : T1
        {
            page = page - 1;
            if (page < 0) page = 0;
            Func<IQueryable, IEnumerable> getKeys;
            var toGroup = InternalGetPageExtended<T1, T2>(
                filter, sorting, page, itemsPerPage, grouping,
                out getKeys
                );
            var res = new DataPage<T1>
            {
                TotalCount = await toGroup.CountAsync(),
                ItemsPerPage = itemsPerPage,
                Page = page + 1
            };
            res.TotalPages = res.TotalCount / itemsPerPage;
            if (res.TotalCount % itemsPerPage > 0) res.TotalPages++;
            var sorted = sorting(toGroup);
            if (page > 0) toGroup = sorted.Skip(page * itemsPerPage).Take(itemsPerPage);
            else toGroup = sorted.Take(itemsPerPage);
            if (getKeys != null)
            {
                var allIds = typeof(T1) == typeof(T2) ? getKeys(toGroup) : getKeys(toGroup.Project().To<T1>());
                var toStart = connection.Client.CreateDocumentQuery<M>(
                UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId),
                    new FeedOptions { MaxItemCount = itemsPerPage == 0 ? -1 : itemsPerPage }).Where(BuildKeysFilter(allIds));
                var projExp = GetExpression<T1>();
                IQueryable<T1> proj = null;
                if (projExp!=null)
                    proj = toStart.Select(projExp);
                else
                    proj = toStart.Project().To<T1>();
                toGroup = proj as IQueryable<T2>;
                if (toGroup == null) toGroup = proj.Project().To<T2>();
                toGroup = sorting(toGroup);
            }
            List<T1> results = new List<T1>();
            if (typeof(T1) == typeof(T2) )
            {
                var fQuery = toGroup.AsDocumentQuery();
                
                while (fQuery.HasMoreResults)
                {
                    results.AddRange(await fQuery.ExecuteNextAsync<T1>());
                }
            }
            else if(grouping==null)
            {
                var fQuery = toGroup.Project().To<T1>().AsDocumentQuery();

                while (fQuery.HasMoreResults)
                {
                    results.AddRange(await fQuery.ExecuteNextAsync<T1>());
                }
            }
            else
            {
                var fQuery = toGroup.AsDocumentQuery();

                while (fQuery.HasMoreResults)
                {
                    results.AddRange((await fQuery.ExecuteNextAsync<T2>()).Select(m => (T1)m));
                }
            }

            res.Data = results;
            return res;
        }

        public virtual async Task SaveChanges()
        {
            var uri = UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId);
            if (toAdd != null)
            {
                
                foreach (var item in toAdd)
                {
                    await connection.Client.CreateDocumentAsync(uri, item);
                }
                toAdd = null;
            }
            if(toDelete != null)
            {
                foreach(var item in toDelete)
                {
                    if (modificationFilter != null)
                    {
                        var res = await connection.Client.CreateDocumentQuery<M>(
                            uri,
                            item.Item2 == null ?
                            new FeedOptions { MaxItemCount = 1, PartitionKey = new PartitionKey(item.Item2) } :
                            new FeedOptions { MaxItemCount = 1 })
                                .Where(modificationFilter)
                                .Where(GetKeySelector(item.Item1))
                                .CountAsync();
                        if (res < 1) continue;
                    }
                    await connection.Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, item.Item1),
                        item.Item2 == null ? null : new RequestOptions { PartitionKey = new PartitionKey(item.Item2) });
                }
                toDelete = null;
            }
            if(toModifyFull != null)
            {
                foreach(var item in toModifyFull)
                {
                    string key = keyProperty.GetValue(item) as string;
                    object partitionKey = partitionProperty.GetValue(item);
                    if(modificationFilter != null)
                    {
                        var res = await connection.Client.CreateDocumentQuery<M>(
                            uri,
                            partitionKey == null ?
                            new FeedOptions { MaxItemCount = 1, PartitionKey = new PartitionKey(partitionKey) } :
                            new FeedOptions { MaxItemCount = 1 })
                                .Where(modificationFilter)
                                .Where(GetKeySelector(key))
                                .CountAsync();
                        if (res < 1) continue;
                    }
                    await connection.Client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, key),
                        item, partitionKey == null ? null : new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                }
                toModifyFull = null;
            }
            if (toModifyPartial != null)
            {

                foreach (var item in toModifyPartial)
                {
                    M old = await getById<M>(item.Item2, item.Item3, modificationFilter);
                    if (old == null) continue;
                    item.Item1(old);

                    await connection.Client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, item.Item2),
                        item, item.Item3 == null ? null : new RequestOptions { PartitionKey = new PartitionKey(item.Item3) });
                }
                toModifyPartial = null;
            }
        }
        private void update<T>(bool full, ICollection<T> viewModel)
        {
            if (viewModel == null || viewModel.Count == 0) return;

            if (full)
            {
                if (toModifyFull == null) toModifyFull = new List<M>();
                var copier = RecursiveCopiersCache.Get<T, M>() ?? new RecursiveObjectCopier<T, M>();
                toModifyFull.AddRange(viewModel.Select(m => copier.Copy(m, null)));
            }
            else
            {
                if (toModifyPartial == null) toModifyPartial = new List<Tuple<Action<M>, string, object>>();
                var copier = RecursiveCopiersCache.Get<T, M>() ?? new RecursiveObjectCopier<T, M>();
                var key = getKeyProperty<T>();
                toModifyPartial.AddRange(viewModel.Select<T, Tuple<Action<M>, string, object>>(m =>
                {
                    string tKey; object partitionKey;
                    getKeys(key.GetValue(m), out tKey, out partitionKey);
                    return Tuple.Create<Action<M>, string, object>(x =>
                    {
                        copier.Copy(m, x);
                    }, tKey, partitionKey);
                }
                ));
            }
        }
        public virtual void Update<T>(bool full, params T[] viewModel)
        {
            update(full, viewModel);
        }

        public void UpdateKeys()
        {
            
        }
        private void updateList<T, D>(bool full, IEnumerable<T> oldValues, IEnumerable<T> newValues, Expression<Func<T, D>> keyExpression)
        {
            var cs = ChangeSet.Create(oldValues, newValues, keyExpression);
            add(full, cs.Inserted);
            update(full, cs.Changed);
            delete(cs.Deleted);
        }
        public virtual void UpdateList<T>(bool full, IEnumerable<T> oldValues, IEnumerable<T> newValues)
        {
            if (oldValues == null && newValues == null) return;
            var key = getKeyProperty<T>();
            if (key == null) return;
            var expression = GetPropertyExpression<T>(key);
            internalUpdateList.MakeGenericMethod(new Type[] { typeof(T), key.PropertyType })
            .Invoke(this, new object[] { full, oldValues, newValues, expression });
        }
    }
}
