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

namespace AiUnity.NLog.Core.Internal
{
    using AiUnity.NLog.Core.Common;
    using AiUnity.Common.Extensions;
    using System.Collections.Generic;
    using AiUnity.Common.InternalLog;
    using AiUnity.Common.Log;

    /// <summary>
    /// Logger configuration.
    /// </summary>
    internal class LoggerConfiguration
    {
        private readonly Dictionary<LogLevels, TargetWithFilterChain> targetsByLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerConfiguration" /> class.
        /// </summary>
        /// <param name="targetsByLevel">The targets by level.</param>
        public LoggerConfiguration(Dictionary<LogLevels, TargetWithFilterChain> targetsByLevel)
        {
            this.targetsByLevel = targetsByLevel;
        }

        /// <summary>
        /// Gets targets for the specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>Chain of targets with attached filters.</returns>
        public TargetWithFilterChain GetTargetsForLevel(LogLevels level)
        {
            //return this.targetsByLevel[(int)level];
            TargetWithFilterChain targetWithFilterChain;
            this.targetsByLevel.TryGetValue(level, out targetWithFilterChain);
            return targetWithFilterChain;
        }

        /// <summary>
        /// Determines whether the specified level is enabled.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>
        /// A value of <c>true</c> if the specified level is enabled; otherwise, <c>false</c>.
        /// </returns>
        public bool IsEnabled(LogLevels level)
        {
            return this.targetsByLevel.ContainsKey(level);
        }
    }
}
#endif