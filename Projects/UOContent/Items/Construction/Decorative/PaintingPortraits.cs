namespace Server.Items
{
    [Serializable(0, false)]
    public partial class LargePainting : Item
    {
        [Constructible]
        public LargePainting() : base(0x0EA0) => Movable = false;
    }

    [Serializable(0, false)]
    [Flippable(0x0E9F, 0x0EC8)]
    public partial class WomanPortrait1 : Item
    {
        [Constructible]
        public WomanPortrait1() : base(0x0E9F) => Movable = false;
    }

    [Serializable(0, false)]
    [Flippable(0x0EE7, 0x0EC9)]
    public partial class WomanPortrait2 : Item
    {
        [Constructible]
        public WomanPortrait2() : base(0x0EE7) => Movable = false;
    }

    [Serializable(0, false)]
    [Flippable(0x0EA2, 0x0EA1)]
    public partial class ManPortrait1 : Item
    {
        [Constructible]
        public ManPortrait1() : base(0x0EA2) => Movable = false;
    }

    [Serializable(0, false)]
    [Flippable(0x0EA3, 0x0EA4)]
    public partial class ManPortrait2 : Item
    {
        [Constructible]
        public ManPortrait2() : base(0x0EA3) => Movable = false;
    }

    [Serializable(0, false)]
    [Flippable(0x0EA6, 0x0EA5)]
    public partial class LadyPortrait1 : Item
    {
        [Constructible]
        public LadyPortrait1() : base(0x0EA6) => Movable = false;

        public LadyPortrait1(Serial serial) : base(serial)
        {
        }
    }

    [Serializable(0, false)]
    [Flippable(0x0EA7, 0x0EA8)]
    public partial class LadyPortrait2 : Item
    {
        [Constructible]
        public LadyPortrait2() : base(0x0EA7) => Movable = false;
    }
}
