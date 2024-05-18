/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2009 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
 * Copyright (c) 2008-2009 Thomas Bruderer <apophis@apophis.ch>
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
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Threading;
using Starksoft.Net.Proxy;

namespace Meebey.SmartIrc4net
{
    public class IrcConnection
    {
        private int _CurrentAddress;
        private StreamReader _Reader;
        private StreamWriter _Writer;
        private readonly ReadThread _ReadThread;
        private readonly WriteThread _WriteThread;
        private readonly IdleWorkerThread _IdleWorkerThread;
        private TcpClient _TcpClient;
        private readonly Hashtable _SendBuffer = Hashtable.Synchronized(new Hashtable());
        private bool _IsConnectionError;
        private bool _IsDisconnecting;

        public bool EnableUTF8Recode { get; set; }
        private Stopwatch PingStopwatch { get; set; }
        private Stopwatch NextPingStopwatch { get; set; }

        public event ReadLineEventHandler OnReadLine;
        public event WriteLineEventHandler OnWriteLine;
        public event EventHandler OnConnecting;
        public event EventHandler OnConnected;
        public event EventHandler OnDisconnecting;
        public event EventHandler OnDisconnected;
        public event EventHandler OnConnectionError;
        public event AutoConnectErrorEventHandler OnAutoConnectError;

        protected bool IsConnectionError
        {
            get
            {
                lock (this)
                {
                    return _IsConnectionError;
                }
            }
            set
            {
                lock (this)
                {
                    _IsConnectionError = value;
                }
                if (value)
                {
                    // signal ReadLine() to check IsConnectionError state
                    _ReadThread.QueuedEvent.Set();
                }
            }
        }

        protected bool IsDisconnecting
        {
            get
            {
                lock (this)
                {
                    return _IsDisconnecting;
                }
            }
            set
            {
                lock (this)
                {
                    _IsDisconnecting = value;
                }
            }
        }

        public string Address
        {
            get
            {
                return AddressList[_CurrentAddress];
            }
        }

        public string[] AddressList { get; private set; } = { "localhost" };

        public int Port { get; private set; }

        public bool AutoReconnect { get; set; }

        public bool AutoRetry { get; set; }

        public int AutoRetryDelay { get; set; } = 30;

        public int AutoRetryLimit { get; set; } = 3;

        public int AutoRetryAttempt { get; private set; }

        public int SendDelay { get; set; } = 200;

        public bool IsRegistered { get; private set; }

        public bool IsConnected { get; private set; }

        public string VersionNumber { get; }

        public string VersionString { get; }

        public Encoding Encoding { get; set; } = Encoding.Default;

        public bool UseSsl { get; set; }

        public bool ValidateServerCertificate { get; set; }

        public X509Certificate SslClientCertificate { get; set; }

        public int SocketReceiveTimeout { get; set; } = 600;

        public int SocketSendTimeout { get; set; } = 600;

        public int IdleWorkerInterval { get; set; } = 60;

        public int PingInterval { get; set; } = 60;

        public int PingTimeout { get; set; } = 300;

        public TimeSpan Lag
        {
            get
            {
                return PingStopwatch.Elapsed;
            }
        }


        public string ProxyHost { get; set; }

        public int ProxyPort { get; set; }

        public ProxyType ProxyType { get; set; } = ProxyType.None;

        public string ProxyUsername { get; set; }

        public string ProxyPassword { get; set; }

        public IrcConnection()
        {

            _SendBuffer[Priority.High] = Queue.Synchronized(new Queue());
            _SendBuffer[Priority.AboveMedium] = Queue.Synchronized(new Queue());
            _SendBuffer[Priority.Medium] = Queue.Synchronized(new Queue());
            _SendBuffer[Priority.BelowMedium] = Queue.Synchronized(new Queue());
            _SendBuffer[Priority.Low] = Queue.Synchronized(new Queue());

            // setup own callbacks
            OnReadLine += new ReadLineEventHandler(SimpleParser);
            OnConnectionError += new EventHandler(OnConnectionErrorHandler);

            _ReadThread = new ReadThread(this);
            _WriteThread = new WriteThread(this);
            _IdleWorkerThread = new IdleWorkerThread(this);
            PingStopwatch = new Stopwatch();
            NextPingStopwatch = new Stopwatch();

            var assm = Assembly.GetAssembly(this.GetType());
            var assm_name = assm.GetName(false);

            var pr = assm.GetCustomAttribute<AssemblyProductAttribute>();

            VersionNumber = assm_name.Version.ToString();
            VersionString = pr.Product + " " + VersionNumber;
        }



        public void Connect(string[] addresslist, int port)
        {
            if (IsConnected)
            {
                throw new AlreadyConnectedException("Already connected to: " + Address + ":" + Port);
            }

            AutoRetryAttempt++;


            AddressList = (string[])addresslist.Clone();
            Port = port;

            OnConnecting?.Invoke(this, EventArgs.Empty);
            try
            {
                _TcpClient = new TcpClient
                {
                    NoDelay = true
                };
                _TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                // set timeout, after this the connection will be aborted
                _TcpClient.ReceiveTimeout = SocketReceiveTimeout * 1000;
                _TcpClient.SendTimeout = SocketSendTimeout * 1000;

                if (ProxyType != ProxyType.None)
                {
                    IProxyClient proxyClient = null;
                    var proxyFactory = new ProxyClientFactory();
                    // HACK: map our ProxyType to Starksoft's ProxyType
                    var proxyType = (Starksoft.Net.Proxy.ProxyType)Enum.Parse(typeof(ProxyType), ProxyType.ToString(), true);

                    if (ProxyUsername == null && ProxyPassword == null)
                        proxyClient = proxyFactory.CreateProxyClient(proxyType);
                    else
                    {
                        proxyClient = proxyFactory.CreateProxyClient(
                            proxyType,
                            ProxyHost,
                            ProxyPort,
                            ProxyUsername,
                            ProxyPassword
                        );
                    }

                    _TcpClient.Connect(ProxyHost, ProxyPort);
                    proxyClient.TcpClient = _TcpClient;
                    proxyClient.CreateConnection(Address, port);
                }
                else
                {
                    _TcpClient.Connect(Address, port);
                }

                Stream stream = _TcpClient.GetStream();
                if (UseSsl)
                {
                    RemoteCertificateValidationCallback certValidation;
                    if (ValidateServerCertificate)
                    {
                        certValidation = ServicePointManager.ServerCertificateValidationCallback;
                        if (certValidation == null)
                        {
                            certValidation = delegate (object sender,
                                X509Certificate certificate,
                                X509Chain chain,
                                SslPolicyErrors sslPolicyErrors)
                            {
                                if (sslPolicyErrors == SslPolicyErrors.None)
                                {
                                    return true;
                                }


                                return false;
                            };
                        }
                    }
                    else
                    {
                        certValidation = delegate { return true; };
                    }
                    bool certValidationWithIrcAsSender(object sender, X509Certificate certificate,
                                 X509Chain chain, SslPolicyErrors sslPolicyErrors)
                    {
                        return certValidation(this, certificate, chain, sslPolicyErrors);
                    }
                    X509Certificate selectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
                    {
                        if (localCertificates == null || localCertificates.Count == 0)
                        {
                            return null;
                        }
                        return localCertificates[0];
                    }
                    var sslStream = new SslStream(stream, false,
                                                        certValidationWithIrcAsSender,
                                                        selectionCallback);
                    try
                    {
                        if (SslClientCertificate != null)
                        {
                            var certs = new X509Certificate2Collection
                            {
                                SslClientCertificate
                            };
                            sslStream.AuthenticateAsClient(Address, certs,
                                                           SslProtocols.Default,
                                                           false);
                        }
                        else
                        {
                            sslStream.AuthenticateAsClient(Address);
                        }
                    }
                    catch (IOException ex)
                    {

                        throw new CouldNotConnectException("Could not connect to: " + Address + ":" + Port + " " + ex.Message, ex);
                    }
                    stream = sslStream;
                }
                if (EnableUTF8Recode)
                {
                    _Reader = new StreamReader(stream, new PrimaryOrFallbackEncoding(new UTF8Encoding(false, true), Encoding));
                    _Writer = new StreamWriter(stream, new UTF8Encoding(false, false));
                }
                else
                {
                    _Reader = new StreamReader(stream, Encoding);
                    _Writer = new StreamWriter(stream, Encoding);

                    if (Encoding.GetPreamble().Length > 0)
                    {
                        // HACK: we have an encoding that has some kind of preamble
                        // like UTF-8 has a BOM, this will confuse the IRCd!
                        // Thus we send a \r\n so the IRCd can safely ignore that
                        // garbage.
                        _Writer.WriteLine();
                        // make sure we flush the BOM+CRLF correctly
                        _Writer.Flush();
                    }
                }

                // Connection was succeful, reseting the connect counter
                AutoRetryAttempt = 0;

                // updating the connection error state, so connecting is possible again
                IsConnectionError = false;
                IsConnected = true;

                // lets power up our threads
                _ReadThread.Start();
                _WriteThread.Start();
                _IdleWorkerThread.Start();


                OnConnected?.Invoke(this, EventArgs.Empty);
            }
            catch (AuthenticationException ex)
            {

                throw new CouldNotConnectException("Could not connect to: " + Address + ":" + Port + " " + ex.Message, ex);
            }
            catch (Exception e)
            {
                if (_Reader != null)
                {
                    try
                    {
                        _Reader.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
                if (_Writer != null)
                {
                    try
                    {
                        _Writer.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
                _TcpClient?.Close();
                IsConnected = false;
                IsConnectionError = true;


                if (e is CouldNotConnectException)
                {
                    // error was fatal, bail out
                    throw;
                }

                if (AutoRetry &&
                    (AutoRetryLimit == -1 ||
                     AutoRetryLimit == 0 ||
                     AutoRetryLimit <= AutoRetryAttempt))
                {
                    OnAutoConnectError?.Invoke(this, new AutoConnectErrorEventArgs(Address, Port, e));

                    Thread.Sleep(AutoRetryDelay * 1000);
                    NextAddress();
                    // FIXME: this is recursion
                    Connect(AddressList, Port);
                }
                else
                {
                    throw new CouldNotConnectException("Could not connect to: " + Address + ":" + Port + " " + e.Message, e);
                }
            }
        }

        public void Connect(string address, int port)
        {
            Connect(new string[] { address }, port);
        }

        public void Reconnect()
        {

            Disconnect();
            Connect(AddressList, Port);
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new NotConnectedException("The connection could not be disconnected because there is no active connection");
            }


            OnDisconnecting?.Invoke(this, EventArgs.Empty);

            IsDisconnecting = true;

            _IdleWorkerThread.Stop();
            _ReadThread.Stop();
            _WriteThread.Stop();
            _TcpClient.Close();
            IsConnected = false;
            IsRegistered = false;

            // signal ReadLine() to check IsConnected state
            _ReadThread.QueuedEvent.Set();

            IsDisconnecting = false;

            OnDisconnected?.Invoke(this, EventArgs.Empty);


        }

        public void Listen(bool blocking)
        {
            if (blocking)
            {
                while (IsConnected)
                {
                    ReadLine(true);
                }
            }
            else
            {
                while (ReadLine(false).Length > 0)
                {
                    // loop as long as we receive messages
                }
            }
        }

        public void Listen()
        {
            Listen(true);
        }

        public void ListenOnce(bool blocking)
        {
            ReadLine(blocking);
        }

        public void ListenOnce()
        {
            ListenOnce(true);
        }

        public string ReadLine(bool blocking)
        {
            var data = "";
            if (blocking)
            {
                // block till the queue has data, but bail out on connection error
                while (IsConnected &&
                       !IsConnectionError &&
                       _ReadThread.Queue.Count == 0)
                {
                    _ReadThread.QueuedEvent.WaitOne();
                }
            }

            if (IsConnected &&
                _ReadThread.Queue.Count > 0)
            {
                data = (string)(_ReadThread.Queue.Dequeue());
            }

            if (data != null && data.Length > 0)
            {

                OnReadLine?.Invoke(this, new ReadLineEventArgs(data));
            }

            if (IsConnectionError &&
                !IsDisconnecting &&
                OnConnectionError != null)
            {
                OnConnectionErrorHandler(this, EventArgs.Empty);
            }

            return data;
        }

        public void WriteLine(string data, Priority priority)
        {
            if (priority == Priority.Critical)
            {
                if (!IsConnected)
                {
                    throw new NotConnectedException();
                }

                WriteLineInternal(data);
            }
            else
            {
                ((Queue)_SendBuffer[priority]).Enqueue(data);
                _WriteThread.QueuedEvent.Set();
            }
        }

        public void WriteLine(string data)
        {
            WriteLine(data, Priority.Medium);
        }

        private bool WriteLineInternal(string data)
        {
            if (IsConnected)
            {
                try
                {
                    lock (_Writer)
                    {
                        _Writer.Write(data + "\r\n");
                        _Writer.Flush();
                    }
                }
                catch (IOException)
                {

                    IsConnectionError = true;
                    return false;
                }
                catch (ObjectDisposedException)
                {

                    IsConnectionError = true;
                    return false;
                }


                OnWriteLine?.Invoke(this, new WriteLineEventArgs(data));
                return true;
            }

            return false;
        }

        private void NextAddress()
        {
            _CurrentAddress++;
            if (_CurrentAddress >= AddressList.Length)
            {
                _CurrentAddress = 0;
            }

        }

        private void SimpleParser(object sender, ReadLineEventArgs args)
        {
            var rawline = args.Line;
            var rawlineex = rawline.Split(new char[] { ' ' });
            string line;
            if (rawline[0] == ':')
            {
                var prefix = rawlineex[0].Substring(1);
                line = rawline.Substring(prefix.Length + 2);
            }
            else
            {
                line = rawline;
            }
            var lineex = line.Split(new char[] { ' ' });
            var command = lineex[0];
            var replycode = ReplyCode.Null;
            if (Int32.TryParse(command, out var intReplycode))
            {
                replycode = (ReplyCode)intReplycode;
            }
            if (replycode != ReplyCode.Null)
            {
                switch (replycode)
                {
                    case ReplyCode.Welcome:
                        IsRegistered = true;

                        break;
                }
            }
            else
            {
                switch (command)
                {
                    case "ERROR":
                        // FIXME: handle server errors differently than connection errors!
                        //IsConnectionError = true;
                        break;
                    case "PONG":
                        PingStopwatch.Stop();
                        NextPingStopwatch.Reset();
                        NextPingStopwatch.Start();


                        break;
                }
            }
        }

        private void OnConnectionErrorHandler(object sender, EventArgs e)
        {
            try
            {
                if (AutoReconnect)
                {
                    // prevent connect -> exception -> connect flood loop
                    Thread.Sleep(AutoRetryDelay * 1000);
                    // lets try to recover the connection
                    Reconnect();
                }
                else
                {
                    // make sure we clean up
                    Disconnect();
                }
            }
            catch (ConnectionException)
            {
            }
        }

        private class ReadThread
        {

            private readonly IrcConnection _Connection;
            private Thread _Thread;
            public AutoResetEvent QueuedEvent;

            public Queue Queue { get; } = Queue.Synchronized(new Queue());

            public ReadThread(IrcConnection connection)
            {
                _Connection = connection;
                QueuedEvent = new AutoResetEvent(false);
            }

            public void Start()
            {
                _Thread = new Thread(new ThreadStart(Worker))
                {
                    Name = "ReadThread (" + _Connection.Address + ":" + _Connection.Port + ")",
                    IsBackground = true
                };
                _Thread.Start();
            }

            public void Stop()
            {



                _Thread.Abort();
                // make sure we close the stream after the thread is gone, else
                // the thread will think the connection is broken!

                _Thread.Join();


                try
                {
                    _Connection._Reader.Close();
                }
                catch (ObjectDisposedException)
                {
                }

                // clean up our receive queue else we continue processing old
                // messages when the read thread is restarted!
                Queue.Clear();
            }

            private void Worker()
            {

                try
                {
                    var data = "";
                    try
                    {
                        while (_Connection.IsConnected &&
                               ((data = _Connection._Reader.ReadLine()) != null))
                        {
                            Queue.Enqueue(data);
                            QueuedEvent.Set();

                        }
                    }
                    catch (IOException)
                    {

                    }
                    finally
                    {

                        // only flag this as connection error if we are not
                        // cleanly disconnecting
                        if (!_Connection.IsDisconnecting)
                        {
                            _Connection.IsConnectionError = true;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();

                }
                catch (Exception)
                {

                }
            }
        }

        private class WriteThread
        {
            private readonly IrcConnection _Connection;
            private Thread _Thread;
            private int _HighCount;
            private int _AboveMediumCount;
            private int _MediumCount;
            private int _BelowMediumCount;
            private int _LowCount;
            private int _AboveMediumSentCount;
            private int _MediumSentCount;
            private int _BelowMediumSentCount;
            private readonly int _AboveMediumThresholdCount = 4;
            private readonly int _MediumThresholdCount = 2;
            private readonly int _BelowMediumThresholdCount = 1;
            private int _BurstCount;

            public AutoResetEvent QueuedEvent;

            public WriteThread(IrcConnection connection)
            {
                _Connection = connection;
                QueuedEvent = new AutoResetEvent(false);
            }

            public void Start()
            {
                _Thread = new Thread(new ThreadStart(Worker))
                {
                    Name = "WriteThread (" + _Connection.Address + ":" + _Connection.Port + ")",
                    IsBackground = true
                };
                _Thread.Start();
            }

            public void Stop()
            {


                _Thread.Abort();
                // make sure we close the stream after the thread is gone, else
                // the thread will think the connection is broken!
                _Thread.Join();

                try
                {
                    _Connection._Writer.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            private void Worker()
            {

                try
                {
                    try
                    {
                        while (_Connection.IsConnected)
                        {
                            QueuedEvent.WaitOne();
                            var isBufferEmpty = false;
                            do
                            {
                                isBufferEmpty = CheckBuffer() == 0;
                                Thread.Sleep(_Connection.SendDelay);
                            } while (!isBufferEmpty);
                        }
                    }
                    catch (IOException)
                    {

                    }
                    finally
                    {

                        // only flag this as connection error if we are not
                        // cleanly disconnecting
                        if (!_Connection.IsDisconnecting)
                        {
                            _Connection.IsConnectionError = true;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();

                }
                catch (Exception)
                {

                }
            }

            #region WARNING: complex scheduler, don't even think about changing it!
            // WARNING: complex scheduler, don't even think about changing it!
            private int CheckBuffer()
            {
                _HighCount = ((Queue)_Connection._SendBuffer[Priority.High]).Count;
                _AboveMediumCount = ((Queue)_Connection._SendBuffer[Priority.AboveMedium]).Count;
                _MediumCount = ((Queue)_Connection._SendBuffer[Priority.Medium]).Count;
                _BelowMediumCount = ((Queue)_Connection._SendBuffer[Priority.BelowMedium]).Count;
                _LowCount = ((Queue)_Connection._SendBuffer[Priority.Low]).Count;

                var msgCount = _HighCount +
                               _AboveMediumCount +
                               _MediumCount +
                               _BelowMediumCount +
                               _LowCount;

                // only send data if we are succefully registered on the IRC network
                if (!_Connection.IsRegistered)
                {
                    return msgCount;
                }

                if (CheckHighBuffer() &&
                    CheckAboveMediumBuffer() &&
                    CheckMediumBuffer() &&
                    CheckBelowMediumBuffer() &&
                    CheckLowBuffer())
                {
                    // everything is sent, resetting all counters
                    _AboveMediumSentCount = 0;
                    _MediumSentCount = 0;
                    _BelowMediumSentCount = 0;
                    _BurstCount = 0;
                }

                if (_BurstCount < 3)
                {
                    _BurstCount++;
                    //_CheckBuffer();
                }

                return msgCount;
            }

            private bool CheckHighBuffer()
            {
                if (_HighCount > 0)
                {
                    var data = (string)((Queue)_Connection._SendBuffer[Priority.High]).Dequeue();
                    if (_Connection.WriteLineInternal(data) == false)
                    {

                        ((Queue)_Connection._SendBuffer[Priority.High]).Enqueue(data);
                        return false;
                    }

                    if (_HighCount > 1)
                    {
                        // there is more data to send
                        return false;
                    }
                }

                return true;
            }

            private bool CheckAboveMediumBuffer()
            {
                if ((_AboveMediumCount > 0) &&
                    (_AboveMediumSentCount < _AboveMediumThresholdCount))
                {
                    var data = (string)((Queue)_Connection._SendBuffer[Priority.AboveMedium]).Dequeue();
                    if (_Connection.WriteLineInternal(data) == false)
                    {

                        ((Queue)_Connection._SendBuffer[Priority.AboveMedium]).Enqueue(data);
                        return false;
                    }
                    _AboveMediumSentCount++;

                    if (_AboveMediumSentCount < _AboveMediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool CheckMediumBuffer()
            {
                if ((_MediumCount > 0) &&
                    (_MediumSentCount < _MediumThresholdCount))
                {
                    var data = (string)((Queue)_Connection._SendBuffer[Priority.Medium]).Dequeue();
                    if (_Connection.WriteLineInternal(data) == false)
                    {

                        ((Queue)_Connection._SendBuffer[Priority.Medium]).Enqueue(data);
                        return false;
                    }
                    _MediumSentCount++;

                    if (_MediumSentCount < _MediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool CheckBelowMediumBuffer()
            {
                if ((_BelowMediumCount > 0) &&
                    (_BelowMediumSentCount < _BelowMediumThresholdCount))
                {
                    var data = (string)((Queue)_Connection._SendBuffer[Priority.BelowMedium]).Dequeue();
                    if (_Connection.WriteLineInternal(data) == false)
                    {

                        ((Queue)_Connection._SendBuffer[Priority.BelowMedium]).Enqueue(data);
                        return false;
                    }
                    _BelowMediumSentCount++;

                    if (_BelowMediumSentCount < _BelowMediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool CheckLowBuffer()
            {
                if (_LowCount > 0)
                {
                    if ((_HighCount > 0) ||
                        (_AboveMediumCount > 0) ||
                        (_MediumCount > 0) ||
                        (_BelowMediumCount > 0))
                    {
                        return true;
                    }

                    var data = (string)((Queue)_Connection._SendBuffer[Priority.Low]).Dequeue();
                    if (_Connection.WriteLineInternal(data) == false)
                    {

                        ((Queue)_Connection._SendBuffer[Priority.Low]).Enqueue(data);
                        return false;
                    }

                    if (_LowCount > 1)
                    {
                        return false;
                    }
                }

                return true;
            }
            // END OF WARNING, below this you can read/change again ;)
            #endregion
        }

        private class IdleWorkerThread
        {
            private readonly IrcConnection _Connection;
            private Thread _Thread;

            public IdleWorkerThread(IrcConnection connection)
            {
                _Connection = connection;
            }

            public void Start()
            {
                _Connection.PingStopwatch.Reset();
                _Connection.NextPingStopwatch.Reset();
                _Connection.NextPingStopwatch.Start();

                _Thread = new Thread(new ThreadStart(Worker))
                {
                    Name = "IdleWorkerThread (" + _Connection.Address + ":" + _Connection.Port + ")",
                    IsBackground = true
                };
                _Thread.Start();
            }

            public void Stop()
            {
                _Thread.Abort();
                _Thread.Join();
            }

            private void Worker()
            {

                try
                {
                    while (_Connection.IsConnected)
                    {
                        Thread.Sleep(_Connection.IdleWorkerInterval * 1000);

                        // only send active pings if we are registered
                        if (!_Connection.IsRegistered)
                        {
                            continue;
                        }

                        var last_ping_sent = (int)_Connection.PingStopwatch.Elapsed.TotalSeconds;
                        var last_pong_rcvd = (int)_Connection.NextPingStopwatch.Elapsed.TotalSeconds;
                        // determins if the resoponse time is ok
                        if (last_ping_sent < _Connection.PingTimeout)
                        {
                            if (_Connection.PingStopwatch.IsRunning)
                            {
                                // there is a pending ping request, we have to wait
                                continue;
                            }

                            // determines if it need to send another ping yet
                            if (last_pong_rcvd > _Connection.PingInterval)
                            {
                                _Connection.NextPingStopwatch.Stop();
                                _Connection.PingStopwatch.Reset();
                                _Connection.PingStopwatch.Start();
                                _Connection.WriteLine(Rfc2812.Ping(_Connection.Address), Priority.Critical);
                            } // else connection is fine, just continue
                        }
                        else
                        {
                            if (_Connection.IsDisconnecting)
                            {
                                break;
                            }

                            // only flag this as connection error if we are not
                            // cleanly disconnecting
                            _Connection.IsConnectionError = true;
                            break;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();

                }
                catch (Exception)
                {

                }
            }
        }
    }
}
