/*
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
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents an IRC client with additional features.
    /// </summary>
    public class IrcFeatures : IrcClient
    {
        /// <summary>
        /// Gets or sets the external IP address.
        /// </summary>
        public IPAddress ExternalIpAdress { get; set; }

        /// <summary>
        /// Gets a read-only collection of DCC connections.
        /// </summary>
        public ReadOnlyCollection<DccConnection> DccConnections => new ReadOnlyCollection<DccConnection>(_DccConnections);

        /// <summary>
        /// Gets a dictionary of CTCP delegates. The keys are CTCP commands and the values are the corresponding delegates.
        /// </summary>
        public Dictionary<string, CtcpDelegate> CtcpDelegates { get; } = new Dictionary<string, CtcpDelegate>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        /// Gets or sets the user information for CTCP.
        /// </summary>
        public string CtcpUserInfo { get; set; }

        /// <summary>
        /// Gets or sets the URL for CTCP.
        /// </summary>
        public string CtcpUrl { get; set; }

        /// <summary>
        /// Gets or sets the source for CTCP.
        /// </summary>
        public string CtcpSource { get; set; }

        /// <summary>
        /// A list of DCC connections.
        /// </summary>
        internal readonly List<DccConnection> _DccConnections = new List<DccConnection>();

        /// <summary>
        /// The speed of the DCC connection.
        /// </summary>
        internal DccSpeed Speed = DccSpeed.RfcSendAhead;

        /// <summary>
        /// Event triggered when a DCC chat request is received.
        /// </summary>
        public event DccConnectionHandler OnDccChatRequestEvent;

        /// <summary>
        /// Event triggered when a DCC send request is received.
        /// </summary>
        public event DccSendRequestHandler OnDccSendRequestEvent;
        
        /// <summary>
        /// Event triggered when a DCC chat starts.
        /// </summary>
        public event DccConnectionHandler OnDccChatStartEvent;
        
        /// <summary>
        /// Event triggered when a DCC send starts.
        /// </summary>
        public event DccConnectionHandler OnDccSendStartEvent;
        
        /// <summary>
        /// Event triggered when a line is received in a DCC chat.
        /// </summary>
        public event DccChatLineHandler OnDccChatReceiveLineEvent;
        
        /// <summary>
        /// Event triggered when a block is received in a DCC send.
        /// </summary>
        public event DccSendPacketHandler OnDccSendReceiveBlockEvent;
        
        /// <summary>
        /// Event triggered when a line is sent in a DCC chat.
        /// </summary>
        public event DccChatLineHandler OnDccChatSentLineEvent;
        
        /// <summary>
        /// Event triggered when a block is sent in a DCC send.
        /// </summary>
        public event DccSendPacketHandler OnDccSendSentBlockEvent;
        
        /// <summary>
        /// Event triggered when a DCC chat stops.
        /// </summary>
        public event DccConnectionHandler OnDccChatStopEvent;
        
        /// <summary>
        /// Event triggered when a DCC send stops.
        /// </summary>
        public event DccConnectionHandler OnDccSendStopEvent;

        /// <summary>
        /// Invokes the DCC chat request event.
        /// </summary>
        public void DccChatRequestEvent(DccEventArgs e) => OnDccChatRequestEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC send request event.
        /// </summary>
        public void DccSendRequestEvent(DccSendRequestEventArgs e) => OnDccSendRequestEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC chat start event.
        /// </summary>
        public void DccChatStartEvent(DccEventArgs e) => OnDccChatStartEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC send start event.
        /// </summary>
        public void DccSendStartEvent(DccEventArgs e) => OnDccSendStartEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC chat receive line event.
        /// </summary>
        public void DccChatReceiveLineEvent(DccChatEventArgs e) => OnDccChatReceiveLineEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC send receive block event.
        /// </summary>
        public void DccSendReceiveBlockEvent(DccSendEventArgs e) => OnDccSendReceiveBlockEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC chat sent line event.
        /// </summary>
        public void DccChatSentLineEvent(DccChatEventArgs e) => OnDccChatSentLineEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC send sent block event.
        /// </summary>
        internal void DccSendSentBlockEvent(DccSendEventArgs e) => OnDccSendSentBlockEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC chat stop event.
        /// </summary>
        public void DccChatStopEvent(DccEventArgs e) => OnDccChatStopEvent?.Invoke(this, e);

        /// <summary>
        /// Invokes the DCC send stop event.
        /// </summary>
        public void DccSendStopEvent(DccEventArgs e) => OnDccSendStopEvent?.Invoke(this, e);

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcFeatures"/> class.
        /// </summary>
        public IrcFeatures() : base()
        {
            // This method calls all the ctcp handlers defined below (or added anywhere else)
            this.OnCtcpRequest += new CtcpEventHandler(this.CtcpRequestsHandler);

            // Adding ctcp handler, all commands are lower case (.ToLower() in handler)
            CtcpDelegates.Add("version", this.CtcpVersionDelegate);
            CtcpDelegates.Add("clientinfo", this.CtcpClientInfoDelegate);
            CtcpDelegates.Add("time", this.CtcpTimeDelegate);
            CtcpDelegates.Add("userinfo", this.CtcpUserInfoDelegate);
            CtcpDelegates.Add("url", this.CtcpUrlDelegate);
            CtcpDelegates.Add("source", this.CtcpSourceDelegate);
            CtcpDelegates.Add("finger", this.CtcpFingerDelegate);
            // The DCC Handler
            CtcpDelegates.Add("dcc", this.CtcpDccDelegate);
            // Don't remove the Ping handler without your own implementation
            CtcpDelegates.Add("ping", this.CtcpPingDelegate);
        }

        /// <summary>
        /// Initializes a DCC chat with the specified user.
        /// </summary>
        public void InitDccChat(string user) => this.InitDccChat(user, false);

        /// <summary>
        /// Initializes a DCC chat with the specified user, with the option to set it as passive.
        /// </summary>
        public void InitDccChat(string user, bool passive) => this.InitDccChat(user, passive, Priority.Medium);

        /// <summary>
        /// Initializes a DCC chat with the specified user, with the option to set it as passive and specify the priority.
        /// </summary>
        public void InitDccChat(string user, bool passive, Priority priority)
        {
            var chat = new DccChat(this, user, ExternalIpAdress, passive, priority);
            _DccConnections.Add(chat);
            ThreadPool.QueueUserWorkItem(new WaitCallback(chat.InitWork));
            RemoveInvalidDccConnections();
        }

        /// <summary>
        /// Sends a file to the specified user.
        /// </summary>
        public void SendFile(string user, string filepath)
        {
            var fi = new FileInfo(filepath);
            if (fi.Exists)
                this.SendFile(user, new FileStream(filepath, FileMode.Open), fi.Name, fi.Length, DccSpeed.RfcSendAhead, false, Priority.Medium);
        }

        /// <summary>
        /// Sends a file to the specified user, with the option to set it as passive.
        /// </summary>
        public void SendFile(string user, string filepath, bool passive)
        {
            var fi = new FileInfo(filepath);
            if (fi.Exists)
                this.SendFile(user, new FileStream(filepath, FileMode.Open), fi.Name, fi.Length, DccSpeed.RfcSendAhead, passive, Priority.Medium);
        }

        /// <summary>
        /// Sends a file to the specified user, with the option to set the speed and whether it's passive.
        /// </summary>
        public void SendFile(string user, Stream file, string filename, long filesize, DccSpeed speed, bool passive)
        {
            this.SendFile(user, file, filename, filesize, speed, passive, Priority.Medium);
        }

        /// <summary>
        /// Sends a file to the specified user, with the option to set the speed, whether it's passive, and the priority.
        /// </summary>
        public void SendFile(string user, Stream file, string filename, long filesize, DccSpeed speed, bool passive, Priority priority)
        {
            var send = new DccSend(this, user, ExternalIpAdress, file, filename, filesize, speed, passive, priority);
            _DccConnections.Add(send);
            ThreadPool.QueueUserWorkItem(new WaitCallback(send.InitWork));
            RemoveInvalidDccConnections();
        }

        /// <summary>
        /// Handles CTCP requests.
        /// </summary>
        private void CtcpRequestsHandler(object sender, CtcpEventArgs e)
        {
            if (CtcpDelegates.ContainsKey(e.CtcpCommand))
                CtcpDelegates[e.CtcpCommand].Invoke(e);

            RemoveInvalidDccConnections();
        }

        /// <summary>
        /// Handles CTCP version requests.
        /// </summary>
        private void CtcpVersionDelegate(CtcpEventArgs e) => SendMessage(SendType.CtcpReply, e.Data.Nick, "VERSION " + (CtcpVersion ?? VersionString));

        /// <summary>
        /// Handles CTCP client info requests.
        /// </summary>
        private void CtcpClientInfoDelegate(CtcpEventArgs e)
        {
            var clientInfo = "CLIENTINFO";
            foreach (var kvp in CtcpDelegates)
                clientInfo = clientInfo + " " + kvp.Key.ToUpper();

            SendMessage(SendType.CtcpReply, e.Data.Nick, clientInfo);
        }

        /// <summary>
        /// Handles CTCP ping requests.
        /// </summary>
        private void CtcpPingDelegate(CtcpEventArgs e)
        {
            if (e.Data.Message.Length > 7)
                SendMessage(SendType.CtcpReply, e.Data.Nick, "PING " + e.Data.Message.Substring(6, (e.Data.Message.Length - 7)));
            else
                SendMessage(SendType.CtcpReply, e.Data.Nick, "PING");    //according to RFC, it should be PONG!
        }

#pragma warning disable IDE0051 // Part of a patterns
        /// <summary>
        /// Handles CTCP RFC ping requests.
        /// </summary>
        private void CtcpRfcPingDelegate(CtcpEventArgs e)
        {
            if (e.Data.Message.Length > 7)
                SendMessage(SendType.CtcpReply, e.Data.Nick, "PONG " + e.Data.Message.Substring(6, (e.Data.Message.Length - 7)));
            else
                SendMessage(SendType.CtcpReply, e.Data.Nick, "PONG");
        }
#pragma warning restore IDE0051 // Remove unused private members

        /// <summary>
        /// Handles CTCP time requests.
        /// </summary>
        private void CtcpTimeDelegate(CtcpEventArgs e) => SendMessage(SendType.CtcpReply, e.Data.Nick, "TIME " + DateTime.Now.ToString("r"));

        /// <summary>
        /// Handles CTCP user info requests.
        /// </summary>
        private void CtcpUserInfoDelegate(CtcpEventArgs e) => SendMessage(SendType.CtcpReply, e.Data.Nick, "USERINFO " + (CtcpUserInfo ?? "No user info given."));

        /// <summary>
        /// Handles CTCP URL requests.
        /// </summary>
        private void CtcpUrlDelegate(CtcpEventArgs e) => SendMessage(SendType.CtcpReply, e.Data.Nick, "URL " + (CtcpUrl ?? "http://www.google.com"));

        /// <summary>
        /// Handles CTCP source requests.
        /// </summary>
        private void CtcpSourceDelegate(CtcpEventArgs e) => SendMessage(SendType.CtcpReply, e.Data.Nick, "SOURCE " + (CtcpSource ?? "http://smartirc4net.meebey.net"));

        /// <summary>
        /// Handles CTCP finger requests.
        /// </summary>
        private void CtcpFingerDelegate(CtcpEventArgs e) => SendMessage(SendType.CtcpReply, e.Data.Nick, "FINGER Don't touch little Helga there! ");//SendMessage(SendType.CtcpReply, e.Data.Nick, "FINGER " + this.Realname + " (" + this.Email + ") Idle " + this.Idle + " seconds (" + ((string.IsNullOrEmpty(this.Reason))?this.Reason:"-") + ") " );

        /// <summary>
        /// Handles CTCP DCC requests.
        /// </summary>
        private void CtcpDccDelegate(CtcpEventArgs e)
        {
            if (e.Data.MessageArray.Length < 2)
                SendMessage(SendType.CtcpReply, e.Data.Nick, "ERRMSG DCC missing parameters");
            else
            {
                switch (e.Data.MessageArray[1])
                {
                    case "CHAT":
                        var chat = new DccChat(this, ExternalIpAdress, e);
                        _DccConnections.Add(chat);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(chat.InitWork));
                        break;
                    case "SEND":
                        if (e.Data.MessageArray.Length > 6 && (FilterMarker(e.Data.MessageArray[6]) != "T"))
                        {
                            long session = -1;
                            long.TryParse(FilterMarker(e.Data.MessageArray[6]), out session);
                            foreach (var dc in _DccConnections)
                            {
                                if (dc.IsSession(session))
                                {
                                    ((DccSend)dc).SetRemote(e);
                                    ((DccSend)dc).AcceptRequest(null, 0);
                                    return;
                                }
                            }
                            SendMessage(SendType.CtcpReply, e.Data.Nick, "ERRMSG Invalid passive DCC");
                        }
                        else
                        {
                            var send = new DccSend(this, ExternalIpAdress, e);
                            _DccConnections.Add(send);
                            ThreadPool.QueueUserWorkItem(new WaitCallback(send.InitWork));
                        }
                        break;
                    case "RESUME":
                        foreach (var dc in _DccConnections)
                        {
                            if ((dc is DccSend send) && (send.TryResume(e)))
                            {
                                return;
                            }
                        }
                        SendMessage(SendType.CtcpReply, e.Data.Nick, "ERRMSG Invalid DCC RESUME");
                        break;
                    case "ACCEPT":
                        foreach (var dc in _DccConnections)
                        {
                            if ((dc is DccSend send) && (send.TryAccept(e)))
                            {
                                return;
                            }
                        }
                        SendMessage(SendType.CtcpReply, e.Data.Nick, "ERRMSG Invalid DCC ACCEPT");
                        break;
                    case "XMIT":
                        SendMessage(SendType.CtcpReply, e.Data.Nick, "ERRMSG DCC XMIT not implemented");
                        break;
                    default:
                        SendMessage(SendType.CtcpReply, e.Data.Nick, "ERRMSG DCC " + e.CtcpParameter + " unavailable");
                        break;
                }
            }
        }

        private void RemoveInvalidDccConnections()
        {
            // 
            var invalidDc = new List<DccConnection>();
            foreach (var dc in _DccConnections)
                if ((!dc.Valid) && (!dc.Connected))
                    invalidDc.Add(dc);

            foreach (var dc in invalidDc)
                _DccConnections.Remove(dc);
        }

        private string FilterMarker(string msg)
        {
            var result = "";
            foreach (var c in msg)
                if (c != IrcConstants.CtcpChar)
                    result += c;

            return result;
        }
    }
}
