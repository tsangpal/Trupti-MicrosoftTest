﻿//------------------------------------------------------------------------------
// <copyright file="XToolsSessionListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using HoloToolkit.Sharing;

namespace SessionManager.Network
{
    public class XToolsSessionListener : SessionListener
    {
        public event Action<Session> JoiningSession;
        public event Action<Session> JoinSucceeded;
        public event Action<Session> JoinFailed;
        public event Action<Session> SessionDisconnected;

        private Session session;

        public XToolsSessionListener(Session session)
        {
            this.session = session;
        }

        public override void OnJoiningSession()
        {
            if (JoiningSession != null)
            {
                JoiningSession(this.session);
            }
        }
        public override void OnJoinSucceeded()
        {
            if (JoinSucceeded != null)
            {
                JoinSucceeded(this.session);
            }
        }

        public override void OnJoinFailed()
        {
            if (JoinFailed != null)
            {
                JoinFailed(this.session);
            }
        }

        public override void OnSessionDisconnected()
        {
            if (SessionDisconnected != null)
            {
                SessionDisconnected(this.session);
            }
        }
    }
}
