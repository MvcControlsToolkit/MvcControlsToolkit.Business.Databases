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
using MvcControlsToolkit.Business.DocumentDB.Internal;

namespace MvcControlsToolkit.Business.DocumentDB
{
    [Flags]
    public enum SimulateOperations {None=0, Add=1, Delete=2, Update=4, All=7}
    public class DocumentDBCRUDRepository<M> : ICRUDRepository
        where M : class, new()
    {
        IDocumentDBConnection connection;
        string collectionId;
        List<M> toAdd = null;
        List<Tuple<string, object>> toDelete = null;
        List<M> toModifyFull = null;
        List<Tuple<Action<M>, string, object>> toModifyPartial = null;
        Expression<Func<M, bool>> selectFilter, modificationFilter;
        static PropertyInfo keyProperty, partitionProperty, combinedKeyProperty;
        private static readonly ConcurrentDictionary<Type, PropertyInfo> KeyProperty = new ConcurrentDictionary<Type, PropertyInfo>();
        private static MethodInfo internalUpdateList = typeof(DocumentDBCRUDRepository<M>).GetTypeInfo().GetMethod("updateList", BindingFlags.Instance | BindingFlags.NonPublic);
        PropertyInfo lastKeyProperty = null;
        private SimulateOperations simulateOperations;
        public DocumentsUpdateSimulationResult<M> SimulationResult { get; private set; }
        public static string DefaultCombinedKey(string id, string partition)
        {
            return JsonConvert.SerializeObject(new KeyValuePair<string, string>(id, partition));
        }
        public static void DefaultSplitCombinedKey(string combinedKey, out string id, out string partition)
        {
            id = null;
            partition = null;
            if (combinedKey == null) return;
            var res = JsonConvert.DeserializeObject(combinedKey, typeof(KeyValuePair<string, string>));
            if (res == null) return;
            var pair = (KeyValuePair<string, string>)res;
            id = pair.Key; partition = pair.Value;

        }
        private static Dictionary<Type, LambdaExpression> propertySelection = new Dictionary<Type, LambdaExpression>();
        public static void DeclareProjection<K, PK>(Expression<Func<M, K>> proj, Expression<Func<K, PK>> key)
            where K : class, new()
        {
            if (proj == null) return;

            var projCopy = proj.CloneExpression(true) as Expression<Func<M, K>>;
            var copier = RecursiveCopiersCache.DeclareCopierSpecifications<M, K>(proj);
            if (true)//if(copier.HasIEnumerables || partitionProperty != null)
            {
                Func<IQueryable, int, int, IEnumerable> getKeys;
                var res = GetQueryExpression<K>(out getKeys);
                if (res == null) DeclareQueryProjection(projCopy, key);
                DependencyTracker tracker = new DependencyTracker();
                copier.FullExpression.CloneExpression(true, tracker);
                var actualExpression = tracker.GetExpression(typeof(M));
                propertySelection[typeof(K)] = actualExpression;
            }
            else DefaultCRUDRepository<Microsoft.EntityFrameworkCore.DbContext, M>.DeclareProjection(projCopy);

        }
        public static RecursiveObjectCopier<K, M> DeclareUpdateProjection<K>(Expression<Func<K, M>> proj)
        {
            if (proj == null) return null;
            return RecursiveCopiersCache.DeclareCopierSpecifications<K, M>(proj);
        }
        private static readonly ConcurrentDictionary<Type, Tuple<object, Func<IQueryable, int, int, IEnumerable>>> FilterProjections = new ConcurrentDictionary<Type, Tuple<object, Func<IQueryable, int, int, IEnumerable>>>();
        public static void DeclareQueryProjection<K, PK>(Expression<Func<M, K>> proj, Expression<Func<K, PK>> key)
        {
            if (proj == null || key == null) return;
            Func<IQueryable, int, int, IEnumerable> keys = (x, toSkip, toTake) =>
            {
                var iquery = (x as IQueryable<K>).Select(key);
                var query = (toTake >0 ? iquery.Take(toTake) : iquery).AsDocumentQuery();
                var result = new List<PK>();
                int count = 0;

                while (query.HasMoreResults)
                {
                    var toWait = query.ExecuteNextAsync<PK>();
                    toWait.Wait();
                    if (count >= toSkip)
                        result.AddRange(toWait.Result);
                    count += toWait.Result.Count;
                }
                return result;
            };
            FilterProjections[typeof(K)] = Tuple.
                Create<object, Func<IQueryable, int, int,  IEnumerable>>(ProjectionExpression<M>.BuildExpression(proj, typeof(K).GetTypeInfo().IsInterface ? typeof(K) : null),
                keys);
        }
        public static Func<M, K> GetCompiledExpression<K>()
        {
            return DefaultCRUDRepository<Microsoft.EntityFrameworkCore.DbContext, M>.GetCompiledExpression<K>();
        }
        public static Expression<Func<M, M>> GetProprtiesSelectionExpression<K>()
        {
            LambdaExpression res;
            if (propertySelection.TryGetValue(typeof(K), out res)) return res as Expression<Func<M, M>>;
            else return null;
        }
        public static Expression<Func<M, K>> GetExpression<K>()
        {
            return DefaultCRUDRepository<Microsoft.EntityFrameworkCore.DbContext, M>.GetExpression<K>();
        }
        public static Expression<Func<M, K>> GetQueryExpression<K>(out Func<IQueryable, int, int, IEnumerable> getKeys)
        {
            Tuple<object, Func<IQueryable, int, int, IEnumerable>> pres = null;
            FilterProjections.TryGetValue(typeof(K), out pres);
            getKeys = null;
            if (pres == null) return null;
            getKeys = pres.Item2;
            return pres.Item1 as Expression<Func<M, K>>;
        }
        static DocumentDBCRUDRepository()
        {

            foreach (var m in typeof(M).GetTypeInfo().GetProperties())
            {
                if (keyProperty == null && ((m.Name.ToLowerInvariant() == "id" && keyProperty == null) || m.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName == "id"))
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
                typeof(K).GetTypeInfo().GetProperty((combinedKeyProperty ?? keyProperty)?.Name);
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
            Expression<Func<M, bool>> selectFilter = null,
            Expression<Func<M, bool>> modificationFilter = null,
            SimulateOperations simulateOperations= SimulateOperations.None)
        {
            if (string.IsNullOrEmpty(collectionId)) throw new ArgumentNullException(nameof(collectionId));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.collectionId = collectionId;
            this.modificationFilter = modificationFilter;
            this.selectFilter = selectFilter;
            this.simulateOperations = simulateOperations;

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
            var copier = RecursiveCopiersCache.Get<T, M>() ?? new RecursiveObjectCopier<T, M>();
            lastKeyProperty = getKeyProperty<T>();
            foreach (var m in viewModel)
            {
                var fm = copier.Copy(m, null);
                lastKeyProperty.SetValue(m, (combinedKeyProperty ?? keyProperty).GetValue(fm));
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
        private async Task<T> getById<T>(string trueKey, object partition, Expression<Func<M, bool>> selectFilter)
        {


            if (selectFilter == null)
            {
                var query = Table(1, partition)
                   .Where(GetKeySelector(trueKey));
                return await FirstOrDefault<T>(query);
                //PartitionKey pKey = null;
                //if (partition != null) pKey = new PartitionKey(partition);
                //try
                //{
                //    var document = await connection.Client
                //        .ReadDocumentAsync<M>(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, trueKey),
                //        pKey == null ? null : new RequestOptions { PartitionKey = pKey });
                //    if (typeof(M) == typeof(T)) return (T)(dynamic)document;

                //    var res = document;
                //    return GetCompiledExpression<T>()(res);
                //}
                //catch (Exception ex)
                //{
                //    DocumentClientException e = (ex as DocumentClientException) ?? (ex.InnerException as DocumentClientException);
                //    if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                //    {
                //        return default(T);
                //    }
                //    else
                //    {
                //        throw;
                //    }
                //}
            }
            else
            {
                var query = Table(1, partition)
                    .Where(selectFilter)
                    .Where(GetKeySelector(trueKey));
                return await FirstOrDefault<T>(query);
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
        private List<PropertyInfo> unrollExpression(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Quote) exp = (exp as UnaryExpression).Operand;
            LambdaExpression expression = exp as LambdaExpression;
            exp = expression.Body;
            var res = new List<PropertyInfo>();
            while (exp.NodeType == ExpressionType.MemberAccess)
            {
                var access = exp as MemberExpression;
                res.Add(access.Member as PropertyInfo);
                exp = access.Expression;
            }
            res.Reverse();
            return res;
        }
        private Func<IQueryable<M>, IOrderedQueryable<M>> convertSorting<K, L> (Func<IQueryable<K>, IOrderedQueryable<K>> sorting)
            where K: L
        {
            return null;
            var res = (sorting(new SortingConversionQueryable<K> {
                Expression = Expression.New(typeof(SortingConversionQueryable<K>))
            }) as SortingConversionQueryable<K>).GetSorting();

            if (res == null || res.Count == 0) return null;
            var copier = RecursiveCopiersCache.Get<M, K>();
            var i = 0;
            foreach(var pair in res)
            {
                var unrolled = unrollExpression(pair.Path);
                var inv = (copier as ICopierInverter).InvertPropertyChain(unrolled);
                if (inv == null) return null;
                pair.Path = inv.CloneExpression((inv as LambdaExpression).Parameters[0], Expression.Parameter(typeof(M), "sorting_par"+i));
                i++;
            }

            return x =>
            {
                IOrderedQueryable<M> y=null;
                foreach (var pair in res)
                {
                    if (y == null)
                    {
                        if (pair.Asc) y = x.OrderBy(pair.Path as LambdaExpression);
                        else y = x.OrderByDescending(pair.Path as LambdaExpression);
                    }
                    else
                    {
                        if (pair.Asc) y = y.ThenBy(pair.Path as LambdaExpression);
                        else y = y.ThenByDescending(pair.Path as LambdaExpression);
                    }
                }
                return y;
            };
        }
        private IQueryable<T2> InternalGetPageExtended<T1, T2>(
            Expression<Func<T1, bool>> filter,
            Func<IQueryable<T2>, IOrderedQueryable<T2>> sorting,
            int page,
            int itemsPerPage,
            Func<IQueryable<T1>, IQueryable<T2>> grouping,
            out Func<IQueryable, int, int, IEnumerable> getKeys,
            Func<IQueryable<M>, IOrderedQueryable<M>> origSorting
            )
         {
            //if (sorting == null) throw new ArgumentNullException(nameof(sorting));

            IQueryable<M> start;
            if(itemsPerPage>0)
                start = connection.Client.CreateDocumentQuery<M>(
                    UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId),
                        new FeedOptions { MaxItemCount = itemsPerPage });
            else
                start = connection.Client.CreateDocumentQuery<M>(
                    UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId));

            if (selectFilter != null) start = start.Where(selectFilter);
            if (origSorting != null) start = origSorting(start);

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
        public virtual async Task<DataPage<T1>> GetPageExtended<T1, T2>(Expression<Func<T1, bool>> filter, Func<IQueryable<T2>, IOrderedQueryable<T2>> sorting, int page, int itemsPerPage, Func<IQueryable<T1>, IQueryable<T2>> grouping = null) 
            where T2 : T1
        {
            bool noCount = false;
            var origSorting = convertSorting<T2, T1>(sorting);
            var finalSorting = sorting;
            if (origSorting != null) sorting = null;
            if (page < 0)
            {
                noCount = true;
                page = -page;
            }
            page = page - 1;
            if (page < 0) page = 0;
            Func<IQueryable, int, int, IEnumerable> getKeys;
            var toGroup = InternalGetPageExtended<T1, T2>(
                filter, sorting, page, itemsPerPage, grouping,
                out getKeys, origSorting
                );
            var res = new DataPage<T1>
            {
                TotalCount = noCount ? -1 : await toGroup.CountAsync(),
                ItemsPerPage = itemsPerPage,
                Page = page + 1,
                TotalPages=-1
            };
            if (!noCount)
            {
                res.TotalPages = res.TotalCount / itemsPerPage;
                if (res.TotalCount % itemsPerPage > 0) res.TotalPages++;
            }
            var sorted = sorting != null  ? sorting(toGroup) : toGroup;
            //if (page > 0) toGroup = itemsPerPage > 0 ? sorted.Skip(page * itemsPerPage).Take(itemsPerPage) : sorted;
            //else toGroup = itemsPerPage > 0 ?  sorted.Take(itemsPerPage) : sorted;
            toGroup = itemsPerPage > 0 && getKeys ==null ? sorted.Take((page+1)*itemsPerPage) : sorted;
            if (getKeys != null)
            {
                var allIds = typeof(T1) == typeof(T2) ? 
                    getKeys(toGroup, itemsPerPage>0 ? page * itemsPerPage : 0, itemsPerPage > 0 ? (page+1) * itemsPerPage : 0) : 
                    getKeys(toGroup.Project().To<T1>(),itemsPerPage > 0 ? page * itemsPerPage : 0, itemsPerPage > 0 ? (page + 1) * itemsPerPage : 0);
                IQueryable<M> toStart;
                if(itemsPerPage>0)
                    toStart = connection.Client.CreateDocumentQuery<M>(
                        UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId),
                        new FeedOptions { MaxItemCount = itemsPerPage}).Where(BuildKeysFilter(allIds));
                else
                    toStart = connection.Client.CreateDocumentQuery<M>(
                        UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId))
                            .Where(BuildKeysFilter(allIds));
                var selectionExpression = GetProprtiesSelectionExpression<T1>();
                if(selectionExpression != null)
                {
                    var iQuery = toStart.Select(selectionExpression);
                    List<M> pResults = new List<M>();
                    var fQuery = iQuery.AsDocumentQuery();

                    while (fQuery.HasMoreResults)
                    {
                        pResults.AddRange(await fQuery.ExecuteNextAsync<M>());
                    }
                    var copier = RecursiveCopiersCache.Get<M, T1>();
                    var fres=pResults.Select(m => (T2)copier.Copy(m, default(T1)));
                    if (finalSorting != null) fres = finalSorting(fres.AsQueryable());
                    res.Data = fres.AsEnumerable().Select(m => (T1)m).ToList();
                    return res;
                }
                var projExp = GetExpression<T1>();
                IQueryable<T1> proj = null;
                if (projExp!=null)
                    proj = toStart.Select(projExp);
                else
                    proj = toStart.Project().To<T1>();
                toGroup = proj as IQueryable<T2>;
                if (toGroup == null) toGroup = proj.Project().To<T2>();
                if(sorting != null) toGroup = sorting(toGroup);
            }
            List<T1> results = new List<T1>();
            int count=0;
            int toSkip = itemsPerPage > 0 ? page * itemsPerPage : 0;
            if (typeof(T1) == typeof(T2) )
            {
                var fQuery = toGroup.AsDocumentQuery();
                
                while (fQuery.HasMoreResults)
                {
                    var curr = await fQuery.ExecuteNextAsync<T1>();
                    if(count>=toSkip)
                        results.AddRange(curr);
                    count += curr.Count;
                }
            }
            else if(grouping==null)
            {
                var fQuery = toGroup.Project().To<T1>().AsDocumentQuery();

                while (fQuery.HasMoreResults)
                {
                    var curr = await fQuery.ExecuteNextAsync<T1>();
                    if (count >= toSkip)
                        results.AddRange(curr);
                    count += curr.Count;
                }
            }
            else
            {
                var fQuery = toGroup.AsDocumentQuery();

                while (fQuery.HasMoreResults)
                {
                    var curr = await fQuery.ExecuteNextAsync<T2>();
                    if (count >= toSkip)
                        results.AddRange(curr.Select(m => (T1)m));
                    count += curr.Count;
                    
                }
            }

            res.Data = results;
            return res;
        }
        private async Task handleAddition(
            M item, 
            UpdateOperationsStatus<M> succeded, 
            UpdateOperationsStatus<M> failed,
            DocumentsUpdateSimulationResult<M> simulation,
            ConcurrentBag<Exception> exceptions)
        {
            
            var uri = UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId);
            try
            {
                if (simulation != null) simulation.Additions.Add(item);
                else await connection.Client.CreateDocumentAsync(uri, item);
                succeded.Additions.Add(item);
            }
            catch(Exception ex)
            {
                exceptions.Add(ex);
                failed.Additions.Add(item);
            }
            
        }
        private async Task handleFullUpdate(
            M item,
            UpdateOperationsStatus<M> succeded,
            UpdateOperationsStatus<M> failed,
            DocumentsUpdateSimulationResult<M> simulation,
            ConcurrentBag<Exception> exceptions)
        {

            var uri = UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId);
            try
            {
                
                
                string key = keyProperty.GetValue(item) as string;
                object partitionKey = partitionProperty.GetValue(item);
                if (modificationFilter != null)
                {
                    var res = await Table(1, partitionKey)
                            .Where(modificationFilter)
                            .Where(GetKeySelector(key))
                            .CountAsync();
                    if (res >= 1)
                    {
                        if (simulation != null) simulation.Updates.Add(item);
                        else
                            await connection.Client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, key),
                                item, partitionKey == null ? null : new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    }
                            
                }
                else
                {
                    if (simulation != null) simulation.Updates.Add(item);
                    else
                        await connection.Client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, key),
                            item, partitionKey == null ? null : new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                }    
                succeded.FullUpdates.Add(item);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                failed.FullUpdates.Add(item);
            }

        }
        private async Task handlePartialUpdate(
            Tuple<Action<M>, string, object> item,
            UpdateOperationsStatus<M> succeded,
            UpdateOperationsStatus<M> failed,
            DocumentsUpdateSimulationResult<M> simulation,
            ConcurrentBag<Exception> exceptions)
        {
            try
            {
                M old = await getById<M>(item.Item2, item.Item3, modificationFilter);
                if(old != null)
                {
                    item.Item1(old);
                    if (simulation != null) simulation.Updates.Add(old);
                    else
                        await connection.Client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, item.Item2),
                            item, item.Item3 == null ? null : new RequestOptions { PartitionKey = new PartitionKey(item.Item3) });
                    succeded.PartialUpdates.Add(item);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                failed.PartialUpdates.Add(item);
            }
        }
        private async Task handleDelete(
            Tuple<string, object> item,
            UpdateOperationsStatus<M> succeded,
            UpdateOperationsStatus<M> failed,
            DocumentsUpdateSimulationResult<M> simulation,
            ConcurrentBag<Exception> exceptions)
        {
            try
            {
                if (modificationFilter != null)
                {
                    var res = await Table(1, item.Item2)
                            .Where(modificationFilter)
                            .Where(GetKeySelector(item.Item1))
                            .CountAsync();
                    if (res >= 1)
                    {
                        if (simulation != null) simulation.Deletes.Add(item);
                        else
                            await connection.Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, item.Item1),
                        item.Item2 == null ? null : new RequestOptions { PartitionKey = new PartitionKey(item.Item2) });
                    }
                }
                else
                {
                    if (simulation != null) simulation.Deletes.Add(item);
                    else
                        await connection.Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(connection.DatabaseId, collectionId, item.Item1),
                    item.Item2 == null ? null : new RequestOptions { PartitionKey = new PartitionKey(item.Item2) });
                }
                succeded.Deletes.Add(item);
            }
            catch(Exception ex)
            {
                exceptions.Add(ex);
                failed.Deletes.Add(item);
            }
        }
        public virtual async Task SaveChanges()
        {
            var succ = new UpdateOperationsStatus<M>();
            var failed = new UpdateOperationsStatus<M>();
            DocumentsUpdateSimulationResult<M> simulation =
                simulateOperations == SimulateOperations.None ?
                    null
                    :
                    new DocumentsUpdateSimulationResult<M>();
            var exceptions = new ConcurrentBag<Exception>();
            List<Task> allTasks = new List<Task>();

            if (toAdd != null)
            {
                
                foreach (var item in toAdd)
                {
                    allTasks.Add(handleAddition(item, 
                        succ, 
                        failed, 
                        (simulateOperations & SimulateOperations.Add) == 
                            SimulateOperations.Add ? simulation : null, 
                        exceptions));
                    
                }
                
            }
            if(toDelete != null)
            {
                foreach (var item in toDelete)
                {
                    allTasks.Add(handleDelete(item, 
                        succ, 
                        failed,
                        (simulateOperations & SimulateOperations.Delete) ==
                            SimulateOperations.Delete ? simulation : null, 
                        exceptions));
                }
                
            }
            if(toModifyFull != null)
            {
                foreach(var item in toModifyFull)
                {
                    allTasks.Add(handleFullUpdate(item,
                        succ,
                        failed,
                        (simulateOperations & SimulateOperations.Update) ==
                            SimulateOperations.Update ? simulation : null,
                        exceptions));
                }
                
            }
            if (toModifyPartial != null)
            {

                foreach (var item in toModifyPartial)
                {
                    allTasks.Add(handlePartialUpdate(item,
                        succ,
                        failed,
                        (simulateOperations & SimulateOperations.Update) ==
                            SimulateOperations.Update ? simulation : null,
                        exceptions));
                }
                
            }
            await Task.WhenAll(allTasks);
            toModifyPartial = null;
            toModifyFull = null;
            toDelete = null;
            toAdd = null;
            SimulationResult = simulation;
            if (exceptions.Count > 0)
                throw new DocumentsUpdateException<M>(exceptions, failed, succ);
            
        }
        public async Task RetryChanges(DocumentsUpdateException<M> exception)
        {
            toAdd = exception.Failed.Additions.ToList();
            toModifyFull= exception.Failed.FullUpdates.ToList();
            toModifyPartial = exception.Failed.PartialUpdates.ToList();
            toDelete = exception.Failed.Deletes.ToList();
            await SaveChanges();
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

        public IQueryable<M> Table(int? pageSize=null, object partitionKey=null, string continuationToken=null)
        {
            return connection.Client.CreateDocumentQuery<M>(UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId),
                partitionKey != null ?
                    new FeedOptions { MaxItemCount = pageSize, PartitionKey = new PartitionKey(partitionKey), RequestContinuation = continuationToken } :
                    new FeedOptions { MaxItemCount = pageSize, RequestContinuation=continuationToken });
        }
        public IQueryable<N> Table<N>(int? pageSize = null, object partitionKey = null, string continuationToken = null)
        {
            return connection.Client.CreateDocumentQuery<N>(UriFactory.CreateDocumentCollectionUri(connection.DatabaseId, collectionId),
                partitionKey == null ?
                    new FeedOptions { MaxItemCount = pageSize, PartitionKey = new PartitionKey(partitionKey), RequestContinuation = continuationToken } :
                    new FeedOptions { MaxItemCount = pageSize, RequestContinuation = continuationToken });
        }
        public async Task<IList<K>>  ToList<K>(IQueryable<M> query, int toSkip=0)
        {
            var selection=GetProprtiesSelectionExpression<K>();
            query = query.Select(selection);
            var fquery = query.AsDocumentQuery<M>();
            var res = new List<M>();
            int count = 0;
            while(fquery.HasMoreResults)
            {
                var pres = await fquery.ExecuteNextAsync<M>();
                if (count >= toSkip)
                    res.AddRange(pres);
            };
            if (typeof(K) == typeof(M)) return res as List<K>;
            var copier = RecursiveCopiersCache.Get<M, K>();
            return res.Select(m => copier.Copy(m, default(K)))
                .ToList();
        }
        public async Task<DataSequence<K, string>> ToSequence<K>(IQueryable<M> query)
        {
            var selection = GetProprtiesSelectionExpression<K>();
            query = query.Select(selection);
            var fquery = query.AsDocumentQuery<M>();
            
            var pres = await fquery.ExecuteNextAsync<M>();
            var res = new DataSequence<K, string>
            {
                Continuation = pres.ResponseContinuation
            };
            if (typeof(K) == typeof(M))
            {
                res.Data = pres.ToList() as List<K>;
                return res;
            }
            var copier = RecursiveCopiersCache.Get<M, K>();
            res.Data = pres.Select(m => copier.Copy(m, default(K)))
                .ToList();
            return res;
        }
        public async Task<K> FirstOrDefault<K>(IQueryable<M> query) 
        {
            var selection = GetProprtiesSelectionExpression<K>();
            query = query.Select(selection);
            var fquery = query.AsDocumentQuery<M>();

            var pres = (await fquery.ExecuteNextAsync<M>()).FirstOrDefault();
            if (typeof(K) == typeof(M)) return pres == null ? default(K) : (K)(pres as object);
            var copier = RecursiveCopiersCache.Get<M, K>();
            return copier.Copy(pres, default(K));
        }
    }
}
