namespace Server.Mobiles
{
    public class Minter : Banker
    {
        [Constructible]
        public Minter() => Title = "the minter";

        public Minter(Serial serial) : base(serial)
        {
        }

        public override NpcGuild NpcGuild => NpcGuild.MerchantsGuild;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
