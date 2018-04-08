using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CCBot
{
    public class Main : BaseCommandModule
    {
        private Dependencies dep;
        private DiscordChannel _category;

        public async Task<DiscordChannel> GetOrCreateCategory()
        {
            if (_category == null || _category.Children.Count() >= 40)
            {
                _category = await Dependencies.Settings.MainGuild.CreateChannelAsync($"S{Dependencies.Settings.SeasonNumber} Conspiracy Channel", ChannelType.Category);
            }

            return _category;
        }

        public Main(Dependencies dep)
        {
            this.dep = dep;
            if (Dependencies.Settings.SeasonNumber != null)
                this._category = Dependencies.Settings.Category;
        }

        [Command("setup")]
        [RequireOwner]
        public async Task SetupCMD(CommandContext ctx)
        {
            await ctx.RespondAsync("Lets start! First, please Mention the Game Master Role!");
            var m1 = await dep.Interactivity.WaitForMessageAsync(x => x.MentionedRoles.Count == 1, new TimeSpan(100, 0, 0));
            Dependencies.Settings.GameMasterRole = m1.MentionedRoles[0];

            await ctx.RespondAsync("Thank you! Next the Alive Participant Role!");
            var m2 = await dep.Interactivity.WaitForMessageAsync(x => x.MentionedRoles.Count == 1, new TimeSpan(100, 0, 0));
            Dependencies.Settings.AliveParticipantRole = m2.MentionedRoles[0];

            await ctx.RespondAsync("What is the Dead Participant Role?");
            var m3 = await dep.Interactivity.WaitForMessageAsync(x => x.MentionedRoles.Count == 1, new TimeSpan(100, 0, 0));
            Dependencies.Settings.DeadParticipantRole = m3.MentionedRoles[0];

            await ctx.RespondAsync("What is the Frozen Participant Role?");
            var m5 = await dep.Interactivity.WaitForMessageAsync(x => x.MentionedRoles.Count == 1, new TimeSpan(100, 0, 0));
            Dependencies.Settings.FrozenPlayerRole = m5.MentionedRoles[0];

            await ctx.RespondAsync("And last, the Season number?");
            var m4 = await dep.Interactivity.WaitForMessageAsync(x => x.Content != "");
            Dependencies.Settings.SeasonNumber = m4.Message.Content;

            await ctx.RespondAsync("Thanks, i'll set everything up. Just a moment.");

            Dependencies.Settings.Channels = new List<CC>();
            Dependencies.Settings.MainGuild = ctx.Guild;
            Dependencies.Settings.SignedUpPlayers = new List<Player>();
            Dependencies.Settings.Category = await GetOrCreateCategory();

            await ctx.RespondAsync("Ok. Everything done.");
        }

        [Command("save")]
        public async Task SaveCMD(CommandContext ctx)
        {
            File.WriteAllText("Settings.json", Dependencies.Settings.ToJson());
        }

        [Command("poll")]
        [Description("Game Master only. Creates a poll of all Alive Players")]
        public async Task PollCMD(CommandContext ctx, string Title, string Description)
        {
            if (!ctx.Member.Roles.Any(x => x.Id == Dependencies.Settings.GameMasterRole.Id))
            {
                await ctx.RespondAsync($"Game Master only {ctx.Member.Mention}!");
                throw new Exception();
            }

            var v = new DiscordEmbedBuilder();
            v.WithTitle(Title);
            v.WithDescription(Description);
            v.WithFooter("Vote by clicking the Reactions below.");

            var p = Dependencies.Settings.SignedUpPlayers;
            var p2 = new List<DiscordEmoji>();
            int i = 0;
            while(p.Count != 0)
            {
                var v2 = p[0];
                v.AddField(v2.Emoji.GetDiscordName(), v2.user.Username, true);
                p2.Add(v2.Emoji);
                p.Remove(v2);
                i++;
                if (i == 20)
                {
                    i = 0;
                    var v5 = await ctx.RespondAsync(embed: v.Build());
                    foreach (var v4 in p2)
                    {
                        await v5.CreateReactionAsync(v4);
                    }
                    p2 = new List<DiscordEmoji>();
                    v.ClearFields();
                }
            }
            if (i != 0)
            {
                i = 0;
                var v5 = await ctx.RespondAsync(embed: v.Build());
                foreach (var v4 in p2)
                {
                    await v5.CreateReactionAsync(v4);
                }
                p2 = new List<DiscordEmoji>();
                v.ClearFields();
            }
        }

        [Command("create")]
        public async Task CreateCMD(CommandContext ctx, string Name, [Description("The Emojis choosen by the Members")]params DiscordEmoji[] Members)
        {
            List<DiscordMember> m = new List<DiscordMember>();

            foreach (var v in Members)
            {
                m.Add(Dependencies.Settings.SignedUpPlayers.First(x => x.Emoji.Name == v.Name).user);
            }

            await CreateCMD(ctx, Name, m.ToArray());
        }

        [Command("create")]
        public async Task CreateCMD(CommandContext ctx, string Name, params DiscordMember[] Members)
        {
            Name = Name.Replace(' ', '_');

            if (ctx.Guild == null)
            {
                await ctx.RespondAsync("Sorry, not here!");
                throw new Exception();
            }

            var g = Dependencies.Settings.MainGuild;
            if (ctx.Guild.Id != g.Id)
            {
                await ctx.RespondAsync("Sorry, not here!");
                throw new Exception();
            }

            if (!ctx.Member.Roles.Contains(Dependencies.Settings.AliveParticipantRole))
            {
                await ctx.RespondAsync("Sorry, Participants only!");
                throw new Exception();
            }

            if (!Members.Any(x => x.Roles.Contains(Dependencies.Settings.AliveParticipantRole)))
            {
                await ctx.RespondAsync("Sorry, Participants only!");
                throw new Exception();
            }

            var members = Members.ToList();

            for (int a = 0; a < members.Count; a++)
            {
                for (int b = 0; b < members.Count; b++)
                {
                    if (a != b)
                    {
                        if (members[a].Id == members[b].Id)
                        {
                            members.Remove(members[b]);
                        }
                    }
                }
            }

            members.Remove(ctx.Member);

            var newChannel = await g.CreateChannelAsync("S" + Dependencies.Settings.SeasonNumber + "_" + Name, ChannelType.Text, parent: await GetOrCreateCategory(), overwrites: GetOverwrites(members, g, ctx.Member));
            var cc = new CC(ctx.Member, members, newChannel);
            await newChannel.SendMessageAsync("Look, look! Over @here, a new **conspiracy channel** has been created!", embed: GetListEmbed(cc));
            Dependencies.Settings.Channels.Add(cc);
        }

        [Command("listplayers")]
        [Description("Game Master only.")]
        public async Task ListPlayersCMD(CommandContext ctx)
        {
            if (!ctx.Member.Roles.Any(x => x.Id == Dependencies.Settings.GameMasterRole.Id))
            {
                await ctx.RespondAsync($"Game Master only {ctx.Member.Mention}!");
                throw new Exception();
            }

            var v = new DiscordEmbedBuilder();
            v.WithTimestamp(DateTime.UtcNow);
            v.WithTitle("All Players");

            int i = 0;
            foreach (var v2 in Dependencies.Settings.SignedUpPlayers)
            {
                var s = v2.user.Roles.Any(x => Dependencies.Settings.AliveParticipantRole.Id == x.Id) ? "Has Role" : "Does not have Role";
                v.AddField(v2.user.DisplayName, $"{s} | Alive: {v2.IsAlive}", i % 3 == 0);
                i++;
            }

            await ctx.RespondAsync(embed: v);
        }

        [Command("list")]
        public async Task ListCMD(CommandContext ctx)
        {
            var v = Dependencies.Settings.Channels.First(x => x.Channel.Id == ctx.Channel.Id);
            if (v == null)
            {
                await ctx.RespondAsync("Didnt Recognize CC");
                throw new Exception();
            }
            else
                await ctx.RespondAsync(embed: GetListEmbed(v));
        }

        [Command("add")]
        public async Task AddCMD(CommandContext ctx, DiscordMember userToAdd)
        {
            if (!ctx.Member.Roles.Contains(Dependencies.Settings.AliveParticipantRole))
            {
                await ctx.RespondAsync("Sorry, Participants only!");
                throw new Exception();
            }

            var v = Dependencies.Settings.Channels.First(x => x.Channel.Id == ctx.Channel.Id);
            if (v == null)
            {
                await ctx.RespondAsync("Didnt Recognize CC");
                throw new Exception();
            }
            else
            {
                if (v.Owner.Id == ctx.Member.Id)
                {
                    await v.Channel.AddOverwriteAsync(userToAdd, Permissions.AccessChannels);
                    await ctx.RespondAsync($"Everybody, welcome {userToAdd.Mention}");
                }
                else
                {
                    await ctx.RespondAsync("You are not the Owner.");
                    throw new Exception();
                }
            }
        }

        [Command("remove")]
        public async Task RemoveCMD(CommandContext ctx, DiscordMember userToAdd)
        {
            var v = Dependencies.Settings.Channels.First(x => x.Channel.Id == ctx.Channel.Id);
            if (v == null)
            {
                await ctx.RespondAsync("Didnt Recognize CC");
                throw new Exception();
            }
            else
            {
                if (v.Owner.Id == ctx.Member.Id)
                {
                    await v.Channel.AddOverwriteAsync(userToAdd, Permissions.None, Permissions.None);
                    await ctx.RespondAsync($"Sorry, {userToAdd.Mention} see ya!");
                }
                else
                {
                    await ctx.RespondAsync("You are not the Owner.");
                    throw new Exception();
                }
            }
        }

        [Command("signup")]
        public async Task SignupCMD(CommandContext ctx, DiscordEmoji emoji)
        {
            if (Dependencies.Settings.SignedUpPlayers.Any(x => x.user.Id == ctx.User.Id))
            {
                await ctx.RespondAsync("You are signed up already!");
                throw new Exception();
            }

            if (Dependencies.Settings.SignedUpPlayers.Any(x => x.Emoji.Name == emoji.Name))
            {
                await ctx.RespondAsync("This emoji is already used! Sorry");
                throw new Exception();
            }

            Dependencies.Settings.SignedUpPlayers.Add(new Player(ctx.Member, emoji));
        }

        [Command("signup")]
        public async Task SignupCMD(CommandContext ctx, DiscordMember ToSignup, DiscordEmoji emoji)
        {
            if (!ctx.Member.Roles.Any(x => x.Id == Dependencies.Settings.GameMasterRole.Id))
            {
                await ctx.RespondAsync("Sorry, but you can only signup yourself");
                throw new Exception();
            }

            if (Dependencies.Settings.SignedUpPlayers.Any(x => x.user.Id == ToSignup.Id))
            {
                await ctx.RespondAsync("He is signed up already!");
                throw new Exception();
            }

            if (Dependencies.Settings.SignedUpPlayers.Any(x => x.Emoji.Name == emoji.Name))
            {
                await ctx.RespondAsync("This emoji is already used! Sorry");
                throw new Exception();
            }

            Dependencies.Settings.SignedUpPlayers.Add(new Player(ToSignup, emoji));
        }


        [Command("kill")]
        [Description("Game Master Only. Kills a player.")]
        public async Task KillCMD(CommandContext ctx, DiscordMember member)
        {
            if (!ctx.Member.Roles.Any(x => x.Id == Dependencies.Settings.GameMasterRole.Id))
            {
                await ctx.RespondAsync($"Game Master only {ctx.Member.Mention}!");
                throw new Exception();
            }

            int i = 0;
            foreach (var v in Dependencies.Settings.SignedUpPlayers)
            {
                if (v.user.Id == member.Id)
                {
                    if (v.IsAlive == false)
                    {
                        await ctx.RespondAsync($"{member.Mention} is already Dead.");
                        throw new Exception();
                    }

                    Dependencies.Settings.SignedUpPlayers[i].IsAlive = false;
                    var v2 = Dependencies.Settings.SignedUpPlayers[i].user;

                    var v3 = await Dependencies.Settings.MainGuild.GetMemberAsync(v2.Id);
                    await v3.RevokeRoleAsync(Dependencies.Settings.AliveParticipantRole);
                    await v3.GrantRoleAsync(Dependencies.Settings.DeadParticipantRole);

                    for (int i2 = 0; i2 < Dependencies.Settings.Channels.Count; i2++)
                    {
                        Dependencies.Settings.Channels[i2].Members = Dependencies.Settings.Channels[i2].Members.Where(x => x.Id != v3.Id).ToList();
                    }

                    await ctx.RespondAsync($"Killed {v2.Mention}");
                }
                i++;
            }
        }

        private DiscordEmbed GetListEmbed(CC cc)
        {
            /*embed = discord.Embed(title = "@here a new Conspiracy channel has been made", color = 0xdf0000)
            embed.set_author(name = "[CREATOR]",, icon_url = "http://icons.iconarchive.com/icons/wineass/ios7-redesign/512/Sample-icon.png")
            embed.add_field(name = 1., value =[PERSON], inline = True)
            embed.add_field(name = 2., value =[PERSON], inline = True)
            embed.add_field(name = 3., value =[PERSON], inline = True)
            embed.set_footer(text = "Have fun!")
            await self.bot.say(embed = embed)*/
            DiscordEmbedBuilder v = new DiscordEmbedBuilder();

            v.WithTitle("List of Members:");
            v.WithColor(DiscordColor.DarkRed);
            v.WithFooter("Have fun!");
            v.AddField("Owner", cc.Owner.DisplayName);
            int i = 1;
            foreach (var v2 in cc.Members)
            {
                v.AddField(i.ToString() + ".", v2.DisplayName, i % 3 == 0);
                i++;
            }

            return v.Build();
        }

        [Command("GiveParticipantRoles")]
        [Description("Game Master Only. Hands out the Participant Role.")]
        public async Task GiveRolesCMD(CommandContext ctx)
        {
            if (!ctx.Member.Roles.Any(x => x.Id == Dependencies.Settings.GameMasterRole.Id))
            {
                await ctx.RespondAsync($"Game Master only {ctx.Member.Mention}!");
                throw new Exception();
            }

            foreach (var v in Dependencies.Settings.SignedUpPlayers)
            {
                await v.user.GrantRoleAsync(Dependencies.Settings.AliveParticipantRole);
            }
        }

        private List<DiscordOverwriteBuilder> GetOverwrites(List<DiscordMember> v, DiscordGuild guild, DiscordMember owner)
        {
            v.Add(owner);

            List<DiscordOverwriteBuilder> res = new List<DiscordOverwriteBuilder>();
            //@everyone - READ FALSE, WRITE FALSE
            DiscordOverwriteBuilder v1 = new DiscordOverwriteBuilder();
            v1.For(guild.EveryoneRole);
            v1.Deny(Permissions.AccessChannels);
            v1.Deny(Permissions.SendMessages);
            res.Add(v1);

            //Frozen - WRITE FALSE
            DiscordOverwriteBuilder v7 = new DiscordOverwriteBuilder();
            v7.For(Dependencies.Settings.FrozenPlayerRole);
            v7.Deny(Permissions.SendMessages);
            res.Add(v7);

            //Participant - WRITE TRUE
            DiscordOverwriteBuilder v2 = new DiscordOverwriteBuilder();
            v2.For(Dependencies.Settings.AliveParticipantRole);
            v2.Allow(Permissions.SendMessages);
            res.Add(v2);

            //Dead - Read TRUE, Write FALSE
            DiscordOverwriteBuilder v3 = new DiscordOverwriteBuilder();
            v3.For(Dependencies.Settings.DeadParticipantRole);
            v3.Allow(Permissions.AccessChannels);
            v3.Deny(Permissions.SendMessages);
            res.Add(v3);

            //GM - Read TRUE, Write True
            DiscordOverwriteBuilder v4 = new DiscordOverwriteBuilder();
            v4.For(Dependencies.Settings.GameMasterRole);
            v4.Allow(Permissions.SendMessages);
            v4.Allow(Permissions.AccessChannels);
            res.Add(v4);

            foreach (var v5 in v)
            {
                DiscordOverwriteBuilder v6 = new DiscordOverwriteBuilder();
                v6.For(v5);
                v6.Allow(Permissions.AccessChannels);
                res.Add(v6);
            }

            return res;
        }
    }
}