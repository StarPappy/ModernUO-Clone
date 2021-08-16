namespace Server.Items
{
    [Serializable(0)]
    public partial class SquirrelStatueSouthAddon : BaseAddon
    {
        [Constructible]
        public SquirrelStatueSouthAddon()
        {
            AddComponent(new AddonComponent(0x2D11), 0, 0, 0);
        }

        public override BaseAddonDeed Deed => new SquirrelStatueSouthDeed();
    }

    public class SquirrelStatueSouthDeed : BaseAddonDeed
    {
        [Constructible]
        public SquirrelStatueSouthDeed()
        {
        }

        public override BaseAddon Addon => new SquirrelStatueSouthAddon();
        public override int LabelNumber => 1072884; // squirrel statue (south)
    }
}
