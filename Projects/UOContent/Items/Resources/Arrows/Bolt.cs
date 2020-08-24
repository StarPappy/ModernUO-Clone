namespace Server.Items
{
    public class Bolt : Item, ICommodity
    {
        [Constructible]
        public Bolt(int amount = 1) : base(0x1BFB)
        {
            Stackable = true;
            Amount = amount;
        }

        public Bolt(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;
        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;

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
