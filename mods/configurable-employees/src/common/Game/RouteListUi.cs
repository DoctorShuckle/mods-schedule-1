using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

namespace ConfigurableEmployees
{
    /// <summary>
    /// The packager's management screen has a fixed number of route rows (five). More routes need
    /// more rows, and more than a few rows need a scroll area to fit the panel. The rows and the
    /// layout live in the scene, not in code, so this clones the last row and wraps the list in a
    /// ScrollRect. It changes nothing unless more than the game's rows are asked for.
    ///
    /// Untested in the game: the sizes below are the ones the older employee mods used.
    /// </summary>
    internal static class RouteListUi
    {
        /// <summary>True while the management screen is open (set by the Open and Close patches).</summary>
        public static bool ScreenOpen;

        /// <summary>True once the routes list has been given a scroll area, so the wheel is ours.</summary>
        public static bool Extended;

        public static void Extend(RouteListFieldUI ui, int wanted)
        {
            if (ui == null || ui.RouteEntries == null || ui.RouteEntries.Length == 0) return;

            var rows = new List<RouteEntryUI>();
            for (int i = 0; i < ui.RouteEntries.Length; i++) rows.Add(ui.RouteEntries[i]);

            int toAdd = RouteRows.RowsToAdd(wanted, rows.Count);
            if (toAdd == 0) return;

            RouteEntryUI last = rows[rows.Count - 1];
            Transform contents = last.transform.parent;
            for (int i = 0; i < toAdd; i++)
            {
                GameObject copy = Object.Instantiate(last.gameObject, contents);
                copy.name = "Entry (" + rows.Count + ")";
                rows.Add(copy.GetComponent<RouteEntryUI>());
            }

#if IL2CPP
            ui.RouteEntries = new Il2CppReferenceArray<RouteEntryUI>(rows.ToArray());
#else
            ui.RouteEntries = rows.ToArray();
#endif

            // The "add route" button sits after the rows; keep it there.
            if (ui.AddButton != null && ui.AddButton.transform.parent == contents) ui.AddButton.transform.SetAsLastSibling();

            MakeScrollable(ui, contents);
            Extended = true;
            ModLog.Info("Routes list now has " + rows.Count + " rows.");
        }

        private static void MakeScrollable(RouteListFieldUI ui, Transform contents)
        {
            if (ui.transform.Find("ScrollArea") != null) return;

            ui.FieldText = "Routes (scroll for more)";

            var area = new GameObject("ScrollArea");
            area.AddComponent<RectTransform>();
            area.transform.SetParent(ui.transform, false);
            area.AddComponent<RectMask2D>();
            RectTransform areaRect = area.GetComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0f, 1f);
            areaRect.anchorMax = new Vector2(1f, 1f);
            areaRect.pivot = new Vector2(0f, 1f);
            areaRect.anchoredPosition = new Vector2(0f, -62.52f);
            areaRect.sizeDelta = new Vector2(0f, 170f);

            ContentSizeFitter fitter = contents.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contents.SetParent(area.transform, false);
            RectTransform contentsRect = contents.GetComponent<RectTransform>();
            contentsRect.pivot = new Vector2(0f, 1f);
            contentsRect.anchoredPosition = Vector2.zero;

            ScrollRect scroll = area.AddComponent<ScrollRect>();
            scroll.content = contentsRect;
            scroll.viewport = areaRect;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 5f;
        }
    }
}
