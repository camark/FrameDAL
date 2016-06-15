﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;
using FrameDAL.Dialect;

namespace FrameDAL.DbHelper
{
    /// <summary>
    /// Author: Vincent Lau.
    /// Access数据库访问助手
    /// </summary>
    public class AccessHelper : BaseHelper
    {
        private IDialect _Dialect;

        /// <summary>
        /// 获取Access数据库方言
        /// </summary>
        public override IDialect Dialect
        {
            get { return _Dialect; }
        }

        public AccessHelper() 
        {
            _Dialect = new AccessDialect();
        }

        public AccessHelper(string connStr) : base(connStr) 
        {
            _Dialect = new AccessDialect();
        }

        /// <summary>
        /// 创建一个DbConnection对象
        /// </summary>
        /// <returns>DbConnection对象</returns>
        public override DbConnection NewConnection()
        {
            return new OleDbConnection(ConnectionString);
        }

        /// <summary>
        /// 使用给定参数创建DbCommand对象
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="trans">数据库事务</param>
        /// <param name="sqlText">含有问号占位符的SQL命令</param>
        /// <param name="parameters">SQL命令中的参数值</param>
        /// <returns>返回DbCommand对象</returns>
        public override DbCommand PrepareCommand(DbConnection conn, DbTransaction trans, string sqlText, params object[] parameters)
        {
            DbCommand cmd = new OleDbCommand();
            if (conn != null) cmd.Connection = conn;
            if (trans != null) cmd.Transaction = trans;

            cmd.CommandText = sqlText;
            if (parameters != null && parameters.Length != 0)
            {
                AddParamsToCmd(cmd as OleDbCommand, parameters);
            }
            return cmd;
        }

        /// <summary>
        /// 创建一个数据适配器对象
        /// </summary>
        /// <param name="cmd">命令对象</param>
        /// <returns>返回一个数据适配器对象</returns>
        public override DbDataAdapter NewDataAdapter(DbCommand cmd)
        {
            return new OleDbDataAdapter(cmd as OleDbCommand);
        }

        private void AddParamsToList(List<object> arr, ICollection parameters)
        {
            foreach (object param in parameters)
            {
                if (param is ICollection && !(param is byte[]))
                {
                    AddParamsToList(arr, param as ICollection);
                }
                else
                {
                    if(param == null)
                        throw new NotSupportedException("查询参数列表中有元素为null。");
                    arr.Add(param == null ? DBNull.Value : param);
                }
            }
        }

        private void AddParamsToCmd(OleDbCommand cmd, object[] parameters)
        {
            List<object> arr = new List<object>();
            AddParamsToList(arr, parameters);

            StringBuilder sb = new StringBuilder();
            string[] temp = cmd.CommandText.Split('?');
            for (int i = 0; i < temp.Length - 1; i++)
            {
                string paramName = "@param" + i;
                sb.Append(temp[i] + paramName);
                cmd.Parameters.AddWithValue(paramName, arr[i]);
            }
            sb.Append(temp[temp.Length - 1]);
            cmd.CommandText = sb.ToString();
        }
    }
}
