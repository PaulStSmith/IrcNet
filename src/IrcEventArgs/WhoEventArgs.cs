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

using System;

namespace Meebey.SmartIrc4net
{
    public class WhoEventArgs : IrcEventArgs
    {
        /// <summary>
        /// Gets the channel name.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Channel => WhoInfo.Channel;

        /// <summary>
        /// Gets the nickname of the user.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Nick => WhoInfo.Nick;

        /// <summary>
        /// Gets the identity of the user.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Ident => WhoInfo.Ident;

        /// <summary>
        /// Gets the host of the user.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Host => WhoInfo.Host;

        /// <summary>
        /// Gets the real name of the user.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Realname => WhoInfo.Realname;

        /// <summary>
        /// Gets a value indicating whether the user is away.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public bool IsAway => WhoInfo.IsAway;

        /// <summary>
        /// Gets a value indicating whether the user is an operator.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public bool IsOp => WhoInfo.IsOp;

        /// <summary>
        /// Gets a value indicating whether the user has voice privileges.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public bool IsVoice => WhoInfo.IsVoice;

        /// <summary>
        /// Gets a value indicating whether the user is an IRC operator.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public bool IsIrcOp => WhoInfo.IsIrcOp;

        /// <summary>
        /// Gets the server of the user.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Server => WhoInfo.Server;

        /// <summary>
        /// Gets the hop count of the user.
        /// </summary>
        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public int HopCount => WhoInfo.HopCount;

        /// <summary>
        /// Gets the WhoInfo object containing detailed information about the user.
        /// </summary>
        public WhoInfo WhoInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WhoEventArgs"/> class.
        /// </summary>
        /// <param name="data">The IRC message data.</param>
        /// <param name="whoInfo">The WhoInfo object containing detailed information about the user.</param>
        internal WhoEventArgs(IrcMessageData data, WhoInfo whoInfo) : base(data)
        {
            WhoInfo = whoInfo;
        }
    }
}
