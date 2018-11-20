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
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Threading;
	using AiUnity.NLog.Core.Common;
	using AiUnity.NLog.Core.Internal;
	using AiUnity.NLog.Core.Layouts;
	using AiUnity.NLog.Core.Time;
    using AiUnity.Common.InternalLog;
    using AiUnity.NLog.Core.Config;
    using AiUnity.Common.Log;

    /// <summary>
    /// Represents the logging event.
    /// </summary>
    public class LogEventInfo
	{
		/// <summary>
		/// Gets the date of the first log event created.
		/// </summary>
		public static readonly DateTime ZeroDate = DateTime.UtcNow;

		private static int globalSequenceId;

		private readonly object layoutCacheLock = new object();

		private string formattedMessage;
		private IDictionary<Layout, string> layoutCache;
		private IDictionary<object, object> properties;
        // Unity
        //private IDictionary eventContextAdapter;

            /// <summary>
        /// Initializes a new instance of the <see cref="LogEventInfo" /> class.
        /// </summary>
        public LogEventInfo()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LogEventInfo"/> class.
		/// </summary>
		/// <param name="level">Log level.</param>
		/// <param name="loggerName">Logger name.</param>
		/// <param name="formatProvider">An IFormatProvider that supplies culture-specific formatting information.</param>
		/// <param name="message">Log message including parameter placeholders.</param>
		/// <param name="parameters">Parameter array.</param>
		/// <param name="exception">Exception information.</param>
		public LogEventInfo(LogLevels level, string loggerName, UnityEngine.Object context, IFormatProvider formatProvider, string message, object[] parameters = null, Exception exception = null)
		{
			this.TimeStamp = TimeSource.Current.Time;
			this.Level = level;
			this.LoggerName = loggerName;
			this.Message = message;
			this.Parameters = parameters;
			this.FormatProvider = formatProvider;
			this.Exception = exception;
			this.SequenceID = Interlocked.Increment(ref globalSequenceId);
			this.Context = context;

			if (NeedToPreformatMessage(parameters)) {
				this.CalcFormattedMessage();
			}
		}

        // Internal logger singleton
        private static IInternalLogger Logger { get { return NLogInternalLogger.Instance; } }

		/// <summary>
		/// Gets the unique identifier of log event which is automatically generated
		/// and monotonously increasing.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID", Justification = "Backwards compatibility")]
		public int SequenceID { get; private set; }

		/// <summary>
		/// Gets or sets the timestamp of the logging event.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TimeStamp", Justification = "Backwards compatibility.")]
		public DateTime TimeStamp { get; set; }

		/// <summary>
		/// Gets or sets the level of the logging event.
		/// </summary>
		public LogLevels Level { get; set; }

		/// <summary>
		/// Gets a value indicating whether stack trace has been set for this event.
		/// </summary>
		public bool HasStackTrace
		{
			get { return this.StackTrace != null; }
		}

		/// <summary>
		/// Gets the stack frame of the method that did the logging.
		/// </summary>
		public StackFrame UserStackFrame
		{
			get { return (this.StackTrace != null) ? this.StackTrace.GetFrame(this.UserStackFrameNumber) : null; }
		}

		/// <summary>
		/// Gets the number index of the stack frame that represents the user
		/// code (not the NLog code).
		/// </summary>
		public int UserStackFrameNumber { get; private set; }

		/// <summary>
		/// Gets the entire stack trace.
		/// </summary>
		public StackTrace StackTrace { get; private set; }

		/// <summary>
		/// Gets or sets the exception information.
		/// </summary>
		public Exception Exception { get; set; }

		/// <summary>
		/// Gets or sets the logger name.
		/// </summary>
		public string LoggerName { get; set; }

		/// <summary>
		/// Gets or sets the log message including any parameter placeholders.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Gets or sets the parameter values or null if no parameters have been specified.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "For backwards compatibility.")]
		public object[] Parameters { get; set; }

		/// <summary>
		/// Gets or sets the format provider that was provided while logging or <see langword="null" />
		/// when no formatProvider was specified.
		/// </summary>
		public IFormatProvider FormatProvider { get; set; }

		/// <summary>
		/// Gets the formatted message.
		/// </summary>
		public string FormattedMessage
		{
			get 
			{
				if (this.formattedMessage == null)
				{
					this.CalcFormattedMessage();
				}

				return this.formattedMessage;
			}
		}

		/// <summary>
		/// Gets the dictionary of per-event context properties.
		/// </summary>
		public IDictionary<object, object> Properties
		{
			get
			{
				if (this.properties == null)
				{
					this.InitEventContext();
				}

				return this.properties;
			}
		}

		// Unity
		public UnityEngine.Object Context { get; private set; }

        public bool FromUnityLogListener { get; set; }
        internal ISupportsInitialize UnityLogListener { get; set; }


        /// <summary>
        /// Creates the null event.
        /// </summary>
        /// <returns>Null log event.</returns>
        public static LogEventInfo CreateNullEvent()
		{
			return new LogEventInfo(0, string.Empty, null, null, string.Empty);
		}

		/// <summary>
		/// Creates the log event.
		/// </summary>
		/// <param name="logLevel">The log level.</param>
		/// <param name="loggerName">Name of the logger.</param>
		/// <param name="formatProvider">The format provider.</param>
		/// <param name="message">The message.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>Instance of <see cref="LogEventInfo"/>.</returns>
		public static LogEventInfo Create(LogLevels logLevel, string loggerName, UnityEngine.Object context, IFormatProvider formatProvider, string message, object[] parameters = null, Exception exception = null)
		{
			return new LogEventInfo(logLevel, loggerName, context, formatProvider, message, parameters, exception);
		}

		/// <summary>
		/// Creates <see cref="AsyncLogEventInfo"/> from this <see cref="LogEventInfo"/> by attaching the specified asynchronous continuation.
		/// </summary>
		/// <param name="asyncContinuation">The asynchronous continuation.</param>
		/// <returns>Instance of <see cref="AsyncLogEventInfo"/> with attached continuation.</returns>
		public AsyncLogEventInfo WithContinuation(AsyncContinuation asyncContinuation)
		{
			return new AsyncLogEventInfo(this, asyncContinuation);
		}

		/// <summary>
		/// Returns a string representation of this log event.
		/// </summary>
		/// <returns>String representation of the log event.</returns>
		public override string ToString()
		{
			return "Log Event: Logger='" + this.LoggerName + "' Level=" + this.Level + " Message='" + this.FormattedMessage + "' SequenceID=" + this.SequenceID;
		}

		/// <summary>
		/// Sets the stack trace for the event info.
		/// </summary>
		/// <param name="stackTrace">The stack trace.</param>
		/// <param name="userStackFrame">Index of the first user stack frame within the stack trace.</param>
		public void SetStackTrace(StackTrace stackTrace, int userStackFrame)
		{
			this.StackTrace = stackTrace;
			this.UserStackFrameNumber = userStackFrame;
		}

        internal string AddCachedLayoutValue(Layout layout, string value)
		{
			lock (this.layoutCacheLock)
			{
				if (this.layoutCache == null)
				{
					this.layoutCache = new Dictionary<Layout, string>();
				}

				this.layoutCache[layout] = value;
			}

			return value;
		}

		internal bool TryGetCachedLayoutValue(Layout layout, out string value)
		{
			lock (this.layoutCacheLock)
			{
				if (this.layoutCache == null)
				{
					value = null;
					return false;
				}

				return this.layoutCache.TryGetValue(layout, out value);
			}
		}

		private static bool NeedToPreformatMessage(object[] parameters)
		{
			// we need to preformat message if it contains any parameters which could possibly
			// do logging in their ToString()
			if (parameters == null || parameters.Length == 0)
			{
				return false;
			}

			if (parameters.Length > 3)
			{
				// too many parameters, too costly to check
				return true;
			}

			if (!IsSafeToDeferFormatting(parameters[0]))
			{
				return true;
			}

			if (parameters.Length >= 2)
			{
				if (!IsSafeToDeferFormatting(parameters[1]))
				{
					return true;
				}
			}

			if (parameters.Length >= 3)
			{
				if (!IsSafeToDeferFormatting(parameters[2]))
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsSafeToDeferFormatting(object value)
		{
			if (value == null)
			{
				return true;
			}

			return value.GetType().IsPrimitive || (value is string);
		}

		private void CalcFormattedMessage()
		{
			if (this.Parameters == null || this.Parameters.Length == 0)
			{
				this.formattedMessage = this.Message;
			}
			else
			{
				try
				{
					this.formattedMessage = string.Format(this.FormatProvider ?? NLogManager.Instance.DefaultCultureInfo(), this.Message, this.Parameters);
				}
				catch (Exception exception)
				{
					this.formattedMessage = this.Message;
					if (exception.MustBeRethrown())
					{
						throw;
					}

					Logger.Warn("Error when formatting a message: {0}", exception);
				}
			}
		}

		private void InitEventContext()
		{
			this.properties = new Dictionary<object, object>();
			// Unity
			//this.eventContextAdapter = new DictionaryAdapter<object, object>(this.properties);
		}
	}
}
#endif