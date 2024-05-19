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
    /// Represents the method that will handle an event when a line is read from the IRC server.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="ReadLineEventArgs"/> that contains the event data.</param>
    public delegate void ReadLineEventHandler(object sender, ReadLineEventArgs e);

    /// <summary>
    /// Represents the method that will handle an event when a line is written to the IRC server.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="WriteLineEventArgs"/> that contains the event data.</param>
    public delegate void WriteLineEventHandler(object sender, WriteLineEventArgs e);

    /// <summary>
    /// Represents the method that will handle an event when an error occurs during auto connect.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="AutoConnectErrorEventArgs"/> that contains the event data.</param>
    public delegate void AutoConnectErrorEventHandler(object sender, AutoConnectErrorEventArgs e);
}
