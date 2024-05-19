/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2010, 2012-2014 Mirco Bauer <meebey@meebey.net>
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents an IRC client.
    /// </summary>
    public class IrcClient : IrcCommands
    {
        private string[] _NicknameList;
        private int _CurrentNickname;
        private readonly Dictionary<string, string> _AutoRejoinChannels = new Dictionary<string, string>();
        private readonly StringCollection _Motd = new StringCollection();
        private bool _MotdReceived;
        private readonly Array _ReplyCodes = Enum.GetValues(typeof(ReplyCode));
        private readonly StringCollection _JoinedChannels = new StringCollection();
        private readonly Hashtable _Channels = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));
        private readonly Hashtable _IrcUsers = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));
        private List<ChannelInfo> _ChannelList;
        private readonly object _ChannelListSyncRoot = new object();
        private AutoResetEvent _ChannelListReceivedEvent;
        private List<WhoInfo> _WhoList;
        private readonly object _WhoListSyncRoot = new object();
        private AutoResetEvent _WhoListReceivedEvent;
        private List<BanInfo> _BanList;
        private readonly object _BanListSyncRoot = new object();
        private AutoResetEvent _BanListReceivedEvent;
        private List<BanInfo> _BanExceptList;
        private readonly Object _BanExceptListSyncRoot = new Object();
        private AutoResetEvent _BanExceptListReceivedEvent;
        private List<BanInfo> _InviteExceptList;
        private readonly Object _InviteExceptListSyncRoot = new Object();
        private AutoResetEvent _InviteExceptListReceivedEvent;
        private readonly ServerProperties _ServerProperties = new ServerProperties();
        private static readonly Regex _ReplyCodeRegex = new Regex("^:?[^ ]+? ([0-9]{3}) .+$", RegexOptions.Compiled);
        private static readonly Regex _PingRegex = new Regex("^PING :.*", RegexOptions.Compiled);
        private static readonly Regex _ErrorRegex = new Regex("^ERROR :.*", RegexOptions.Compiled);
        private static readonly Regex _ActionRegex = new Regex("^:?.*? PRIVMSG (.).* :" + "\x1" + "ACTION .*" + "\x1" + "$", RegexOptions.Compiled);
        private static readonly Regex _CtcpRequestRegex = new Regex("^:?.*? PRIVMSG .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled);
        private static readonly Regex _MessageRegex = new Regex("^:?.*? PRIVMSG (.).* :.*$", RegexOptions.Compiled);
        private static readonly Regex _CtcpReplyRegex = new Regex("^:?.*? NOTICE .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled);
        private static readonly Regex _NoticeRegex = new Regex("^:?.*? NOTICE (.).* :.*$", RegexOptions.Compiled);
        private static readonly Regex _InviteRegex = new Regex("^:?.*? INVITE .* .*$", RegexOptions.Compiled);
        private static readonly Regex _JoinRegex = new Regex("^:?.*? JOIN .*$", RegexOptions.Compiled);
        private static readonly Regex _TopicRegex = new Regex("^:?.*? TOPIC .* :.*$", RegexOptions.Compiled);
        private static readonly Regex _NickRegex = new Regex("^:?.*? NICK .*$", RegexOptions.Compiled);
        private static readonly Regex _KickRegex = new Regex("^:?.*? KICK .* .*$", RegexOptions.Compiled);
        private static readonly Regex _PartRegex = new Regex("^:?.*? PART .*$", RegexOptions.Compiled);
        private static readonly Regex _ModeRegex = new Regex("^:?.*? MODE (.*) .*$", RegexOptions.Compiled);
        private static readonly Regex _QuitRegex = new Regex("^:?.*? QUIT :.*$", RegexOptions.Compiled);
        private static readonly Regex _BounceMessageRegex = new Regex("^Try server (.+), port ([0-9]+)$", RegexOptions.Compiled);

        /// <summary>
        /// Maps channel modes to their respective characters.
        /// </summary>
        ChannelModeMap ChannelModeMap { get; set; }

        /// <summary>
        /// Occurs when the client has successfully registered.
        /// </summary>
        public event EventHandler OnRegistered;
        /// <summary>
        /// Occurs when a PING message is received from the server.
        /// </summary>
        public event PingEventHandler OnPing;
        /// <summary>
        /// Occurs when a PONG message is received from the server.
        /// </summary>
        public event PongEventHandler OnPong;
        /// <summary>
        /// Occurs when any raw IRC message is received.
        /// </summary>
        public event IrcEventHandler OnRawMessage;
        /// <summary>
        /// Occurs when an error is encountered.
        /// </summary>
        public event ErrorEventHandler OnError;
        /// <summary>
        /// Occurs when an error message is received from the server.
        /// </summary>
        public event IrcEventHandler OnErrorMessage;
        /// <summary>
        /// Occurs when a JOIN message is received, indicating that a user has joined a channel.
        /// </summary>
        public event JoinEventHandler OnJoin;
        /// <summary>
        /// Occurs when a NAMES message is received, listing the users in a channel.
        /// </summary>
        public event NamesEventHandler OnNames;
        /// <summary>
        /// Occurs when a LIST message is received, listing the channels on the server.
        /// </summary>
        public event ListEventHandler OnList;
        /// <summary>
        /// Occurs when a PART message is received, indicating that a user has left a channel.
        /// </summary>
        public event PartEventHandler OnPart;
        /// <summary>
        /// Occurs when a QUIT message is received, indicating that a user has quit IRC.
        /// </summary>
        public event QuitEventHandler OnQuit;
        /// <summary>
        /// Occurs when a KICK message is received, indicating that a user has been kicked from a channel.
        /// </summary>
        public event KickEventHandler OnKick;
        /// <summary>
        /// Occurs when an AWAY message is received, indicating that a user has set or removed their away status.
        /// </summary>
        public event AwayEventHandler OnAway;
        /// <summary>
        /// Occurs when an UNAWAY message is received, indicating that a user has removed their away status.
        /// </summary>
        public event IrcEventHandler OnUnAway;
        /// <summary>
        /// Occurs when a NOWAWAY message is received, indicating that a user has set their away status.
        /// </summary>
        public event IrcEventHandler OnNowAway;
        /// <summary>
        /// Occurs when an INVITE message is received, indicating that a user has been invited to a channel.
        /// </summary>
        public event InviteEventHandler OnInvite;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a ban has been set on a channel.
        /// </summary>
        public event BanEventHandler OnBan;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a ban has been removed from a channel.
        /// </summary>
        public event UnbanEventHandler OnUnban;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a ban exception has been set on a channel.
        /// </summary>
        public event BanEventHandler OnBanException;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a ban exception has been removed from a channel.
        /// </summary>
        public event UnbanEventHandler OnUnBanException;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that an invite exception has been set on a channel.
        /// </summary>
        public event BanEventHandler OnInviteException;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that an invite exception has been removed from a channel.
        /// </summary>
        public event UnbanEventHandler OnUnInviteException;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been given owner status in a channel.
        /// </summary>
        public event OwnerEventHandler OnOwner;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been removed from owner status in a channel.
        /// </summary>
        public event DeownerEventHandler OnDeowner;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been given channel admin status in a channel.
        /// </summary>
        public event ChannelAdminEventHandler OnChannelAdmin;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been removed from channel admin status in a channel.
        /// </summary>
        public event DeChannelAdminEventHandler OnDeChannelAdmin;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been given operator status in a channel.
        /// </summary>
        public event OpEventHandler OnOp;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been removed from operator status in a channel.
        /// </summary>
        public event DeopEventHandler OnDeop;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been given half-operator status in a channel.
        /// </summary>
        public event HalfopEventHandler OnHalfop;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been removed from half-operator status in a channel.
        /// </summary>
        public event DehalfopEventHandler OnDehalfop;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been given voice status in a channel.
        /// </summary>
        public event VoiceEventHandler OnVoice;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that a user has been removed from voice status in a channel.
        /// </summary>
        public event DevoiceEventHandler OnDevoice;
        /// <summary>
        /// Occurs when a WHO message is received, listing the users on the server or in a channel.
        /// </summary>
        public event WhoEventHandler OnWho;
        /// <summary>
        /// Occurs when a MOTD (Message of the Day) message is received from the server.
        /// </summary>
        public event MotdEventHandler OnMotd;
        /// <summary>
        /// Occurs when a TOPIC message is received, indicating the topic of a channel.
        /// </summary>
        public event TopicEventHandler OnTopic;
        /// <summary>
        /// Occurs when a TOPIC message is received, indicating that the topic of a channel has changed.
        /// </summary>
        public event TopicChangeEventHandler OnTopicChange;
        /// <summary>
        /// Occurs when a NICK message is received, indicating that a user has changed their nickname.
        /// </summary>
        public event NickChangeEventHandler OnNickChange;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that the mode of a channel has changed.
        /// </summary>
        public event IrcEventHandler OnModeChange;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that the mode of a user has changed.
        /// </summary>
        public event IrcEventHandler OnUserModeChange;
        /// <summary>
        /// Occurs when a MODE message is received, indicating that the mode of a channel has changed.
        /// </summary>
        public event EventHandler<ChannelModeChangeEventArgs> OnChannelModeChange;
        /// <summary>
        /// Occurs when a PRIVMSG message is received, indicating a message to a channel.
        /// </summary>
        public event IrcEventHandler OnChannelMessage;
        /// <summary>
        /// Occurs when an ACTION message is received, indicating an action in a channel.
        /// </summary>
        public event ActionEventHandler OnChannelAction;
        /// <summary>
        /// Occurs when a NOTICE message is received, indicating a notice to a channel.
        /// </summary>
        public event IrcEventHandler OnChannelNotice;
        /// <summary>
        /// Occurs when a channel has been actively synced.
        /// </summary>
        public event IrcEventHandler OnChannelActiveSynced;
        /// <summary>
        /// Occurs when a channel has been passively synced.
        /// </summary>
        public event IrcEventHandler OnChannelPassiveSynced;
        /// <summary>
        /// Occurs when a PRIVMSG message is received, indicating a private message.
        /// </summary>
        public event IrcEventHandler OnQueryMessage;
        /// <summary>
        /// Occurs when an ACTION message is received, indicating an action in a private message.
        /// </summary>
        public event ActionEventHandler OnQueryAction;
        /// <summary>
        /// Occurs when a NOTICE message is received, indicating a notice in a private message.
        /// </summary>
        public event IrcEventHandler OnQueryNotice;
        /// <summary>
        /// Occurs when a CTCP request is received.
        /// </summary>
        public event CtcpEventHandler OnCtcpRequest;
        /// <summary>
        /// Occurs when a CTCP reply is received.
        /// </summary>
        public event CtcpEventHandler OnCtcpReply;
        /// <summary>
        /// Occurs when a bounce message is received from the server.
        /// </summary>
        public event BounceEventHandler OnBounce;

        /// <summary>
        /// Gets or sets a value indicating whether active channel syncing is enabled.
        /// </summary>
        public bool ActiveChannelSyncing { get; set; }

        /// <summary>
        /// Gets a value indicating whether passive channel syncing is enabled.
        /// </summary>
        public bool PassiveChannelSyncing { get; private set; }

        /// <summary>
        /// Gets or sets the CTCP version.
        /// </summary>
        public string CtcpVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically join on invite.
        /// </summary>
        public bool AutoJoinOnInvite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically rejoin.
        /// </summary>
        public bool AutoRejoin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically rejoin on kick.
        /// </summary>
        public bool AutoRejoinOnKick { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically relogin.
        /// </summary>
        public bool AutoRelogin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to handle nick automatically.
        /// </summary>
        public bool AutoNickHandling { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to support non-RFC.
        /// </summary>
        public bool SupportNonRfc { get; set; }

        /// <summary>
        /// Gets the nickname of the IRC client.
        /// </summary>
        public string Nickname { get; private set; }

        /// <summary>
        /// Gets the list of nicknames for the IRC client.
        /// </summary>
        public string[] NicknameList { get; private set; }

        /// <summary>
        /// Gets the real name of the IRC client.
        /// </summary>
        public string Realname { get; private set; }

        /// <summary>
        /// Gets the username of the IRC client.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Gets the user mode of the IRC client.
        /// </summary>
        public string Usermode { get; private set; }

        /// <summary>
        /// Gets the integer representation of the user mode of the IRC client.
        /// </summary>
        public int IUsermode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the IRC client is away.
        /// </summary>
        public bool IsAway { get; private set; }

        /// <summary>
        /// Gets the password of the IRC client.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets the collection of channels the IRC client has joined.
        /// </summary>
        public StringCollection JoinedChannels { get; private set; }

        /// <summary>
        /// Gets the message of the day from the server.
        /// </summary>
        public StringCollection Motd { get; private set; }

        /// <summary>
        /// Gets the synchronization object for the ban list.
        /// </summary>
        public object BanListSyncRoot { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the client supports non-RFC channels.
        /// </summary>
        public bool SupportNonRfcLocked { get; private set; }

        /// <summary>
        /// Gets the server properties of the IRC client.
        /// </summary>
        public ServerProperties ServerProperties { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcClient"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor initializes the ChannelModeMap and sets up several event handlers.
        /// </remarks>
        public IrcClient()
        {
            // Event handler for processing each line read from the IRC server
            OnReadLine += new ReadLineEventHandler(_Worker);

            // Event handler for actions to take when the client is disconnected
            OnDisconnected += new EventHandler(_OnDisconnected);

            // Event handler for actions to take when a connection error occurs
            OnConnectionError += new EventHandler(_OnConnectionError);

            // Initialize the map of channel modes
            ChannelModeMap = new ChannelModeMap();
        }

        /// <summary>
        /// Connects to the IRC server.
        /// </summary>
        /// <param name="addresslist">An array of server addresses to connect to.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <remarks>
        /// This method locks the SupportNonRfc property, initializes the ChannelModeMap, and then calls the base class's Connect method.
        /// </remarks>
        public new void Connect(string[] addresslist, int port)
        {
            SupportNonRfcLocked = true;
            ChannelModeMap = new ChannelModeMap();
            base.Connect(addresslist, port);
        }

        /// <summary>
        /// Reconnects to the IRC server.
        /// </summary>
        /// <param name="login">If set to <c>true</c>, the client will automatically log in after reconnecting.</param>
        /// <param name="channels">If set to <c>true</c>, the client will automatically rejoin the channels it was in prior to disconnecting.</param>
        /// <remarks>
        /// This method will store the channels to rejoin if needed, reconnect to the server, log in if specified, and rejoin channels if specified.
        /// </remarks>
        public void Reconnect(bool login, bool channels)
        {
            if (channels)
                _StoreChannelsToRejoin();

            base.Reconnect();
            if (login)
            {
                //reset the nick to the original nicklist
                _CurrentNickname = 0;
                // FIXME: honor _Nickname (last used nickname)
                Login(_NicknameList, Realname, IUsermode, Username, Password);
            }
            if (channels)
                _RejoinChannels();
        }

        /// <summary>
        /// Logs in to the IRC server.
        /// </summary>
        /// <param name="nicklist">An array of nicknames to use for the login. The first nickname in the array will be used first, and if it is taken, the next nicknames in the array will be used in order.</param>
        /// <param name="realname">The real name of the user.</param>
        /// <param name="usermode">The user mode to set for the user. This is an integer where each digit is a binary representation of a user mode.</param>
        /// <param name="username">The username to use for the login. If this is null or empty, the username of the user currently logged in to the operating system will be used.</param>
        /// <param name="password">The password to use for the login. If this is null or empty, no password will be sent to the server.</param>
        /// <remarks>
        /// This method sends the PASS command to the server if a password is specified, followed by the NICK and USER commands to complete the login process.
        /// </remarks>
        public void Login(string[] nicklist, string realname, int usermode, string username, string password)
        {

            _NicknameList = (string[])nicklist.Clone();
            // here we set the nickname which we will try first
            Nickname = _NicknameList[0].Replace(" ", "");
            Realname = realname;
            IUsermode = usermode;

            Username = username != null && username.Length > 0 ? username.Replace(" ", "") : Environment.UserName.Replace(" ", "");

            if (password != null && password.Length > 0)
            {
                Password = password;
                RfcPass(Password, Priority.Critical);
            }

            RfcNick(Nickname, Priority.Critical);
            RfcUser(Username, IUsermode, Realname, Priority.Critical);
        }

        /// <summary>
        /// Logs in to the IRC server with the specified nicknames, real name, user mode, and username.
        /// </summary>
        /// <param name="nicklist">An array of nicknames to use for the login.</param>
        /// <param name="realname">The real name of the user.</param>
        /// <param name="usermode">The user mode to set for the user.</param>
        /// <param name="username">The username to use for the login.</param>
        public void Login(string[] nicklist, string realname, int usermode, string username)
        {
            Login(nicklist, realname, usermode, username, "");
        }

        /// <summary>
        /// Logs in to the IRC server with the specified nicknames, real name, and user mode.
        /// </summary>
        /// <param name="nicklist">An array of nicknames to use for the login.</param>
        /// <param name="realname">The real name of the user.</param>
        /// <param name="usermode">The user mode to set for the user.</param>
        public void Login(string[] nicklist, string realname, int usermode)
        {
            Login(nicklist, realname, usermode, "", "");
        }

        /// <summary>
        /// Logs in to the IRC server with the specified nicknames and real name.
        /// </summary>
        /// <param name="nicklist">An array of nicknames to use for the login.</param>
        /// <param name="realname">The real name of the user.</param>
        public void Login(string[] nicklist, string realname)
        {
            Login(nicklist, realname, 0, "", "");
        }

        /// <summary>
        /// Logs in to the IRC server with the specified nickname, real name, user mode, username, and password.
        /// </summary>
        /// <param name="nick">The nickname to use for the login.</param>
        /// <param name="realname">The real name of the user.</param>
        /// <param name="usermode">The user mode to set for the user.</param>
        /// <param name="username">The username to use for the login.</param>
        /// <param name="password">The password to use for the login.</param>
        public void Login(string nick, string realname, int usermode, string username, string password)
        {
            Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, username, password);
        }

        /// <summary>
        /// Logs in to the IRC server with the specified nickname, real name, user mode, and username.
        /// </summary>
        /// <param name="nick">The nickname to use for the login.</param>
        /// <param name="realname">The real name of the user.</param>
        /// <param name="usermode">The user mode to set for the user.</param>
        /// <param name="username">The username to use for the login.</param>
        public void Login(string nick, string realname, int usermode, string username)
        {
            Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, username, "");
        }

        /// <summary>
        /// Logs in to the IRC server with the specified nickname, real name, and user mode.
        /// </summary>
        /// <param name="nick">The nickname to use for the login.</param>
        /// <param name="realname">The real name of the user.</param>
        /// <param name="usermode">The user mode to set for the user.</param>
        public void Login(string nick, string realname, int usermode)
        {
            Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, "", "");
        }

        /// <summary>
        /// Logs in to the IRC server with the specified nickname and real name.
        /// </summary>
        /// <param name="nick">The nickname to use for the login.</param>
        /// <param name="realname">The real name of the user.</param>
        public void Login(string nick, string realname)
        {
            Login(new string[] { nick, nick + "_", nick + "__" }, realname, 0, "", "");
        }

        /// <summary>
        /// Determines whether the specified nickname is the same as the current user's nickname.
        /// </summary>
        /// <param name="nickname">The nickname to compare with the current user's nickname.</param>
        /// <returns>true if the specified nickname is the same as the current user's nickname; otherwise, false.</returns>
        public bool IsMe(string nickname)
        {
            return String.Compare(Nickname, nickname, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Determines whether the current user has joined the specified channel.
        /// </summary>
        /// <param name="channelname">The name of the channel to check.</param>
        /// <returns>true if the current user has joined the specified channel; otherwise, false.</returns>
        public bool IsJoined(string channelname)
        {
            return IsJoined(channelname, Nickname);
        }

        /// <summary>
        /// Determines whether the specified user has joined the specified channel.
        /// </summary>
        /// <param name="channelname">The name of the channel to check.</param>
        /// <param name="nickname">The nickname of the user to check.</param>
        /// <returns>true if the specified user has joined the specified channel; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when channelname or nickname is null.</exception>
        public bool IsJoined(string channelname, string nickname)
        {
            if (channelname == null)
                throw new System.ArgumentNullException("channelname");

            if (nickname == null)
                throw new System.ArgumentNullException("nickname");

            var channel = GetChannel(channelname);
            if (channel != null &&
                channel.UnsafeUsers != null &&
                channel.UnsafeUsers.ContainsKey(nickname))
                return true;

            return false;
        }

        /// <summary>
        /// Retrieves the <see cref="IrcUser"/> object associated with the specified nickname.
        /// </summary>
        /// <param name="nickname">The nickname of the user.</param>
        /// <returns>The <see cref="IrcUser"/> object associated with the specified nickname, or null if the user is not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the nickname is null.</exception>
        public IrcUser GetIrcUser(string nickname)
        {
            if (nickname == null)
                throw new System.ArgumentNullException("nickname");

            return (IrcUser)_IrcUsers[nickname];
        }

        /// <summary>
        /// Retrieves the <see cref="ChannelUser"/> object associated with the specified nickname in the specified channel.
        /// </summary>
        /// <param name="channelname">The name of the channel.</param>
        /// <param name="nickname">The nickname of the user.</param>
        /// <returns>The <see cref="ChannelUser"/> object associated with the specified nickname in the specified channel, or null if the user or channel is not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channelname or nickname is null.</exception>
        public ChannelUser GetChannelUser(string channelname, string nickname)
        {
            if (channelname == null)
                throw new System.ArgumentNullException("channel");

            if (nickname == null)
                throw new System.ArgumentNullException("nickname");

            var channel = GetChannel(channelname);
            return channel != null ? (ChannelUser)channel.UnsafeUsers[nickname] : null;
        }

        /// <summary>
        /// Retrieves the <see cref="Channel"/> object associated with the specified channel name.
        /// </summary>
        /// <param name="channelname">The name of the channel.</param>
        /// <returns>The <see cref="Channel"/> object associated with the specified channel name, or null if the channel is not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channelname is null.</exception>
        public Channel GetChannel(string channelname)
        {
            if (channelname == null)
                throw new System.ArgumentNullException("channelname");

            return (Channel)_Channels[channelname];
        }

        /// <summary>
        /// Retrieves the names of all channels that the client is currently joined to.
        /// </summary>
        /// <returns>An array of channel names that the client is currently joined to.</returns>
        public string[] GetChannels()
        {
            var channels = new string[_Channels.Values.Count];
            var i = 0;
            foreach (Channel channel in _Channels.Values)
            {
                channels[i++] = channel.Name;
            }

            return channels;
        }

        /// <summary>
        /// Retrieves a list of channels from the IRC server that match the specified mask.
        /// </summary>
        /// <param name="mask">The mask to match against channel names. This can include wildcards.</param>
        /// <returns>A list of <see cref="ChannelInfo"/> objects representing the channels that match the specified mask.</returns>
        /// <remarks>
        /// This method sends a LIST command to the server with the specified mask, and then waits for the server to send the list of matching channels.
        /// The method will block until the entire list has been received.
        /// </remarks>
        public IList<ChannelInfo> GetChannelList(string mask)
        {
            var list = new List<ChannelInfo>();
            lock (_ChannelListSyncRoot)
            {
                _ChannelList = list;
                _ChannelListReceivedEvent = new AutoResetEvent(false);

                // request list
                RfcList(mask);
                // wait till we have the complete list
                _ChannelListReceivedEvent.WaitOne();

                _ChannelListReceivedEvent = null;
                _ChannelList = null;
            }

            return list;
        }

        /// <summary>
        /// Retrieves a list of users from the IRC server that match the specified mask.
        /// </summary>
        /// <param name="mask">The mask to match against user nicknames. This can include wildcards.</param>
        /// <returns>A list of <see cref="WhoInfo"/> objects representing the users that match the specified mask.</returns>
        /// <remarks>
        /// This method sends a WHO command to the server with the specified mask, and then waits for the server to send the list of matching users.
        /// The method will block until the entire list has been received.
        /// </remarks>
        public IList<WhoInfo> GetWhoList(string mask)
        {
            var list = new List<WhoInfo>();
            lock (_WhoListSyncRoot)
            {
                _WhoList = list;
                _WhoListReceivedEvent = new AutoResetEvent(false);

                // request list
                RfcWho(mask);
                // wait till we have the complete list
                _WhoListReceivedEvent.WaitOne();

                _WhoListReceivedEvent = null;
                _WhoList = null;
            }

            return list;
        }

        /// <summary>
        /// Retrieves a list of ban information for the specified channel from the IRC server.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <returns>A list of <see cref="BanInfo"/> objects representing the ban information for the specified channel.</returns>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel and a "b" parameter, and then waits for the server to send the list of ban information.
        /// The method will block until the entire list has been received.
        /// </remarks>
        public IList<BanInfo> GetBanList(string channel)
        {
            var list = new List<BanInfo>();
            lock (_BanListSyncRoot)
            {
                _BanList = list;
                _BanListReceivedEvent = new AutoResetEvent(false);

                // request list
                Ban(channel);
                // wait till we have the complete list
                _BanListReceivedEvent.WaitOne();

                _BanListReceivedEvent = null;
                _BanList = null;
            }

            return list;
        }

        /// <summary>
        /// Retrieves a list of ban exception information for the specified channel from the IRC server.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <returns>A list of <see cref="BanInfo"/> objects representing the ban exception information for the specified channel.</returns>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel and a "e" parameter, and then waits for the server to send the list of ban exception information.
        /// The method will block until the entire list has been received.
        /// </remarks>
        public IList<BanInfo> GetBanExceptionList(string channel)
        {
            var list = new List<BanInfo>();
            if (!_ServerProperties.BanExceptionCharacter.HasValue)
            {
                return list;
            }
            lock (_BanExceptListSyncRoot)
            {
                _BanExceptList = list;
                _BanExceptListReceivedEvent = new AutoResetEvent(false);

                BanException(channel);
                _BanExceptListReceivedEvent.WaitOne();

                _BanExceptListReceivedEvent = null;
                _BanExceptList = null;
            }

            return list;
        }

        /// <summary>
        /// Retrieves a list of invite exception information for the specified channel from the IRC server.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <returns>A list of <see cref="BanInfo"/> objects representing the invite exception information for the specified channel.</returns>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel and a "I" parameter, and then waits for the server to send the list of invite exception information.
        /// The method will block until the entire list has been received.
        /// </remarks>
        public IList<BanInfo> GetInviteExceptionList(string channel)
        {
            var list = new List<BanInfo>();
            if (!_ServerProperties.InviteExceptionCharacter.HasValue)
                return list;

            lock (_InviteExceptListSyncRoot)
            {
                _InviteExceptList = list;
                _InviteExceptListReceivedEvent = new AutoResetEvent(false);

                InviteException(channel);
                _InviteExceptListReceivedEvent.WaitOne();

                _InviteExceptListReceivedEvent = null;
                _InviteExceptList = null;
            }

            return list;
        }

        /// <summary>
        /// Parses a raw IRC message string into an <see cref="IrcMessageData"/> object.
        /// </summary>
        /// <param name="rawline">The raw IRC message string to parse.</param>
        /// <returns>An <see cref="IrcMessageData"/> object that represents the parsed IRC message.</returns>
        /// <remarks>
        /// This method parses the raw IRC message string and extracts the prefix, command, parameters, and other parts of the message.
        /// It then creates an <see cref="IrcMessageData"/> object with these parts and returns it.
        /// </remarks>
        public IrcMessageData MessageParser(string rawline)
        {
            if (rawline == null)
                throw new ArgumentNullException(nameof(rawline));

            if (rawline.Length == 0)
                throw new ArgumentException("Value must not be empty.", nameof(rawline));

            string line;
            string[] linear;
            string messagecode;
            string from;
            string nick = null;
            string ident = null;
            string host = null;
            string channel = null;
            string message = null;
            var tags = new Dictionary<string, string>();
            ReceiveType type;
            ReplyCode replycode;
            int exclamationpos;
            int atpos;
            int colonpos;

            // IRCv3.2 message tags: http://ircv3.net/specs/core/message-tags-3.2.html
            if (rawline[0] == '@')
            {
                var spcidx = rawline.IndexOf(' ');
                var rawTags = rawline.Substring(1, spcidx - 1);
                // strip tags from further parsing for backwards compatibility
                line = rawline.Substring(spcidx + 1);

                var sTags = rawTags.Split(new char[] { ';' });
                foreach (var s in sTags)
                {
                    var eqidx = s.IndexOf("=");

                    if (eqidx != -1)
                    {
                        tags.Add(s.Substring(0, eqidx), _UnescapeTagValue(s.Substring(eqidx + 1)));
                    }
                    else
                    {
                        tags.Add(s, null);
                    }
                }
            }
            else
            {
                line = rawline;
            }

            if (line[0] == ':')
            {
                line = line.Substring(1);
            }
            linear = line.Split(new char[] { ' ' });

            // conform to RFC 2812
            from = linear[0];
            messagecode = linear[1];
            exclamationpos = from.IndexOf("!", StringComparison.Ordinal);
            atpos = from.IndexOf("@", StringComparison.Ordinal);
            colonpos = line.IndexOf(" :", StringComparison.Ordinal);
            if (colonpos != -1)
            {
                // we want the exact position of ":" not beginning from the space
                colonpos += 1;
            }
            if (exclamationpos != -1)
            {
                nick = from.Substring(0, exclamationpos);
            }
            else
            {
                if (atpos == -1)
                {
                    // no ident and no host, should be nick then
                    if (!from.Contains("."))
                    {
                        // HACK: from seems to be a nick instead of servername
                        nick = from;
                    }
                }
                else
                {
                    nick = from.Substring(0, atpos);
                }
            }
            if ((atpos != -1) &&
                (exclamationpos != -1))
            {
                ident = from.Substring(exclamationpos + 1, (atpos - exclamationpos) - 1);
            }
            if (atpos != -1)
            {
                host = from.Substring(atpos + 1);
            }

            replycode = int.TryParse(messagecode, out var tmp) ? (ReplyCode)tmp : ReplyCode.Null;

            type = _GetMessageType(line);
            if (colonpos != -1)
            {
                message = line.Substring(colonpos + 1);
            }

            switch (type)
            {
                case ReceiveType.Join:
                case ReceiveType.Kick:
                case ReceiveType.Part:
                case ReceiveType.TopicChange:
                case ReceiveType.ChannelModeChange:
                case ReceiveType.ChannelMessage:
                case ReceiveType.ChannelAction:
                case ReceiveType.ChannelNotice:
                    channel = linear[2];
                    break;
                case ReceiveType.Who:
                case ReceiveType.Topic:
                case ReceiveType.Invite:
                case ReceiveType.BanList:
                case ReceiveType.ChannelMode:
                    channel = linear[3];
                    break;
                case ReceiveType.Name:
                    channel = linear[4];
                    break;
            }

            switch (replycode)
            {
                case ReplyCode.List:
                case ReplyCode.ListEnd:
                case ReplyCode.ErrorNoChannelModes:
                case ReplyCode.InviteList:
                case ReplyCode.ExceptionList:
                    channel = linear[3];
                    break;
            }

            if ((channel != null) &&
                (channel[0] == ':'))
            {
                channel = channel.Substring(1);
            }

            IrcMessageData data;
            data = new IrcMessageData(this, from, nick, ident, host, channel, message, rawline, type, replycode, tags);

            return data;
        }

        // ISUPPORT-honoring versions of some IrcCommands methods

        /// <summary>
        /// Retrieves a list of ban exceptions for the specified channel from the IRC server.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel and a "e" parameter, and then waits for the server to send the list of ban exceptions.
        /// The method will block until the entire list has been received.
        /// </remarks>
        public override void BanException(string channel)
        {
            var bexchar = _ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ListChannelMasks("+" + bexchar.Value, channel);
            }
            else
            {
                base.BanException(channel);
            }
        }

        /// <summary>
        /// Adds a ban exception for the specified hostmask in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmask">The hostmask to add to the ban exception list.</param>
        /// <param name="priority">The priority with which the command should be sent to the server.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel, a "+e" parameter, and the specified hostmask.
        /// The command is sent with the specified priority.
        /// </remarks>
        public override void BanException(string channel, string hostmask, Priority priority)
        {
            var bexchar = _ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("+" + bexchar.Value, channel, hostmask, priority);
            }
            else
            {
                base.BanException(channel, hostmask, priority);
            }
        }

        /// <summary>
        /// Adds a ban exception for the specified hostmask in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmask">The hostmask to add to the ban exception list.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel, a "+e" parameter, and the specified hostmask.
        /// </remarks>
        public override void BanException(string channel, string hostmask)
        {
            var bexchar = _ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("+" + bexchar.Value, channel, hostmask);
            }
            else
            {
                base.BanException(channel, hostmask);
            }
        }

        /// <summary>
        /// Adds multiple ban exceptions for the specified hostmasks in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmasks">An array of hostmasks to add to the ban exception list.</param>
        /// <remarks>
        /// This method sends multiple MODE commands to the server with the specified channel, a "+e" parameter, and each of the specified hostmasks.
        /// </remarks>
        public override void BanException(string channel, string[] hostmasks)
        {
            var bexchar = _ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("+" + bexchar.Value, channel, hostmasks);
            }
            else
            {
                base.BanException(channel, hostmasks);
            }
        }

        /// <summary>
        /// Removes a ban exception for the specified hostmask in the specified channel with a given priority.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmask">The hostmask to remove from the ban exception list.</param>
        /// <param name="priority">The priority with which the command should be sent to the server.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel, a "-e" parameter, and the specified hostmask.
        /// The command is sent with the specified priority.
        /// </remarks>
        public override void UnBanException(string channel, string hostmask, Priority priority)
        {
            var bexchar = _ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("-" + bexchar.Value, channel, hostmask, priority);
            }
            else
            {
                base.UnBanException(channel, hostmask, priority);
            }
        }

        /// <summary>
        /// Removes a ban exception for the specified hostmask in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmask">The hostmask to remove from the ban exception list.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel, a "-e" parameter, and the specified hostmask.
        /// </remarks>
        public override void UnBanException(string channel, string hostmask)
        {
            var bexchar = _ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("-" + bexchar.Value, channel, hostmask);
            }
            else
            {
                base.UnBanException(channel, hostmask);
            }
        }

        /// <summary>
        /// Removes multiple ban exceptions for the specified hostmasks in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmasks">An array of hostmasks to remove from the ban exception list.</param>
        /// <remarks>
        /// This method sends multiple MODE commands to the server with the specified channel, a "-e" parameter, and each of the specified hostmasks.
        /// </remarks>
        public override void UnBanException(string channel, string[] hostmasks)
        {
            var bexchar = _ServerProperties.BanExceptionCharacter;
            if (bexchar.HasValue)
            {
                ModifyChannelMasks("-" + bexchar.Value, channel, hostmasks);
            }
            else
            {
                base.UnBanException(channel, hostmasks);
            }
        }

        /// <summary>
        /// Adds an invite exception for the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel and a "+I" parameter.
        /// If the server does not support invite exceptions, it falls back to the base implementation.
        /// </remarks>
        public override void InviteException(string channel)
        {
            var iexchar = _ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ListChannelMasks("+" + iexchar.Value, channel);
            }
            else
            {
                base.InviteException(channel);
            }
        }

        /// <summary>
        /// Adds an invite exception for the specified hostmask in the specified channel with a given priority.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmask">The hostmask to add to the invite exception list.</param>
        /// <param name="priority">The priority with which the command should be sent to the server.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel, a "+I" parameter, and the specified hostmask.
        /// The command is sent with the specified priority.
        /// If the server does not support invite exceptions, it falls back to the base implementation.
        /// </remarks>
        public override void InviteException(string channel, string hostmask, Priority priority)
        {
            var iexchar = _ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("+" + iexchar.Value, channel, hostmask, priority);
            }
            else
            {
                base.InviteException(channel, hostmask, priority);
            }
        }

        /// <summary>
        /// Adds an invite exception for the specified hostmask in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmask">The hostmask to add to the invite exception list.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel, a "+I" parameter, and the specified hostmask.
        /// If the server does not support invite exceptions, it falls back to the base implementation.
        /// </remarks>
        public override void InviteException(string channel, string hostmask)
        {
            var iexchar = _ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("+" + iexchar.Value, channel, hostmask);
            }
            else
            {
                base.InviteException(channel, hostmask);
            }
        }

        /// <summary>
        /// Adds multiple invite exceptions for the specified hostmasks in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmasks">An array of hostmasks to add to the invite exception list.</param>
        /// <remarks>
        /// This method sends multiple MODE commands to the server with the specified channel, a "+I" parameter, and each of the specified hostmasks.
        /// If the server does not support invite exceptions, it falls back to the base implementation.
        /// </remarks>
        public override void InviteException(string channel, string[] hostmasks)
        {
            var iexchar = _ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("+" + iexchar.Value, channel, hostmasks);
            }
            else
            {
                base.InviteException(channel, hostmasks);
            }
        }

        /// <summary>
        /// Removes an invite exception for the specified hostmask in the specified channel with a given priority.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmask">The hostmask to remove from the invite exception list.</param>
        /// <param name="priority">The priority with which the command should be sent to the server.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel, a "-I" parameter, and the specified hostmask.
        /// The command is sent with the specified priority.
        /// If the server does not support invite exceptions, it falls back to the base implementation.
        /// </remarks>
        public override void UnInviteException(string channel, string hostmask, Priority priority)
        {
            var iexchar = _ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("-" + iexchar.Value, channel, hostmask, priority);
            }
            else
            {
                base.UnInviteException(channel, hostmask, priority);
            }
        }

        /// <summary>
        /// Removes an invite exception for the specified hostmask in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmask">The hostmask to remove from the invite exception list.</param>
        /// <remarks>
        /// This method sends a MODE command to the server with the specified channel, a "-I" parameter, and the specified hostmask.
        /// If the server does not support invite exceptions, it falls back to the base implementation.
        /// </remarks>
        public override void UnInviteException(string channel, string hostmask)
        {
            var iexchar = _ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("-" + iexchar.Value, channel, hostmask);
            }
            else
            {
                base.UnInviteException(channel, hostmask);
            }
        }

        /// <summary>
        /// Removes multiple invite exceptions for the specified hostmasks in the specified channel.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="hostmasks">An array of hostmasks to remove from the invite exception list.</param>
        /// <remarks>
        /// This method sends multiple MODE commands to the server with the specified channel, a "-I" parameter, and each of the specified hostmasks.
        /// If the server does not support invite exceptions, it falls back to the base implementation.
        /// </remarks>
        public override void UnInviteException(string channel, string[] hostmasks)
        {
            var iexchar = _ServerProperties.InviteExceptionCharacter;
            if (iexchar.HasValue)
            {
                ModifyChannelMasks("-" + iexchar.Value, channel, hostmasks);
            }
            else
            {
                base.UnInviteException(channel, hostmasks);
            }
        }

        /// <summary>
        /// Creates an IrcUser object with the specified nickname.
        /// </summary>
        /// <param name="nickname">The nickname of the user.</param>
        /// <returns>An IrcUser object that represents the user with the specified nickname.</returns>
        protected virtual IrcUser CreateIrcUser(string nickname)
        {
            return new IrcUser(nickname, this);
        }

        /// <summary>
        /// Creates a Channel object with the specified name.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <returns>A Channel object that represents the channel with the specified name.</returns>
        protected virtual Channel CreateChannel(string name)
        {
            return SupportNonRfc ? new NonRfcChannel(name) : new Channel(name);
        }

        /// <summary>
        /// Creates a ChannelUser object with the specified channel and IrcUser.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="ircUser">The IrcUser object that represents the user.</param>
        /// <returns>A ChannelUser object that represents the user in the specified channel.</returns>
        protected virtual ChannelUser CreateChannelUser(string channel, IrcUser ircUser)
        {
            return SupportNonRfc ? new NonRfcChannelUser(channel, ircUser) : new ChannelUser(channel, ircUser);
        }

        /// <summary>
        /// Handles the event that occurs when a line of text is read from the server.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A ReadLineEventArgs object that contains the event data.</param>
        private void _Worker(object sender, ReadLineEventArgs e)
        {
            // lets see if we have events or internal messagehandler for it
            _HandleEvents(MessageParser(e.Line));
        }

        /// <summary>
        /// Handles the event that occurs when the client is disconnected from the server.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void _OnDisconnected(object sender, EventArgs e)
        {
            if (AutoRejoin)
            {
                _StoreChannelsToRejoin();
            }
            _SyncingCleanup();
        }

        /// <summary>
        /// Handles the event that occurs when a connection error happens.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void _OnConnectionError(object sender, EventArgs e)
        {
            try
            {
                // AutoReconnect is handled in IrcConnection._OnConnectionError
                if (AutoReconnect && AutoRelogin)
                {
                    Login(_NicknameList, Realname, IUsermode, Username, Password);
                }
                if (AutoReconnect && AutoRejoin)
                {
                    _RejoinChannels();
                }
            }
            catch (NotConnectedException)
            {
                // HACK: this is hacky, we don't know if the Reconnect was actually successful
                // means sending IRC commands without a connection throws NotConnectedExceptions 
            }
        }

        /// <summary>
        /// Stores the channels to rejoin after a disconnection.
        /// </summary>

        private void _StoreChannelsToRejoin()
        {

            lock (_AutoRejoinChannels)
            {
                _AutoRejoinChannels.Clear();
                if (ActiveChannelSyncing || PassiveChannelSyncing)
                {
                    // store the key using channel sync
                    foreach (Channel channel in _Channels.Values)
                    {
                        _AutoRejoinChannels.Add(channel.Name, channel.Key);
                    }
                }
                else
                {
                    foreach (var channel in _JoinedChannels)
                    {
                        _AutoRejoinChannels.Add(channel, null);
                    }
                }
            }
        }

        /// <summary>
        /// Rejoins the channels that were stored after a disconnection.
        /// </summary>
        private void _RejoinChannels()
        {

            lock (_AutoRejoinChannels)
            {
                RfcJoin(_AutoRejoinChannels.Keys.ToArray(),
                        _AutoRejoinChannels.Values.ToArray(),
                        Priority.High);
                _AutoRejoinChannels.Clear();
            }
        }

        /// <summary>
        /// Cleans up the state of the client after a disconnection.
        /// </summary>
        private void _SyncingCleanup()
        {
            // lets clean it baby, powered by Mr. Proper

            _JoinedChannels.Clear();
            if (ActiveChannelSyncing)
            {
                _Channels.Clear();
                _IrcUsers.Clear();
            }

            IsAway = false;

            _MotdReceived = false;
            _Motd.Clear();
        }

        /// <summary>
        /// Gets the next nickname from the nickname list.
        /// </summary>
        /// <returns>The next nickname.</returns>
        private string _NextNickname()
        {
            _CurrentNickname++;
            //if we reach the end stay there
            if (_CurrentNickname >= _NicknameList.Length)
            {
                _CurrentNickname--;
            }
            return NicknameList[_CurrentNickname];
        }

        /// <summary>
        /// Unescapes a tag value that was received from the server.
        /// </summary>
        /// <param name="tagValue">The tag value to unescape.</param>
        /// <returns>The unescaped tag value.</returns>
        private string _UnescapeTagValue(string tagValue)
        {
            int pos;
            var lastPos = 0;
            var unescaped = new StringBuilder(tagValue.Length);
            string sequence;

            while (lastPos < tagValue.Length && (pos = tagValue.IndexOf('\\', lastPos)) >= 0)
            {
                unescaped.Append(tagValue.Substring(lastPos, pos - lastPos));
                sequence = tagValue.Substring(pos, 2);

                if (sequence == @"\:")
                {
                    unescaped.Append(";");
                }
                else if (sequence == @"\s")
                {
                    unescaped.Append(" ");
                }
                else if (sequence == @"\\")
                {
                    unescaped.Append(@"\");
                }
                else if (sequence == @"\r")
                {
                    unescaped.Append("\r");
                }
                else if (sequence == @"\n")
                {
                    unescaped.Append("\n");
                }

                lastPos = pos + sequence.Length;
            }

            if (lastPos < tagValue.Length)
            {
                unescaped.Append(tagValue.Substring(lastPos));
            }

            return unescaped.ToString();
        }

        /// <summary>
        /// Determines the type of a message that was received from the server.
        /// </summary>
        /// <param name="rawline">The raw line of text that was received from the server.</param>
        /// <returns>The type of the message.</returns>
        private ReceiveType _GetMessageType(string rawline)
        {
            var found = _ReplyCodeRegex.Match(rawline);
            if (found.Success)
            {
                var code = found.Groups[1].Value;
                var replycode = (ReplyCode)int.Parse(code);

                // check if this replycode is known in the RFC
                if (Array.IndexOf(_ReplyCodes, replycode) == -1)
                {
                    return ReceiveType.Unknown;
                }

                switch (replycode)
                {
                    case ReplyCode.Welcome:
                    case ReplyCode.YourHost:
                    case ReplyCode.Created:
                    case ReplyCode.MyInfo:
                    case ReplyCode.Bounce:
                        return ReceiveType.Login;
                    case ReplyCode.LuserClient:
                    case ReplyCode.LuserOp:
                    case ReplyCode.LuserUnknown:
                    case ReplyCode.LuserMe:
                    case ReplyCode.LuserChannels:
                        return ReceiveType.Info;
                    case ReplyCode.MotdStart:
                    case ReplyCode.Motd:
                    case ReplyCode.EndOfMotd:
                        return ReceiveType.Motd;
                    case ReplyCode.NamesReply:
                    case ReplyCode.EndOfNames:
                        return ReceiveType.Name;
                    case ReplyCode.WhoReply:
                    case ReplyCode.EndOfWho:
                        return ReceiveType.Who;
                    case ReplyCode.ListStart:
                    case ReplyCode.List:
                    case ReplyCode.ListEnd:
                        return ReceiveType.List;
                    case ReplyCode.BanList:
                    case ReplyCode.EndOfBanList:
                        return ReceiveType.BanList;
                    case ReplyCode.Topic:
                    case ReplyCode.NoTopic:
                        return ReceiveType.Topic;
                    case ReplyCode.WhoIsUser:
                    case ReplyCode.WhoIsServer:
                    case ReplyCode.WhoIsOperator:
                    case ReplyCode.WhoIsIdle:
                    case ReplyCode.WhoIsChannels:
                    case ReplyCode.EndOfWhoIs:
                        return ReceiveType.WhoIs;
                    case ReplyCode.WhoWasUser:
                    case ReplyCode.EndOfWhoWas:
                        return ReceiveType.WhoWas;
                    case ReplyCode.UserModeIs:
                        return ReceiveType.UserMode;
                    case ReplyCode.ChannelModeIs:
                        return ReceiveType.ChannelMode;
                    default:
                        return ((int)replycode >= 400) &&
                            ((int)replycode <= 599)
                            ? ReceiveType.ErrorMessage
                            : ReceiveType.Unknown;
                }
            }

            found = _PingRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Unknown;
            }

            found = _ErrorRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Error;
            }

            found = _ActionRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelAction;
                    default:
                        return ReceiveType.QueryAction;
                }
            }

            found = _CtcpRequestRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.CtcpRequest;
            }

            found = _MessageRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelMessage;
                    default:
                        return ReceiveType.QueryMessage;
                }
            }

            found = _CtcpReplyRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.CtcpReply;
            }

            found = _NoticeRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelNotice;
                    default:
                        return ReceiveType.QueryNotice;
                }
            }

            found = _InviteRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Invite;
            }

            found = _JoinRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Join;
            }

            found = _TopicRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.TopicChange;
            }

            found = _NickRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.NickChange;
            }

            found = _KickRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Kick;
            }

            found = _PartRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Part;
            }

            found = _ModeRegex.Match(rawline);
            if (found.Success)
            {
                if (IsMe(found.Groups[1].Value))
                {
                    return ReceiveType.UserModeChange;
                }
                else
                {
                    return ReceiveType.ChannelModeChange;
                }
            }

            found = _QuitRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Quit;
            }


            return ReceiveType.Unknown;
        }

        /// <summary>
        /// Handles the events that occur when a message is received from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _HandleEvents(IrcMessageData ircdata)
        {
            OnRawMessage?.Invoke(this, new IrcEventArgs(ircdata));

            string code;
            // special IRC messages
            code = ircdata.RawMessageArray[0];
            switch (code)
            {
                case "PING":
                    _Event_PING(ircdata);
                    break;
                case "ERROR":
                    _Event_ERROR(ircdata);
                    break;
            }

            code = ircdata.RawMessageArray[1];
            switch (code)
            {
                case "PRIVMSG":
                    _Event_PRIVMSG(ircdata);
                    break;
                case "NOTICE":
                    _Event_NOTICE(ircdata);
                    break;
                case "JOIN":
                    _Event_JOIN(ircdata);
                    break;
                case "PART":
                    _Event_PART(ircdata);
                    break;
                case "KICK":
                    _Event_KICK(ircdata);
                    break;
                case "QUIT":
                    _Event_QUIT(ircdata);
                    break;
                case "TOPIC":
                    _Event_TOPIC(ircdata);
                    break;
                case "NICK":
                    _Event_NICK(ircdata);
                    break;
                case "INVITE":
                    _Event_INVITE(ircdata);
                    break;
                case "MODE":
                    _Event_MODE(ircdata);
                    break;
                case "PONG":
                    _Event_PONG(ircdata);
                    break;
            }

            if (ircdata.ReplyCode != ReplyCode.Null)
            {
                switch (ircdata.ReplyCode)
                {
                    case ReplyCode.Welcome:
                        _Event_RPL_WELCOME(ircdata);
                        break;
                    case ReplyCode.Topic:
                        _Event_RPL_TOPIC(ircdata);
                        break;
                    case ReplyCode.NoTopic:
                        _Event_RPL_NOTOPIC(ircdata);
                        break;
                    case ReplyCode.NamesReply:
                        _Event_RPL_NAMREPLY(ircdata);
                        break;
                    case ReplyCode.EndOfNames:
                        _Event_RPL_ENDOFNAMES(ircdata);
                        break;
                    case ReplyCode.List:
                        _Event_RPL_LIST(ircdata);
                        break;
                    case ReplyCode.ListEnd:
                        _Event_RPL_LISTEND(ircdata);
                        break;
                    case ReplyCode.WhoReply:
                        _Event_RPL_WHOREPLY(ircdata);
                        break;
                    case ReplyCode.EndOfWho:
                        _Event_RPL_ENDOFWHO(ircdata);
                        break;
                    case ReplyCode.ChannelModeIs:
                        _Event_RPL_CHANNELMODEIS(ircdata);
                        break;
                    case ReplyCode.BanList:
                        _Event_RPL_BANLIST(ircdata);
                        break;
                    case ReplyCode.EndOfBanList:
                        _Event_RPL_ENDOFBANLIST(ircdata);
                        break;
                    case ReplyCode.ErrorNoChannelModes:
                        _Event_ERR_NOCHANMODES(ircdata);
                        break;
                    case ReplyCode.Motd:
                        _Event_RPL_MOTD(ircdata);
                        break;
                    case ReplyCode.EndOfMotd:
                        _Event_RPL_ENDOFMOTD(ircdata);
                        break;
                    case ReplyCode.Away:
                        _Event_RPL_AWAY(ircdata);
                        break;
                    case ReplyCode.UnAway:
                        _Event_RPL_UNAWAY(ircdata);
                        break;
                    case ReplyCode.NowAway:
                        _Event_RPL_NOWAWAY(ircdata);
                        break;
                    case ReplyCode.TryAgain:
                        _Event_RPL_TRYAGAIN(ircdata);
                        break;
                    case ReplyCode.ErrorNicknameInUse:
                        _Event_ERR_NICKNAMEINUSE(ircdata);
                        break;
                    case ReplyCode.InviteList:
                        _Event_RPL_INVITELIST(ircdata);
                        break;
                    case ReplyCode.EndOfInviteList:
                        _Event_RPL_ENDOFINVITELIST(ircdata);
                        break;
                    case ReplyCode.ExceptionList:
                        _Event_RPL_EXCEPTLIST(ircdata);
                        break;
                    case ReplyCode.EndOfExceptionList:
                        _Event_RPL_ENDOFEXCEPTLIST(ircdata);
                        break;
                    case ReplyCode.Bounce:
                        _Event_RPL_BOUNCE(ircdata);
                        break;
                }
            }

            if (ircdata.Type == ReceiveType.ErrorMessage)
            {
                _Event_ERR(ircdata);
            }
        }

        /// <summary>
        /// Removes an IRC user with the specified nickname.
        /// </summary>
        /// <param name="nickname">The nickname of the user to remove.</param>
        /// <returns>true if the user was removed; otherwise, false.</returns>
        private bool _RemoveIrcUser(string nickname)
        {
            var user = GetIrcUser(nickname);
            if (user != null)
            {
                if (user.JoinedChannels.Length == 0)
                {
                    // he is nowhere else, lets kill him
                    _IrcUsers.Remove(nickname);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes a user from a channel.
        /// </summary>
        /// <param name="channelname">The name of the channel.</param>
        /// <param name="nickname">The nickname of the user to remove.</param>
        private void _RemoveChannelUser(string channelname, string nickname)
        {
            var chan = GetChannel(channelname);
            chan.UnsafeUsers.Remove(nickname);
            chan.UnsafeOps.Remove(nickname);
            chan.UnsafeVoices.Remove(nickname);
            if (SupportNonRfc)
            {
                var nchan = (NonRfcChannel)chan;
                nchan.UnsafeOwners.Remove(nickname);
                nchan.UnsafeChannelAdmins.Remove(nickname);
                nchan.UnsafeHalfops.Remove(nickname);
            }
        }

        /// <summary>
        /// Interprets the mode of a channel.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        /// <param name="changeInfos">A list of information about the changes to the channel mode.</param>
        private void _InterpretChannelMode(IrcMessageData ircdata, List<ChannelModeChangeInfo> changeInfos)
        {
            Channel channel = null;
            if (ActiveChannelSyncing)
            {
                channel = GetChannel(ircdata.Channel);
            }
            foreach (var changeInfo in changeInfos)
            {
                var temp = changeInfo.Parameter;
                var add = changeInfo.Action == ChannelModeChangeAction.Set;
                var remove = changeInfo.Action == ChannelModeChangeAction.Unset;
                switch (changeInfo.Mode)
                {
                    case ChannelMode.Op:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the op list
                                    try
                                    {
                                        channel.UnsafeOps.Add(temp, GetIrcUser(temp));

                                    }
                                    catch (ArgumentException)
                                    {

                                    }

                                    // update the user op status
                                    var cuser = GetChannelUser(ircdata.Channel, temp);
                                    cuser.IsOp = true;

                                }
                                else
                                {

                                }
                            }

                            OnOp?.Invoke(this, new OpEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the op list
                                    channel.UnsafeOps.Remove(temp);

                                    // update the user op status
                                    GetChannelUser(ircdata.Channel, temp).IsOp = false;

                                }
                                else
                                {

                                }
                            }

                            OnDeop?.Invoke(this, new DeopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.Owner:
                        if (SupportNonRfc)
                        {
                            if (add)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the owner list
                                        try
                                        {
                                            ((NonRfcChannel)channel).UnsafeOwners.Add(temp, GetIrcUser(temp));

                                        }
                                        catch (ArgumentException)
                                        {

                                        }

                                        // update the user owner status
                                        ((NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp)).IsOwner = true;

                                    }
                                    else
                                    {

                                    }
                                }

                                OnOwner?.Invoke(this, new OwnerEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                            if (remove)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the owner list
                                        ((NonRfcChannel)channel).UnsafeOwners.Remove(temp);

                                        // update the user owner status
                                        ((NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp)).IsOwner = false;

                                    }
                                    else
                                    {

                                    }
                                }

                                OnDeowner?.Invoke(this, new DeownerEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case ChannelMode.Admin:
                        if (SupportNonRfc)
                        {
                            if (add)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the channel admin list
                                        try
                                        {
                                            ((NonRfcChannel)channel).UnsafeChannelAdmins.Add(temp, GetIrcUser(temp));

                                        }
                                        catch (ArgumentException)
                                        {

                                        }

                                        // update the user channel admin status
                                        ((NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp)).IsChannelAdmin = true;

                                    }
                                    else
                                    {

                                    }
                                }

                                OnChannelAdmin?.Invoke(this, new ChannelAdminEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                            if (remove)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the channel admin list
                                        ((NonRfcChannel)channel).UnsafeChannelAdmins.Remove(temp);

                                        // update the user channel admin status
                                        ((NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp)).IsChannelAdmin = false;

                                    }
                                    else
                                    {

                                    }
                                }

                                OnDeChannelAdmin?.Invoke(this, new DeChannelAdminEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case ChannelMode.HalfOp:
                        if (SupportNonRfc)
                        {
                            if (add)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the halfop list
                                        try
                                        {
                                            ((NonRfcChannel)channel).UnsafeHalfops.Add(temp, GetIrcUser(temp));

                                        }
                                        catch (ArgumentException)
                                        {

                                        }

                                        // update the user halfop status
                                        ((NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp)).IsHalfop = true;

                                    }
                                    else
                                    {

                                    }
                                }

                                OnHalfop?.Invoke(this, new HalfopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                            if (remove)
                            {
                                if (ActiveChannelSyncing && channel != null)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the halfop list
                                        ((NonRfcChannel)channel).UnsafeHalfops.Remove(temp);

                                        // update the user halfop status
                                        ((NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp)).IsHalfop = false;

                                    }
                                    else
                                    {

                                    }
                                }

                                OnDehalfop?.Invoke(this, new DehalfopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case ChannelMode.Voice:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the voice list
                                    try
                                    {
                                        channel.UnsafeVoices.Add(temp, GetIrcUser(temp));

                                    }
                                    catch (ArgumentException)
                                    {

                                    }

                                    // update the user voice status
                                    GetChannelUser(ircdata.Channel, temp).IsVoice = true;

                                }
                                else
                                {

                                }
                            }

                            OnVoice?.Invoke(this, new VoiceEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the voice list
                                    channel.UnsafeVoices.Remove(temp);

                                    // update the user voice status
                                    GetChannelUser(ircdata.Channel, temp).IsVoice = false;

                                }
                                else
                                {

                                }
                            }

                            OnDevoice?.Invoke(this, new DevoiceEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.Ban:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                try
                                {
                                    channel.Bans.Add(temp);

                                }
                                catch (ArgumentException)
                                {

                                }
                            }
                            OnBan?.Invoke(this, new BanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Bans.Remove(temp);

                            }
                            OnUnban?.Invoke(this, new UnbanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.BanException:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                try
                                {
                                    channel.BanExceptions.Add(temp);

                                }
                                catch (ArgumentException)
                                {

                                }
                            }
                            OnBanException?.Invoke(this, new BanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.BanExceptions.Remove(temp);

                            }
                            OnUnBanException?.Invoke(this, new UnbanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.InviteException:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                try
                                {
                                    channel.InviteExceptions.Add(temp);

                                }
                                catch (ArgumentException)
                                {

                                }
                            }
                            OnInviteException?.Invoke(this, new BanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.InviteExceptions.Remove(temp);

                            }
                            OnUnInviteException?.Invoke(this, new UnbanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                        }
                        break;
                    case ChannelMode.UserLimit:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                try
                                {
                                    channel.UserLimit = int.Parse(temp);

                                }
                                catch (FormatException)
                                {

                                }
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.UserLimit = 0;

                            }
                        }
                        break;
                    case ChannelMode.Key:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Key = temp;

                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Key = "";

                            }
                        }
                        break;
                    default:
                        if (add)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                if (channel.Mode.IndexOf(changeInfo.ModeChar) == -1)
                                {
                                    channel.Mode += changeInfo.ModeChar;

                                }
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing && channel != null)
                            {
                                channel.Mode = channel.Mode.Replace(changeInfo.ModeChar.ToString(), String.Empty);

                            }
                        }
                        break;
                }
            }
        }

        #region Internal Messagehandlers

        /// <summary>
        /// Handles the PING event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_PING(IrcMessageData ircdata)
        {
            var server = ircdata.RawMessageArray[1].Substring(1);

            RfcPong(server, Priority.Critical);

            OnPing?.Invoke(this, new PingEventArgs(ircdata, server));
        }

        /// <summary>
        /// Handles the PONG event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_PONG(IrcMessageData ircdata)
        {
            OnPong?.Invoke(this, new PongEventArgs(ircdata, ircdata.Irc.Lag));
        }

        /// <summary>
        /// Handles the ERROR event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_ERROR(IrcMessageData ircdata)
        {
            var message = ircdata.Message;


            OnError?.Invoke(this, new ErrorEventArgs(ircdata, message));
        }

        /// <summary>
        /// Handles the JOIN event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_JOIN(IrcMessageData ircdata)
        {
            var who = ircdata.Nick;
            var channelname = ircdata.Channel;

            if (IsMe(who))
            {
                _JoinedChannels.Add(channelname);
            }

            if (ActiveChannelSyncing)
            {
                Channel channel;
                if (IsMe(who))
                {
                    // we joined the channel
                    // we joined the channel
                    // HACK: only create and add the channel to _Channels if it
                    // doesn't exist yet. This check should not be needed but
                    // the IRCd could send a duplicate JOIN message and break
                    // our client state
                    channel = GetChannel(channelname);
                    if (channel == null)
                    {

                        channel = CreateChannel(channelname);
                    }
                    else
                    {

                    }
                    _Channels[channelname] = channel;

                    // request channel mode
                    RfcMode(channelname);
                    // request wholist
                    RfcWho(channelname);
                    // request ban exception list
                    if (_ServerProperties.BanExceptionCharacter.HasValue)
                    {
                        BanException(channelname);
                    }
                    // request invite exception list
                    if (_ServerProperties.InviteExceptionCharacter.HasValue)
                    {
                        InviteException(channelname);
                    }
                    // request banlist
                    Ban(channelname);
                }
                else
                {
                    // someone else joined the channel
                    // request the who data
                    RfcWho(who);
                }


                channel = GetChannel(channelname);
                var ircuser = GetIrcUser(who);

                if (ircuser == null)
                {
                    ircuser = new IrcUser(who, this)
                    {
                        Ident = ircdata.Ident,
                        Host = ircdata.Host
                    };
                    _IrcUsers.Add(who, ircuser);
                }

                // HACK: IRCnet's anonymous channel mode feature breaks our
                // channnel sync here as they use the same nick for ALL channel
                // users!
                // Example: :anonymous!anonymous@anonymous. JOIN :$channel
                if (who == "anonymous" &&
                    ircdata.Ident == "anonymous" &&
                    ircdata.Host == "anonymous." &&
                    IsJoined(channelname, who))
                {
                    // ignore
                }
                else
                {
                    var channeluser = CreateChannelUser(channelname, ircuser);
                    channel.UnsafeUsers[who] = channeluser;
                }
            }

            OnJoin?.Invoke(this, new JoinEventArgs(ircdata, channelname, who));
        }

        /// <summary>
        /// Handles the PART event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_PART(IrcMessageData ircdata)
        {
            var who = ircdata.Nick;
            var channel = ircdata.Channel;
            var partmessage = ircdata.Message;

            if (IsMe(who))
            {
                _JoinedChannels.Remove(channel);
            }

            if (ActiveChannelSyncing)
            {
                if (IsMe(who))
                {

                    _Channels.Remove(channel);
                }
                else
                {

                    // HACK: IRCnet's anonymous channel mode feature breaks our
                    // channnel sync here as they use the same nick for ALL channel
                    // users!
                    // Example: :anonymous!anonymous@anonymous. PART $channel :$msg
                    if (who == "anonymous" &&
                        ircdata.Ident == "anonymous" &&
                        ircdata.Host == "anonymous." &&
                        !IsJoined(channel, who))
                    {
                        // ignore
                    }
                    else
                    {
                        _RemoveChannelUser(channel, who);
                        _RemoveIrcUser(who);
                    }
                }
            }

            OnPart?.Invoke(this, new PartEventArgs(ircdata, channel, who, partmessage));
        }

        /// <summary>
        /// Handles the KICK event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_KICK(IrcMessageData ircdata)
        {
            var channelname = ircdata.Channel;
            var who = ircdata.Nick;
            if (String.IsNullOrEmpty(who))
            {
                // the server itself kicked
                who = ircdata.From;
            }
            var whom = ircdata.RawMessageArray[3];
            var reason = ircdata.Message;
            var isme = IsMe(whom);

            if (isme)
            {
                _JoinedChannels.Remove(channelname);
            }

            if (ActiveChannelSyncing)
            {
                if (isme)
                {
                    _Channels.Remove(channelname);
                    if (AutoRejoinOnKick)
                    {
                        RfcJoin(GetChannel(channelname).Name, GetChannel(channelname).Key);
                    }
                }
                else
                {
                    _RemoveChannelUser(channelname, whom);
                    _RemoveIrcUser(whom);
                }
            }
            else
            {
                if (isme && AutoRejoinOnKick)
                {
                    RfcJoin(channelname);
                }
            }

            OnKick?.Invoke(this, new KickEventArgs(ircdata, channelname, who, whom, reason));
        }

        /// <summary>
        /// Handles the QUIT event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_QUIT(IrcMessageData ircdata)
        {
            var who = ircdata.Nick;
            var reason = ircdata.Message;

            // no need to handle if we quit, disconnect event will take care

            if (ActiveChannelSyncing)
            {
                // sanity checks, freshirc is very broken about RFC
                var user = GetIrcUser(who);
                if (user != null)
                {
                    var joined_channels = user.JoinedChannels;
                    if (joined_channels != null)
                    {
                        foreach (var channel in joined_channels)
                            _RemoveChannelUser(channel, who);
                        _RemoveIrcUser(who);
                    }
                }
            }

            OnQuit?.Invoke(this, new QuitEventArgs(ircdata, who, reason));
        }

        /// <summary>
        /// Handles the PRIVMSG event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_PRIVMSG(IrcMessageData ircdata)
        {

            switch (ircdata.Type)
            {
                case ReceiveType.ChannelMessage:
                    OnChannelMessage?.Invoke(this, new IrcEventArgs(ircdata));
                    break;
                case ReceiveType.ChannelAction:
                    OnChannelAction?.Invoke(this, new ActionEventArgs(ircdata, ircdata.Message.Substring(8, ircdata.Message.Length - 9)));
                    break;
                case ReceiveType.QueryMessage:
                    OnQueryMessage?.Invoke(this, new IrcEventArgs(ircdata));
                    break;
                case ReceiveType.QueryAction:
                    OnQueryAction?.Invoke(this, new ActionEventArgs(ircdata, ircdata.Message.Substring(8, ircdata.Message.Length - 9)));
                    break;
                case ReceiveType.CtcpRequest:
                    if (OnCtcpRequest != null)
                    {
                        string cmd;
                        var space_pos = ircdata.Message.IndexOf(' ');
                        var param = "";
                        if (space_pos != -1)
                        {
                            cmd = ircdata.Message.Substring(1, space_pos - 1);
                            param = ircdata.Message.Substring(space_pos + 1,
                                        ircdata.Message.Length - space_pos - 2);
                        }
                        else
                        {
                            cmd = ircdata.Message.Substring(1, ircdata.Message.Length - 2);
                        }
                        OnCtcpRequest(this, new CtcpEventArgs(ircdata, cmd, param));
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles the NOTICE event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_NOTICE(IrcMessageData ircdata)
        {
            switch (ircdata.Type)
            {
                case ReceiveType.ChannelNotice:
                    OnChannelNotice?.Invoke(this, new IrcEventArgs(ircdata));
                    break;
                case ReceiveType.QueryNotice:
                    OnQueryNotice?.Invoke(this, new IrcEventArgs(ircdata));
                    break;
                case ReceiveType.CtcpReply:
                    if (OnCtcpReply != null)
                    {
                        string cmd;
                        var space_pos = ircdata.Message.IndexOf(' ');
                        var param = "";
                        if (space_pos != -1)
                        {
                            cmd = ircdata.Message.Substring(1, space_pos - 1);
                            param = ircdata.Message.Substring(space_pos + 1,
                                        ircdata.Message.Length - space_pos - 2);
                        }
                        else
                        {
                            cmd = ircdata.Message.Substring(1, ircdata.Message.Length - 2);
                        }
                        OnCtcpReply(this, new CtcpEventArgs(ircdata, cmd, param));
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles the TOPIC event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_TOPIC(IrcMessageData ircdata)
        {
            var who = ircdata.Nick;
            var channel = ircdata.Channel;
            var newtopic = ircdata.Message;

            if (ActiveChannelSyncing && IsJoined(channel))
            {
                GetChannel(channel).Topic = newtopic;
            }

            OnTopicChange?.Invoke(this, new TopicChangeEventArgs(ircdata, channel, who, newtopic));
        }

        /// <summary>
        /// Handles the NICK event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_NICK(IrcMessageData ircdata)
        {
            var oldnickname = ircdata.Nick;
            //string newnickname = ircdata.Message;
            // the colon in the NICK message is optional, thus we can't rely on Message
            var newnickname = ircdata.RawMessageArray[2];

            // so let's strip the colon if it's there
            if (newnickname.StartsWith(":"))
            {
                newnickname = newnickname.Substring(1);
            }

            if (IsMe(ircdata.Nick))
            {
                // nickname change is your own
                Nickname = newnickname;
            }

            if (ActiveChannelSyncing)
            {
                var ircuser = GetIrcUser(oldnickname);

                // if we don't have any info about him, don't update him!
                // (only queries or ourself in no channels)
                if (ircuser != null)
                {
                    var joinedchannels = ircuser.JoinedChannels;

                    // update his nickname
                    ircuser.Nick = newnickname;
                    // remove the old entry 
                    // remove first to avoid duplication, Foo -> foo
                    _IrcUsers.Remove(oldnickname);
                    // add him as new entry and new nickname as key
                    _IrcUsers.Add(newnickname, ircuser);

                    // now the same for all channels he is joined
                    Channel channel;
                    ChannelUser channeluser;
                    foreach (var channelname in joinedchannels)
                    {
                        channel = GetChannel(channelname);
                        channeluser = GetChannelUser(channelname, oldnickname);
                        // remove first to avoid duplication, Foo -> foo
                        channel.UnsafeUsers.Remove(oldnickname);
                        channel.UnsafeUsers.Add(newnickname, channeluser);
                        if (SupportNonRfc && ((NonRfcChannelUser)channeluser).IsOwner)
                        {
                            var nchannel = (NonRfcChannel)channel;
                            nchannel.UnsafeOwners.Remove(oldnickname);
                            nchannel.UnsafeOwners.Add(newnickname, channeluser);
                        }
                        if (SupportNonRfc && ((NonRfcChannelUser)channeluser).IsChannelAdmin)
                        {
                            var nchannel = (NonRfcChannel)channel;
                            nchannel.UnsafeChannelAdmins.Remove(oldnickname);
                            nchannel.UnsafeChannelAdmins.Add(newnickname, channeluser);
                        }
                        if (channeluser.IsOp)
                        {
                            channel.UnsafeOps.Remove(oldnickname);
                            channel.UnsafeOps.Add(newnickname, channeluser);
                        }
                        if (SupportNonRfc && ((NonRfcChannelUser)channeluser).IsHalfop)
                        {
                            var nchannel = (NonRfcChannel)channel;
                            nchannel.UnsafeHalfops.Remove(oldnickname);
                            nchannel.UnsafeHalfops.Add(newnickname, channeluser);
                        }
                        if (channeluser.IsVoice)
                        {
                            channel.UnsafeVoices.Remove(oldnickname);
                            channel.UnsafeVoices.Add(newnickname, channeluser);
                        }
                    }
                }
            }

            OnNickChange?.Invoke(this, new NickChangeEventArgs(ircdata, oldnickname, newnickname));
        }

        /// <summary>
        /// Handles the INVITE event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_INVITE(IrcMessageData ircdata)
        {
            var channel = ircdata.Channel;
            var inviter = ircdata.Nick;

            if (AutoJoinOnInvite && channel.Trim() != "0")
                RfcJoin(channel);

            OnInvite?.Invoke(this, new InviteEventArgs(ircdata, channel, inviter));
        }

        /// <summary>
        /// Handles the MODE event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_MODE(IrcMessageData ircdata)
        {
            if (IsMe(ircdata.RawMessageArray[2]))
            {
                // my user mode changed
                Usermode = ircdata.RawMessageArray[3].Substring(1);

                OnUserModeChange?.Invoke(this, new IrcEventArgs(ircdata));
            }
            else
            {
                // channel mode changed
                var mode = ircdata.RawMessageArray[3];
                var parameter = String.Join(" ", ircdata.RawMessageArray, 4, ircdata.RawMessageArray.Length - 4);
                var changeInfos = ChannelModeChangeInfo.Parse(ChannelModeMap, ircdata.Channel, mode, parameter);
                _InterpretChannelMode(ircdata, changeInfos);

                OnChannelModeChange?.Invoke(this, new ChannelModeChangeEventArgs(ircdata, ircdata.Channel, changeInfos));
            }


            OnModeChange?.Invoke(this, new IrcEventArgs(ircdata));
        }

        /// <summary>
        /// Handles the CHANNELMODEIS event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_CHANNELMODEIS(IrcMessageData ircdata)
        {
            if (ActiveChannelSyncing &&
                IsJoined(ircdata.Channel))
            {
                // reset stored mode first, as this is the complete mode
                var chan = GetChannel(ircdata.Channel);
                chan.Mode = String.Empty;
                var mode = ircdata.RawMessageArray[4];
                var parameter = String.Join(" ", ircdata.RawMessageArray, 5, ircdata.RawMessageArray.Length - 5);
                var changeInfos = ChannelModeChangeInfo.Parse(ChannelModeMap, ircdata.Channel, mode, parameter);
                _InterpretChannelMode(ircdata, changeInfos);
            }
        }

        /// <summary>
        /// Handles the WELCOME event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_WELCOME(IrcMessageData ircdata)
        {
            // updating our nickname, that we got (maybe cutted...)
            Nickname = ircdata.RawMessageArray[2];

            OnRegistered?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles the TOPIC event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_TOPIC(IrcMessageData ircdata)
        {
            var topic = ircdata.Message;
            var channel = ircdata.Channel;

            if (ActiveChannelSyncing && IsJoined(channel))
                GetChannel(channel).Topic = topic;

            OnTopic?.Invoke(this, new TopicEventArgs(ircdata, channel, topic));
        }

        /// <summary>
        /// Handles the NOTOPIC event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_NOTOPIC(IrcMessageData ircdata)
        {
            var channel = ircdata.Channel;

            if (ActiveChannelSyncing && IsJoined(channel))
                GetChannel(channel).Topic = "";

            OnTopic?.Invoke(this, new TopicEventArgs(ircdata, channel, ""));
        }

        /// <summary>
        /// Handles the NAMREPLY event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_NAMREPLY(IrcMessageData ircdata)
        {
            var channelname = ircdata.Channel;
            var userlist = ircdata.MessageArray ?? (ircdata.RawMessageArray.Length > 5 ? (new string[] { ircdata.RawMessageArray[5] }) : (new string[] { }));
            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                string nickname;
                bool owner;
                bool chanadmin;
                bool op;
                bool halfop;
                bool voice;
                foreach (var user in userlist)
                {
                    if (user.Length <= 0)
                        continue;

                    owner = false;
                    chanadmin = false;
                    op = false;
                    halfop = false;
                    voice = false;

                    nickname = user;

                    foreach (var kvp in _ServerProperties.ChannelPrivilegeModesPrefixes)
                    {
                        if (nickname[0] == kvp.Value)
                        {
                            nickname = nickname.Substring(1);

                            switch (kvp.Key)
                            {
                                case 'q':
                                    owner = true;
                                    break;
                                case 'a':
                                    chanadmin = true;
                                    break;
                                case 'o':
                                    op = true;
                                    break;
                                case 'h':
                                    halfop = true;
                                    break;
                                case 'v':
                                    voice = true;
                                    break;
                            }
                        }
                    }

                    var ircuser = GetIrcUser(nickname);
                    var channeluser = GetChannelUser(channelname, nickname);

                    if (ircuser == null)
                    {

                        ircuser = new IrcUser(nickname, this);
                        _IrcUsers.Add(nickname, ircuser);
                    }

                    if (channeluser == null)
                    {


                        channeluser = CreateChannelUser(channelname, ircuser);
                        var channel = GetChannel(channelname);

                        channel.UnsafeUsers.Add(nickname, channeluser);
                        if (SupportNonRfc && owner)
                            ((NonRfcChannel)channel).UnsafeOwners.Add(nickname, channeluser);

                        if (SupportNonRfc && chanadmin)
                            ((NonRfcChannel)channel).UnsafeChannelAdmins.Add(nickname, channeluser);

                        if (op)
                            channel.UnsafeOps.Add(nickname, channeluser);

                        if (SupportNonRfc && halfop)
                            ((NonRfcChannel)channel).UnsafeHalfops.Add(nickname, channeluser);

                        if (voice)
                            channel.UnsafeVoices.Add(nickname, channeluser);

                    }

                    channeluser.IsOp = op;
                    channeluser.IsVoice = voice;
                    if (SupportNonRfc)
                    {
                        var nchanneluser = (NonRfcChannelUser)channeluser;
                        nchanneluser.IsOwner = owner;
                        nchanneluser.IsChannelAdmin = chanadmin;
                        nchanneluser.IsHalfop = halfop;
                    }
                }
            }

            var filteredUserlist = new List<string>(userlist.Length);
            // filter user modes from nicknames
            foreach (var user in userlist)
            {
                if (String.IsNullOrEmpty(user))
                {
                    continue;
                }

                var temp = user;
                foreach (var kvp in _ServerProperties.ChannelPrivilegeModesPrefixes)
                {
                    if (temp[0] == kvp.Value)
                    {
                        temp = temp.Substring(1);
                    }
                }
                filteredUserlist.Add(temp);

            }

            OnNames?.Invoke(this, new NamesEventArgs(ircdata, channelname, filteredUserlist.ToArray(), userlist));
        }

        /// <summary>
        /// Handles the LIST event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_LIST(IrcMessageData ircdata)
        {
            var channelName = ircdata.Channel;
            var userCount = Int32.Parse(ircdata.RawMessageArray[4]);
            var topic = ircdata.Message;

            ChannelInfo info = null;
            if (OnList != null || _ChannelList != null)
                info = new ChannelInfo(channelName, userCount, topic);

            _ChannelList?.Add(info);

            OnList?.Invoke(this, new ListEventArgs(ircdata, info));
        }

#pragma warning disable IDE0060 // Part of a pattern
        /// <summary>
        /// Handles the LISTEND event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_LISTEND(IrcMessageData ircdata)
        {
            _ChannelListReceivedEvent?.Set();
        }
#pragma warning restore IDE0060 // Remove unused parameter

#pragma warning disable IDE0060 // Part of a pattern
        /// <summary>
        /// Handles the TRYAGAIN event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_TRYAGAIN(IrcMessageData ircdata)
        {
            _ChannelListReceivedEvent?.Set();
        }
#pragma warning restore IDE0060 // Remove unused parameter

        /*
        // BUG: RFC2812 says LIST and WHO might return ERR_TOOMANYMATCHES which
        // is not defined :(
        private void _Event_ERR_TOOMANYMATCHES(IrcMessageData ircdata)
        {
            if (_ListInfosReceivedEvent != null) {
                _ListInfosReceivedEvent.Set();
            }
        }
        */

        /// <summary>
        /// Handles the ENDOFNAMES event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_ENDOFNAMES(IrcMessageData ircdata)
        {
            var channelname = ircdata.RawMessageArray[3];
            if (ActiveChannelSyncing && IsJoined(channelname))
                OnChannelPassiveSynced?.Invoke(this, new IrcEventArgs(ircdata));
        }

        /// <summary>
        /// Handles the RPL_AWAY event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_AWAY(IrcMessageData ircdata)
        {
            var who = ircdata.RawMessageArray[3];
            var awaymessage = ircdata.Message;

            if (ActiveChannelSyncing)
            {
                var ircuser = GetIrcUser(who);
                if (ircuser != null)
                    ircuser.IsAway = true;
            }

            OnAway?.Invoke(this, new AwayEventArgs(ircdata, who, awaymessage));
        }

        /// <summary>
        /// Handles the RPL_UNAWAY event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_UNAWAY(IrcMessageData ircdata)
        {
            IsAway = false;

            OnUnAway?.Invoke(this, new IrcEventArgs(ircdata));
        }

        /// <summary>
        /// Handles the RPL_NOWAWAY event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_NOWAWAY(IrcMessageData ircdata)
        {
            IsAway = true;

            OnNowAway?.Invoke(this, new IrcEventArgs(ircdata));
        }

        /// <summary>
        /// Handles the RPL_WHOREPLY event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_WHOREPLY(IrcMessageData ircdata)
        {
            var info = WhoInfo.Parse(ircdata);
            var channel = info.Channel;
            var nick = info.Nick;

            _WhoList?.Add(info);

            if (ActiveChannelSyncing &&
                IsJoined(channel))
            {
                // checking the irc and channel user I only do for sanity!
                // according to RFC they must be known to us already via RPL_NAMREPLY
                // psyBNC is not very correct with this... maybe other bouncers too
                var ircuser = GetIrcUser(nick);
                var channeluser = GetChannelUser(channel, nick);

                if (ircuser != null)
                {
                    ircuser.Ident = info.Ident;
                    ircuser.Host = info.Host;
                    ircuser.Server = info.Server;
                    ircuser.Nick = info.Nick;
                    ircuser.HopCount = info.HopCount;
                    ircuser.Realname = info.Realname;
                    ircuser.IsAway = info.IsAway;
                    ircuser.IsIrcOp = info.IsIrcOp;
                    ircuser.IsRegistered = info.IsRegistered;

                    switch (channel[0])
                    {
                        case '#':
                        case '!':
                        case '&':
                        case '+':
                            // this channel may not be where we are joined!
                            // see RFC 1459 and RFC 2812, it must return a channelname
                            // we use this channel info when possible...
                            if (channeluser != null)
                            {
                                channeluser.IsOp = info.IsOp;
                                channeluser.IsVoice = info.IsVoice;
                            }
                            break;
                    }
                }
            }

            OnWho?.Invoke(this, new WhoEventArgs(ircdata, info));
        }

#pragma warning disable IDE0060 // Part of a pattern
        /// <summary>
        /// Handles the RPL_ENDOFWHO event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_ENDOFWHO(IrcMessageData ircdata)
        {
            _WhoListReceivedEvent?.Set();
        }
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Handles the RPL_MOTD event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_MOTD(IrcMessageData ircdata)
        {
            if (!_MotdReceived)
            {
                _Motd.Add(ircdata.Message);
            }

            OnMotd?.Invoke(this, new MotdEventArgs(ircdata, ircdata.Message));
        }

#pragma warning disable IDE0060 // Part of a pattern
        /// <summary>
        /// Handles the RPL_ENDOFMOTD event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_ENDOFMOTD(IrcMessageData ircdata)
        {
            _MotdReceived = true;
        }
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Handles the RPL_BANLIST event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_BANLIST(IrcMessageData ircdata)
        {
            var channelname = ircdata.Channel;

            var info = BanInfo.Parse(ircdata);
            _BanList?.Add(info);

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                var channel = GetChannel(channelname);
                if (channel.IsSynced)
                    return;

                channel.Bans.Add(info.Mask);
            }
        }

        /// <summary>
        /// Handles the RPL_ENDOFBANLIST event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_ENDOFBANLIST(IrcMessageData ircdata)
        {
            var channelname = ircdata.Channel;

            _BanListReceivedEvent?.Set();

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                var channel = GetChannel(channelname);
                if (channel.IsSynced)
                    return;

                channel.ActiveSyncStop = DateTime.Now;
                channel.IsSynced = true;

                OnChannelActiveSynced?.Invoke(this, new IrcEventArgs(ircdata));
            }
        }

        /// <summary>
        /// Handles the RPL_EXCEPTLIST event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_EXCEPTLIST(IrcMessageData ircdata)
        {
            var channelname = ircdata.Channel;

            var info = BanInfo.Parse(ircdata);
            _BanExceptList?.Add(info);

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                var channel = GetChannel(channelname);
                if (channel.IsSynced)
                    return;

                channel.BanExceptions.Add(info.Mask);
            }
        }

#pragma warning disable IDE0060 // Part of a pattern
        /// <summary>
        /// Handles the RPL_ENDOFEXCEPTLIST event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_ENDOFEXCEPTLIST(IrcMessageData ircdata)
        {
            // string channelname = ircdata.Channel;
            _BanExceptListReceivedEvent?.Set();
        }
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Handles the RPL_INVITELIST event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_INVITELIST(IrcMessageData ircdata)
        {
            var channelname = ircdata.Channel;

            var info = BanInfo.Parse(ircdata);
            _InviteExceptList?.Add(info);

            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                var channel = GetChannel(channelname);
                if (channel.IsSynced)
                    return;

                channel.InviteExceptions.Add(info.Mask);
            }
        }

#pragma warning disable IDE0060 // Part of a pattern
        /// <summary>
        /// Handles the RPL_ENDOFINVITELIST event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_ENDOFINVITELIST(IrcMessageData ircdata)
        {
            // string channelname = ircdata.Channel;

            _InviteExceptListReceivedEvent?.Set();
        }
#pragma warning restore IDE0060 // Remove unused parameter

        // MODE +b might return ERR_NOCHANMODES for mode-less channels (like +chan) 
        /// <summary>
        /// Handles the ERR_NOCHANMODES event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_ERR_NOCHANMODES(IrcMessageData ircdata)
        {
            var channelname = ircdata.RawMessageArray[3];
            if (ActiveChannelSyncing && IsJoined(channelname))
            {
                var channel = GetChannel(channelname);
                if (channel.IsSynced)
                    return;

                channel.ActiveSyncStop = DateTime.Now;
                channel.IsSynced = true;

                OnChannelActiveSynced?.Invoke(this, new IrcEventArgs(ircdata));
            }
        }

        /// <summary>
        /// Handles the ERR event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_ERR(IrcMessageData ircdata)
        {
            OnErrorMessage?.Invoke(this, new IrcEventArgs(ircdata));
        }

        /// <summary>
        /// Handles the ERR_NICKNAMEINUSE event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
#pragma warning disable IDE0060 // Part of a pattern
        private void _Event_ERR_NICKNAMEINUSE(IrcMessageData ircdata)
        {
            if (!AutoNickHandling)
                return;

            string nickname;
            // if a nicklist has been given loop through the nicknames
            // if the upper limit of this list has been reached and still no nickname has registered
            // then generate a random nick
            if (_CurrentNickname == NicknameList.Length - 1)
            {
                var rand = new Random();
                var number = rand.Next(999);
                nickname = Nickname.Length > 5 ? Nickname.Substring(0, 5) + number : Nickname.Substring(0, Nickname.Length - 1) + number;
            }
            else
                nickname = _NextNickname();

            // change the nickname
            RfcNick(nickname, Priority.Critical);
        }
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Handles the RPL_BOUNCE event from the server.
        /// </summary>
        /// <param name="ircdata">The data of the IRC message.</param>
        private void _Event_RPL_BOUNCE(IrcMessageData ircdata)
        {
            // HACK: might be BOUNCE or ISUPPORT; try to detect
            if (ircdata.Message != null && ircdata.Message.StartsWith("Try server "))
            {
                // BOUNCE
                string host = null;
                var port = -1;
                // try to parse out host and port
                var match = _BounceMessageRegex.Match(ircdata.Message);
                if (match.Success)
                {
                    host = match.Groups[1].Value;
                    port = int.Parse(match.Groups[2].Value);
                }

                OnBounce?.Invoke(this, new BounceEventArgs(ircdata, host, port));
                return;
            }

            // ISUPPORT
            _ServerProperties.ParseFromRawMessage(ircdata.RawMessageArray);
            if (ircdata.RawMessageArray.Any(x => x.StartsWith("CHANMODES=")))
            {
                var chanModes = _ServerProperties.RawProperties["CHANMODES"];
                if (!String.IsNullOrEmpty(chanModes))
                    ChannelModeMap = new ChannelModeMap(chanModes);
            }
            if (_ServerProperties.RawProperties.ContainsKey("NAMESX"))
                WriteLine("PROTOCTL NAMESX", Priority.Critical);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is IrcClient client &&
                   EqualityComparer<object>.Default.Equals(_BanListSyncRoot, client._BanListSyncRoot);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return -1424568596 + EqualityComparer<object>.Default.GetHashCode(_BanListSyncRoot);
            }
        }
        #endregion
    }
}
