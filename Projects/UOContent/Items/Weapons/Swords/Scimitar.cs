namespace Server.Items
{
  [Flippable(0x13B6, 0x13B5)]
  public class Scimitar : BaseSword
  {
    [Constructible]
    public Scimitar() : base(0x13B6) => Weight = 5.0;

    public Scimitar(Serial serial) : base(serial)
    {
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ParalyzingBlow;

    public override int AosStrengthReq => 25;
    public override int AosMinDamage => 13;
    public override int AosMaxDamage => 15;
    public override int AosSpeed => 37;
    public override float MlSpeed => 3.00f;

    public override int OldStrengthReq => 10;
    public override int OldMinDamage => 4;
    public override int OldMaxDamage => 30;
    public override int OldSpeed => 43;

    public override int DefHitSound => 0x23B;
    public override int DefMissSound => 0x23A;

    public override int InitMinHits => 31;
    public override int InitMaxHits => 90;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}