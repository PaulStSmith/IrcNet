//  SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
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
    public class ServerPropertiesTests
    {
        [Test]
        public void ParseFromRawMessage()
        {
            var rawline = ":irc.example.com 005 meebey3 CALLERID CASEMAPPING=rfc1459 DEAF=D KICKLEN=160 MODES=4 NICKLEN=30 PREFIX=(ov)@+ STATUSMSG=@+ TOPICLEN=390 NETWORK=EFnet MAXLIST=beI:25 MAXTARGETS=4 CHANTYPES=#& :are supported by this server";
            var props = new ServerProperties();
            props.ParseFromRawMessage(rawline.Split(' '));
            ClassicAssert.AreEqual(13, props.RawProperties.Count);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("CALLERID"));
            ClassicAssert.AreEqual(null, props.RawProperties["CALLERID"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("CASEMAPPING"));
            ClassicAssert.AreEqual("rfc1459", props.RawProperties["CASEMAPPING"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("DEAF"));
            ClassicAssert.AreEqual("D", props.RawProperties["DEAF"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("KICKLEN"));
            ClassicAssert.AreEqual("160", props.RawProperties["KICKLEN"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("MODES"));
            ClassicAssert.AreEqual("4", props.RawProperties["MODES"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("NICKLEN"));
            ClassicAssert.AreEqual("30", props.RawProperties["NICKLEN"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("PREFIX"));
            ClassicAssert.AreEqual("(ov)@+", props.RawProperties["PREFIX"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("STATUSMSG"));
            ClassicAssert.AreEqual("@+", props.RawProperties["STATUSMSG"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("TOPICLEN"));
            ClassicAssert.AreEqual("390", props.RawProperties["TOPICLEN"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("NETWORK"));
            ClassicAssert.AreEqual("EFnet", props.RawProperties["NETWORK"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("MAXLIST"));
            ClassicAssert.AreEqual("beI:25", props.RawProperties["MAXLIST"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("MAXTARGETS"));
            ClassicAssert.AreEqual("4", props.RawProperties["MAXTARGETS"]);
            ClassicAssert.IsTrue(props.RawProperties.ContainsKey("CHANTYPES"));
            ClassicAssert.AreEqual("#&", props.RawProperties["CHANTYPES"]);

            rawline = ":irc.example.com 005 meebey3 CHANLIMIT=#&:25 CHANNELLEN=50 CHANMODES=eIb,k,l,imnpstMRS KNOCK ELIST=CMNTU SAFELIST AWAYLEN=160 EXCEPTS=e INVEX=I :are supported by this server";
            props.ParseFromRawMessage(rawline.Split(' '));
            ClassicAssert.AreEqual(13+9, props.RawProperties.Count);
            ClassicAssert.AreEqual("#&:25", props.RawProperties["CHANLIMIT"]);
            ClassicAssert.AreEqual("50", props.RawProperties["CHANNELLEN"]);
            ClassicAssert.AreEqual("eIb,k,l,imnpstMRS", props.RawProperties["CHANMODES"]);
            ClassicAssert.AreEqual(null, props.RawProperties["KNOCK"]);
            ClassicAssert.AreEqual("CMNTU", props.RawProperties["ELIST"]);
            ClassicAssert.AreEqual(null, props.RawProperties["SAFELIST"]);
            ClassicAssert.AreEqual("160", props.RawProperties["AWAYLEN"]);
            ClassicAssert.AreEqual("e", props.RawProperties["EXCEPTS"]);
            ClassicAssert.AreEqual("I", props.RawProperties["INVEX"]);
        }
    }
}