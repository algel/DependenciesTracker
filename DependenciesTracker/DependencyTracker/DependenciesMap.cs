using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace DependenciesTracking
{
    public sealed class DependenciesMap<T> : IDependenciesMap<T>
    {
        private readonly IList<PathItemBase<T>> _mapItems = new List<PathItemBase<T>>();

        internal ReadOnlyCollection<PathItemBase<T>> MapItems { get; }

        public DependenciesMap()
        {
            MapItems = new ReadOnlyCollection<PathItemBase<T>>(_mapItems);
        }

        public IDependenciesMap<T> AddDependency<TProperty>(Action<T, TProperty> dependentPropertySetter, Func<T, TProperty> calculator, Expression<Func<T, object>> obligatoryDependencyPath, params Expression<Func<T, object>>[] dependencyPaths)
        {
            if (dependentPropertySetter == null)
                throw new ArgumentNullException(nameof(dependentPropertySetter));
            if (calculator == null)
                throw new ArgumentNullException(nameof(calculator));
            if (obligatoryDependencyPath == null)
                throw new ArgumentNullException(nameof(obligatoryDependencyPath));
            if (dependencyPaths == null) 
                throw new ArgumentNullException(nameof(dependencyPaths));
            if (dependencyPaths.Any(p => p == null))
                throw new ArgumentException("On of items in dependencyPaths is null.");

            foreach (var builtPath in BuildPaths(dependencyPaths.StartWith(obligatoryDependencyPath), o => dependentPropertySetter(o, calculator(o))))
                _mapItems.Add(builtPath);

            return this;
        }

        public IDependenciesMap<T> AddDependency<TProperty>(Expression<Func<T, TProperty>> dependentProperty, Func<T, TProperty> calculator,
                                                    Expression<Func<T, object>> obligatoryDependencyPath,
                                                    params Expression<Func<T, object>>[] dependencyPaths)
        {
            if (dependentProperty == null)
                throw new ArgumentNullException(nameof(dependentProperty));

            return AddDependency(BuildSetter(dependentProperty), calculator, obligatoryDependencyPath, dependencyPaths);
        }

        private static Exception MakeNotSupportedExpressionForDependentProperty(Expression notSuppportedExpression)
        {
            Debug.Assert(notSuppportedExpression != null);

            return new NotSupportedException($"Expression {notSuppportedExpression} is not supported. The only property or field member expression with no chains (i.e. one level from root object) is supported.");
        }

        private static IEnumerable<PathItemBase<T>> BuildPaths(IEnumerable<Expression<Func<T, object>>> dependencyPaths, Action<T> calculateAndSet)
        {
            if (calculateAndSet == null)
                throw new ArgumentNullException(nameof(calculateAndSet));

            return dependencyPaths.Select(pathExpression => BuildPath(pathExpression, calculateAndSet)).ToList();
        }

        private static PathItemBase<T> BuildPath(Expression<Func<T, object>> pathExpession, Action<T> calculateAndSet)
        {
            var convertExpression = pathExpession.Body as UnaryExpression;
            if (convertExpression != null &&
                (convertExpression.NodeType != ExpressionType.Convert || convertExpression.Type != typeof(object)))
                throw new NotSupportedException($"Unary expression {convertExpression} is not supported. Only \"convert to object\" expression is allowed in the end of path.");

            var currentExpression = convertExpression != null ? convertExpression.Operand : pathExpession.Body;

            PathItemBase<T> rootPathItem = null;

            while (!(currentExpression is ParameterExpression))
            {
                switch (currentExpression)
                {
                    case MethodCallExpression methodCall when !methodCall.Method.IsGenericMethod || !methodCall.Method.GetGenericMethodDefinition().Equals(CollectionExtensions.EachElementMethodInfo):
                        throw new NotSupportedException($"Call of method {methodCall.Method} is not supported. Only {CollectionExtensions.EachElementMethodInfo} call is supported for collections in path");
                    case MethodCallExpression methodCall:
                        rootPathItem = new CollectionPathItem<T>(rootPathItem, rootPathItem == null ? calculateAndSet : null);

                        var methodCallArgument = methodCall.Arguments.Single();
                        currentExpression = methodCallArgument;
                        continue;
                }

                var memberExpression = currentExpression as MemberExpression;
                if (memberExpression == null)
                    throw new NotSupportedException($"Expected expression is member expression. Expression {currentExpression} is not supported.");

                var property = memberExpression.Member;
                var compiledGetter = BuildGetter(memberExpression.Expression.Type, property.Name);

                rootPathItem = new PropertyPathItem<T>(compiledGetter, property.Name, rootPathItem, rootPathItem == null ? calculateAndSet : null);

                currentExpression = memberExpression.Expression;
            }

            //The chain doesn't contain any element (i.e. the expression contains only root object root => root)
            if (rootPathItem == null)
                throw new NotSupportedException($"The path {pathExpession} is too short. It contains a root object only.");

            rootPathItem = new PropertyPathItem<T>(o => o, string.Empty, rootPathItem, null);

            return rootPathItem;
        }

        private static Func<object, object> BuildGetter(Type parameterType, string propertyOrFieldName)
        {
            var parameter = Expression.Parameter(typeof(object), "obj");
            var convertedParameter = Expression.Convert(parameter, parameterType);
            var propertyGetter = Expression.PropertyOrField(convertedParameter, propertyOrFieldName);

            Debug.WriteLine(propertyGetter);

            var lambdaExpression = Expression.Lambda<Func<object, object>>(Expression.Convert(propertyGetter, typeof(object)), parameter);
            Debug.WriteLine(lambdaExpression);
            return lambdaExpression.Compile();

        }

        private static Action<T, TProperty> BuildSetter<TProperty>(Expression<Func<T, TProperty>> dependentProperty)
        {
            Debug.Assert(dependentProperty.Body != null);

            if (!(dependentProperty.Body is MemberExpression memberExpression))
                throw MakeNotSupportedExpressionForDependentProperty(dependentProperty.Body);

            if (!(memberExpression.Expression is ParameterExpression))
                throw MakeNotSupportedExpressionForDependentProperty(memberExpression);

            var objectParameter = Expression.Parameter(typeof(T), "obj");
            var assignParameter = Expression.Parameter(typeof(TProperty), "val");
            var property = Expression.PropertyOrField(objectParameter, memberExpression.Member.Name);
            var lambda = Expression.Lambda<Action<T, TProperty>>(Expression.Assign(property, assignParameter), objectParameter, assignParameter);
            Debug.WriteLine(lambda);
            return lambda.Compile();
        }

        public IDependenciesTracker StartTracking(T trackedObject, bool provokeDependentPropertiesUpdate = true)
        {
            return new DependenciesTracker<T>(this, trackedObject, provokeDependentPropertiesUpdate);
        }
    }
}