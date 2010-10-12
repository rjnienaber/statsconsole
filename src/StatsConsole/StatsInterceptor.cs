﻿#region license
// Copyright 2010 Trafalgar Management Services Licensed under the Apache License,
// Version 2.0 (the "License"); you may not use this file except in compliance with the
// License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
// ANY KIND, either express or implied. See the License for the specific language governing
// permissions and limitations under the License. 
#endregion

using Castle.Core.Interceptor;
using Castle.DynamicProxy;

namespace StatsConsole
{
    public class StatsInterceptor : IInterceptor
    {
        public string Category { get; set; }

        public void Intercept(IInvocation invocation)
        {
            Stats.Current.TimeOperation(invocation.Method.Name, Category, invocation.Proceed);
        }

        public static S Wrap<S, T>(string category)
            where T : class, S, new()
            where S : class
        {
            var concreteClass = new T();
            if (!Context.ShouldRecordStats) return concreteClass;

            var generator = new ProxyGenerator();
            var interceptor = new StatsInterceptor { Category = category };
            return generator.CreateInterfaceProxyWithTarget<S>(concreteClass, interceptor);
        }

        public static S Wrap<S>(object target, string category)
            where S : class
        {
            if (!Context.ShouldRecordStats) return target as S;

            var generator = new ProxyGenerator();
            var interceptor = new StatsInterceptor { Category = category };
            return generator.CreateInterfaceProxyWithTargetInterface(typeof(S), target, interceptor) as S;
        }
    }
}