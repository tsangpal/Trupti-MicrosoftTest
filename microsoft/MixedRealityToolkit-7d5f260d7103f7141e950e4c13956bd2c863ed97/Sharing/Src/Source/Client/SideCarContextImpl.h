//////////////////////////////////////////////////////////////////////////
// SideCarContextImpl.h
//
// Implementation of the SideCarContext interface
//
// Copyright (C) 2014 Microsoft Corp.  All Rights Reserved
//////////////////////////////////////////////////////////////////////////

#pragma once

XTOOLS_NAMESPACE_BEGIN

class SideCarContextImpl : public SideCarContext, public IUpdateable
{
	XTOOLS_REFLECTION_DECLARE(SideCarContextImpl)

public:
	SideCarContextImpl(const NetworkConnectionPtr& barabooConnection);

	//////////////////////////////////////////////////////////////////////////
	// SidecarContext Functions:

	// Get the connection to the baraboo paired to this PC.  
	// You can get the connection whether the pairing active or not, and use it to 
	// listen for when the connection happens
	virtual NetworkConnectionPtr GetBarabooConnection() XTOVERRIDE;

	// Get the download manager for downloading data over http  
	virtual DownloadManagerPtr GetDownloadManager() XTOVERRIDE;


	//////////////////////////////////////////////////////////////////////////
	// IUpdateable Functions

	virtual void Update();

private:
	NetworkConnectionPtr	m_barabooConnection;
	DownloadManagerPtr		m_downloadManager;
};

DECLARE_PTR(SideCarContextImpl)

XTOOLS_NAMESPACE_END