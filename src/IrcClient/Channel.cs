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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents an IRC channel.
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// Gets the name of the channel.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the key (password) of the channel.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets a clone of the hashtable containing the users of the channel.
        /// </summary>
        public Hashtable Users { get; } = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Gets a clone of the hashtable containing the operators of the channel.
        /// </summary>
        public Hashtable Ops { get; } = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Gets a clone of the hashtable containing the voices of the channel.
        /// </summary>
        public Hashtable Voices { get; } = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the list of bans in the channel.
        /// </summary>
        public StringCollection Bans { get; } = new StringCollection();

        /// <summary>
        /// Gets the list of ban exceptions in the channel.
        /// </summary>
        public List<string> BanExceptions { get; } = new List<string>();

        /// <summary>
        /// Gets the list of invite exceptions in the channel.
        /// </summary>
        public List<string> InviteExceptions { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the topic of the channel.
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user limit of the channel.
        /// </summary>
        public int UserLimit { get; set; }

        /// <summary>
        /// Gets or sets the mode of the channel.
        /// </summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// Gets the start time of the active sync.
        /// </summary>
        public DateTime ActiveSyncStart { get; }

        /// <summary>
        /// Gets or sets the stop time of the active sync.
        /// </summary>
        public DateTime ActiveSyncStop { get; set; }

        /// <summary>
        /// Gets the duration of the active sync.
        /// </summary>
        public TimeSpan ActiveSyncTime { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the channel is synced.
        /// </summary>
        public bool IsSynced { get; set; }

        /// <summary>
        /// Gets the hashtable containing the users of the channel.
        /// </summary>
        internal Hashtable UnsafeUsers { get; } = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the hashtable containing the operators of the channel.
        /// </summary>
        internal Hashtable UnsafeOps { get; } = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the hashtable containing the voices of the channel.
        /// </summary>
        internal Hashtable UnsafeVoices { get; } = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Initializes a new instance of the Channel class.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        internal Channel(string name)
        {
            Name = name;
            ActiveSyncStart = DateTime.Now;
        }


    }
}
