﻿// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License.  You may obtain a copy
// of the License at http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
// 
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

namespace Microsoft.HBase.Client.Internal
{
   using System;
   using System.Collections.Generic;
   using System.Reflection;
   using System.Threading;
   using System.Threading.Tasks;

   internal class DefaultRetryPolicy : IRetryPolicy
   {
      private bool _initialized;
      private DateTimeOffset _started;

      /// <inheritdoc/>
      public bool ShouldRetryAttempt(Exception e)
      {
         if (!_initialized)
         {
            Init();
         }

         // Temporary fix that disables retry policy as it doesn't work correctly in common error cases
         return false;
      }

      private Exception GetFirstException(Exception e)
      {
         var asAgg = e as AggregateException;
         if (asAgg.IsNotNull())
         {
            AggregateException exs = asAgg.Flatten();
            if (exs.InnerException.IsNotNull())
            {
               return GetFirstException(exs.InnerException);
            }
         }

         var asTargetOfInvoke = e as TargetInvocationException;
         if (asTargetOfInvoke.IsNotNull())
         {
            return GetFirstException(asTargetOfInvoke.InnerException);
         }

         var asTaskCancel = e as TaskCanceledException;
         if (asTaskCancel.IsNotNull())
         {
            if (asTaskCancel.InnerException.IsNotNull())
            {
               return GetFirstException(asTaskCancel.InnerException);
            }
         }
         return e;
      }

      private void Init()
      {
         _started = DateTimeOffset.UtcNow;
         // _nodeCount = UnderDevelopmentApi.GetNodeCount();
         _initialized = true;
      }

      private bool IsFatalException(Exception e)
      {
         var fatalTypes = new List<Type> { typeof(ArgumentException) };

         bool rv = false;
         if (e.IsNotNull())
         {
            Exception finalException = GetFirstException(e);
            Type exceptionType = finalException.GetType();
            foreach (Type t in fatalTypes)
            {
               if (t.IsAssignableFrom(exceptionType))
               {
                  rv = true;
                  break;
               }
            }
         }

         return rv;
      }
   }
}
