// -----------------------------------------------------------------------------------------
// <copyright file="ResponseParsingBase.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Provides a base class that is used internally to parse XML streams from storage service operations.
    /// </summary>
    /// <typeparam name="T">The type to be parsed.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if WINDOWS_RT
    internal
#else
    public
#endif
 abstract class ResponseParsingBase<T> : IDisposable
    {
        /// <summary>
        /// Indicates that all parsable objects have been consumed. This field is reserved and should not be used.
        /// </summary>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.MaintainabilityRules",
            "SA1401:FieldsMustBePrivate",
            Justification = "Unable to change while remaining backwards compatible.")]
        protected bool allObjectsParsed;

        /// <summary>
        /// Stores any objects that have not yet been parsed. This field is reserved and should not be used.
        /// </summary>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.MaintainabilityRules",
            "SA1401:FieldsMustBePrivate",
            Justification = "Unable to change while remaining backwards compatible.")]
        protected IList<T> outstandingObjectsToParse = new List<T>();

        /// <summary>
        /// The reader used for parsing. This field is reserved and should not be used.
        /// </summary>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.MaintainabilityRules",
            "SA1401:FieldsMustBePrivate",
            Justification = "Unable to change while remaining backwards compatible.")]
        protected XmlReader reader;

        /// <summary>
        /// The IEnumerator over the parsed content.
        /// </summary>
        private IEnumerator<T> parser;

        /// <summary>
        /// Used to make sure that parsing is only done once, since a stream is not re-entrant.
        /// </summary>
        private bool enumerableConsumed;

        /// <summary>
        /// Initializes a new instance of the ResponseParsingBase class.
        /// </summary>
        /// <param name="stream">The stream to be parsed.</param>
        protected ResponseParsingBase(Stream stream)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreWhitespace = false;
            this.reader = XmlReader.Create(stream, readerSettings);
            this.parser = this.ParseXmlAndClose().GetEnumerator();
        }

        /// <summary>
        /// Gets the parsable objects. This method is reserved and should not be used.
        /// </summary>
        /// <value>The objects to parse.</value>
        protected IEnumerable<T> ObjectsToParse
        {
            get
            {
                if (this.enumerableConsumed)
                {
                    throw new InvalidOperationException(SR.ResourceConsumed);
                }

                this.enumerableConsumed = true;

                while (!this.allObjectsParsed && this.parser.MoveNext())
                {
                    if (this.parser.Current != null)
                    {
                        yield return this.parser.Current;
                    }
                }

                foreach (T parsableObject in this.outstandingObjectsToParse)
                {
                    yield return parsableObject;
                }

                this.outstandingObjectsToParse = null;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. 
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Parses the XML response. This method is reserved and should not be used.
        /// </summary>
        /// <returns>A collection of enumerable objects.</returns>
        protected abstract IEnumerable<T> ParseXml();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources, and optional
        /// managed resources.
        /// </summary>
        /// <param name="disposing"><c>True</c> to release both managed and unmanaged resources; otherwise, <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.reader != null)
                {
#if WINDOWS_RT || NETCORE
                    this.reader.Dispose();
#else
                    this.reader.Close();
#endif
                }
            }

            this.reader = null;
        }

        /// <summary>
        /// This method is reserved and should not be used.
        /// </summary>
        /// <param name="consumable"><c>True</c> when the object is consumable.</param>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1045:DoNotPassTypesByReference",
            MessageId = "0#",
            Justification = "The consumable flag needs to be referenced so updates will propagate to this method.")]
        protected void Variable(ref bool consumable)
        {
            if (!consumable)
            {
                while (this.parser.MoveNext())
                {
                    if (this.parser.Current != null)
                    {
                        this.outstandingObjectsToParse.Add(this.parser.Current);
                    }

                    if (consumable)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Parses the XML and close.
        /// </summary>
        /// <returns>A list of parsed results.</returns>
        private IEnumerable<T> ParseXmlAndClose()
        {
            foreach (T item in this.ParseXml())
            {
                yield return item;
            }

#if WINDOWS_RT || NETCORE
            this.reader.Dispose();
#else
            this.reader.Close();
#endif
            this.reader = null;
        }
    }
}
