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

using System.Collections.Generic;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents the data of an IRC message.
    /// </summary>
    public class IrcMessageData
    {
        /// <summary>
        /// Gets the IRC client that received the message.
        /// </summary>
        public IrcClient Irc { get; }

        /// <summary>
        /// Gets the full nickname of the user who sent the message.
        /// </summary>
        public string From { get; }

        /// <summary>
        /// Gets the nickname of the user who sent the message.
        /// </summary>
        public string Nick { get; }

        /// <summary>
        /// Gets the ident of the user who sent the message.
        /// </summary>
        public string Ident { get; }

        /// <summary>
        /// Gets the host of the user who sent the message.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the channel where the message was sent.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Gets the content of the message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the content of the message split into an array of words.
        /// </summary>
        public string[] MessageArray { get; }

        /// <summary>
        /// Gets the raw content of the message.
        /// </summary>
        public string RawMessage { get; }

        /// <summary>
        /// Gets the raw content of the message split into an array of words.
        /// </summary>
        public string[] RawMessageArray { get; }

        /// <summary>
        /// Gets the tags of the message.
        /// </summary>
        public Dictionary<string, string> Tags { get; }

        /// <summary>
        /// Gets the type of the message.
        /// </summary>
        public ReceiveType Type { get; }

        /// <summary>
        /// Gets the reply code of the message.
        /// </summary>
        public ReplyCode ReplyCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IrcMessageData"/> class.
        /// </summary>
        /// <param name="ircclient">The IRC client that received the message.</param>
        /// <param name="from">The full nickname of the user who sent the message.</param>
        /// <param name="nick">The nickname of the user who sent the message.</param>
        /// <param name="ident">The ident of the user who sent the message.</param>
        /// <param name="host">The host of the user who sent the message.</param>
        /// <param name="channel">The channel where the message was sent.</param>
        /// <param name="message">The content of the message.</param>
        /// <param name="rawmessage">The raw content of the message.</param>
        /// <param name="type">The type of the message.</param>
        /// <param name="replycode">The reply code of the message.</param>
        /// <param name="tags">The tags of the message.</param>
        public IrcMessageData(IrcClient ircclient, string from, string nick, string ident, string host, string channel, string message, string rawmessage, ReceiveType type, ReplyCode replycode, Dictionary<string, string> tags)
        {
            Irc = ircclient;
            RawMessage = rawmessage;
            RawMessageArray = rawmessage.Split(new char[] { ' ' });
            Type = type;
            ReplyCode = replycode;
            From = from;
            Nick = nick;
            Ident = ident;
            Host = host;
            Channel = channel;
            if (message != null)
            {
                // message is optional
                Message = message;
                MessageArray = message.Split(new char[] { ' ' });
            }
            Tags = tags;
        }
    }
}
