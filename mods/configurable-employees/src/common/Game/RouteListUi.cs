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
    /// For a controller, the game does not use plain Unity navigation: each screen's UIPanel keeps an
    /// explicit list of the elements it can move between, filled in the editor. The extra rows are not
    /// in it, so they are added (see RegisterWithPanel), and the scroll area follows the selection (see
    /// ScrollIntoView) because the panel only knows how to scroll a scroll area of its own.
    ///
    /// Untested in the game: the sizes below are the ones the older employee mods used.
    /// </summary>
    internal static class RouteListUi
    {
        /// <summary>True while the management screen is open (set by the Open and Close patches).</summary>
        public static bool ScreenOpen;

        /// <summary>True once the routes list has been given a scroll area, so the wheel is ours.</summary>
        public static bool Extended;

        // The scroll area made by MakeScrollable, so a selection can be scrolled into view.
        private static ScrollRect _scroll;

        public static void Extend(RouteListFieldUI ui, int wanted)
        {
            if (ui == null || ui.RouteEntries == null || ui.RouteEntries.Length == 0) return;

            var rows = new List<RouteEntryUI>();
            for (int i = 0; i < ui.RouteEntries.Length; i++) rows.Add(ui.RouteEntries[i]);

            int toAdd = RouteRows.RowsToAdd(wanted, rows.Count);
            if (toAdd == 0) return;

            RouteEntryUI last = rows[rows.Count - 1];
            Transform contents = last.transform.parent;
            var added = new List<RouteEntryUI>();
            for (int i = 0; i < toAdd; i++)
            {
                GameObject copy = Object.Instantiate(last.gameObject, contents);
                copy.name = "Entry (" + rows.Count + ")";
                RouteEntryUI row = copy.GetComponent<RouteEntryUI>();
                rows.Add(row);
                added.Add(row);
            }

#if IL2CPP
            ui.RouteEntries = new Il2CppReferenceArray<RouteEntryUI>(rows.ToArray());
#else
            ui.RouteEntries = rows.ToArray();
#endif

            // The "add route" button sits after the rows; keep it there.
            if (ui.AddButton != null && ui.AddButton.transform.parent == contents) ui.AddButton.transform.SetAsLastSibling();

            MakeScrollable(ui, contents);
            RegisterWithPanel(ui, last, added);
            Extended = true;
            ModLog.Info("Routes list now has " + rows.Count + " rows.");
        }

        /// <summary>
        /// Adds the extra rows' buttons to the game's gamepad navigation. They belong in the same
        /// UIPanel as the original rows' buttons, and a panel sets itself as the ParentPanel of every
        /// button it lists, so the original row says which panel that is. Without this the extra rows
        /// cannot be selected with a controller.
        /// </summary>
        private static void RegisterWithPanel(RouteListFieldUI ui, RouteEntryUI original, List<RouteEntryUI> added)
        {
            UIPanel panel = null;
            foreach (UISelectable selectable in original.GetComponentsInChildren<UISelectable>(true))
            {
                if (selectable.ParentPanel != null) { panel = selectable.ParentPanel; break; }
            }

            if (panel == null)
            {
                // The assumption behind this fix did not hold; say so rather than guess at a panel.
                ModLog.Warning("Routes list: the original rows' buttons belong to no panel, so the extra rows will not work with a controller.");
                return;
            }

            int count = 0;
            foreach (RouteEntryUI row in added)
                foreach (UISelectable selectable in row.GetComponentsInChildren<UISelectable>(true))
                    if (panel.AddSelectable(selectable)) count++;
            ModLog.Info("Routes list: added " + count + " buttons of the extra rows to the controller navigation of panel '" + panel.name + "'.");
        }

        /// <summary>
        /// A selectable was selected (a controller moved onto it): if it is in the routes list, scroll
        /// the list so it is visible. Called for every selection in the game, so it leaves at once for
        /// anything else.
        /// </summary>
        public static void ScrollIntoView(UISelectable selectable)
        {
            if (!Extended || _scroll == null || selectable == null) return;

            RectTransform content = _scroll.content;
            RectTransform viewport = _scroll.viewport;
            if (content == null || viewport == null || !selectable.transform.IsChildOf(content)) return;

            // The rows may have just been shown, so let the layout settle before measuring.
            Canvas.ForceUpdateCanvases();
            RectTransform row = selectable.RectTransform;
            Vector2 rowMin = viewport.InverseTransformPoint(row.TransformPoint(row.rect.min));
            Vector2 rowMax = viewport.InverseTransformPoint(row.TransformPoint(row.rect.max));

            float delta = RouteRows.ScrollDelta(rowMin.y, rowMax.y, viewport.rect.min.y, viewport.rect.max.y);
            if (delta == 0f) return;

            Vector3 direction = viewport.TransformDirection(new Vector3(0f, delta, 0f));
            content.localPosition -= content.InverseTransformDirection(direction);
        }

        private static void MakeScrollable(RouteListFieldUI ui, Transform contents)
        {
            Transform existing = ui.transform.Find("ScrollArea");
            if (existing != null)
            {
                _scroll = existing.GetComponent<ScrollRect>();
                return;
            }

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
            _scroll = scroll;
        }
    }
}
