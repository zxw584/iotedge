// ---------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ---------------------------------------------------------------

namespace Microsoft.Azure.Devices.Routing.Core.Query.Builtins
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.Azure.Devices.Routing.Core.Query.JsonPath;
    using Microsoft.Azure.Devices.Routing.Core.Query.Types;
    using Microsoft.Azure.Devices.Routing.Core.Util;

    public class TwinChangeIncludes : Builtin
    {
        protected override BuiltinExecutor[] Executors => new[]
        {
            new BuiltinExecutor
            {
                InputArgs = new Args(typeof(string)),
                ContextArgs = new Args(typeof(IMessage), typeof(Route)),
                ExecutorFunc = Create
            },
        };

        public override bool IsBodyQuery => true;

        public override bool IsValidMessageSource(MessageSource source)
        {
            return source == MessageSource.TwinChangeEvents;
        }

        public override bool IsEnabled(RouteCompilerFlags routeCompilerFlags)
        {
            return routeCompilerFlags.HasFlag(RouteCompilerFlags.TwinChangeIncludes);
        }

        static Expression Create(Expression[] args, Expression[] contextArgs)
        {
            var inputExpression = args.FirstOrDefault() as ConstantExpression;
            string queryString = inputExpression?.Value as string;

            if (string.IsNullOrEmpty(queryString))
            {
                throw new ArgumentException("twin_change_includes argument cannot be empty");
            }

            // Validate query string as JsonPath
            string errorDetails;
            if (!TwinChangeJsonPathValidator.IsSupportedJsonPath(queryString, out errorDetails))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0}",
                        errorDetails));
            }

            return Expression.Call(typeof(TwinChangeIncludes).GetMethod("Runtime", BindingFlags.NonPublic | BindingFlags.Static), args.Union(contextArgs));
        }

        // ReSharper disable once UnusedMember.Local
        static Bool Runtime(string queryString, IMessage message, Route route)
        {
            string deviceId = null;

            try
            {
                message.SystemProperties.TryGetValue(SystemProperties.DeviceId, out deviceId);

                var queryValue = message.GetQueryValue(queryString);

                if (queryValue == QueryValue.Null)
                {
                    return Bool.False;
                }
                else if (!Undefined.IsDefined(queryValue))
                {
                    return Bool.Undefined;
                }
                else
                {
                    return Bool.True;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                Events.RuntimeError(route, message, deviceId, ex);
                return Bool.Undefined;
            }
        }

        static class Events
        {
            const string Source = nameof(TwinChangeIncludes);

            public static void RuntimeError(Route route, IMessage message, string deviceId, Exception ex)
            {
                //Routing.Log.Warning("TwinChangeIncludesRuntimeError", Source,
                //    string.Format(CultureInfo.InvariantCulture, "RouteId: '{0}', Condition: '{1}'", route.Id, route.Condition),
                //    ex, route.IotHubName, deviceId);

                //Routing.UserAnalyticsLogger.LogRouteEvaluationError(message, route, ex);
            }
        }
    }
}