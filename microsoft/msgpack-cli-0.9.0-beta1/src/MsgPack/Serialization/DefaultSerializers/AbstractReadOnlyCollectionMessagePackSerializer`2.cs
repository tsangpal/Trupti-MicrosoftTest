#region -- License Terms --
// 
// MessagePack for CLI
// 
// Copyright (C) 2015 FUJIWARA, Yusuke
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// 
#endregion -- License Terms --

using System;
using System.Collections.Generic;
#if FEATURE_TAP
using System.Threading;
using System.Threading.Tasks;
#endif // FEATURE_TAP

using MsgPack.Serialization.CollectionSerializers;
using MsgPack.Serialization.Polymorphic;

namespace MsgPack.Serialization.DefaultSerializers
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance", Justification = "User may not use this hierarchy." )]
	internal sealed class AbstractReadOnlyCollectionMessagePackSerializer<TCollection, TItem> : ReadOnlyCollectionMessagePackSerializer<TCollection, TItem>
		where TCollection : IReadOnlyCollection<TItem>
	{
		private readonly ICollectionInstanceFactory _concreteCollectionInstanceFactory;
		private readonly IPolymorphicDeserializer _polymorphicDeserializer;
		private readonly MessagePackSerializer _concreteDeserializer;

		internal override SerializerCapabilities InternalGetCapabilities()
		{
			return this._concreteDeserializer.Capabilities | SerializerCapabilities.PackTo;
		}

		public AbstractReadOnlyCollectionMessagePackSerializer(
			SerializationContext ownerContext,
			Type targetType,
			PolymorphismSchema schema
		)
			: base( ownerContext, schema )
		{
			MessagePackSerializer serializer;
			AbstractCollectionSerializerHelper.GetConcreteSerializer(
				ownerContext,
				schema,
				typeof( TCollection ),
				targetType,
				typeof( EnumerableMessagePackSerializerBase<,> ),
				out this._concreteCollectionInstanceFactory,
				out serializer
			);
			this._polymorphicDeserializer = serializer as IPolymorphicDeserializer;
			this._concreteDeserializer = serializer;
		}

		internal override TCollection InternalUnpackFromCore( Unpacker unpacker )
		{
			if ( this._polymorphicDeserializer != null )
			{
				// This boxing is OK because TCollection should be reference type because TCollection is abstract class or interface.
				return
					( TCollection )this._polymorphicDeserializer.PolymorphicUnpackFrom( unpacker );
			}
			else if ( this._concreteDeserializer != null )
			{
				return ( TCollection ) this._concreteDeserializer.UnpackFrom( unpacker );
			}
			else
			{
				return base.InternalUnpackFromCore( unpacker );
			}
		}

#if FEATURE_TAP

		internal override Task<TCollection> InternalUnpackFromAsyncCore( Unpacker unpacker, CancellationToken cancellationToken )
		{
			if ( this._polymorphicDeserializer != null )
			{
				return
					this._polymorphicDeserializer.PolymorphicUnpackFromAsync( unpacker, cancellationToken )
						.ContinueWith(
							t => ( TCollection ) t.Result,
							cancellationToken,
							TaskContinuationOptions.ExecuteSynchronously,
							TaskScheduler.Current
						);
			}
			else if ( this._concreteDeserializer != null )
			{
				return this._concreteDeserializer.UnpackFromAsync( unpacker, cancellationToken )
					.ContinueWith(
						t => ( TCollection ) t.Result,
						cancellationToken,
						TaskContinuationOptions.ExecuteSynchronously,
						TaskScheduler.Current
					);
			}
			else
			{
				return base.InternalUnpackFromAsyncCore( unpacker, cancellationToken );
			}
		}

#endif // FEATURE_TAP

		protected override TCollection CreateInstance( int initialCapacity )
		{
			// This boxing is OK because TCollection should be reference type because TCollection is abstract class or interface.
			return
				( TCollection )this._concreteCollectionInstanceFactory.CreateInstance( initialCapacity );
		}
	}
}