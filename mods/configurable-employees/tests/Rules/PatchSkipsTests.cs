using System.Collections.Generic;
using Xunit;

namespace ConfigurableEmployees.Tests
{
    public class PatchSkipsTests
    {
        [Theory]
        [InlineData("Botanist_TaskDuration_Patch", "TaskDuration", true)]
        [InlineData("Botanist_TaskDuration_Patch", " mousescrolldelta , taskduration ", true)]
        [InlineData("GameInput_MouseScrollDelta_Patch", "TaskDuration;MouseScrollDelta", true)]
        [InlineData("NPC_InitializeNPC_Patch", "TaskDuration", false)]
        [InlineData("NPC_InitializeNPC_Patch", "", false)]
        [InlineData("NPC_InitializeNPC_Patch", " , ;", false)]
        [InlineData("NPC_InitializeNPC_Patch", null, false)]
        public void MatchesPartOfTheName(string patch, string list, bool expected)
            => Assert.Equal(expected, PatchSkips.IsSkipped(patch, list));
    }
}
