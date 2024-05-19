/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2005 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
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

using System.Collections.Specialized;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents an IRC user.
    /// </summary>
    public class IrcUser
    {
        private readonly IrcClient _IrcClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcUser"/> class.
        /// </summary>
        /// <param name="nickname">The nickname of the user.</param>
        /// <param name="ircclient">The IRC client that the user is connected to.</param>
        internal IrcUser(string nickname, IrcClient ircclient)
        {
            _IrcClient = ircclient;
            Nick = nickname;
        }

        /// <summary>
        /// Gets or sets the nickname of the user.
        /// </summary>
        public string Nick { get; set; } = null;

        /// <summary>
        /// Gets or sets the ident of the user.
        /// </summary>
        public string Ident { get; set; } = null;

        /// <summary>
        /// Gets or sets the host of the user.
        /// </summary>
        public string Host { get; set; } = null;

        /// <summary>
        /// Gets or sets the real name of the user.
        /// </summary>
        public string Realname { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the user is an IRC operator.
        /// </summary>
        public bool IsIrcOp { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the user is registered.
        /// </summary>
        public bool IsRegistered { get; internal set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the user is away.
        /// </summary>
        public bool IsAway { get; set; } = false;

        /// <summary>
        /// Gets or sets the server of the user.
        /// </summary>
        public string Server { get; set; } = null;

        /// <summary>
        /// Gets or sets the hop count of the user.
        /// </summary>
        public int HopCount { get; set; } = -1;

        /// <summary>
        /// Gets the channels that the user has joined.
        /// </summary>
        public string[] JoinedChannels
        {
            get
            {
                Channel channel;
                string[] result;
                var channels = _IrcClient.GetChannels();
                var joinedchannels = new StringCollection();
                foreach (var channelname in channels)
                {
                    channel = _IrcClient.GetChannel(channelname);
                    if (channel.UnsafeUsers.ContainsKey(Nick))
                    {
                        joinedchannels.Add(channelname);
                    }
                }

                result = new string[joinedchannels.Count];
                joinedchannels.CopyTo(result, 0);
                return result;
                //return joinedchannels;
            }
        }
    }
}
