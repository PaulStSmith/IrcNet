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

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents the base class for DCC event arguments.
    /// </summary>
    public class DccEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the DCC connection associated with the event.
        /// </summary>
        public DccConnection Dcc { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DccEventArgs"/> class.
        /// </summary>
        /// <param name="dcc">The DCC connection associated with the event.</param>
        internal DccEventArgs(DccConnection dcc)
        {
            Dcc = dcc;
        }
    }
}
