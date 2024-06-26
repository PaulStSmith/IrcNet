﻿//  SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
//
//  Copyright (c) 2016 Mirco Bauer <meebey@meebey.net>
//
//  Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Meebey.SmartIrc4net
{
    [TestFixture]
    public class IrcClientTests
    {
        [Test]
        public void MessageParser()
        {
            var client = new IrcClient();

            var rawline = ":irc.example.com 001 meebey3 :Welcome to the EFnet Internet Relay Chat Network meebey3";
            var msg = client.MessageParser(rawline);
            ClassicAssert.AreSame(client, msg.Irc);
            ClassicAssert.AreEqual(rawline, msg.RawMessage);
            ClassicAssert.AreEqual("irc.example.com", msg.From);
            ClassicAssert.AreEqual(null, msg.Nick);
            ClassicAssert.AreEqual(null, msg.Ident);
            ClassicAssert.AreEqual(null, msg.Host);
            ClassicAssert.AreEqual(ReplyCode.Welcome, msg.ReplyCode);
            ClassicAssert.AreEqual(ReceiveType.Login, msg.Type);
            ClassicAssert.AreEqual("Welcome to the EFnet Internet Relay Chat Network meebey3", msg.Message);
            ClassicAssert.AreEqual(null, msg.Channel);
            ClassicAssert.IsNotNull(msg.Tags);
            ClassicAssert.AreEqual(0, msg.Tags.Count);

            rawline = ":irc.example.com 002 meebey3 :Your host is irc.example.com[127.0.0.1/6667], running version hybrid-7.2.2+oftc1.6.9";
            msg = client.MessageParser(rawline);
            ClassicAssert.AreSame(client, msg.Irc);
            ClassicAssert.AreEqual(rawline, msg.RawMessage);
            ClassicAssert.AreEqual("irc.example.com", msg.From);
            ClassicAssert.AreEqual(null, msg.Nick);
            ClassicAssert.AreEqual(null, msg.Ident);
            ClassicAssert.AreEqual(null, msg.Host);
            ClassicAssert.AreEqual(ReplyCode.YourHost, msg.ReplyCode);
            ClassicAssert.AreEqual(ReceiveType.Login, msg.Type);
            ClassicAssert.AreEqual("Your host is irc.example.com[127.0.0.1/6667], running version hybrid-7.2.2+oftc1.6.9", msg.Message);
            ClassicAssert.AreEqual(null, msg.Channel);
            ClassicAssert.IsNotNull(msg.Tags);
            ClassicAssert.AreEqual(0, msg.Tags.Count);

            rawline = ":irc.example.com 003 meebey3 :This server was created Aug  7 2011 at 12:43:41";
            msg = client.MessageParser(rawline);
            ClassicAssert.AreSame(client, msg.Irc);
            ClassicAssert.AreEqual(rawline, msg.RawMessage);
            ClassicAssert.AreEqual("irc.example.com", msg.From);
            ClassicAssert.AreEqual(null, msg.Nick);
            ClassicAssert.AreEqual(null, msg.Ident);
            ClassicAssert.AreEqual(null, msg.Host);
            ClassicAssert.AreEqual(ReplyCode.Created, msg.ReplyCode);
            ClassicAssert.AreEqual(ReceiveType.Login, msg.Type);
            ClassicAssert.AreEqual("This server was created Aug  7 2011 at 12:43:41", msg.Message);
            ClassicAssert.AreEqual(null, msg.Channel);
            ClassicAssert.IsNotNull(msg.Tags);
            ClassicAssert.AreEqual(0, msg.Tags.Count);

            rawline = ":irc.example.com 004 meebey3 irc.example.com hybrid-7.2.2+oftc1.6.9 CDGPRSabcdfgiklnorsuwxyz biklmnopstveI bkloveI";
            msg = client.MessageParser(rawline);
            ClassicAssert.AreSame(client, msg.Irc);
            ClassicAssert.AreEqual(rawline, msg.RawMessage);
            ClassicAssert.AreEqual("irc.example.com", msg.From);
            ClassicAssert.AreEqual(null, msg.Nick);
            ClassicAssert.AreEqual(null, msg.Ident);
            ClassicAssert.AreEqual(null, msg.Host);
            ClassicAssert.AreEqual(ReplyCode.MyInfo, msg.ReplyCode);
            ClassicAssert.AreEqual(ReceiveType.Login, msg.Type);
            ClassicAssert.AreEqual(null, msg.Message);
            ClassicAssert.AreEqual(null, msg.Channel);
            ClassicAssert.IsNotNull(msg.Tags);
            ClassicAssert.AreEqual(0, msg.Tags.Count);

            rawline = ":irc.example.com 005 meebey3 CALLERID CASEMAPPING=rfc1459 DEAF=D KICKLEN=160 MODES=4 NICKLEN=30 PREFIX=(ov)@+ STATUSMSG=@+ TOPICLEN=390 NETWORK=EFnet MAXLIST=beI:25 MAXTARGETS=4 CHANTYPES=#& :are supported by this server";
            msg = client.MessageParser(rawline);
            ClassicAssert.AreSame(client, msg.Irc);
            ClassicAssert.AreEqual(rawline, msg.RawMessage);
            ClassicAssert.AreEqual("irc.example.com", msg.From);
            ClassicAssert.AreEqual(null, msg.Nick);
            ClassicAssert.AreEqual(null, msg.Ident);
            ClassicAssert.AreEqual(null, msg.Host);
            ClassicAssert.AreEqual(ReplyCode.Bounce, msg.ReplyCode);
            ClassicAssert.AreEqual(ReceiveType.Login, msg.Type);
            ClassicAssert.AreEqual("are supported by this server", msg.Message);
            ClassicAssert.AreEqual(null, msg.Channel);
            ClassicAssert.IsNotNull(msg.Tags);
            ClassicAssert.AreEqual(0, msg.Tags.Count);

            rawline = ":irc.example.com 005 meebey3 CHANLIMIT=#&:25 CHANNELLEN=50 CHANMODES=eIb,k,l,imnpstMRS KNOCK ELIST=CMNTU SAFELIST AWAYLEN=160 EXCEPTS=e INVEX=I :are supported by this server";
            msg = client.MessageParser(rawline);
            ClassicAssert.AreSame(client, msg.Irc);
            ClassicAssert.AreEqual(rawline, msg.RawMessage);
            ClassicAssert.AreEqual("irc.example.com", msg.From);
            ClassicAssert.AreEqual(null, msg.Nick);
            ClassicAssert.AreEqual(null, msg.Ident);
            ClassicAssert.AreEqual(null, msg.Host);
            ClassicAssert.AreEqual(ReplyCode.Bounce, msg.ReplyCode);
            ClassicAssert.AreEqual(ReceiveType.Login, msg.Type);
            ClassicAssert.AreEqual("are supported by this server", msg.Message);
            ClassicAssert.AreEqual(null, msg.Channel);
            ClassicAssert.IsNotNull(msg.Tags);
            ClassicAssert.AreEqual(0, msg.Tags.Count);

            rawline = ":i_ron!~zbuddy@37.187.47.25 JOIN :#debian.de";
            msg = client.MessageParser(rawline);
            ClassicAssert.AreSame(client, msg.Irc);
            ClassicAssert.AreEqual(rawline, msg.RawMessage);
            ClassicAssert.AreEqual("i_ron!~zbuddy@37.187.47.25", msg.From);
            ClassicAssert.AreEqual("i_ron", msg.Nick);
            ClassicAssert.AreEqual("~zbuddy", msg.Ident);
            ClassicAssert.AreEqual("37.187.47.25", msg.Host);
            ClassicAssert.AreEqual(ReplyCode.Null, msg.ReplyCode);
            ClassicAssert.AreEqual(ReceiveType.Join, msg.Type);
            ClassicAssert.AreEqual("#debian.de", msg.Message);
            ClassicAssert.AreEqual("#debian.de", msg.Channel);
            ClassicAssert.IsNotNull(msg.Tags);
            ClassicAssert.AreEqual(0, msg.Tags.Count);
        }
    }
}

