using U5.Core.Formats.Npc;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class NpcParserTests
    {
        [Fact]
        public void Parse_ParsesEightMapScheduleLayout()
        {
            byte[] bytes = new byte[NpcFile.ExpectedFileSize];

            int slotIndex = 1;
            int scheduleOffset = slotIndex * NpcFile.ScheduleSize;
            bytes[scheduleOffset + 0] = 0x01;
            bytes[scheduleOffset + 1] = 0x02;
            bytes[scheduleOffset + 2] = 0x03;
            bytes[scheduleOffset + 3] = 10;
            bytes[scheduleOffset + 4] = 11;
            bytes[scheduleOffset + 5] = 12;
            bytes[scheduleOffset + 6] = 20;
            bytes[scheduleOffset + 7] = 21;
            bytes[scheduleOffset + 8] = 22;
            bytes[scheduleOffset + 9] = 0xFF;
            bytes[scheduleOffset + 10] = 0x00;
            bytes[scheduleOffset + 11] = 0x01;
            bytes[scheduleOffset + 12] = 7;
            bytes[scheduleOffset + 13] = 17;
            bytes[scheduleOffset + 14] = 19;
            bytes[scheduleOffset + 15] = 5;
            bytes[512 + slotIndex] = 84;
            bytes[544 + slotIndex] = 130;

            NpcParser parser = new NpcParser();
            NpcFile file = parser.Parse(bytes, "TOWNE.NPC");

            Assert.True(file.IsExpectedLength);
            Assert.Equal(NpcFileKind.Towne, file.Kind);
            Assert.Equal(8, file.Maps.Count);
            Assert.Equal("Moonglow", file.Maps[0].MapName);

            NpcSlot slot = file.Maps[0].Slots[1];
            Assert.True(slot.HasAnyData);
            Assert.Equal((byte)84, slot.Type);
            Assert.Equal((byte)130, slot.DialogNumber);
            Assert.Equal("Barkeeper", slot.DialogMeaning);
            Assert.Equal(3, slot.Schedule.Stops.Count);
            Assert.Equal((byte)10, slot.Schedule.Stops[0].X);
            Assert.Equal((sbyte)-1, slot.Schedule.Stops[0].Z);
            Assert.Equal(new byte[] { 7, 17, 19, 5 }, slot.Schedule.Times);
        }
    }
}
