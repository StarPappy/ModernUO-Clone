namespace Server.Items
{
    [Serializable(0, false)]
    [TypeAlias("Server.Items.GargishStuddedKilt")]
    public partial class GargishStuddedKiltType1 : BaseArmor
    {
        [Constructible]
        public GargishStuddedKiltType1() : base(0x288) => Weight = 10.0;

        public override Race RequiredRace => Race.Gargoyle;
        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 6;

        public override int InitMinHits => 40;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 40;
        public override int OldStrReq => 40;

        public override int ArmorBase => 16;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.Half;
    }
}
