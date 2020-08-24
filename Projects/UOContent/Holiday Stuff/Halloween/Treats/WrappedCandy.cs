namespace Server.Items
{
    public class WrappedCandy : CandyCane
    {
        [Constructible]
        public WrappedCandy(int amount = 1)
            : base(0x469e) =>
            Stackable = true;

        public WrappedCandy(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1096950; /* wrapped candy */

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
