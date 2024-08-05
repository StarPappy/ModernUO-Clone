using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator( 0 )]
[Flippable( 0x2B69, 0x3160 )]
public partial class DarkwoodGorget : WoodlandGorget
{
    [Constructible]
    public DarkwoodGorget()
    {
        Hue = 0x455;
        SetHue = 0x494;

        Attributes.BonusHits = 2;
        Attributes.DefendChance = 5;

        SetAttributes.ReflectPhysical = 25;
        SetAttributes.BonusStr = 10;
        SetAttributes.Luck = 100;

        SetSelfRepair = 3;

        SetPhysicalBonus = 2;
        SetFireBonus = 5;
        SetColdBonus = 5;
        SetPoisonBonus = 3;
        SetEnergyBonus = 5;
    }

    public DarkwoodGorget( Serial serial )
        : base( serial )
    {
    }

    public override bool IsArtifact => true;

    public override int LabelNumber => 1073483; // Darkwood Gorget
    public override SetItem SetID => SetItem.Darkwood;
    public override int Pieces => 6;
    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 5;
    public override int BasePoisonResistance => 7;
    public override int BaseEnergyResistance => 5;

    public override int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem,
        int resHue
    )
    {
        if ( resHue > 0 )
        {
            Hue = resHue;
        }

        var resourceType = typeRes;

        if ( resourceType == null )
        {
            resourceType = craftItem.Resources[0].ItemType;
        }

        Resource = CraftResources.GetFromType( resourceType );

        switch ( Resource )
        {
            case CraftResource.Bloodwood:
                Attributes.RegenHits = 2;
                break;
            case CraftResource.Heartwood:
                Attributes.Luck = 40;
                break;
            case CraftResource.YewWood:
                Attributes.RegenHits = 1;
                break;
        }

        return 0;
    }
}
