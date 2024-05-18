/*
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2013 Ondřej Hošek <ondra.hosek@gmail.com>
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
using System.Collections.Generic;

namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// Represents the properties of an IRC server.
    /// </summary>
    public class ServerProperties
    {
        /// <summary>
        /// Gets the raw properties of the server.
        /// </summary>
        public Dictionary<string, string> RawProperties { get; internal set; }

        /// <summary>
        /// Gets the case mapping type used by the server.
        /// </summary>
        public CaseMappingType CaseMapping
        {
            get
            {
                if (!HaveNonNullKey("CASEMAPPING"))
                {
                    // default is rfc1459
                    return CaseMappingType.Rfc1459;
                }

                switch (RawProperties["CASEMAPPING"])
                {
                    case "ascii":
                        return CaseMappingType.Ascii;
                    case "rfc1459":
                        return CaseMappingType.Rfc1459;
                    case "strict-rfc1459":
                        return CaseMappingType.StrictRfc1459;
                    default:
                        return CaseMappingType.Unknown;
                }
            }
        }

        /// <summary>
        /// Gets the channel join limits of the server.
        /// </summary>
        public IDictionary<string, int> ChannelJoinLimits
        {
            get
            {
                return ParseStringNumberPairs("CHANLIMIT", null, null);
            }
        }

        /// <summary>
        /// Gets the list channel modes of the server.
        /// </summary>
        public string ListChannelModes
        {
            get
            {
                var splitmodes = SplitChannelModes;
                if (splitmodes == null)
                {
                    return null;
                }
                return splitmodes[0];
            }
        }

        /// <summary>
        /// Gets the parametric channel modes of the server.
        /// </summary>
        public string ParametricChannelModes
        {
            get
            {
                var splitmodes = SplitChannelModes;
                if (splitmodes == null)
                {
                    return null;
                }
                return splitmodes[1];
            }
        }

        /// <summary>
        /// Gets the set parametric channel modes of the server.
        /// </summary>
        public string SetParametricChannelModes
        {
            get
            {
                var splitmodes = SplitChannelModes;
                if (splitmodes == null)
                {
                    return null;
                }
                return splitmodes[2];
            }
        }

        /// <summary>
        /// Gets the parameterless channel modes of the server.
        /// </summary>
        public string ParameterlessChannelModes
        {
            get
            {
                var splitmodes = SplitChannelModes;
                if (splitmodes == null)
                {
                    return null;
                }
                return splitmodes[3];
            }
        }

        /// <summary>
        /// Gets the maximum length of a channel name on the server.
        /// </summary>
        public int ChannelNameLength
        {
            get
            {
                // defaults as specified by RFC1459
                var len = ParseNumber("CHANNELLEN", 200, 200);
                return len ?? -1;
            }
        }

        /// <summary>
        /// Gets the channel types supported by the server.
        /// </summary>
        public char[] ChannelTypes
        {
            get
            {
                if (!HaveNonNullKey("CHANTYPES"))
                {
                    // sane default
                    return "#&".ToCharArray();
                }

                return RawProperties["CHANTYPES"].ToCharArray();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the server supports channel participant notices.
        /// </summary>
        public bool SupportsChannelParticipantNotices
        {
            get
            {
                return RawProperties.ContainsKey("CNOTICE");
            }
        }

        /// <summary>
        /// Gets a value indicating whether the server supports channel participant private messages.
        /// </summary>
        public bool SupportsChannelParticipantPrivMsgs
        {
            get
            {
                return RawProperties.ContainsKey("CPRIVMSG");
            }
        }

        /// <summary>
        /// Gets the list extensions supported by the server.
        /// </summary>
        public ListExtensions ListExtensions
        {
            get
            {
                if (!HaveNonNullKey("ELIST"))
                {
                    return ListExtensions.None;
                }

                var eliststr = RawProperties["ELIST"];
                var exts = ListExtensions.None;
                foreach (var e in eliststr.ToUpperInvariant())
                {
                    switch (e)
                    {
                        case 'C':
                            exts |= ListExtensions.CreationTime;
                            break;
                        case 'M':
                            exts |= ListExtensions.ContainsParticipantWithMask;
                            break;
                        case 'N':
                            exts |= ListExtensions.DoesNotContainParticipantWithMask;
                            break;
                        case 'T':
                            exts |= ListExtensions.TopicAge;
                            break;
                        case 'U':
                            exts |= ListExtensions.ParticipantCount;
                            break;
                    }
                }

                return exts;
            }
        }

        /// <summary>
        /// Gets the character used for ban exceptions on the server.
        /// </summary>
        public char? BanExceptionCharacter
        {
            get
            {
                if (!RawProperties.ContainsKey("EXCEPTS"))
                {
                    return null;
                }

                var exstr = RawProperties["EXCEPTS"];
                if (exstr == null)
                {
                    // default: +e
                    return 'e';
                }
                else if (exstr.Length != 1)
                {
                    // invalid; assume lack of support
                    return null;
                }
                return exstr[0];
            }
        }

        /// <summary>
        /// Gets the character used for invite exceptions on the server.
        /// </summary>
        public char? InviteExceptionCharacter
        {
            get
            {
                if (!RawProperties.ContainsKey("INVEX"))
                {
                    return null;
                }

                var exstr = RawProperties["INVEX"];
                if (exstr == null)
                {
                    // default: +I
                    return 'I';
                }
                else if (exstr.Length != 1)
                {
                    // invalid; assume lack of support
                    return null;
                }
                return exstr[0];
            }
        }

        /// <summary>
        /// Gets the maximum length of a kick message on the server.
        /// </summary>
        public int? KickMessageLength
        {
            get
            {
                return ParseNumber("KICKLEN", null, null);
            }
        }

        /// <summary>
        /// Gets the list mode limits of the server.
        /// </summary>
        public IDictionary<string, int> ListModeLimits
        {
            get
            {
                return ParseStringNumberPairs("MAXLIST", null, null);
            }
        }

        /// <summary>
        /// Gets the maximum number of parametric mode sets on the server.
        /// </summary>
        public int? MaxParametricModeSets
        {
            get
            {
                // 3 if not set, infinity if value-less
                return ParseNumber("MODES", 3, -1);
            }
        }

        /// <summary>
        /// Gets the name of the network that the server belongs to.
        /// </summary>
        public string NetworkName
        {
            get
            {
                if (!HaveNonNullKey("NETWORK"))
                {
                    return null;
                }
                return RawProperties["NETWORK"];
            }
        }

        /// <summary>
        /// Gets the maximum length of a nickname on the server.
        /// </summary>
        public int? MaxNicknameLength
        {
            get
            {
                // RFC1459 default if unset
                return ParseNumber("NICKLEN", 9, null);
            }
        }

        /// <summary>
        /// Gets the channel privilege modes prefixes of the server.
        /// </summary>
        public IList<KeyValuePair<char, char>> ChannelPrivilegeModesPrefixes
        {
            get
            {
                var modesList = new List<KeyValuePair<char, char>>();

                if (!RawProperties.ContainsKey("PREFIX"))
                {
                    // assume voice and ops
                    modesList.Add(new KeyValuePair<char, char>('o', '@'));
                    modesList.Add(new KeyValuePair<char, char>('v', '+'));
                    return modesList;
                }
                var prefixstr = RawProperties["PREFIX"];
                if (prefixstr == null)
                    // supports no modes (!)
                    return modesList;

                // format: (modes)prefixes
                if (prefixstr[0] != '(')
                    return null;

                var modesPrefixes = prefixstr.Substring(1).Split(')');
                if (modesPrefixes.Length != 2)
                    // assuming the pathological case of a ')' mode
                    // character is impossible, this is invalid
                    return null;

                var modes = modesPrefixes[0];
                var prefixes = modesPrefixes[1];
                if (modes.Length != prefixes.Length)
                    return null;

                for (var i = 0; i < modes.Length; ++i)
                    modesList.Add(new KeyValuePair<char, char>(modes[i], prefixes[i]));

                return modesList;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the LIST command is safe on the server.
        /// </summary>
        public bool ListIsSafe
        {
            get
            {
                return RawProperties.ContainsKey("SAFELIST");
            }
        }

        /// <summary>
        /// Gets the maximum number of entries in the silence list on the server.
        /// </summary>
        public int MaxSilenceListEntries
        {
            get
            {
                // SILENCE requires a value, but assume 0 if unspecified
                return ParseNumber("SILENCE", 0, 0) ?? 0;
            }
        }

        /// <summary>
        /// Gets the participants that can receive status notices on the server.
        /// </summary>
        public string StatusNoticeParticipants
        {
            get
            {
                if (!HaveNonNullKey("STATUSMSG"))
                {
                    // STATUSMSG requires a value, but assume none
                    // if unspecified
                    return "";
                }
                return RawProperties["STATUSMSG"];
            }
        }

        /// <summary>
        /// Gets the maximum number of targets for each command on the server.
        /// </summary>
        public IDictionary<string, int> MaxCommandTargets
        {
            get
            {
                var emptydict = new Dictionary<string, int>();
                return ParseStringNumberPairs("TARGMAX", emptydict, null);
            }
        }

        /// <summary>
        /// Gets the maximum length of a topic on the server.
        /// </summary>
        public int MaxTopicLength
        {
            get
            {
                // SILENCE requires a value, but assume infinity
                // if unspecified or invalid
                return ParseNumber("TOPICLEN", -1, -1) ?? -1;
            }
        }

        /// <summary>
        /// Gets the maximum number of entries in the watch list on the server.
        /// </summary>
        public int MaxWatchListEntries
        {
            get
            {
                // SILENCE requires a value, but assume 0 if unspecified
                return ParseNumber("WATCH", 0, 0) ?? 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerProperties"/> class.
        /// </summary>
        internal ServerProperties()
        {
            RawProperties = new Dictionary<string, string>();
        }

        /// <summary>
        /// Parses the properties from a raw message.
        /// </summary>
        /// <param name="rawMessage">The raw message to parse the properties from.</param>
        internal void ParseFromRawMessage(string[] rawMessage)
        {
            // split the message (0 = server, 1 = code, 2 = my nick)
            for (var i = 3; i < rawMessage.Length; ++i)
            {
                var msg = rawMessage[i];
                if (msg.StartsWith(":"))
                {
                    // addendum; we're done
                    break;
                }

                var keyval = msg.Split('=');
                if (keyval.Length == 1)
                {
                    // keyword only
                    RawProperties[keyval[0]] = null;
                }
                else if (keyval.Length == 2)
                {
                    // key and value
                    RawProperties[keyval[0]] = keyval[1];
                }
            }
        }

        /// <summary>
        /// Checks if a key has a non-null value in the raw properties.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>true if the key has a non-null value; otherwise, false.</returns>
        bool HaveNonNullKey(string key)
        {
            if (!RawProperties.ContainsKey(key))
            {
                return false;
            }
            return RawProperties[key] != null;
        }

        /// <summary>
        /// Parses pairs of strings and numbers from the raw properties.
        /// </summary>
        /// <param name="key">The key to parse the pairs from.</param>
        /// <param name="unsetDefault">The default value to return if the key is not set.</param>
        /// <param name="emptyDefault">The default value to return if the key is set but its value is empty.</param>
        /// <returns>A dictionary of the parsed pairs.</returns>
        IDictionary<string, int> ParseStringNumberPairs(string key, IDictionary<string, int> unsetDefault, IDictionary<string, int> emptyDefault)
        {
            if (!RawProperties.ContainsKey(key))
            {
                return unsetDefault;
            }

            var valstr = RawProperties[key];
            if (valstr == null)
            {
                return emptyDefault;
            }

            var valmap = new Dictionary<string, int>();
            // comma splits the specs
            foreach (var limit in valstr.Split(','))
            {
                // colon splits keys and value
                var split = limit.Split(':');
                if (split.Length != 2)
                {
                    // invalid spec; don't trust the whole thing
                    return null;
                }
                var chantypes = split[0];
                var valuestr = split[1];
                int value;
                if (valuestr == string.Empty)
                    return null;
                else if (!int.TryParse(valuestr, out value))
                    // invalid integer; don't trust the whole thing
                    return null;

                valmap[chantypes] = value;
            }

            return valmap;
        }

        /// <summary>
        /// Parses a number from the raw properties.
        /// </summary>
        /// <param name="key">The key to parse the number from.</param>
        /// <param name="unsetDefault">The default value to return if the key is not set.</param>
        /// <param name="emptyDefault">The default value to return if the key is set but its value is empty.</param>
        /// <returns>The parsed number, or the default value if the key is not set or its value is empty.</returns>
        int? ParseNumber(string key, int? unsetDefault, int? emptyDefault)
        {
            if (!RawProperties.ContainsKey(key))
            {
                return unsetDefault;
            }
            var numstr = RawProperties[key];
            if (numstr == null)
            {
                return emptyDefault;
            }
            if (!int.TryParse(numstr, out var num))
            {
                return null;
            }
            return num;
        }

        /// <summary>
        /// Splits the channel modes from the raw properties.
        /// </summary>
        string[] SplitChannelModes
        {
            get
            {
                if (!HaveNonNullKey("CHANMODES"))
                {
                    return null;
                }
                var splits = RawProperties["CHANMODES"].Split(',');
                if (splits.Length != 4)
                {
                    return null;
                }
                return splits;
            }
        }
    }

    /// <summary>
    /// Represents the case mapping type used by the IRC server.
    /// </summary>
    public enum CaseMappingType
    {
        /// <summary>
        /// The case mapping type is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The ASCII case mapping type.
        /// </summary>
        Ascii,

        /// <summary>
        /// The RFC1459 case mapping type.
        /// </summary>
        Rfc1459,

        /// <summary>
        /// The strict RFC1459 case mapping type.
        /// </summary>
        StrictRfc1459,
    }

    /// <summary>
    /// Represents the extensions for the LIST command in IRC.
    /// </summary>
    [Flags]
    public enum ListExtensions
    {
        /// <summary>
        /// No extensions are used.
        /// </summary>
        None = 0,

        /// <summary>
        /// The creation time of the channel is included in the LIST command.
        /// </summary>
        CreationTime = (1 << 0),

        /// <summary>
        /// The LIST command includes channels that contain a participant with a certain mask.
        /// </summary>
        ContainsParticipantWithMask = (1 << 1),

        /// <summary>
        /// The LIST command includes channels that do not contain a participant with a certain mask.
        /// </summary>
        DoesNotContainParticipantWithMask = (1 << 2),

        /// <summary>
        /// The age of the topic is included in the LIST command.
        /// </summary>
        TopicAge = (1 << 3),

        /// <summary>
        /// The count of participants is included in the LIST command.
        /// </summary>
        ParticipantCount = (1 << 4)
    }
}
