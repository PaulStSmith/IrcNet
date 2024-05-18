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
    /// Represents the arguments for an event that occurs when a user quits IRC.
    /// </summary>
    public class QuitEventArgs : IrcEventArgs
    {
        /// <summary>
        /// Gets the user who quit.
        /// </summary>
        public string Who { get; }

        /// <summary>
        /// Gets the quit message.
        /// </summary>
        public string QuitMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuitEventArgs"/> class.
        /// </summary>
        /// <param name="data">The IRC message data.</param>
        /// <param name="who">The user who quit.</param>
        /// <param name="quitmessage">The quit message.</param>
        internal QuitEventArgs(IrcMessageData data, string who, string quitmessage) : base(data)
        {
            Who = who;
            QuitMessage = quitmessage;
        }
    }
}
