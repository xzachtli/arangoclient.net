// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 
using System;
using System.Linq;
using System.Linq.Expressions;
using ArangoDB.Client.Common.Remotion.Linq.Clauses.Expressions;
using ArangoDB.Client.Common.Remotion.Linq.Parsing.ExpressionTreeVisitors.TreeEvaluation;
using Remotion.Utilities;

namespace ArangoDB.Client.Common.Remotion.Linq.Parsing.ExpressionTreeVisitors
{
  /// <summary>
  /// Takes an expression tree and first analyzes it for evaluatable subtrees (using <see cref="EvaluatableTreeFindingExpressionTreeVisitor"/>), i.e.
  /// subtrees that can be pre-evaluated before actually generating the query. Examples for evaluatable subtrees are operations on constant
  /// values (constant folding), access to closure variables (variables used by the LINQ query that are defined in an outer scope), or method
  /// calls on known objects or their members. In a second step, it replaces all of the evaluatable subtrees (top-down and non-recursive) by 
  /// their evaluated counterparts.
  /// </summary>
  /// <remarks>
  /// This visitor visits each tree node at most twice: once via the <see cref="EvaluatableTreeFindingExpressionTreeVisitor"/> for analysis and once
  /// again to replace nodes if possible (unless the parent node has already been replaced).
  /// </remarks>
  public class PartialEvaluatingExpressionTreeVisitor : ExpressionTreeVisitor
  {
    /// <summary>
    /// Takes an expression tree and finds and evaluates all its evaluatable subtrees.
    /// </summary>
    public static Expression EvaluateIndependentSubtrees (Expression expressionTree)
    {
      ArgumentUtility.CheckNotNull ("expressionTree", expressionTree);

      var partialEvaluationInfo = EvaluatableTreeFindingExpressionTreeVisitor.Analyze (expressionTree);

      var visitor = new PartialEvaluatingExpressionTreeVisitor (expressionTree, partialEvaluationInfo);
      return visitor.VisitExpression (expressionTree);
    }

    // _partialEvaluationInfo contains a list of the expressions that are safe to be evaluated.
    private readonly PartialEvaluationInfo _partialEvaluationInfo;

    private PartialEvaluatingExpressionTreeVisitor (Expression treeRoot, PartialEvaluationInfo partialEvaluationInfo)
    {
      ArgumentUtility.CheckNotNull ("treeRoot", treeRoot);
      ArgumentUtility.CheckNotNull ("partialEvaluationInfo", partialEvaluationInfo);

      _partialEvaluationInfo = partialEvaluationInfo;
    }

    protected internal override Expression VisitUnknownNonExtensionExpression (Expression expression)
    {
      //ignore
      return expression;
    }

    public override Expression VisitExpression (Expression expression)
    {
      // Only evaluate expressions which do not use any of the surrounding parameter expressions. Don't evaluate
      // lambda expressions (even if you could), we want to analyze those later on.
      if (expression == null)
        return null;

      if (expression.NodeType == ExpressionType.Lambda || !_partialEvaluationInfo.IsEvaluatableExpression (expression))
        return base.VisitExpression (expression);

      Expression evaluatedExpression;
      try
      {
        evaluatedExpression = EvaluateSubtree (expression);
      }
      catch (Exception ex)
      {
        // Evaluation caused an exception. Skip evaluation of this expression and proceed as if it weren't evaluable.
        var baseVisitedExpression = base.VisitExpression (expression);
        // Then wrap the result to capture the exception for the back-end.
        return new PartialEvaluationExceptionExpression (ex, baseVisitedExpression);
      }

      if (evaluatedExpression != expression)
        return EvaluateIndependentSubtrees (evaluatedExpression);
      
      return evaluatedExpression;
    }

    /// <summary>
    /// Evaluates an evaluatable <see cref="Expression"/> subtree, i.e. an independent expression tree that is compilable and executable
    /// without any data being passed in. The result of the evaluation is returned as a <see cref="ConstantExpression"/>; if the subtree
    /// is already a <see cref="ConstantExpression"/>, no evaluation is performed.
    /// </summary>
    /// <param name="subtree">The subtree to be evaluated.</param>
    /// <returns>A <see cref="ConstantExpression"/> holding the result of the evaluation.</returns>
    protected Expression EvaluateSubtree (Expression subtree)
    {
      ArgumentUtility.CheckNotNull ("subtree", subtree);

      if (subtree.NodeType == ExpressionType.Constant)
      {
        var constantExpression = (ConstantExpression) subtree;
        var valueAsIQueryable = constantExpression.Value as IQueryable;
        if (valueAsIQueryable != null && valueAsIQueryable.Expression != constantExpression)
          return valueAsIQueryable.Expression;

        return constantExpression;
      }
      else
      {
        Expression<Func<object>> lambdaWithoutParameters = Expression.Lambda<Func<object>> (Expression.Convert (subtree, typeof (object)));
        var compiledLambda = lambdaWithoutParameters.Compile();

        object value = compiledLambda ();
        return Expression.Constant (value, subtree.Type);
      }
    }
  }
}
