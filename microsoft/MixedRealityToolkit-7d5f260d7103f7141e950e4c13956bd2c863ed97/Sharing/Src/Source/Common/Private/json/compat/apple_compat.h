/***
 * ==++==
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * ==--==
 * =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
 *
 * apple_compat.h
 *
 * =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
 ****/

#pragma once


#include <cstdint>
#include <sstream>
#include <iostream>
#define __cdecl

#include "nosal.h"
#include "safeint3.hpp"

#define CPPREST_NOEXCEPT noexcept

#define novtable /* no novtable equivalent */
#define __declspec(x) __attribute__ ((x))

// ignore these:
#define dllimport

#include <stdint.h>
#include <assert.h>

#define __assume(x) do { if (!(x)) __builtin_unreachable(); } while (false)

typedef wchar_t utf16char;
typedef std::wstring utf16string;
typedef std::wstringstream utf16stringstream;
typedef std::wostringstream utf16ostringstream;
typedef std::wostream utf16ostream;
typedef std::wistream utf16istream;
typedef std::wistringstream utf16istringstream;

#define CASABLANCA_UNREFERENCED_PARAMETER(x) (void)x
#define _ASSERTE(x) assert(x)

#ifdef CASABLANCA_DEPRECATION_NO_WARNINGS
#define CASABLANCA_DEPRECATED(x)
#else
#define CASABLANCA_DEPRECATED(x) __attribute__((deprecated(x)))
#endif
