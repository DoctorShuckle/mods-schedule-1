using System;

namespace ConfigurableEmployees
{
    /// <summary>The troubleshooting list of patches to leave off (SkipPatches in the cfg).</summary>
    internal static class PatchSkips
    {
        /// <summary>
        /// True when the troubleshooting list (comma or semicolon separated) names this patch.
        /// A name matches any part of the patch class's name, without regard to case, so
        /// "TaskDuration" and "Botanist_TaskDuration_Start_Patch" both select the botanist timings.
        /// </summary>
        public static bool IsSkipped(string patchName, string skipList)
        {
            if (string.IsNullOrWhiteSpace(patchName) || string.IsNullOrWhiteSpace(skipList)) return false;
            foreach (string part in skipList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = part.Trim();
                if (name.Length > 0 && patchName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
