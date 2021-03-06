// -----------------------------------------------------------------------------------------
// <copyright file="ComplexEntity.cs" company="Microsoft">
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

#if WINDOWS_DESKTOP
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.WindowsAzure.Storage.Table.Entities
{
    public class ComplexEntity : TableEntity 
    {
        public const int NumberOfNonNullProperties = 27;

        public ComplexEntity()
            : base()
        {
        }

        public ComplexEntity(string pk, string rk)
            : base(pk, rk)
        {
        }

        public CloudStorageAccount UnsupportedProperty { get; set; }

        private DateTimeOffset? dateTimeOffsetNull = null;
        public DateTimeOffset? DateTimeOffsetNull
        {
            get { return dateTimeOffsetNull; }
            set { dateTimeOffsetNull = value; }
        }

        private DateTimeOffset? dateTimeOffsetN = DateTimeOffset.Now;
        public DateTimeOffset? DateTimeOffsetN
        {
            get { return dateTimeOffsetN; }
            set { dateTimeOffsetN = value; }
        }

        private DateTimeOffset dateTimeOffset = DateTimeOffset.Now;
        public DateTimeOffset DateTimeOffset
        {
            get { return dateTimeOffset; }
            set { dateTimeOffset = value; }
        }

        private DateTime? dateTimeNull = null;
        public DateTime? DateTimeNull
        {
            get { return dateTimeNull; }
            set { dateTimeNull = value; }
        }

        private DateTime? dateTimeN = DateTime.UtcNow;
        public DateTime? DateTimeN
        {
            get { return dateTimeN; }
            set { dateTimeN = value; }
        }

        private DateTime dateTime = DateTime.UtcNow;
        public DateTime DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        private Boolean? boolObjNull = null;
        public Boolean? BoolNull
        {
            get { return boolObjNull; }
            set { boolObjNull = value; }
        }

        private Boolean? boolObjN = false;
        public Boolean? BoolN
        {
            get { return boolObjN; }
            set { boolObjN = value; }
        }

        private Boolean boolObj = false;
        public Boolean Bool
        {
            get { return boolObj; }
            set { boolObj = value; }
        }

        private bool? boolPrimitiveNull = null;
        public bool? BoolPrimitiveNull
        {
            get { return boolPrimitiveNull; }
            set { boolPrimitiveNull = value; }
        }

        private bool? boolPrimitiveN = false;
        public bool? BoolPrimitiveN
        {
            get { return boolPrimitiveN; }
            set { boolPrimitiveN = value; }
        }

        private bool boolPrimitive = false;
        public bool BoolPrimitive
        {
            get { return boolPrimitive; }
            set { boolPrimitive = value; }
        }

        private Byte[] binary = new Byte[] { 1, 2, 3, 4 };
        public Byte[] Binary
        {
            get { return binary; }
            set { binary = value; }
        }

        private Byte[] binaryNull = null;
        public Byte[] BinaryNull
        {
            get { return binaryNull; }
            set { binaryNull = value; }
        }

        private byte[] binaryPrimitive = new byte[] { 1, 2, 3, 4 };
        public byte[] BinaryPrimitive
        {
            get { return binaryPrimitive; }
            set { binaryPrimitive = value; }
        }

        private double? doublePrimitiveNull = null;
        public double? DoublePrimitiveNull
        {
            get { return doublePrimitiveNull; }
            set { doublePrimitiveNull = value; }
        }

        private double? doublePrimitiveN = (double)1234.1234;
        public double? DoublePrimitiveN
        {
            get { return doublePrimitiveN; }
            set { doublePrimitiveN = value; }
        }

        private double doublePrimitive = (double)1234.1234;
        public double DoublePrimitive
        {
            get { return doublePrimitive; }
            set { doublePrimitive = value; }
        }

        private Double? doubleOBjNull = null;
        public Double? DoubleNull
        {
            get { return doubleOBjNull; }
            set { doubleOBjNull = value; }
        }

        private Double? doubleOBjN = (Double)1234.1234;
        public Double? DoubleN
        {
            get { return doubleOBjN; }
            set { doubleOBjN = value; }
        }

        private Double doubleOBj = (Double)1234.1234;
        public Double Double
        {
            get { return doubleOBj; }
            set { doubleOBj = value; }
        }

        private Double doubleInteger = (Double)1234;
        public Double DoubleInteger
        {
            get { return doubleInteger; }
            set { doubleInteger = value; }
        }

        private Guid? guidNull = null;
        public Guid? GuidNull
        {
            get { return guidNull; }
            set { guidNull = value; }
        }

        private Guid? guidN = Guid.NewGuid();
        public Guid? GuidN
        {
            get { return guidN; }
            set { guidN = value; }
        }

        private Guid guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return guid; }
            set { guid = value; }
        }

        private int? integerPrimitiveNull = null;
        public int? IntegerPrimitiveNull
        {
            get { return integerPrimitiveNull; }
            set { integerPrimitiveNull = value; }
        }

        private int? integerPrimitiveN = 1234;
        public int? IntegerPrimitiveN
        {
            get { return integerPrimitiveN; }
            set { integerPrimitiveN = value; }
        }

        private int integerPrimitive = 1234;
        public int IntegerPrimitive
        {
            get { return integerPrimitive; }
            set { integerPrimitive = value; }
        }

        private Int32? int32Null = null;
        public Int32? Int32Null
        {
            get { return int32Null; }
            set { int32Null = value; }
        }

        private Int32? int32N = 1234;
        public Int32? Int32N
        {
            get { return int32N; }
            set { int32N = value; }
        }

        private Int32 int32 = 1234;
        public Int32 Int32
        {
            get { return int32; }
            set { int32 = value; }
        }

        private long? longPrimitiveNull = null;
        public long? LongPrimitiveNull
        {
            get { return longPrimitiveNull; }
            set { longPrimitiveNull = value; }
        }

        private long? longPrimitiveN = 123456789012;
        public long? LongPrimitiveN
        {
            get { return longPrimitiveN; }
            set { longPrimitiveN = value; }
        }

        private long longPrimitive = 123456789012;
        public long LongPrimitive
        {
            get { return longPrimitive; }
            set { longPrimitive = value; }
        }

        private Int64? int64Null = null;
        public Int64? Int64Null
        {
            get { return int64Null; }
            set { int64Null = value; }
        }

        private Int64? int64N = 123456789012;
        public Int64? Int64N
        {
            get { return int64N; }
            set { int64N = value; }
        }

        private Int64 int64 = 123456789012;
        public Int64 Int64
        {
            get { return int64; }
            set { int64 = value; }
        }

        private string stringObj = "test";
        public string String
        {
            get { return stringObj; }
            set { stringObj = value; }
        }

        public static void AssertEquality(ComplexEntity a, ComplexEntity b)
        {
            Assert.AreEqual(a.String, b.String);
            Assert.AreEqual(a.Int64, b.Int64);
            Assert.AreEqual(a.Int64N, b.Int64N);
            Assert.AreEqual(a.Int64Null, b.Int64Null);
            Assert.AreEqual(a.LongPrimitive, b.LongPrimitive);
            Assert.AreEqual(a.LongPrimitiveN, b.LongPrimitiveN);
            Assert.AreEqual(a.LongPrimitiveNull, b.LongPrimitiveNull);
            Assert.AreEqual(a.Int32, b.Int32);
            Assert.AreEqual(a.Int32N, b.Int32N);
            Assert.AreEqual(a.Int32Null, b.Int32Null);
            Assert.AreEqual(a.IntegerPrimitive, b.IntegerPrimitive);
            Assert.AreEqual(a.integerPrimitiveN, b.IntegerPrimitiveN);
            Assert.AreEqual(a.IntegerPrimitiveNull, b.IntegerPrimitiveNull);
            Assert.AreEqual(a.Guid, b.Guid);
            Assert.AreEqual(a.GuidN, b.GuidN);
            Assert.AreEqual(a.GuidNull, b.GuidNull);
            Assert.AreEqual(a.Double, b.Double);
            Assert.AreEqual(a.DoubleN, b.DoubleN);
            Assert.AreEqual(a.DoubleNull, b.DoubleNull);
            Assert.AreEqual(a.DoublePrimitive, b.DoublePrimitive);
            Assert.AreEqual(a.DoublePrimitiveN, b.DoublePrimitiveN);
            Assert.AreEqual(a.DoublePrimitiveNull, b.DoublePrimitiveNull);
            Assert.AreEqual(a.BinaryPrimitive.GetValue(0), b.BinaryPrimitive.GetValue(0));
            Assert.AreEqual(a.BinaryPrimitive.GetValue(1), b.BinaryPrimitive.GetValue(1));
            Assert.AreEqual(a.BinaryPrimitive.GetValue(2), b.BinaryPrimitive.GetValue(2));
            Assert.AreEqual(a.BinaryPrimitive.GetValue(3), b.BinaryPrimitive.GetValue(3));
            Assert.AreEqual(a.Binary.GetValue(0), b.Binary.GetValue(0));
            Assert.AreEqual(a.Binary.GetValue(1), b.Binary.GetValue(1));
            Assert.AreEqual(a.Binary.GetValue(2), b.Binary.GetValue(2));
            Assert.AreEqual(a.Binary.GetValue(3), b.Binary.GetValue(3));
            Assert.AreEqual(a.BoolPrimitive, b.BoolPrimitive);
            Assert.AreEqual(a.BoolPrimitiveN, b.BoolPrimitiveN);
            Assert.AreEqual(a.BoolPrimitiveNull, b.BoolPrimitiveNull);
            Assert.AreEqual(a.Bool, b.Bool);
            Assert.AreEqual(a.BoolN, b.BoolN);
            Assert.AreEqual(a.BoolNull, b.BoolNull);
            Assert.AreEqual(a.DateTimeOffsetN, b.DateTimeOffsetN);
            Assert.AreEqual(a.DateTimeOffset, b.DateTimeOffset);
            Assert.AreEqual(a.DateTimeOffsetNull, b.DateTimeOffsetNull);
            Assert.AreEqual(a.DateTime, b.DateTime);
            Assert.AreEqual(a.DateTimeN, b.DateTimeN);
            Assert.AreEqual(a.DateTimeNull, b.DateTimeNull);
        }

        public static EdmType ComplexEntityPropertyResolver(string pk, string rk, string propName, string propValue)
        {
            switch (propName)
            {
                case "String": 
                    return EdmType.String;

                case "Int64": 
                case "Int64N": 
                case "Int64Null": 
                case "LongPrimitive": 
                case "LongPrimitiveN": 
                case "LongPrimitiveNull": 
                    return EdmType.Int64;

                case "Int32":
                case "Int32N":
                case "Int32Null":
                case "IntegerPrimitive":
                case "IntegerPrimitiveN":
                case "IntegerPrimitiveNull": 
                    return EdmType.Int32;

                case "Guid":
                case "GuidN":
                case "GuidNull": 
                    return EdmType.Guid;

                case "Double":
                case "DoubleInteger":
                case "DoubleN":
                case "DoubleNull":
                case "DoublePrimitive":
                case "DoublePrimitiveN":
                case "DoublePrimitiveNull": 
                    return EdmType.Double;

                case "BinaryPrimitive":
                case "Binary": 
                    return EdmType.Binary;

                case "BoolPrimitive":
                case "BoolPrimitiveN":
                case "BoolPrimitiveNull":
                case "Bool": 
                case "BoolN": 
                case "BoolNull": 
                    return EdmType.Boolean;

                case "DateTimeOffsetN":
                case "DateTimeOffset":
                case "DateTimeOffsetNull":
                case "DateTime":
                case "DateTimeN": 
                case "DateTimeNull": 
                    return EdmType.DateTime;

                default: 
                    return EdmType.String;
            }
        }
    }

    internal class DerivedComplexEntity : ComplexEntity
    {
        public DerivedComplexEntity()
            : base()
        {
        }

        private string stringProperty = "stringProperty";

        public string StringProperty
        {
            get { return stringProperty; }
            set { stringProperty = value; }
        }

        public static void AssertProperties(DerivedComplexEntity derivedComplexEntity)
        {
            IDictionary<string, EntityProperty> propertiesAccessor = derivedComplexEntity.WriteEntity(null);
            Assert.IsTrue(propertiesAccessor.Any(p => p.Key == "StringProperty"));
            Assert.IsTrue(propertiesAccessor.Any(p => p.Key == "DateTimeOffsetN"));
        }
    }
}
