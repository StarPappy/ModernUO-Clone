namespace Server.Items
{
    [Serializable(1)]
    public partial class TestItem1 : Item
    {
        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.Administrator)]")]
        private int _someProperty;
    }
}
