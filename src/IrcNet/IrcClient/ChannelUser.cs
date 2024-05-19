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

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents a user in a channel.
    /// </summary>
    public class ChannelUser
    {
        /// <summary>
        /// Gets the channel name.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Gets the IRC user.
        /// </summary>
        public IrcUser IrcUser { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is an operator.
        /// </summary>
        public bool IsOp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user has voice.
        /// </summary>
        public bool IsVoice { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelUser"/> class.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        /// <param name="ircuser">The name of the IRC user.</param>
        internal ChannelUser(string channel, IrcUser ircuser)
        {
            Channel = channel;
            IrcUser = ircuser;
        }



        /// <summary>
        /// Gets a value indicating whether the user is an IRC operator.
        /// </summary>
        public bool IsIrcOp => IrcUser.IsIrcOp;

        /// <summary>
        /// Gets a value indicating whether the user is away.
        /// </summary>
        public bool IsAway => IrcUser.IsAway;

        /// <summary>
        /// Gets the user's nickname.
        /// </summary>
        public string Nick => IrcUser.Nick;

        /// <summary>
        /// Gets the user's ident.
        /// </summary>
        public string Ident => IrcUser.Ident;

        /// <summary>
        /// Gets the user's host.
        /// </summary>
        public string Host => IrcUser.Host;

        /// <summary>
        /// Gets the user's real name.
        /// </summary>
        public string Realname => IrcUser.Realname;

        /// <summary>
        /// Gets the server the user is connected to.
        /// </summary>
        public string Server => IrcUser.Server;

        /// <summary>
        /// Gets the user's hop count.
        /// </summary>
        public int HopCount => IrcUser.HopCount;

        /// <summary>
        /// Gets the channels the user has joined.
        /// </summary>
        public string[] JoinedChannels => IrcUser.JoinedChannels;
    }
}
