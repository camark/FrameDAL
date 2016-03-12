﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using FrameDAL.Core;
using FrameDAL.Config;
using FrameDAL.Utility;
using FrameDAL.Attributes;
using FrameDAL.SqlExpressions;

namespace FrameDAL.Linq.Translation
{
    public sealed class TranslateResult
    {
        public SqlExpression SqlExpression { get; private set; }

        public Expression Projector { get; private set; }

        public LambdaExpression Aggregator { get; private set; }

        public TranslateResult(SqlExpression sqlExpr, Expression projector, LambdaExpression aggregator)
        {
            this.SqlExpression = sqlExpr;
            this.Projector = projector;
            this.Aggregator = aggregator;
        }

        public TranslateResult(SqlExpression sqlExpr, Expression projector) : this(sqlExpr, projector, null) { }

        public TranslateResult(SqlExpression sqlExpr) : this(sqlExpr, null, null) { }
    }

    public class ExpressionTranslator : ExpressionVisitor
    {
        private Dictionary<ParameterExpression, Expression> map;
        private int aliasCount;
        private Configuration config;

        private ExpressionTranslator(Configuration config)
        {
            this.config = config;
            this.map = new Dictionary<ParameterExpression, Expression>();
        }

        public static TranslateResult Translate(Expression expr, Configuration config)
        {
            return new ExpressionTranslator(config).TranslateVisit(expr).TranslateResult;
        }

        private class Bundle
        {
            public Expression Expression { get; private set; }

            public TranslateResult TranslateResult { get; private set; }

            public Bundle(Expression expression, TranslateResult translateResult)
            {
                this.Expression = expression;
                this.TranslateResult = translateResult;
            }
        }

        private TranslateResult _curResult;
        private bool _flag;

        private TranslateResult CurrentResult
        {
            get { return _curResult; }
            set
            {
                _curResult = value;
                _flag = !_flag;
                if (!_flag) throw new Exception("Debug Exception!");
            }
        }

        private class Status
        {
            public TranslateResult CurrentResult { get; private set; }

            public bool Flag { get; private set; }

            public Status(TranslateResult curResult, bool flag)
            {
                this.CurrentResult = curResult;
                this.Flag = flag;
            }
        }

        private Status PreTranslate()
        {
            Status status = new Status(_curResult, _flag);
            _curResult = null;
            _flag = false;
            return status;
        }

        private TranslateResult PostTranslate(Status status)
        {
            TranslateResult result = _curResult;
            _curResult = status.CurrentResult;
            _flag = status.Flag;
            return result;
        }

        private Bundle TranslateVisit(Expression expr)
        {
            Status status = PreTranslate();
            expr = this.Visit(expr);
            return new Bundle(expr, PostTranslate(status));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) || m.Method.DeclaringType == typeof(Enumerable))
            {
                switch (m.Method.Name)
                {
                    case "Where":
                        return this.VisitWhere(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                    case "Select":
                        return this.VisitSelect(m.Type, m.Arguments[0], (LambdaExpression)StripQuotes(m.Arguments[1]));
                }
                throw new NotSupportedException("不支持的方法：" + m.Method.Name);
            }
            return base.VisitMethodCall(m);
        }

        private Expression VisitWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            Bundle bundle = this.TranslateVisit(source);
            this.map[predicate.Parameters[0]] = bundle.TranslateResult.Projector;
            SqlExpression where = this.TranslateVisit(predicate.Body).TranslateResult.SqlExpression;

            return null;
        }

        private Expression VisitSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            return null;
        }

        protected override Expression VisitConstant(System.Linq.Expressions.ConstantExpression node)
        {
            if(IsTable(node.Value))
            {
                CurrentResult = GetDefaultProjection((IQueryable) node.Value);
            }
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Expression e;
            if (this.map.TryGetValue(node, out e))
            {
                return e;
            }
            return node;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            Expression source = this.Visit(m.Expression);
            switch(source.NodeType)
            {
                case ExpressionType.Call:
                    MethodCallExpression call = (MethodCallExpression)source;
                    if(call != null && call.Method.Name == "NewDefaultProjectedObject")
                    {
                        var bindings = ((System.Linq.Expressions.ConstantExpression)call.Arguments[0]).Value as List<MemberBinding>;
                        for(int i = 0, n = bindings.Count; i < n; i++)
                        {
                            MemberAssignment assign = bindings[i] as MemberAssignment;
                            if (assign != null && MembersMatch(assign.Member, m.Member))
                            {
                                return assign.Expression;
                            }
                        }
                    }
                    break;
                case ExpressionType.MemberInit:
                    MemberInitExpression min = (MemberInitExpression)source;
                    for (int i = 0, n = min.Bindings.Count; i < n; i++)
                    {
                        MemberAssignment assign = min.Bindings[i] as MemberAssignment;
                        if (assign != null && MembersMatch(assign.Member, m.Member))
                        {
                            return assign.Expression;
                        }
                    }
                    break;
                case ExpressionType.New:
                    NewExpression nex = (NewExpression)source;
                    if (nex.Members != null)
                    {
                        for (int i = 0, n = nex.Members.Count; i < n; i++)
                        {
                            if (MembersMatch(nex.Members[i], m.Member))
                            {
                                return nex.Arguments[i];
                            }
                        }
                    }
                    break;
            }
            if(source == m.Expression)
            {
                return m;
            }
            return Expression.MakeMemberAccess(source, m.Member);
        }

        protected override Expression VisitUnary(System.Linq.Expressions.UnaryExpression node)
        {
            SqlExpressionType? exprType = null;
            switch(node.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    exprType = SqlExpressionType.Negate;
                    break;
                case ExpressionType.Not:
                    exprType = SqlExpressionType.Not;
                    break;
                case ExpressionType.UnaryPlus:
                    exprType = SqlExpressionType.UnaryPlus;
                    break;
            }
            if(exprType != null)
            {
                Bundle bundle = this.TranslateVisit(node.Operand);
                this.CurrentResult = new TranslateResult(
                    new FrameDAL.SqlExpressions.UnaryExpression(
                        exprType.Value, bundle.TranslateResult.SqlExpression));
                if(bundle.Expression != node.Operand)
                {
                    return Expression.MakeUnary(node.NodeType, bundle.Expression, node.Type);
                }
                return node;
            }
            return base.VisitUnary(node);
        }

        protected override Expression VisitBinary(System.Linq.Expressions.BinaryExpression node)
        {
            SqlExpressionType? exprType = null;
            switch(node.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    exprType = SqlExpressionType.Add;
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    exprType = SqlExpressionType.Subtract;
                    break;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    exprType = SqlExpressionType.Multiply;
                    break;
                case ExpressionType.Divide:
                    exprType = SqlExpressionType.Divide;
                    break;
                case ExpressionType.Modulo:
                    exprType = SqlExpressionType.Modulo;
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    exprType = SqlExpressionType.And;
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    exprType = SqlExpressionType.Or;
                    break;
                case ExpressionType.LessThan:
                    exprType = SqlExpressionType.LessThan;
                    break;
                case ExpressionType.LessThanOrEqual:
                    exprType = SqlExpressionType.LessThanOrEqual;
                    break;
                case ExpressionType.GreaterThan:
                    exprType = SqlExpressionType.GreaterThan;
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    exprType = SqlExpressionType.GreaterThanOrEqual;
                    break;
                case ExpressionType.Equal:
                    exprType = SqlExpressionType.Equal;
                    break;
                case ExpressionType.NotEqual:
                    exprType = SqlExpressionType.NotEqual;
                    break;
                case ExpressionType.RightShift:
                    exprType = SqlExpressionType.RightShift;
                    break;
                case ExpressionType.LeftShift:
                    exprType = SqlExpressionType.LeftShift;
                    break;
                case ExpressionType.ExclusiveOr:
                    exprType = SqlExpressionType.ExclusiveOr;
                    break;
            }
            if(exprType != null)
            {
                Bundle left = this.TranslateVisit(node.Left);
                Bundle right = this.TranslateVisit(node.Right);
                this.CurrentResult = new TranslateResult(
                    new FrameDAL.SqlExpressions.BinaryExpression(
                        exprType.Value, left.TranslateResult.SqlExpression, right.TranslateResult.SqlExpression));
                if(left.Expression != node.Left || right.Expression != node.Right)
                {
                    return Expression.MakeBinary(node.NodeType, left.Expression, right.Expression);
                }
                return node;
            }
            return base.VisitBinary(node);
        }

        private bool MembersMatch(MemberInfo a, MemberInfo b)
        {
            if (a == b)
            {
                return true;
            }
            if (a is MethodInfo && b is PropertyInfo)
            {
                return a == ((PropertyInfo)b).GetGetMethod();
            }
            else if (a is PropertyInfo && b is MethodInfo)
            {
                return ((PropertyInfo)a).GetGetMethod() == b;
            }
            return false;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((System.Linq.Expressions.UnaryExpression)e).Operand;
            }
            return e;
        }

        private ProjectedColumns ProjectColumns(Expression expression, string newAlias, params string[] existingAliases)
        {
            return ColumnProjector.ProjectColumns(expression, null, newAlias, existingAliases);
        }

        private bool IsTable(object value)
        {
            IQueryable q = value as IQueryable;
            return q != null && q.Expression.NodeType == ExpressionType.Constant;
        }

        private string GetNextAlias()
        {
            return "t" + aliasCount++;
        }

        private TranslateResult GetDefaultProjection(IQueryable query)
        {
            string tableAlias = this.GetNextAlias();
            string selectAlias = this.GetNextAlias();
            List<MemberBinding> bindings = new List<MemberBinding>();
            List<ColumnDeclaration> columns = new List<ColumnDeclaration>();
            foreach(PropertyInfo prop in query.ElementType.GetCachedProperties())
            {
                ColumnAttribute column = prop.GetColumnAttribute();
                if (column == null) continue;
                bindings.Add(Expression.Bind(prop, Expression.Constant(new ColumnExpression(selectAlias, column.Name))));
                SqlExpression c;
                if(string.IsNullOrWhiteSpace(column.SQL))
                {
                    c = new ColumnExpression(tableAlias, column.Name);
                }
                else
                {
                    c = new LiteralExpression(column.SQL);
                }
                columns.Add(new ColumnDeclaration(column.Name, c));
            }
            Expression projector = Expression.Call(Expression.Constant(this), newDefaultProjectedObject, Expression.Constant(bindings));
            SqlExpression select = new SelectExpression(
                selectAlias,
                columns,
                new TableExpression(tableAlias, query.ElementType.GetTableAttribute().Name),
                null
                );
            return new TranslateResult(select, projector);
        }

        private static MethodInfo newDefaultProjectedObject 
            = typeof(ExpressionTranslator).GetMethod("NewDefaultProjectedObject", new Type[1] { typeof(IEnumerable<MemberBinding>) });

        internal T NewDefaultProjectedObject<T>(IEnumerable<MemberBinding> bindings)
        {
            object proxy = EntityFactory.GetEntity(typeof(T), config.EnableLazy, false);
            foreach(MemberBinding binding in bindings)
            {
                MemberAssignment assign = (MemberAssignment)binding;
                object value = ((System.Linq.Expressions.ConstantExpression)assign.Expression).Value;
                assign.Member.SetValue(proxy, value);
            }
            return (T)proxy;
        }
    }
}
