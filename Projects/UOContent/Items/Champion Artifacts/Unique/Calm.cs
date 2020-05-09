namespace Server.Items
{
  public class Calm : Halberd
  {
    [Constructible]
    public Calm()
    {
      Hue = 0x2cb;

      Attributes.SpellChanneling = 1;
      Attributes.WeaponSpeed = 20;
      Attributes.WeaponDamage = 50;

      WeaponAttributes.HitLeechMana = 100;
      WeaponAttributes.UseBestSkill = 1;
    }

    public Calm(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1094915; // Calm [Replica]

    public override int InitMinHits => 150;
    public override int InitMaxHits => 150;

    public override bool CanFortify => false;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}