namespace Server.Items
{
    public class LordBritishSuit : BaseSuit
    {
        [Constructible]
        public LordBritishSuit() : base(AccessLevel.GameMaster, 0x0, 0x2042)
        {
        }

        public LordBritishSuit(Serial serial) : base(serial)
        {
        }

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
