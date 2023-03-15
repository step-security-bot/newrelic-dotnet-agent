// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace NewRelic.Agent.Core.Utilities
{
    public static class FuncExtension
    {
        /// <summary>
        /// Returns a memoized copy of this func.  The original func
        /// will be invoked on the first invocation and its return 
        /// value will be cached.  All subsequent invocations will return the
        /// cached value.
        /// 
        /// The new func is thread safe - it can be invoked different threads
        /// but it is guaranteed to only invoke the original func once.
        /// </summary>
        public static Func<R> Memoize<R>(this Func<R> func) where R : class
        {
            return new FuncCache<R>(func).Invoke;
        }

        private class FuncCache<R> where R : class
        {
            private readonly Lazy<R> _lazyFunc;

            public FuncCache(Func<R> func)
            {
                _lazyFunc = new Lazy<R>(func.Invoke);
            }

            public R Invoke() => _lazyFunc.Value;
        }
    }
}
