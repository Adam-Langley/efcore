// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Cosmos.Query.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class CosmosValueConverterCompensatingExpressionVisitor : SqlExpressionVisitor
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
        public CosmosValueConverterCompensatingExpressionVisitor(
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
        protected override Expression VisitEntityProjection(EntityProjectionExpression entityProjectionExpression)
        {
            Check.NotNull(entityProjectionExpression, nameof(entityProjectionExpression));

            return entityProjectionExpression.Update(Visit(entityProjectionExpression.AccessExpression));
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
            var values = (SqlExpression)Visit(inExpression.Values);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return inExpression.Update(item, values);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitKeyAccess(KeyAccessExpression keyAccessExpression)
        {
            Check.NotNull(keyAccessExpression, nameof(keyAccessExpression));

            var result = keyAccessExpression.Update(Visit(keyAccessExpression.AccessExpression));

            if (_insidePredicate
                && !_insideBoolComparison
                && keyAccessExpression.TypeMapping.ClrType == typeof(bool)
                && keyAccessExpression.TypeMapping.Converter != null)
            {
                return _sqlExpressionFactory.Equal(
                    result,
                    _sqlExpressionFactory.Constant(true, result.TypeMapping));
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitObjectAccess(ObjectAccessExpression objectAccessExpression)
        {
            Check.NotNull(objectAccessExpression, nameof(objectAccessExpression));

            return objectAccessExpression.Update(Visit(objectAccessExpression.AccessExpression));
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitObjectArrayProjection(ObjectArrayProjectionExpression objectArrayProjectionExpression)
        {
            Check.NotNull(objectArrayProjectionExpression, nameof(objectArrayProjectionExpression));

            var accessExpression = Visit(objectArrayProjectionExpression.AccessExpression);
            var innerProjection = (EntityProjectionExpression)Visit(objectArrayProjectionExpression.InnerProjection);

            return objectArrayProjectionExpression.Update(accessExpression, innerProjection);
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
        protected override Expression VisitProjection(ProjectionExpression projectionExpression)
        {
            Check.NotNull(projectionExpression, nameof(projectionExpression));

            return projectionExpression.Update(Visit(projectionExpression.Expression));
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override Expression VisitRootReference(RootReferenceExpression rootReferenceExpression)
        {
            Check.NotNull(rootReferenceExpression, nameof(rootReferenceExpression));

            return rootReferenceExpression;
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

            var fromExpression = (RootReferenceExpression)Visit(selectExpression.FromExpression);
            changed |= fromExpression != selectExpression.FromExpression;

            _insidePredicate = true;
            var predicate = (SqlExpression)Visit(selectExpression.Predicate);
            _insidePredicate = false;
            changed |= predicate != selectExpression.Predicate;

            var orderings = new List<OrderingExpression>();
            foreach (var ordering in selectExpression.Orderings)
            {
                var orderingExpression = (SqlExpression)Visit(ordering.Expression);
                changed |= orderingExpression != ordering.Expression;
                orderings.Add(ordering.Update(orderingExpression));
            }

            var limit = (SqlExpression)Visit(selectExpression.Limit);
            var offset = (SqlExpression)Visit(selectExpression.Offset);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return changed
                ? selectExpression.Update(projections, fromExpression, predicate, orderings, limit, offset)
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
        protected override Expression VisitSqlConditional(SqlConditionalExpression sqlConditionalExpression)
        {
            Check.NotNull(sqlConditionalExpression, nameof(sqlConditionalExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = true;
            _insideBoolComparison = false;

            var test = (SqlExpression)Visit(sqlConditionalExpression.Test);
            _insidePredicate = false;
            var ifTrue = (SqlExpression)Visit(sqlConditionalExpression.IfTrue);
            var ifFalse = (SqlExpression)Visit(sqlConditionalExpression.IfFalse);

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return sqlConditionalExpression.Update(test, ifTrue, ifFalse);
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
        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
        {
            Check.NotNull(sqlFunctionExpression, nameof(sqlFunctionExpression));

            var parentInsidePredicate = _insidePredicate;
            var parentInsideBoolComparison = _insideBoolComparison;
            _insidePredicate = false;
            _insideBoolComparison = false;

            var arguments = new SqlExpression[sqlFunctionExpression.Arguments.Count];
            for (var i = 0; i < arguments.Length; i++)
            {
                arguments[i] = (SqlExpression)Visit(sqlFunctionExpression.Arguments[i]);
            }

            _insidePredicate = parentInsidePredicate;
            _insideBoolComparison = parentInsideBoolComparison;

            return sqlFunctionExpression.Update(arguments);
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
        protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
        {
            Check.NotNull(sqlUnaryExpression, nameof(sqlUnaryExpression));

            return sqlUnaryExpression.Update((SqlExpression)Visit(sqlUnaryExpression.Operand));
        }
    }
}
