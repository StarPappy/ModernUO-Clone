using Server.Items;
using Server.Network;
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public abstract partial class BaseGuildmaster : BaseVendor
{
    public BaseGuildmaster(string title) : base(title) =>
        Title = $"the {title} {(Female ? "guildmistress" : "guildmaster")}";

    protected override List<SBInfo> SBInfos { get; } = new();

    public override bool IsActiveVendor => false;

    public override bool ClickTitle => false;

    public virtual int JoinCost => 500;

    public virtual TimeSpan JoinAge => TimeSpan.FromDays(0.0);
    public virtual TimeSpan JoinGameAge => TimeSpan.FromDays(2.0);
    public virtual TimeSpan QuitAge => TimeSpan.FromDays(7.0);
    public virtual TimeSpan QuitGameAge => TimeSpan.FromDays(4.0);

    public override void InitSBInfo()
    {
    }

    public virtual bool CheckCustomReqs(PlayerMobile pm) => true;

    public virtual void SayGuildTo(Mobile m)
    {
        SayTo(m, 1008055 + (int)NpcGuild);
    }

    public virtual void SayWelcomeTo(Mobile m)
    {
        SayTo(
            m,
            1008054
        ); // Welcome to the guild! Thou shalt find that fellow members shall grant thee lower prices in shops.
    }

    public virtual void SayPriceTo(Mobile m)
    {
        m.NetState.SendMessageLocalizedAffix(
            Serial,
            Body,
            MessageType.Regular,
            SpeechHue,
            3,
            1008052,
            Name,
            AffixType.Append,
            JoinCost.ToString()
        );
    }

    public virtual bool WasNamed(string speech)
    {
        var name = Name;

        return name != null && speech.InsensitiveStartsWith(name);
    }

    public override bool HandlesOnSpeech(Mobile from)
    {
        if (from.InRange(Location, 2))
        {
            return true;
        }

        return base.HandlesOnSpeech(from);
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
        var from = e.Mobile;

        if (!e.Handled && from is PlayerMobile pm && pm.InRange(Location, 2) && WasNamed(e.Speech))
        {
            if (e.HasKeyword(0x0004)) // *join* | *member*
            {
                if (pm.NpcGuild == NpcGuild)
                {
                    SayTo(pm, 501047); // Thou art already a member of our guild.
                }
                else if (pm.NpcGuild != NpcGuild.None)
                {
                    SayTo(pm, 501046); // Thou must resign from thy other guild first.
                }
                else if (pm.GameTime < JoinGameAge || pm.Created + JoinAge > Core.Now)
                {
                    SayTo(pm, 501048); // You are too young to join my guild...
                }
                else if (CheckCustomReqs(pm))
                {
                    SayPriceTo(pm);
                }

                e.Handled = true;
            }
            else if (e.HasKeyword(0x0005)) // *resign* | *quit*
            {
                if (pm.NpcGuild != NpcGuild)
                {
                    SayTo(pm, 501052); // Thou dost not belong to my guild!
                }
                else if (pm.NpcGuildJoinTime + QuitAge > Core.Now ||
                         pm.NpcGuildGameTime + QuitGameAge > pm.GameTime)
                {
                    SayTo(pm, 501053); // You just joined my guild! You must wait a week to resign.
                }
                else
                {
                    SayTo(pm, 501054); // I accept thy resignation.
                    pm.NpcGuild = NpcGuild.None;
                }

                e.Handled = true;
            }
        }

        base.OnSpeech(e);
    }

    public override bool OnGoldGiven(Mobile from, Gold dropped)
    {
        if (from is PlayerMobile pm && dropped.Amount == JoinCost)
        {
            if (pm.NpcGuild == NpcGuild)
            {
                SayTo(pm, 501047); // Thou art already a member of our guild.
            }
            else if (pm.NpcGuild != NpcGuild.None)
            {
                SayTo(pm, 501046); // Thou must resign from thy other guild first.
            }
            else if (pm.GameTime < JoinGameAge || pm.Created + JoinAge > Core.Now)
            {
                SayTo(pm, 501048); // You are too young to join my guild...
            }
            else if (CheckCustomReqs(pm))
            {
                SayWelcomeTo(pm);

                pm.NpcGuild = NpcGuild;
                pm.NpcGuildJoinTime = Core.Now;
                pm.NpcGuildGameTime = pm.GameTime;

                dropped.Delete();
                return true;
            }

            return false;
        }

        return base.OnGoldGiven(from, dropped);
    }
}
