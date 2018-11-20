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
namespace AiUnity.NLog.Core.LayoutRenderers
{
    using System;
using AiUnity.NLog.Core.Common;
    using System.IO;
    using System.Text;
    using AiUnity.NLog.Core.Config;
    using AiUnity.NLog.Core.Internal;
    using AiUnity.NLog.Core.Layouts;
    using AiUnity.Common.InternalLog;

    /// <summary>
    /// Renders contents of the specified file.
    /// </summary>
    [LayoutRenderer("file-contents")]
    public class FileContentsLayoutRenderer : LayoutRenderer
    {
        private string lastFileName;
        private string currentFileContents;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileContentsLayoutRenderer" /> class.
        /// </summary>
        public FileContentsLayoutRenderer()
        {
            this.Encoding = Encoding.Default;
            this.lastFileName = string.Empty;
        }

        // Internal logger singleton
        private static IInternalLogger Logger { get { return NLogInternalLogger.Instance; } }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        [DefaultParameter]
        public Layout FileName { get; set; }

        /// <summary>
        /// Gets or sets the encoding used in the file.
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Renders the contents of the specified file and appends it to the specified <see cref="StringBuilder" />.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the rendered data to.</param>
        /// <param name="logEvent">Logging event.</param>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            lock (this)
            {
                string fileName = this.FileName.Render(logEvent);

                if (fileName != this.lastFileName)
                {
                    this.currentFileContents = this.ReadFileContents(fileName);
                    this.lastFileName = fileName;
                }
            }

            builder.Append(this.currentFileContents);
        }

        private string ReadFileContents(string fileName)
        {
            try
            {
                using (var reader = new StreamReader(fileName, this.Encoding))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception exception)
            {
                if (exception.MustBeRethrown())
                {
                    throw;
                }

                Logger.Error("Cannot read file contents: {0} {1}", fileName, exception);
                return string.Empty;
            }
        }
    }
}
#endif
