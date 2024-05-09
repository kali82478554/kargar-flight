﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Modules.Interface
{
    public interface IRepository<T>
    {
        void Add(T entity);
        void Remove(T entity);
        void Update(T entity);
        T GetById(int id);
        IEnumerable<T> GetAll();
        IQueryable<T> GetAllQueryable();
        IQueryable<T> Get(Func<T, bool> filter = null);
        int GetCount();
        bool Any();
    }
}
