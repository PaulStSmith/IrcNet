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
using System.Runtime.Serialization;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents errors that occur during the execution of the SmartIrc4net library.
    /// </summary>
    [Serializable()]
    public class SmartIrc4netException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the SmartIrc4netException class.
        /// </summary>
        public SmartIrc4netException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SmartIrc4netException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SmartIrc4netException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SmartIrc4netException class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="e">The exception that is the cause of the current exception.</param>
        public SmartIrc4netException(string message, Exception e) : base(message, e)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SmartIrc4netException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SmartIrc4netException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
