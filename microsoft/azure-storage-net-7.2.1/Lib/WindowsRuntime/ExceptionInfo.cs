// -----------------------------------------------------------------------------------------
// <copyright file="ExceptionInfo.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Xml;

    /// <summary>
    /// Represents exception information from a request to the Storage service.
    /// </summary>
    public sealed class ExceptionInfo
    {
        /// <summary>
        /// Gets the type of the exception.
        /// </summary>
        /// <value>The type of the exception.</value>
        public string Type { get; internal set; }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <value>The error message that explains the reason for the exception, or an empty string("").</value>
        public string Message { get; internal set; }

        /// <summary>
        /// Gets the name of the operation that causes the error. 
        /// </summary>
        /// <value>The name of the operation that causes the error.</value>
        public string Source { get; internal set; }

        /// <summary>
        /// Gets a string representation of the frames on the call stack at the time the current exception was thrown. 
        /// </summary>
        /// <value>The frames on the call stack at the time the current exception was thrown.</value>
        public string StackTrace { get; internal set; }

        /// <summary>
        /// Gets the <see cref="ExceptionInfo"/> instance that caused the current exception.
        /// </summary>
        /// <value>An instance of <see cref="ExceptionInfo"/> that describes the error that caused the current exception. </value>
        public ExceptionInfo InnerExceptionInfo { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionInfo"/> class.
        /// </summary>
        public ExceptionInfo()
        {
        }

        internal ExceptionInfo(Exception ex)
        {
            this.Type = ex.GetType().Name;
            this.Message = ex.Message;
            this.StackTrace = ex.StackTrace;

            this.Source = ex.Source;
            if (ex.InnerException != null)
            {
                this.InnerExceptionInfo = new ExceptionInfo(ex.InnerException);
            }
        }

        internal static ExceptionInfo ReadFromXMLReader(XmlReader reader)
        {
            ExceptionInfo res = new ExceptionInfo();
            try
            {
                res.ReadXml(reader);
            }
            catch (XmlException)
            {
                return null;
            }

            return res;
        }

        #region IXMLSerializable

        internal void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("ExceptionInfo");
            writer.WriteElementString("Type", this.Type);
            writer.WriteElementString("Message", this.Message);
            writer.WriteElementString("Source", this.Source);
            writer.WriteElementString("StackTrace", this.StackTrace);

            if (this.InnerExceptionInfo != null)
            {
                writer.WriteStartElement("InnerExceptionInfo");
                this.InnerExceptionInfo.WriteXml(writer);
                writer.WriteEndElement();
            }

            // End ExceptionInfo
            writer.WriteEndElement();
        }

        internal void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement("ExceptionInfo");
            this.Type = CommonUtility.ReadElementAsString("Type", reader);
            this.Message = CommonUtility.ReadElementAsString("Message", reader);
            this.Source = CommonUtility.ReadElementAsString("Source", reader);
            this.StackTrace = CommonUtility.ReadElementAsString("StackTrace", reader);

            if (reader.IsStartElement() && reader.LocalName == "InnerExceptionInfo")
            {
                reader.ReadStartElement("InnerExceptionInfo");
                this.InnerExceptionInfo = ReadFromXMLReader(reader);
                reader.ReadEndElement();
            }

            // End ExceptionInfo
            reader.ReadEndElement();
        }

        #endregion
    }
}
