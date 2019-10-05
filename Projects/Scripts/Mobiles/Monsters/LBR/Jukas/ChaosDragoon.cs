using Server.Items;

namespace Server.Mobiles
{
  public class ChaosDragoon : BaseCreature
  {
    [Constructible]
    public ChaosDragoon() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.15, 0.4)
    {
      Body = 0x190;
      Hue = Race.Human.RandomSkinHue();

      SetStr(176, 225);
      SetDex(81, 95);
      SetInt(61, 85);

      SetHits(176, 225);

      SetDamage(24, 26);

      SetDamageType(ResistanceType.Physical, 25);
      SetDamageType(ResistanceType.Fire, 25);
      SetDamageType(ResistanceType.Cold, 25);
      SetDamageType(ResistanceType.Energy, 25);

      //SetResistance( ResistanceType.Physical, 25, 38 );
      //SetResistance( ResistanceType.Fire, 25, 38 );
      //SetResistance( ResistanceType.Cold, 25, 38 );
      //SetResistance( ResistanceType.Poison, 25, 38 );
      //SetResistance( ResistanceType.Energy, 25, 38 );

      SetSkill(SkillName.Fencing, 77.6, 92.5);
      SetSkill(SkillName.Healing, 60.3, 90.0);
      SetSkill(SkillName.Macing, 77.6, 92.5);
      SetSkill(SkillName.Anatomy, 77.6, 87.5);
      SetSkill(SkillName.MagicResist, 77.6, 97.5);
      SetSkill(SkillName.Swords, 77.6, 92.5);
      SetSkill(SkillName.Tactics, 77.6, 87.5);

      Fame = 5000;
      Karma = -5000;

      var res = Utility.Random(6) switch
      {
        0 => CraftResource.BlackScales,
        1 => CraftResource.RedScales,
        2 => CraftResource.BlueScales,
        3 => CraftResource.YellowScales,
        4 => CraftResource.GreenScales,
        5 => CraftResource.WhiteScales,
        _ => CraftResource.None
      };

      var melee = Utility.Random(3) switch
      {
        0 => (BaseWeapon)new Kryss(),
        1 => new Broadsword(),
        2 => new Katana(),
        _ => null
      };

      melee.Movable = false;
      AddItem(melee);

      DragonHelm helm = new DragonHelm();
      helm.Resource = res;
      helm.Movable = false;
      AddItem(helm);

      DragonChest chest = new DragonChest();
      chest.Resource = res;
      chest.Movable = false;
      AddItem(chest);

      DragonArms arms = new DragonArms();
      arms.Resource = res;
      arms.Movable = false;
      AddItem(arms);

      DragonGloves gloves = new DragonGloves();
      gloves.Resource = res;
      gloves.Movable = false;
      AddItem(gloves);

      DragonLegs legs = new DragonLegs();
      legs.Resource = res;
      legs.Movable = false;
      AddItem(legs);

      ChaosShield shield = new ChaosShield();
      shield.Movable = false;
      AddItem(shield);

      AddItem(new Shirt());
      AddItem(new Boots());

      int amount = Utility.RandomMinMax(1, 3);

      switch (res)
      {
        case CraftResource.BlackScales:
          AddItem(new BlackScales(amount));
          break;
        case CraftResource.RedScales:
          AddItem(new RedScales(amount));
          break;
        case CraftResource.BlueScales:
          AddItem(new BlueScales(amount));
          break;
        case CraftResource.YellowScales:
          AddItem(new YellowScales(amount));
          break;
        case CraftResource.GreenScales:
          AddItem(new GreenScales(amount));
          break;
        case CraftResource.WhiteScales:
          AddItem(new WhiteScales(amount));
          break;
      }

      new SwampDragon().Rider = this;
    }

    public ChaosDragoon(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a chaos dragoon corpse";
    public override string DefaultName => "a chaos dragoon";

    public override bool HasBreath => true;
    public override bool AutoDispel => true;
    public override bool BardImmune => !Core.AOS;
    public override bool CanRummageCorpses => true;
    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;

    public override int GetIdleSound() => 0x2CE;

    public override int GetDeathSound() => 0x2CC;

    public override int GetHurtSound() => 0x2D1;

    public override int GetAttackSound() => 0x2C8;

    public override void GenerateLoot()
    {
      AddLoot(LootPack.Rich);
      //AddLoot( LootPack.Gems );
    }

    public override bool OnBeforeDeath()
    {
      IMount mount = Mount;

      if (mount != null)
        mount.Rider = null;

      return base.OnBeforeDeath();
    }

    public override void AlterMeleeDamageTo(Mobile to, ref int damage)
    {
      if (to is Dragon || to is WhiteWyrm || to is SwampDragon || to is Drake || to is Nightmare || to is Hiryu ||
          to is LesserHiryu || to is Daemon)
        damage *= 3;
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
    }
  }
}