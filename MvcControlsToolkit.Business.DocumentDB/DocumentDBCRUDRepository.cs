using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MvcControlsToolkit.Core.Business.Utilities;
namespace MvcControlsToolkit.Business.DocumentDB
{
    public class DocumentDBCRUDRepository<M> : ICRUDRepository
    {
        public Func<object, object> GetKey => throw new NotImplementedException();

        public void Add<T>(bool full, params T[] viewModel)
        {
            throw new NotImplementedException();
        }

        public void Delete<U>(params U[] key)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetById<T, U>(U key)
        {
            throw new NotImplementedException();
        }

        public Task<DataPage<T>> GetPage<T>(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IOrderedQueryable<T>> sorting, int page, int itemsPerPage, Func<IQueryable<T>, IQueryable<T>> grouping = null)
        {
            throw new NotImplementedException();
        }

        public Task<DataPage<T1>> GetPageExtended<T1, T2>(Expression<Func<T1, bool>> filter, Func<IQueryable<T2>, IOrderedQueryable<T2>> sorting, int page, int itemsPerPage, Func<IQueryable<T1>, IQueryable<T2>> grouping = null) where T2 : T1
        {
            throw new NotImplementedException();
        }

        public Task SaveChanges()
        {
            throw new NotImplementedException();
        }

        public void Update<T>(bool full, params T[] viewModel)
        {
            throw new NotImplementedException();
        }

        public void UpdateKeys()
        {
            throw new NotImplementedException();
        }

        public void UpdateList<T>(bool full, IEnumerable<T> oldValues, IEnumerable<T> newValues)
        {
            throw new NotImplementedException();
        }
    }
}
