﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;
using FrameDAL.Core;
using FrameDAL.Attributes;

namespace FrameDAL.Query
{
    public abstract class BaseQuery : IQuery
    {
        public ISession Session 
        {
            get
            {
                return Sess;
            }
            set
            {
                Sess = (BaseSession) value;
            }
        }

        protected BaseSession Sess { get; set; }

        public string SqlText { get; set; }

        public object[] Parameters { get; set; }

        public int FirstResult { get; set; }

        public int PageSize { get; set; }

        public int? ExecuteNonQuery()
        {
            if (Sess.DbHelper.InTransaction())
            {
                return Sess.DbHelper.ExecuteNonQuery(SqlText, Parameters);
            }
            else
            {
                Sess.AddToCache(SqlText, Parameters);
                return null;
            }
        }

        public object ExecuteScalar()
        {
            try
            {
                Sess.BeginTransaction();
                object result = Sess.DbHelper.ExecuteScalar(SqlText, Parameters);
                Sess.CommitTransaction();
                return result;
            }
            catch
            {
                Sess.RollbackTransaction();
                throw;
            }
        }

        protected abstract void BeforeQuery();

        public DataSet ExecuteGetDataSet()
        {
            try
            {
                Sess.BeginTransaction();
                BeforeQuery();
                DataSet result = Sess.DbHelper.ExecuteGetDataSet(SqlText, Parameters);
                Sess.CommitTransaction();
                return result;
            }
            catch
            {
                Sess.RollbackTransaction();
                throw;
            }
        }

        public DataTable ExecuteGetDataTable()
        {
            try
            {
                Sess.BeginTransaction();
                BeforeQuery();
                DataTable result = Sess.DbHelper.ExecuteGetDataTable(SqlText, Parameters);
                Sess.CommitTransaction();
                return result;
            }
            catch
            {
                Sess.RollbackTransaction();
                throw;
            }
        }

        public List<T> ExecuteGetList<T>()
        {
            List<T> results = new List<T>();
            DataTable dt = ExecuteGetDataTable();
            foreach (DataRow row in dt.Rows)
            {
                T entity = (T)AppContext.Instance.GetConstructor(typeof(T)).Invoke(null);
                foreach (PropertyInfo prop in AppContext.Instance.GetProperties(typeof(T)))
                {
                    Column col = AppContext.Instance.GetColumn(prop);
                    if(col == null) continue;
                    AppContext.Instance.SetPropertyValue(entity, prop, row[col.Name]);
                }
                results.Add(entity);
            }
            return results;
        }

        public T ExecuteGetEntity<T>()
        {
            List<T> results = ExecuteGetList<T>();
            if (results.Count > 0)
                return ExecuteGetList<T>()[0];
            else
                return default(T);
        }
    }
}
