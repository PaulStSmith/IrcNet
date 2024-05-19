// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.Linq;
using System.Collections.Generic;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents the action of a channel mode change.
    /// </summary>
    public enum ChannelModeChangeAction
    {
        /// <summary>
        /// The mode is being set.
        /// </summary>
        Set,

        /// <summary>
        /// The mode is being unset.
        /// </summary>
        Unset
    }

    /// <summary>
    /// Represents the different modes a channel can have.
    /// </summary>
    public enum ChannelMode
    {
        /// <summary>
        /// The mode is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The user was elevated to operator status.
        /// </summary>
        Op = 'o',

        /// <summary>
        /// The user was elevated to owner status.
        /// </summary>
        Owner = 'q',

        /// <summary>
        /// The user was elevated to admin status.
        /// </summary>
        Admin = 'a',

        /// <summary>
        /// The user was elevated to half-operator status.
        /// </summary>
        HalfOp = 'h',

        /// <summary>
        /// The user was given voice.
        /// </summary>
        Voice = 'v',

        /// <summary>
        /// The user was banned from the channel.
        /// </summary>
        Ban = 'b',

        /// <summary>
        /// The user was exempt from a ban.
        /// </summary>
        BanException = 'e',

        /// <summary>
        /// The user is exempt from the need to be invited to the channel.
        /// </summary>
        InviteException = 'I',

        /// <summary>
        /// The channel was assigned a password.
        /// </summary>
        Key = 'k',

        /// <summary>
        /// The channel's user limit was set.
        /// </summary>
        UserLimit = 'l',

        /// <summary>
        /// The channel topic was locked.
        /// </summary>
        TopicLock = 't'
    }

    /// <summary>
    /// Specifies when a channel mode has a parameter.
    /// </summary>
    public enum ChannelModeHasParameter
    {
        /// <summary>
        /// The mode always has a parameter.
        /// </summary>
        Always,

        /// <summary>
        /// The mode only has a parameter when it is being set.
        /// </summary>
        OnlySet,

        /// <summary>
        /// The mode never has a parameter.
        /// </summary>
        Never
    }

    /// <summary>
    /// Represents information about a channel mode.
    /// </summary>
    public class ChannelModeInfo
    {
        /// <summary>
        /// Gets or sets the channel mode.
        /// </summary>
        public ChannelMode Mode { get; set; }

        /// <summary>
        /// Gets or sets when the channel mode has a parameter.
        /// </summary>
        public ChannelModeHasParameter HasParameter { get; set; }

        /// <summary>
        /// Initializes a new instance of the ChannelModeInfo class.
        /// </summary>
        /// <param name="mode">The channel mode.</param>
        /// <param name="hasParameter">When the channel mode has a parameter.</param>
        public ChannelModeInfo(ChannelMode mode, ChannelModeHasParameter hasParameter)
        {
            Mode = mode;
            HasParameter = hasParameter;
        }
    }

    /// <summary>
    /// Represents a map of channel modes.
    /// </summary>
    public class ChannelModeMap : Dictionary<char, ChannelModeInfo>
    {
        /// <summary>
        /// Initializes a new instance of the ChannelModeMap class using Smuxi mapping.
        /// </summary>
        public ChannelModeMap() :
            // Smuxi mapping
            this("oqahvbeI,k,l,imnpstr")
        // IRCnet mapping
        //this("beIR,k,l,imnpstaqr")
        {
        }

        /// <summary>
        /// Initializes a new instance of the ChannelModeMap class with a specific set of channel modes.
        /// </summary>
        /// <param name="channelModes">The set of channel modes.</param>
        public ChannelModeMap(string channelModes)
        {
            Parse(channelModes);
        }

        /// <summary>
        /// Parses the specified set of channel modes and adds them to the map.
        /// </summary>
        /// <param name="channelModes">The set of channel modes to parse.</param>
        public void Parse(string channelModes)
        {
            var listAlways = channelModes.Split(',')[0];
            var settingAlways = channelModes.Split(',')[1];
            var onlySet = channelModes.Split(',')[2];
            var never = channelModes.Split(',')[3];

            foreach (var mode in listAlways)
            {
                this[mode] = new ChannelModeInfo((ChannelMode)mode, ChannelModeHasParameter.Always);
            }
            foreach (var mode in settingAlways)
            {
                this[mode] = new ChannelModeInfo((ChannelMode)mode, ChannelModeHasParameter.Always);
            }
            foreach (var mode in onlySet)
            {
                this[mode] = new ChannelModeInfo((ChannelMode)mode, ChannelModeHasParameter.OnlySet);
            }
            foreach (var mode in never)
            {
                this[mode] = new ChannelModeInfo((ChannelMode)mode, ChannelModeHasParameter.Never);
            }
        }
    }

    /// <summary>
    /// Represents information about a channel mode change.
    /// </summary>
    public class ChannelModeChangeInfo
    {
        /// <summary>
        /// Gets the action of the channel mode change.
        /// </summary>
        public ChannelModeChangeAction Action { get; private set; }

        /// <summary>
        /// Gets the channel mode.
        /// </summary>
        public ChannelMode Mode { get; private set; }

        /// <summary>
        /// Gets the character representing the channel mode.
        /// </summary>
        public char ModeChar { get; private set; }

        /// <summary>
        /// Gets the parameter of the channel mode change.
        /// </summary>
        public string Parameter { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ChannelModeChangeInfo class.
        /// </summary>
        public ChannelModeChangeInfo()
        {
        }

        /// <summary>
        /// Parses the specified channel mode map, target, mode, and mode parameters and returns a list of channel mode change information.
        /// </summary>
        /// <param name="modeMap">The channel mode map.</param>
        /// <param name="target">The target of the mode change.</param>
        /// <param name="mode">The mode of the mode change.</param>
        /// <param name="modeParameters">The parameters of the mode change.</param>
        /// <returns>A list of ChannelModeChangeInfo objects representing the parsed data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any of the arguments are null.</exception>
        public static List<ChannelModeChangeInfo> Parse(ChannelModeMap modeMap, string target, string mode, string modeParameters)
        {
            if (modeMap == null)
            {
                throw new ArgumentNullException("modeMap");
            }
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (mode == null)
            {
                throw new ArgumentNullException("mode");
            }
            if (modeParameters == null)
            {
                throw new ArgumentNullException("modeParameters");
            }

            var modeChanges = new List<ChannelModeChangeInfo>();

            var action = ChannelModeChangeAction.Set;
            var parameters = modeParameters.Split(new char[] { ' ' });
            var parametersEnumerator = parameters.GetEnumerator();
            // bring the enumerator to the 1. element
            parametersEnumerator.MoveNext();
            foreach (var modeChar in mode)
            {
                switch (modeChar)
                {
                    case '+':
                        action = ChannelModeChangeAction.Set;
                        break;
                    case '-':
                        action = ChannelModeChangeAction.Unset;
                        break;
                    default:
                        ChannelModeInfo modeInfo;
                        modeMap.TryGetValue(modeChar, out modeInfo);
                        if (modeInfo == null)
                        {
                            // modes not specified in CHANMODES are expected to
                            // always have parameters
                            modeInfo = new ChannelModeInfo((ChannelMode)modeChar, ChannelModeHasParameter.Always);
                        }

                        string parameter = null;
                        var channelMode = modeInfo.Mode;
                        if (!Enum.IsDefined(typeof(ChannelMode), channelMode))
                        {
                            channelMode = ChannelMode.Unknown;
                        }
                        var hasParameter = modeInfo.HasParameter;
                        if (hasParameter == ChannelModeHasParameter.Always ||
                            (hasParameter == ChannelModeHasParameter.OnlySet &&
                             action == ChannelModeChangeAction.Set))
                        {
                            parameter = (string)parametersEnumerator.Current;
                            parametersEnumerator.MoveNext();
                        }

                        modeChanges.Add(new ChannelModeChangeInfo()
                        {
                            Action = action,
                            Mode = channelMode,
                            ModeChar = modeChar,
                            Parameter = parameter
                        });
                        break;
                }
            }

            return modeChanges;
        }
    }
}

