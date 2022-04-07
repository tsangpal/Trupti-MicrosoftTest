//////////////////////////////////////////////////////////////////////////
// ClientWrapperAPI.h
//
// Starting point for SWIG to generate language wrappers for the XTools client library
//
// Copyright (C) 2014 Microsoft Corp.  All Rights Reserved
//////////////////////////////////////////////////////////////////////////

#pragma once

#include "../Common/WrapperAPIs.h"
#include "Settings.h"
#include "PairingResult.h"
#include "PairMaker.h"
#include "PairingListener.h"
#include "PairingManager.h"
#include "SharingManager.h"

// Image tag processing
#include "ImageTagManager.h"
#include "ImageTagLocation.h"
#include "ImageTagLocationListener.h"

// Direct Pairing
#include "DirectPairConnector.h"
#include "DirectPairReceiver.h"

// Visual Pairing
#include "TagImage.h"
#include "VisualPairReceiver.h"
#include "VisualPairConnector.h"
