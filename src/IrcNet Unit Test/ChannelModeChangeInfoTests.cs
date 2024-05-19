//  SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
//
//  Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Meebey.SmartIrc4net
{
    [TestFixture]
    public class ChannelModeChangeInfoTests
    {
        [Test]
        public void ParseWithParameter()
        {
            var modeMap = new ChannelModeMap();
            List<ChannelModeChangeInfo> changeInfos;
            ChannelModeChangeInfo changeInfo;

            changeInfos = ChannelModeChangeInfo.Parse(modeMap, "#test", "+o", "meebey");
            ClassicAssert.IsNotNull(changeInfos);
            ClassicAssert.AreEqual(1, changeInfos.Count);

            changeInfo = changeInfos[0];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Set, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.Op, changeInfo.Mode);
            ClassicAssert.AreEqual('o', changeInfo.ModeChar);
            ClassicAssert.AreEqual("meebey", changeInfo.Parameter);
        }

        [Test]
        public void ParseWithoutParameter()
        {
            var modeMap = new ChannelModeMap();
            List<ChannelModeChangeInfo> changeInfos;
            ChannelModeChangeInfo changeInfo;

            changeInfos = ChannelModeChangeInfo.Parse(modeMap, "#test", "+nt", "");
            ClassicAssert.IsNotNull(changeInfos);
            ClassicAssert.AreEqual(2, changeInfos.Count);

            changeInfo = changeInfos[0];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Set, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.Unknown, changeInfo.Mode);
            ClassicAssert.AreEqual('n', changeInfo.ModeChar);
            ClassicAssert.AreEqual(null, changeInfo.Parameter);

            changeInfo = changeInfos[1];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Set, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.TopicLock, changeInfo.Mode);
            ClassicAssert.AreEqual('t', changeInfo.ModeChar);
            ClassicAssert.AreEqual(null, changeInfo.Parameter);
        }

        [Test]
        public void ParseComplex()
        {
            var modeMap = new ChannelModeMap();
            List<ChannelModeChangeInfo> changeInfos;
            ChannelModeChangeInfo changeInfo;

            changeInfos = ChannelModeChangeInfo.Parse(modeMap, "#test", "-l+o-k+v", "op_nick * voice_nick");
            ClassicAssert.IsNotNull(changeInfos);
            ClassicAssert.AreEqual(4, changeInfos.Count);

            changeInfo = changeInfos[0];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Unset, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.UserLimit, changeInfo.Mode);
            ClassicAssert.AreEqual('l', changeInfo.ModeChar);
            ClassicAssert.AreEqual(null, changeInfo.Parameter);

            changeInfo = changeInfos[1];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Set, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.Op, changeInfo.Mode);
            ClassicAssert.AreEqual('o', changeInfo.ModeChar);
            ClassicAssert.AreEqual("op_nick", changeInfo.Parameter);

            changeInfo = changeInfos[2];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Unset, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.Key, changeInfo.Mode);
            ClassicAssert.AreEqual('k', changeInfo.ModeChar);
            ClassicAssert.AreEqual("*", changeInfo.Parameter);

            changeInfo = changeInfos[3];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Set, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.Voice, changeInfo.Mode);
            ClassicAssert.AreEqual('v', changeInfo.ModeChar);
            ClassicAssert.AreEqual("voice_nick", changeInfo.Parameter);
        }

        [Test]
        public void ParseUnknown()
        {
            var modeMap = new ChannelModeMap();
            List<ChannelModeChangeInfo> changeInfos;
            ChannelModeChangeInfo changeInfo;

            changeInfos = ChannelModeChangeInfo.Parse(modeMap, "#test", "+X-Y", "foo bar");
            ClassicAssert.IsNotNull(changeInfos);
            ClassicAssert.AreEqual(2, changeInfos.Count);

            changeInfo = changeInfos[0];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Set, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.Unknown, changeInfo.Mode);
            ClassicAssert.AreEqual('X', changeInfo.ModeChar);
            ClassicAssert.AreEqual("foo", changeInfo.Parameter);

            changeInfo = changeInfos[1];
            ClassicAssert.AreEqual(ChannelModeChangeAction.Unset, changeInfo.Action);
            ClassicAssert.AreEqual(ChannelMode.Unknown, changeInfo.Mode);
            ClassicAssert.AreEqual('Y', changeInfo.ModeChar);
            ClassicAssert.AreEqual("bar", changeInfo.Parameter);
        }
    }
}

