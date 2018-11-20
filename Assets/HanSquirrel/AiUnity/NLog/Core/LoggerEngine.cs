// 
// Copyright (c) 2004-2011 Jaroslaw Kowalski <jaak@jkowalski.net>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

#if AIUNITY_CODE
namespace AiUnity.NLog.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using AiUnity.NLog.Core.Common;
    using AiUnity.NLog.Core.Config;
    using AiUnity.NLog.Core.Filters;
    using AiUnity.NLog.Core.Internal;
    using AiUnity.NLog.Core.Targets;
    using AiUnity.Common.InternalLog;

    /// <summary>
    /// Implementation of logging engine.
    /// </summary>
    internal static class LoggerEngine
    {
        private const int StackTraceSkipMethods = 0;
        //private static readonly Assembly nlogAssembly = typeof(LoggerImpl).Assembly;
        private static readonly Assembly mscorlibAssembly = typeof(string).Assembly;
        private static readonly Assembly systemAssembly = typeof(Debug).Assembly;

        // Internal logger singleton
        private static IInternalLogger Logger { get { return NLogInternalLogger.Instance; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", Justification = "Using 'NLog' in message.")]
        internal static void Write(Type loggerType, TargetWithFilterChain targets, LogEventInfo logEvent, LogFactory factory)
        {
            if (targets == null)
            {
                return;
            }

            StackTraceUsage stu = targets.GetStackTraceUsage();

            if (stu != StackTraceUsage.None && !logEvent.HasStackTrace)
            {
                StackTrace stackTrace;
                stackTrace = new StackTrace(StackTraceSkipMethods, stu == StackTraceUsage.WithSource);

                int firstUserFrame = FindCallingMethodOnStackTrace(stackTrace, loggerType);

                logEvent.SetStackTrace(stackTrace, firstUserFrame);
            }

            int originalThreadId = Thread.CurrentThread.ManagedThreadId;
            AsyncContinuation exceptionHandler = ex =>
                {
                    if (ex != null)
                    {
                        if (factory.ThrowExceptions && Thread.CurrentThread.ManagedThreadId == originalThreadId)
                        {
                            throw new NLogRuntimeException("Exception occurred in NLog", ex);
                        }
                    }
                };

            for (var t = targets; t != null; t = t.NextInChain)
            {
                if (!WriteToTargetWithFilterChain(t, logEvent, exceptionHandler))
                {
                    break;
                }
            }
        }

        private static int FindCallingMethodOnStackTrace(StackTrace stackTrace, Type loggerType)
        {
            int? firstUserFrame = null;

            if (loggerType != null)
            {
                for (int i = 0; i < stackTrace.FrameCount; ++i)
                {
                    StackFrame frame = stackTrace.GetFrame(i);
                    MethodBase mb = frame.GetMethod();

                    //if (mb.DeclaringType == loggerType || (mb.DeclaringType != null && SkipAssembly(mb.DeclaringType.Assembly)))
                    if (mb.DeclaringType == loggerType || (mb.DeclaringType != null && (SkipAssembly(mb.DeclaringType.Assembly) || SkipNameSpace(mb.DeclaringType.Namespace))))
                        firstUserFrame = i + 1;
                    else if (firstUserFrame != null)
                        break;
                }
            }

            if (firstUserFrame == stackTrace.FrameCount)
                firstUserFrame = null;
            
            if (firstUserFrame == null)
            {
                for (int i = 0; i < stackTrace.FrameCount; ++i)
                {
                    StackFrame frame = stackTrace.GetFrame(i);
                    MethodBase mb = frame.GetMethod();
                    Assembly methodAssembly = null;

                    if (mb.DeclaringType != null)
                    {
                        methodAssembly = mb.DeclaringType.Assembly;
                    }

                    if (SkipAssembly(methodAssembly))
                    {
                        firstUserFrame = i + 1;
                    }
                    else
                    {
                        if (firstUserFrame != 0)
                        {
                            break;
                        }
                    }
                }
            }

            return firstUserFrame ?? 0;
        }

        private static bool SkipAssembly(Assembly assembly)
        {
            //Unity - Does not work when nlog source code utilized
            //if (assembly == nlogAssembly)
            //if (assembly != null && assembly.get.FullName.StartsWith("NLog,"))
            if (assembly != null && assembly.GetModules().Any(m => m.Name.Equals("NLog.dll")))
            {
                return true;
            }

            if (assembly == mscorlibAssembly)
            {
                return true;
            }

            if (assembly == systemAssembly)
            {
                return true;
            }

            if (NLogManager.Instance.HiddenAssemblies.Contains(assembly))
            {
                return true;
            }

            return false;
        }

        private static bool SkipNameSpace(string nameSpace)
        {
            if (string.IsNullOrEmpty(nameSpace))
            {
                return false;
            }

            if (nameSpace.StartsWith("AiUnity.NLog.Core") || nameSpace.StartsWith("AiUnity.CLog.Core") || nameSpace.StartsWith("AiUnity.Common"))
            {
                return true;
            }

            if (nameSpace.StartsWith("UnityEngine"))
            {
                return true;
            }

            if (NLogManager.Instance.HiddenNameSpaces.Any(n => n.StartsWith(nameSpace))) {
                return true;
            }

            return false;
            //return !string.IsNullOrEmpty(nameSpace) && LogManager.Instance.HiddenNameSpaces.Any(n => n.StartsWith(nameSpace));
        }

        private static bool WriteToTargetWithFilterChain(TargetWithFilterChain targetListHead, LogEventInfo logEvent, AsyncContinuation onException)
        {
            Target target = targetListHead.Target;
            FilterResult result = GetFilterResult(targetListHead.FilterChain, logEvent);

            if ((result == FilterResult.Ignore) || (result == FilterResult.IgnoreFinal))
            {
                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("{0}.{1} Rejecting message because of a filter.", logEvent.LoggerName, logEvent.Level);
                }

                if (result == FilterResult.IgnoreFinal)
                {
                    return false;
                }

                return true;
            }

            target.WriteAsyncLogEvent(logEvent.WithContinuation(onException));
            if (result == FilterResult.LogFinal)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the filter result.
        /// </summary>
        /// <param name="filterChain">The filter chain.</param>
        /// <param name="logEvent">The log event.</param>
        /// <returns>The result of the filter.</returns>
        private static FilterResult GetFilterResult(IEnumerable<Filter> filterChain, LogEventInfo logEvent)
        {
            FilterResult result = FilterResult.Neutral;

            try
            {
                foreach (Filter f in filterChain)
                {
                    result = f.GetFilterResult(logEvent);
                    if (result != FilterResult.Neutral)
                    {
                        break;
                    }
                }

                return result;
            }
            catch (Exception exception)
            {
                if (exception.MustBeRethrown())
                {
                    throw;
                }

                Logger.Warn("Exception during filter evaluation: {0}", exception);
                return FilterResult.Ignore;
            }
        }
    }
}
#endif