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
    /// Represents the method that will handle an IRC event.
    /// </summary>
    public delegate void IrcEventHandler(object sender, IrcEventArgs e);
    /// <summary>
    /// Represents the method that will handle a CTCP event.
    /// </summary>
    public delegate void CtcpEventHandler(object sender, CtcpEventArgs e);
    /// <summary>
    /// Represents the method that will handle an action event.
    /// </summary>
    public delegate void ActionEventHandler(object sender, ActionEventArgs e);
    /// <summary>
    /// Represents the method that will handle an error event.
    /// </summary>
    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
    /// <summary>
    /// Represents the method that will handle a ping event.
    /// </summary>
    public delegate void PingEventHandler(object sender, PingEventArgs e);
    /// <summary>
    /// Represents the method that will handle a kick event.
    /// </summary>
    public delegate void KickEventHandler(object sender, KickEventArgs e);
    /// <summary>
    /// Represents the method that will handle a join event.
    /// </summary>
    public delegate void JoinEventHandler(object sender, JoinEventArgs e);
    /// <summary>
    /// Represents the method that will handle a names event.
    /// </summary>
    public delegate void NamesEventHandler(object sender, NamesEventArgs e);
    /// <summary>
    /// Represents the method that will handle a list event.
    /// </summary>
    public delegate void ListEventHandler(object sender, ListEventArgs e);
    /// <summary>
    /// Represents the method that will handle a part event.
    /// </summary>
    public delegate void PartEventHandler(object sender, PartEventArgs e);
    /// <summary>
    /// Represents the method that will handle an invite event.
    /// </summary>
    public delegate void InviteEventHandler(object sender, InviteEventArgs e);
    /// <summary>
    /// Represents the method that will handle an owner event.
    /// </summary>
    public delegate void OwnerEventHandler(object sender, OwnerEventArgs e);
    /// <summary>
    /// Represents the method that will handle a deowner event.
    /// </summary>
    public delegate void DeownerEventHandler(object sender, DeownerEventArgs e);
    /// <summary>
    /// Represents the method that will handle a channel admin event.
    /// </summary>
    public delegate void ChannelAdminEventHandler(object sender, ChannelAdminEventArgs e);
    /// <summary>
    /// Represents the method that will handle a dechannel admin event.
    /// </summary>
    public delegate void DeChannelAdminEventHandler(object sender, DeChannelAdminEventArgs e);
    /// <summary>
    /// Represents the method that will handle an op event.
    /// </summary>
    public delegate void OpEventHandler(object sender, OpEventArgs e);
    /// <summary>
    /// Represents the method that will handle a deop event.
    /// </summary>
    public delegate void DeopEventHandler(object sender, DeopEventArgs e);
    /// <summary>
    /// Represents the method that will handle a halfop event.
    /// </summary>
    public delegate void HalfopEventHandler(object sender, HalfopEventArgs e);
    /// <summary>
    /// Represents the method that will handle a dehalfop event.
    /// </summary>
    public delegate void DehalfopEventHandler(object sender, DehalfopEventArgs e);
    /// <summary>
    /// Represents the method that will handle a voice event.
    /// </summary>
    public delegate void VoiceEventHandler(object sender, VoiceEventArgs e);
    /// <summary>
    /// Represents the method that will handle a devoice event.
    /// </summary>
    public delegate void DevoiceEventHandler(object sender, DevoiceEventArgs e);
    /// <summary>
    /// Represents the method that will handle a ban event.
    /// </summary>
    public delegate void BanEventHandler(object sender, BanEventArgs e);
    /// <summary>
    /// Represents the method that will handle an unban event.
    /// </summary>
    public delegate void UnbanEventHandler(object sender, UnbanEventArgs e);
    /// <summary>
    /// Represents the method that will handle a topic event.
    /// </summary>
    public delegate void TopicEventHandler(object sender, TopicEventArgs e);
    /// <summary>
    /// Represents the method that will handle a topic change event.
    /// </summary>
    public delegate void TopicChangeEventHandler(object sender, TopicChangeEventArgs e);
    /// <summary>
    /// Represents the method that will handle a nick change event.
    /// </summary>
    public delegate void NickChangeEventHandler(object sender, NickChangeEventArgs e);
    /// <summary>
    /// Represents the method that will handle a quit event.
    /// </summary>
    public delegate void QuitEventHandler(object sender, QuitEventArgs e);
    /// <summary>
    /// Represents the method that will handle an away event.
    /// </summary>
    public delegate void AwayEventHandler(object sender, AwayEventArgs e);
    /// <summary>
    /// Represents the method that will handle a who event.
    /// </summary>
    public delegate void WhoEventHandler(object sender, WhoEventArgs e);
    /// <summary>
    /// Represents the method that will handle a MOTD event.
    /// </summary>
    public delegate void MotdEventHandler(object sender, MotdEventArgs e);
    /// <summary>
    /// Represents the method that will handle a pong event.
    /// </summary>
    public delegate void PongEventHandler(object sender, PongEventArgs e);
    /// <summary>
    /// Represents the method that will handle a bounce event.
    /// </summary>
    public delegate void BounceEventHandler(object sender, BounceEventArgs e);
}
