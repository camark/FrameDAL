﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrameDAL.Dialect
{
    public class OracleDialect : BaseDialect
    {
        internal OracleDialect() { }

        /// <summary>
        /// 执行查询之前，对SQL命令进行预处理
        /// </summary>
        /// <param name="sqlText">要进行预处理的SQL</param>
        /// <param name="firstResult">要返回的第一条结果的索引，该索引从0开始</param>
        /// <param name="pageSize">返回的结果数量</param>
        /// <returns>返回预处理后的SQL命令</returns>
        public override string GetPagingSql(string sqlText, int firstResult, int pageSize)
        {
            if (pageSize == 0) return sqlText;
            int minRowNum = firstResult + 1;
            int maxRowNum = firstResult + pageSize;
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM ");
            sb.Append("( ");
            sb.Append("SELECT ROWNUM RN, A.* ");
            sb.Append("FROM (" + sqlText + ") A ");
            sb.Append("WHERE ROWNUM<=" + maxRowNum);
            sb.Append(") ");
            sb.Append("WHERE RN>=" + minRowNum);
            return sb.ToString();
        }
    }
}
