// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Query.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class ValueConverterCompensatingExpressionVisitor : SqlExpressionVisitor
    {
        private readonly ISqlExpressionFactory _sqlExpressionFactory;

        private bool _insidePredicate;
        private bool _insideBoolComparison;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public ValueConverterCompensatingExpressionVisitor(
            [NotNull] ISqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitCase(CaseExpression caseExpression)
        {
            Check.NotNull(caseExpression, nameof(caseExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var testIsCondition = caseExpression.Operand == null;
            _insidePredicate = false;
            var operand = (SqlExpression)Visit(caseExpression.Operand);
            var whenClauses = new List<CaseWhenClause>();
            foreach (var whenClause in caseExpression.WhenClauses)
            {
                _insidePredicate = testIsCondition;
                var test = (SqlExpression)Visit(whenClause.Test);
                _insidePredicate = false;
                var result = (SqlExpression)Visit(whenClause.Result);
                whenClauses.Add(new CaseWhenClause(test, result));
            }

            _insidePredicate = false;
            var elseResult = (SqlExpression)Visit(caseExpression.ElseResult);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return caseExpression.Update(operand, whenClauses, elseResult);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitCollate(CollateExpression collateExpression)
        {
            Check.NotNull(collateExpression, nameof(collateExpression));

            return collateExpression.Update((SqlExpression)Visit(collateExpression.Operand));
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitColumn(ColumnExpression columnExpression)
        {
            Check.NotNull(columnExpression, nameof(columnExpression));

            return _insidePredicate
                && !_insideBoolComparison
                && columnExpression.TypeMapping.ClrType == typeof(bool)
                && columnExpression.TypeMapping.Converter != null
                ? _sqlExpressionFactory.Equal(
                    columnExpression,
                    _sqlExpressionFactory.Constant(true, columnExpression.TypeMapping))
                : (Expression)columnExpression;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitExists(ExistsExpression existsExpression)
        {
            Check.NotNull(existsExpression, nameof(existsExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var subquery = (SelectExpression)Visit(existsExpression.Subquery);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return existsExpression.Update(subquery);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitFromSql(FromSqlExpression fromSqlExpression)
        {
            Check.NotNull(fromSqlExpression, nameof(fromSqlExpression));

            return fromSqlExpression;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitIn(InExpression inExpression)
        {
            Check.NotNull(inExpression, nameof(inExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var item = (SqlExpression)Visit(inExpression.Item);
            var subquery = (SelectExpression)Visit(inExpression.Subquery);
            var values = (SqlExpression)Visit(inExpression.Values);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return inExpression.Update(item, values, subquery);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitLike(LikeExpression likeExpression)
        {
            Check.NotNull(likeExpression, nameof(likeExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var match = (SqlExpression)Visit(likeExpression.Match);
            var pattern = (SqlExpression)Visit(likeExpression.Pattern);
            var escapeChar = (SqlExpression)Visit(likeExpression.EscapeChar);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return likeExpression.Update(match, pattern, escapeChar);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitSelect(SelectExpression selectExpression)
        {
            Check.NotNull(selectExpression, nameof(selectExpression));

            var changed = false;

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var projections = new List<ProjectionExpression>();
            foreach (var item in selectExpression.Projection)
            {
                var updatedProjection = (ProjectionExpression)Visit(item);
                projections.Add(updatedProjection);
                changed |= updatedProjection != item;
            }

            var tables = new List<TableExpressionBase>();
            foreach (var table in selectExpression.Tables)
            {
                var newTable = (TableExpressionBase)Visit(table);
                changed |= newTable != table;
                tables.Add(newTable);
            }

            _insidePredicate = true;
            var predicate = (SqlExpression)Visit(selectExpression.Predicate);
            _insidePredicate = false;
            changed |= predicate != selectExpression.Predicate;

            var groupBy = new List<SqlExpression>();
            foreach (var groupingKey in selectExpression.GroupBy)
            {
                var newGroupingKey = (SqlExpression)Visit(groupingKey);
                changed |= newGroupingKey != groupingKey;
                groupBy.Add(newGroupingKey);
            }

            _insidePredicate = true;
            var havingExpression = (SqlExpression)Visit(selectExpression.Having);
            _insidePredicate = false;
            changed |= havingExpression != selectExpression.Having;

            var orderings = new List<OrderingExpression>();
            foreach (var ordering in selectExpression.Orderings)
            {
                var orderingExpression = (SqlExpression)Visit(ordering.Expression);
                changed |= orderingExpression != ordering.Expression;
                orderings.Add(ordering.Update(orderingExpression));
            }

            var offset = (SqlExpression)Visit(selectExpression.Offset);
            changed |= offset != selectExpression.Offset;

            var limit = (SqlExpression)Visit(selectExpression.Limit);
            changed |= limit != selectExpression.Limit;

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return changed
                ? selectExpression.Update(
                    projections, tables, predicate, groupBy, havingExpression, orderings, limit, offset)
                : selectExpression;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitSqlBinary(SqlBinaryExpression sqlBinaryExpression)
        {
            Check.NotNull(sqlBinaryExpression, nameof(sqlBinaryExpression));

            var parentInsideBoolComparison = _insideBoolComparison;
            _insideBoolComparison = sqlBinaryExpression.OperatorType == ExpressionType.Equal
                || sqlBinaryExpression.OperatorType == ExpressionType.NotEqual;

            var left = (SqlExpression)Visit(sqlBinaryExpression.Left);
            var right = (SqlExpression)Visit(sqlBinaryExpression.Right);

            _insideBoolComparison = parentInsideBoolComparison;

            return sqlBinaryExpression.Update(left, right);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
        {
            Check.NotNull(sqlUnaryExpression, nameof(sqlUnaryExpression));

            return sqlUnaryExpression.Update((SqlExpression)Visit(sqlUnaryExpression.Operand));
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitSqlConstant(SqlConstantExpression sqlConstantExpression)
        {
            Check.NotNull(sqlConstantExpression, nameof(sqlConstantExpression));

            return sqlConstantExpression;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitSqlFragment(SqlFragmentExpression sqlFragmentExpression)
        {
            Check.NotNull(sqlFragmentExpression, nameof(sqlFragmentExpression));

            return sqlFragmentExpression;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
        {
            Check.NotNull(sqlFunctionExpression, nameof(sqlFunctionExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var instance = (SqlExpression)Visit(sqlFunctionExpression.Instance);
            SqlExpression[] arguments = default;
            if (!sqlFunctionExpression.IsNiladic)
            {
                arguments = new SqlExpression[sqlFunctionExpression.Arguments.Count];
                for (var i = 0; i < arguments.Length; i++)
                {
                    arguments[i] = (SqlExpression)Visit(sqlFunctionExpression.Arguments[i]);
                }
            }

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return sqlFunctionExpression.Update(instance, arguments);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitTableValuedFunction(TableValuedFunctionExpression tableValuedFunctionExpression)
        {
            Check.NotNull(tableValuedFunctionExpression, nameof(tableValuedFunctionExpression));

            // TODO: See issue#20180
            return tableValuedFunctionExpression;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitSqlParameter(SqlParameterExpression sqlParameterExpression)
        {
            Check.NotNull(sqlParameterExpression, nameof(sqlParameterExpression));

            return sqlParameterExpression;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitTable(TableExpression tableExpression)
        {
            Check.NotNull(tableExpression, nameof(tableExpression));

            return tableExpression;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitProjection(ProjectionExpression projectionExpression)
        {
            Check.NotNull(projectionExpression, nameof(projectionExpression));

            return projectionExpression.Update((SqlExpression)Visit(projectionExpression.Expression));
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitOrdering(OrderingExpression orderingExpression)
        {
            Check.NotNull(orderingExpression, nameof(orderingExpression));

            return orderingExpression.Update((SqlExpression)Visit(orderingExpression.Expression));
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitCrossJoin(CrossJoinExpression crossJoinExpression)
        {
            Check.NotNull(crossJoinExpression, nameof(crossJoinExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var table = (TableExpressionBase)Visit(crossJoinExpression.Table);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return crossJoinExpression.Update(table);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
        {
            Check.NotNull(crossApplyExpression, nameof(crossApplyExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var table = (TableExpressionBase)Visit(crossApplyExpression.Table);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return crossApplyExpression.Update(table);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
        {
            Check.NotNull(outerApplyExpression, nameof(outerApplyExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var table = (TableExpressionBase)Visit(outerApplyExpression.Table);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return outerApplyExpression.Update(table);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitInnerJoin(InnerJoinExpression innerJoinExpression)
        {
            Check.NotNull(innerJoinExpression, nameof(innerJoinExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var table = (TableExpressionBase)Visit(innerJoinExpression.Table);

            _insidePredicate = true;
            var joinPredicate = (SqlExpression)Visit(innerJoinExpression.JoinPredicate);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return innerJoinExpression.Update(table, joinPredicate);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitLeftJoin(LeftJoinExpression leftJoinExpression)
        {
            Check.NotNull(leftJoinExpression, nameof(leftJoinExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var table = (TableExpressionBase)Visit(leftJoinExpression.Table);

            _insidePredicate = true;
            var joinPredicate = (SqlExpression)Visit(leftJoinExpression.JoinPredicate);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return leftJoinExpression.Update(table, joinPredicate);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitScalarSubquery(ScalarSubqueryExpression scalarSubqueryExpression)
        {
            Check.NotNull(scalarSubqueryExpression, nameof(scalarSubqueryExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var subquery = (SelectExpression)Visit(scalarSubqueryExpression.Subquery);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return scalarSubqueryExpression.Update(subquery);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitRowNumber(RowNumberExpression rowNumberExpression)
        {
            Check.NotNull(rowNumberExpression, nameof(rowNumberExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var changed = false;
            var partitions = new List<SqlExpression>();
            foreach (var partition in rowNumberExpression.Partitions)
            {
                var newPartition = (SqlExpression)Visit(partition);
                changed |= newPartition != partition;
                partitions.Add(newPartition);
            }

            var orderings = new List<OrderingExpression>();
            foreach (var ordering in rowNumberExpression.Orderings)
            {
                var newOrdering = (OrderingExpression)Visit(ordering);
                changed |= newOrdering != ordering;
                orderings.Add(newOrdering);
            }

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return rowNumberExpression.Update(partitions, orderings);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitExcept(ExceptExpression exceptExpression)
        {
            Check.NotNull(exceptExpression, nameof(exceptExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var source1 = (SelectExpression)Visit(exceptExpression.Source1);
            var source2 = (SelectExpression)Visit(exceptExpression.Source2);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return exceptExpression.Update(source1, source2);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitIntersect(IntersectExpression intersectExpression)
        {
            Check.NotNull(intersectExpression, nameof(intersectExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var source1 = (SelectExpression)Visit(intersectExpression.Source1);
            var source2 = (SelectExpression)Visit(intersectExpression.Source2);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return intersectExpression.Update(source1, source2);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitUnion(UnionExpression unionExpression)
        {
            Check.NotNull(unionExpression, nameof(unionExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var source1 = (SelectExpression)Visit(unionExpression.Source1);
            var source2 = (SelectExpression)Visit(unionExpression.Source2);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return unionExpression.Update(source1, source2);
        }
    }
}
