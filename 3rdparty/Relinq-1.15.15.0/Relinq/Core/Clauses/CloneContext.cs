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
using ArangoDB.Client.Common.Remotion.Linq.Clauses.Expressions;
using Remotion.Utilities;

namespace ArangoDB.Client.Common.Remotion.Linq.Clauses
{
  /// <summary>
  /// Aggregates all objects needed in the process of cloning a <see cref="QueryModel"/> and its clauses.
  /// </summary>
  public sealed class CloneContext
  {
    public CloneContext (QuerySourceMapping querySourceMapping)
    {
      ArgumentUtility.CheckNotNull ("querySourceMapping", querySourceMapping);

      QuerySourceMapping = querySourceMapping;
    }

    /// <summary>
    /// Gets the clause mapping used during the cloning process. This is used to adjust the <see cref="QuerySourceReferenceExpression"/> instances
    /// of clauses to point to clauses in the cloned <see cref="QueryModel"/>.
    /// </summary>
    public QuerySourceMapping QuerySourceMapping { get; private set; }
  }
}
