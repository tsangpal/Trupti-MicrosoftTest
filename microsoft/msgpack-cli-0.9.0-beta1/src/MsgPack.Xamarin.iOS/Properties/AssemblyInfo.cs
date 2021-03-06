#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2016 FUJIWARA, Yusuke
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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

using Foundation;

[assembly: AssemblyTitle( "MessagePack for CLI(.NET/Mono)" )]
[assembly: AssemblyDescription( "MessagePack for CLI(.NET/Mono) packing/unpacking library for Xamarin.iOS unified API." )]

[assembly: AssemblyFileVersion( "0.7.2259.1047" )]

[assembly: SecurityRules( SecurityRuleSet.Level2, SkipVerificationInFullTrust = true )]
[assembly: AllowPartiallyTrustedCallers]

#if DEBUG || PERFORMANCE_TEST
[assembly: InternalsVisibleTo( "MsgPackUnitTestXamariniOS" )]
#endif

[assembly: LinkerSafe]

