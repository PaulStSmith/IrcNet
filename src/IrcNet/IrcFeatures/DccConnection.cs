﻿/*
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2008-2009 Thomas Bruderer <apophis@apophis.ch> <http://www.apophis.ch>
 * 
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */
 
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;

namespace Meebey.SmartIrc4net
{
    public class DccConnection
    {
        #region Private Variables
        protected IrcFeatures Irc;
        protected TcpListener DccServer;
        protected TcpClient Connection;
        protected IPEndPoint LocalEndPoint;
        protected IPEndPoint RemoteEndPoint;
        protected IPAddress ExternalIPAdress;
        protected DateTime Timeout;
        protected string User;

        protected bool isConnected = false;
        protected bool isValid = true;

        protected bool reject = false;
        protected long session;
        private class Session
        {
            private static long next;
            internal static long Next
            {
                get
                {
                    return ++next;
                }
            }
        }
        #endregion

        #region Public Fields
        public bool Connected
        {
            get
            {
                return isConnected;
            }
        }

        public bool Valid
        {
            get
            {
                return isValid && (isConnected || (DateTime.Now < Timeout));
            }
        }

        public string Nick
        {
            get
            {
                return User;
            }
        }

        #endregion

        #region Public DCC Events
        public event DccConnectionHandler OnDccChatRequestEvent;
        protected virtual void DccChatRequestEvent(DccEventArgs e)
        {
            OnDccChatRequestEvent?.Invoke(this, e);
            Irc.DccChatRequestEvent(e);
        }

        public event DccSendRequestHandler OnDccSendRequestEvent;
        protected virtual void DccSendRequestEvent(DccSendRequestEventArgs e)
        {
            OnDccSendRequestEvent?.Invoke(this, e);
            Irc.DccSendRequestEvent(e);
        }

        public event DccConnectionHandler OnDccChatStartEvent;
        protected virtual void DccChatStartEvent(DccEventArgs e)
        {
            OnDccChatStartEvent?.Invoke(this, e);
            Irc.DccChatStartEvent(e);
        }

        public event DccConnectionHandler OnDccSendStartEvent;
        protected virtual void DccSendStartEvent(DccEventArgs e)
        {
            OnDccSendStartEvent?.Invoke(this, e);
            Irc.DccSendStartEvent(e);
        }

        public event DccChatLineHandler OnDccChatReceiveLineEvent;
        protected virtual void DccChatReceiveLineEvent(DccChatEventArgs e)
        {
            OnDccChatReceiveLineEvent?.Invoke(this, e);
            Irc.DccChatReceiveLineEvent(e);
        }

        public event DccSendPacketHandler OnDccSendReceiveBlockEvent;
        protected virtual void DccSendReceiveBlockEvent(DccSendEventArgs e)
        {
            OnDccSendReceiveBlockEvent?.Invoke(this, e);
            Irc.DccSendReceiveBlockEvent(e);
        }

        public event DccChatLineHandler OnDccChatSentLineEvent;
        protected virtual void DccChatSentLineEvent(DccChatEventArgs e)
        {
            OnDccChatSentLineEvent?.Invoke(this, e);
            Irc.DccChatSentLineEvent(e);
        }

        public event DccSendPacketHandler OnDccSendSentBlockEvent;
        protected virtual void DccSendSentBlockEvent(DccSendEventArgs e)
        {
            OnDccSendSentBlockEvent?.Invoke(this, e);
            Irc.DccSendSentBlockEvent(e);
        }

        public event DccConnectionHandler OnDccChatStopEvent;
        protected virtual void DccChatStopEvent(DccEventArgs e)
        {
            OnDccChatStopEvent?.Invoke(this, e);
            Irc.DccChatStopEvent(e);
        }

        public event DccConnectionHandler OnDccSendStopEvent;
        protected virtual void DccSendStopEvent(DccEventArgs e)
        {
            OnDccSendStopEvent?.Invoke(this, e);
            Irc.DccSendStopEvent(e);
        }
        #endregion

        internal DccConnection()
        {
            //Each DccConnection gets a Unique Identifier (just used internally until we have a TcpClient connected)
            session = Session.Next;
            // If a Connection is not established within 120 Seconds we invalidate the DccConnection (see property Valid)
            Timeout = DateTime.Now.AddSeconds(120);
        }

        internal virtual void InitWork(Object stateInfo)
        {
            throw new NotSupportedException();
        }

        internal bool IsSession(long session)
        {
            return (session == this.session);
        }

        #region Public Methods
        public void RejectRequest()
        {
            Irc.SendMessage(SendType.CtcpReply, User, "ERRMSG DCC Rejected");
            reject = true;
            isValid = false;
        }


        public void Disconnect()
        {
            isConnected = false;
            isValid = false;
        }

        public override string ToString()
        {
            return "DCC Session " + session + " of " + this.GetType().ToString() + " is " + ((isConnected) ? "connected to " + RemoteEndPoint.Address.ToString() : "not connected") + "[" + this.User + "]";
        }
        #endregion

        #region protected Helper Functions
        protected long HostToDccInt(IPAddress ip)
        {
#pragma warning disable CS0618 // HostToDccInt is IPv4 dependent
            var addr = ip.Address;
            long temp = (addr & 0xff) << 24;
            temp |= (addr & 0xff00) << 8;
            temp |= (addr >> 8) & 0xff00;
            temp |= (addr >> 24) & 0xff;
            return temp;
#pragma warning restore CS0618 // IPAddress.Address is obsolete
        }

        protected string DccIntToHost(long ip)
        {
            IPEndPoint ep = new IPEndPoint(ip, 80);
            char[] sep = { '.' };
            string[] ipparts = ep.Address.ToString().Split(sep);
            return ipparts[3] + "." + ipparts[2] + "." + ipparts[1] + "." + ipparts[0];
        }

        protected byte[] GetAck(long SentBytes)
        {
            byte[] acks = new byte[4];
            acks[0] = (byte)((SentBytes >> 24) % 256);
            acks[1] = (byte)((SentBytes >> 16) % 256);
            acks[2] = (byte)((SentBytes >> 8) % 256);
            acks[3] = (byte)((SentBytes) % 256);
            return acks;
        }

        protected string FilterMarker(string msg)
        {
            string result = "";
            foreach (char c in msg)
            {
                if (c != IrcConstants.CtcpChar)
                    result += c;
            }
            return result;
        }
        #endregion
    }
}
