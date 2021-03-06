//-----------------------------------------------------------------------
// <copyright file="TableErrorCodeStrings.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
    /// <summary>
    /// Provides error code strings that are specific to the Microsoft Azure Table service.
    /// </summary>    
#if WINDOWS_RT
    internal
#else
    public
#endif
    static class TableErrorCodeStrings
    {
        /// <summary>
        /// The request uses X-HTTP-Method with an HTTP verb other than POST.
        /// </summary>
        public static readonly string XMethodNotUsingPost = "XMethodNotUsingPost";

        /// <summary>
        /// The specified X-HTTP-Method is invalid.
        /// </summary>
        public static readonly string XMethodIncorrectValue = "XMethodIncorrectValue";

        /// <summary>
        /// More than one X-HTTP-Method is specified.
        /// </summary>
        public static readonly string XMethodIncorrectCount = "XMethodIncorrectCount";

        /// <summary>
        /// The specified table has no properties.
        /// </summary>
        public static readonly string TableHasNoProperties = "TableHasNoProperties";

        /// <summary>
        /// A property is specified more than once.
        /// </summary>
        public static readonly string DuplicatePropertiesSpecified = "DuplicatePropertiesSpecified";

        /// <summary>
        /// The specified table has no such property.
        /// </summary>
        public static readonly string TableHasNoSuchProperty = "TableHasNoSuchProperty";

        /// <summary>
        /// A duplicate key property was specified.
        /// </summary>
        public static readonly string DuplicateKeyPropertySpecified = "DuplicateKeyPropertySpecified";

        /// <summary>
        /// The specified table already exists.
        /// </summary>
        public static readonly string TableAlreadyExists = "TableAlreadyExists";

        /// <summary>
        /// The specified table was not found.
        /// </summary>
        public static readonly string TableNotFound = "TableNotFound";

        /// <summary>
        /// The specified entity was not found.
        /// </summary>
        public static readonly string EntityNotFound = "EntityNotFound";

        /// <summary>
        /// The specified entity already exists.
        /// </summary>
        public static readonly string EntityAlreadyExists = "EntityAlreadyExists";

        /// <summary>
        /// The partition key was not specified.
        /// </summary>
        public static readonly string PartitionKeyNotSpecified = "PartitionKeyNotSpecified";

        /// <summary>
        /// One or more specified operators are invalid.
        /// </summary>
        public static readonly string OperatorInvalid = "OperatorInvalid";

        /// <summary>
        /// The specified update condition was not satisfied.
        /// </summary>
        public static readonly string UpdateConditionNotSatisfied = "UpdateConditionNotSatisfied";

        /// <summary>
        /// All properties must have values.
        /// </summary>
        public static readonly string PropertiesNeedValue = "PropertiesNeedValue";

        /// <summary>
        /// The partition key property cannot be updated.
        /// </summary>
        public static readonly string PartitionKeyPropertyCannotBeUpdated = "PartitionKeyPropertyCannotBeUpdated";

        /// <summary>
        /// The entity contains more properties than allowed.
        /// </summary>
        public static readonly string TooManyProperties = "TooManyProperties";

        /// <summary>
        /// The entity is larger than the maximum size permitted.
        /// </summary>
        public static readonly string EntityTooLarge = "EntityTooLarge";

        /// <summary>
        /// The property value is larger than the maximum size permitted.
        /// </summary>
        public static readonly string PropertyValueTooLarge = "PropertyValueTooLarge";

        /// <summary>
        /// One or more value types are invalid.
        /// </summary>
        public static readonly string InvalidValueType = "InvalidValueType";

        /// <summary>
        /// The specified table is being deleted.
        /// </summary>
        public static readonly string TableBeingDeleted = "TableBeingDeleted";

        /// <summary>
        /// The Table service server is out of memory.
        /// </summary>
        public static readonly string TableServerOutOfMemory = "TableServerOutOfMemory";

        /// <summary>
        /// The type of the primary key property is invalid.
        /// </summary>
        public static readonly string PrimaryKeyPropertyIsInvalidType = "PrimaryKeyPropertyIsInvalidType";

        /// <summary>
        /// The property name exceeds the maximum allowed length.
        /// </summary>
        public static readonly string PropertyNameTooLong = "PropertyNameTooLong";

        /// <summary>
        /// The property name is invalid.
        /// </summary>
        public static readonly string PropertyNameInvalid = "PropertyNameInvalid";

        /// <summary>
        /// Batch operations are not supported for this operation type.
        /// </summary>
        public static readonly string BatchOperationNotSupported = "BatchOperationNotSupported";

        /// <summary>
        /// JSON format is not supported.
        /// </summary>
        public static readonly string JsonFormatNotSupported = "JsonFormatNotSupported";

        /// <summary>
        /// The specified method is not allowed.
        /// </summary>
        public static readonly string MethodNotAllowed = "MethodNotAllowed";

        /// <summary>
        /// The specified operation is not yet implemented.
        /// </summary>
        public static readonly string NotImplemented = "NotImplemented";

        /// <summary>
        /// The required host information is not present in the request. You must send a non-empty Host header or include the absolute URI in the request line.
        /// </summary>
        public static readonly string HostInformationNotPresent = "HostInformationNotPresent";
    }
}