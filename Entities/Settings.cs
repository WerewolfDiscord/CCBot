// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using CCBot;
//
//    var settings = Settings.FromJson(jsonString);

namespace CCBot
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using J = Newtonsoft.Json.JsonPropertyAttribute;
    using R = Newtonsoft.Json.Required;
    using N = Newtonsoft.Json.NullValueHandling;
    using I = Newtonsoft.Json.JsonIgnoreAttribute;
    using DSharpPlus.Entities;
    using System.Threading.Tasks;

    public partial class Settings
    {
        [I] public DiscordChannel Category { get { if (_categoryCache == null) _categoryCache = MainGuild.GetChannel((ulong)_category); return _categoryCache; } set { if (value == null) return; _category = (long)value.Id; _categoryCache = value; } }
        [I] private DiscordChannel _categoryCache;
        [J("CategoryId")] private long _category;
        [J("SignedUpPlayers")] public List<Player> SignedUpPlayers { get; set; }
        [I] public DiscordGuild MainGuild { get { if (_MainGuildCache == null) _MainGuildCache = Bot._client.GetGuildAsync((ulong)_MainGuild).Result; return _MainGuildCache; } set { if (value == null) return; _MainGuild = (long)value.Id; _MainGuildCache = value; } }
        [I] private DiscordGuild _MainGuildCache;
        [J("MainGuildId")] private long _MainGuild;
        [I] public DiscordRole GameMasterRole { get { if (_GameMasterRoleCache == null) _GameMasterRoleCache = MainGuild.GetRole((ulong)_GameMasterRole); return _GameMasterRoleCache; } set { if (value == null) return; _GameMasterRole = (long)value.Id; _GameMasterRoleCache = value; } }
        [I] private DiscordRole _GameMasterRoleCache;
        [J("GameMasterRoleId")] private long _GameMasterRole;
        [I] public DiscordRole DeadParticipantRole { get { if (_DeadParticipantRoleCache == null) _DeadParticipantRoleCache = MainGuild.GetRole((ulong)_DeadParticipantRole); return _DeadParticipantRoleCache; } set { if (value == null) return; _DeadParticipantRole = (long)value.Id; _DeadParticipantRoleCache = value; } }
        [I] private DiscordRole _DeadParticipantRoleCache;
        [J("DeadParticipantRoleId")] private long _DeadParticipantRole;
        [I] public DiscordRole AliveParticipantRole { get { if (_AliveParticipantRoleCache == null) _AliveParticipantRoleCache = MainGuild.GetRole((ulong)_AliveParticipantRole); return _AliveParticipantRoleCache; } set { if (value == null) return; _AliveParticipantRole = (long)value.Id; _AliveParticipantRoleCache = value; } }
        [I] private DiscordRole _AliveParticipantRoleCache;
        [I] public DiscordRole FrozenPlayerRole { get { if (_FrozenPlayerRoleCache == null) _FrozenPlayerRoleCache = MainGuild.GetRole((ulong)_FrozenPlayerRole); return _FrozenPlayerRoleCache; } set { if (value == null) return; _FrozenPlayerRole = (long)value.Id; _FrozenPlayerRoleCache = value; } }
        [I] private DiscordRole _FrozenPlayerRoleCache;
        [J("FrozenPlayerRoleId")] private long _FrozenPlayerRole;
        [J("AliveParticipantRoleId")] private long _AliveParticipantRole;
        [J("Channels")] public List<CC> Channels { get; set; }
        [J("SeasonNumer")] public string SeasonNumber { get; set; }


        internal void Validate()
        {
            if (SignedUpPlayers == null)
                SignedUpPlayers = new List<Player>();
            if (Channels == null)
                Channels = new List<CC>();
        }
    }

    public partial class CC
    {
        [I] public DiscordMember Owner { get { if (_OwnerCache == null) _OwnerCache = Dependencies.Settings.MainGuild.GetMemberAsync((ulong)_Owner).Result; return _OwnerCache; } set { if (value == null) return; _Owner = (long)value.Id; _OwnerCache = value; } }
        [I] private DiscordMember _OwnerCache;
        [J("OwnerId")] private long _Owner;
        [I] public List<DiscordMember> Members { get { if (_MembersCache == null)
                {
                    _MembersCache = new List<DiscordMember>();
                    foreach (var v in _Members)
                    {
                        _MembersCache.Add(Dependencies.Settings.MainGuild.GetMemberAsync((ulong)v).Result);
                    }
                }
                return _MembersCache; } set {
                if (value == null) return; _Members = new List<long>();
                foreach (var v2 in value)
                {
                    _Members.Add((long)v2.Id);
                }
                _MembersCache = value; } }
        [I] private List<DiscordMember> _MembersCache;
        [J("MemberIds")] private List<long> _Members;
        [I] public DiscordChannel Channel { get { if (_ChannelCache == null) _ChannelCache = Dependencies.Settings.MainGuild.GetChannel((ulong)_Channel); return _ChannelCache; } set { if (value == null) return; _Channel = (long)value.Id; _ChannelCache = value; } }
        [I] private DiscordChannel _ChannelCache;
        [J("ChannelId")] private long _Channel;

        public CC(DiscordMember Owner, List<DiscordMember> Members, DiscordChannel Channel)
        {
            this.Owner = Owner;
            this.Members = Members;
            this.Channel = Channel;
        }
    }

    public partial class Player
    {
        public Player(DiscordMember user, DiscordEmoji Emoji, bool IsAlive = true)
        {
            this.user = user;
            this.Emoji = Emoji;
            this.IsAlive = IsAlive;
        }

        [I] public DiscordMember user { get { if (_userCache == null) _userCache = Dependencies.Settings.MainGuild.GetMemberAsync((ulong)_user).Result; return _userCache; } set { if (value == null) return; _user = (long)value.Id; _userCache = Dependencies.Settings.MainGuild.GetMemberAsync((ulong)_user).Result; } }
        [I] private DiscordMember _userCache;
        [J("userid")] private long _user;
        [I] public DiscordEmoji Emoji { get { if (_EmojiCache == null) _EmojiCache = DiscordEmoji.FromName(Bot._client, _Emoji); return _EmojiCache; } set { if (value == null) return; _Emoji = value.GetDiscordName(); _EmojiCache = DiscordEmoji.FromName(Bot._client, _Emoji); } }
        [I] private DiscordEmoji _EmojiCache;
        [J("EmojiName")] private string _Emoji;
        [J("IsAlive")] public bool IsAlive { get; set; }
    }

    public partial class Settings
    {
        public static Settings FromJson(string json) => JsonConvert.DeserializeObject<Settings>(json, CCBot.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Settings self) => JsonConvert.SerializeObject(self, CCBot.Converter.Settings);
    }

    internal class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
