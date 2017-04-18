using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace FenixRepo.Core
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FenixAttribute : Attribute
    {
        public FenixAttribute(params string[] migrations)
        {
        }
    }    

    public interface IFenixRepository<T> where T : class
    {
        T Add(T item);
        IEnumerable<T> AddRange(List<T> items);        
    }

    public class FenixRepository<T> : FenixRepositoryCreateTable<T>, IFenixRepository<T> where T : class
    {                        
        bool tableNotExistsException(Exception e)
        {           
            return ((e is DbUpdateException) && (e?.InnerException?.InnerException as SqlException)?.Number == 208) || ((e?.InnerException as SqlException).Number == 208);
        }

        TResult BaseWrapper<TResult>(Func<DbSet<T>, TResult> function) where TResult : class
        {            
            using (var context = Factory())
            {
                var table = context.Set<T>();
                TResult answer = null;
                try
                {                    
                    answer = function(context.Set<T>());                    
                    context.SaveChanges();
                    return answer;
                }
                catch (Exception e) when (tableNotExistsException(e))
                {                    
                    lock (typeof(T))
                    {
                        try
                        {                            
                            context.SaveChanges();                            
                            return answer;
                        }
                        catch (Exception e2) when (tableNotExistsException(e2))
                        {
                            CreateTable(context);                                
                        }                            
                    }
                    return BaseWrapper(function);                                        
                }
            }
        }

        public T Add(T item)
        {
            return BaseWrapper(table => table.Add(item));
        }

        public IEnumerable<T> AddRange(List<T> items)
        {
            return BaseWrapper(table => { table.AddRange(items); return items; });
        }        
    }
}
