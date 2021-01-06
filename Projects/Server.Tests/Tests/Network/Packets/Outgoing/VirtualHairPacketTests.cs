using System;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests
{
    public class VirtualHairPacketTests: IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestSendVirtualHairUpdate()
        {
            var m = new Mobile(0x1024u);
            m.DefaultMobileInit();

            var expected = new HairEquipUpdate(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendVirtualHairEquipUpdatePacket(m, HairInfo.FakeSerial(m.Serial), Layer.Hair);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestSendRemoveVirtualHair()
        {
            var m = new Mobile(0x1024u);
            m.DefaultMobileInit();

            var expected = new RemoveHair(m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendRemoveVirtualHairPacket(HairInfo.FakeSerial(m.Serial));

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
