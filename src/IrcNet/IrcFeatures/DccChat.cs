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


    public class DccChat : DccConnection
    {
        #region Private Variables
        private StreamReader _sr;
        private StreamWriter _sw;
        #endregion

        #region Public Properties
        public int Lines { get; private set; }
        #endregion

        internal DccChat(IrcFeatures irc, string user, IPAddress externalIpAdress, bool passive, Priority priority) : base()
        {
            this.Irc = irc;
            this.ExternalIPAdress = externalIpAdress;
            this.User = user;

            if (passive)
            {
                irc.SendMessage(SendType.CtcpRequest, user, "DCC CHAT chat " + HostToDccInt(externalIpAdress).ToString() + " 0 " + session, priority);
                this.Disconnect();
            }
            else
            {
                DccServer = new TcpListener(new IPEndPoint(IPAddress.Any, 0));
                DccServer.Start();
                LocalEndPoint = (IPEndPoint)DccServer.LocalEndpoint;
                irc.SendMessage(SendType.CtcpRequest, user, "DCC CHAT chat " + HostToDccInt(externalIpAdress).ToString() + " " + LocalEndPoint.Port, priority);
            }

        }

        internal DccChat(IrcFeatures irc, IPAddress externalIpAdress, CtcpEventArgs e) : base()
        {
            this.Irc = irc;
            this.ExternalIPAdress = externalIpAdress;
            this.User = e.Data.Nick;

            if (e.Data.MessageArray.Length > 4)
            {
                var okIP = long.TryParse(e.Data.MessageArray[3], out var ip);
                var okPo = int.TryParse(FilterMarker(e.Data.MessageArray[4]), out var port);  // port 0 = passive
                if ((e.Data.MessageArray[2] == "chat") && okIP && okPo)
                {
                    RemoteEndPoint = new IPEndPoint(IPAddress.Parse(DccIntToHost(ip)), port);
                    if (e.Data.MessageArray.Length > 5 && e.Data.MessageArray[5] != "T")
                    {
                        this.AcceptRequest();    // Since we initated the Request, we accept DCC
                        return;                    // No OnDccChatRequestEvent Event! (we know that we want a connection)
                    }
                    DccChatRequestEvent(new DccEventArgs(this));
                    return;
                }
                else
                {
                    irc.SendMessage(SendType.CtcpReply, e.Data.Nick, "ERRMSG DCC Chat Parameter Error");
                }
            }
            else
            {
                irc.SendMessage(SendType.CtcpReply, e.Data.Nick, "ERRMSG DCC Chat not enough parameters");
            }
            this.isValid = false;
        }

        internal override void InitWork(Object stateInfo)
        {
            if (!Valid)
                return;
            if (DccServer != null)
            {
                Connection = DccServer.AcceptTcpClient();
                RemoteEndPoint = (IPEndPoint)Connection.Client.RemoteEndPoint;
                DccServer.Stop();
                isConnected = true;
            }
            else
            {
                while (!isConnected)
                {
                    Thread.Sleep(500);    // We wait till Request is Accepted (or jump out when rejected)
                    if (reject)
                    {
                        isValid = false;
                        return;
                    }
                }
            }

            DccChatStartEvent(new DccEventArgs(this));

            _sr = new StreamReader(Connection.GetStream(), Irc.Encoding);
            _sw = new StreamWriter(Connection.GetStream(), Irc.Encoding)
            {
                AutoFlush = true
            };

            string line;
            while (((line = _sr.ReadLine()) != null) && (isConnected))
            {
                DccChatReceiveLineEvent(new DccChatEventArgs(this, line));
                Lines++;
            }
            isValid = false;
            isConnected = false;
            DccChatStopEvent(new DccEventArgs(this));

        }

        #region Public Methods for the DCC Chat Object
        public bool AcceptRequest()
        {
            if (isConnected)
                return false;
            try
            {
                if (RemoteEndPoint.Port == 0)
                {
                    DccServer = new TcpListener(new IPEndPoint(IPAddress.Any, 0));
                    DccServer.Start();
                    LocalEndPoint = (IPEndPoint)DccServer.LocalEndpoint;
                    Irc.SendMessage(SendType.CtcpRequest, User, "DCC CHAT chat " + HostToDccInt(ExternalIPAdress).ToString() + " " + LocalEndPoint.Port);
                }
                else
                {
                    Connection = new TcpClient();
                    Connection.Connect(RemoteEndPoint);
                    isConnected = true;
                }
                return true;
            }
            catch (Exception)
            {
                isValid = false;
                isConnected = false;
                return false;
            }
        }

        public void WriteLine(string message)
        {
            if (isConnected)
            {
                _sw.WriteLine(message);
                Lines++;
                DccChatSentLineEvent(new DccChatEventArgs(this, message));
            }
            else
            {
                throw new NotConnectedException("DCC Chat is not Connected");
            }
        }
        #endregion

    }
}
