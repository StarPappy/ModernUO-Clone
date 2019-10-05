using System;
using System.Collections.Generic;
using System.Linq;
using Server.Factions;
using Server.Spells;
using Server.Targeting;

namespace Server
{
  public sealed class StormsEye : PowerFactionItem
  {
    public StormsEye()
      : base(3967) =>
      Hue = 1165;

    public StormsEye(Serial serial)
      : base(serial)
    {
    }

    public override string DefaultName => "storms eye";

    public override bool Use(Mobile user)
    {
      if (Movable)
        user.BeginTarget(12, true, TargetFlags.None, delegate(Mobile from, object obj)
        {
          if (Movable && !Deleted)
            if (obj is IPoint3D pt)
            {
              SpellHelper.GetSurfaceTop(ref pt);

              Point3D origin = new Point3D(pt);
              Map facet = from.Map;

              if (facet?.CanFit(pt.X, pt.Y, pt.Z, 16, false, false) != true)
                return;

              Movable = false;

              Effects.SendMovingEffect(
                from, new Entity(Serial.Zero, origin, facet),
                ItemID & 0x3FFF, 7, 0, false, false, Hue - 1
              );

              Timer.DelayCall(TimeSpan.FromSeconds(0.5), delegate
              {
                Delete();

                Effects.PlaySound(origin, facet, 530);
                Effects.PlaySound(origin, facet, 263);

                Effects.SendLocationEffect(
                  origin, facet,
                  14284, 96, 1, 0, 2
                );

                Timer.DelayCall(TimeSpan.FromSeconds(1.0), delegate
                {
                  List<Mobile> targets = facet.GetMobilesInRange(origin, 12).Where(mob =>
                    from.CanBeHarmful(mob, false) && mob.InLOS(new Point3D(origin, origin.Z + 1)) &&
                    Faction.Find(mob) != null).ToList();

                  foreach (Mobile mob in targets)
                  {
                    int damage = mob.Hits * 6 / 10;

                    if (!mob.Player && damage < 10)
                      damage = 10;
                    else if (damage > 75)
                      damage = 75;

                    Effects.SendMovingEffect(
                      new Entity(Serial.Zero, new Point3D(origin, origin.Z + 4), facet), mob,
                      14068, 1, 32, false, false, 1111, 2
                    );

                    from.DoHarmful(mob);

                    SpellHelper.Damage(TimeSpan.FromSeconds(0.50), mob, from, damage / 3.0, 0, 0, 0, 0,
                      100);
                    SpellHelper.Damage(TimeSpan.FromSeconds(0.70), mob, from, damage / 3.0, 0, 0, 0, 0,
                      100);
                    SpellHelper.Damage(TimeSpan.FromSeconds(1.00), mob, from, damage / 3.0, 0, 0, 0, 0,
                      100);

                    Timer.DelayCall(TimeSpan.FromSeconds(0.50), delegate { mob.PlaySound(0x1FB); });
                  }
                });
              });
            }
        });

      return false;
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}
