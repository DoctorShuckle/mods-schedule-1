using System;
using System.Collections.Generic;

namespace TeleportToProperties
{
    /// <summary>
    /// The "TP" icon on the phone's home screen. Pressing it opens the game's own list popup of the
    /// player's places; choosing one teleports there and closes the phone.
    /// </summary>
    internal static class TeleportApp
    {
        private static GameObject _icon;
        private static bool _failureLogged;

        /// <summary>
        /// Adds the icon if it is not there. Called each time the phone opens: the home screen is built
        /// by then, and a new save (a new scene) has dropped the old icon along with the old phone.
        /// </summary>
        public static void EnsureIcon()
        {
            if (_icon != null) return;
            try
            {
                _icon = CreateIcon();
                if (_icon != null) ModLog.Verbose("Added the TP icon to the home screen.");
            }
            catch (Exception ex)
            {
                // Fail open: no icon, the hotkeys still work. Said once, not at every phone open.
                if (!_failureLogged) ModLog.Warning("Could not add the TP icon: " + ex.Message);
                _failureLogged = true;
            }
        }

        private static GameObject CreateIcon()
        {
            if (!PlayerSingleton<GameHomeScreen>.InstanceExists) return null;
            GameHomeScreen home = PlayerSingleton<GameHomeScreen>.Instance;

            Transform container = FindDescendant(home.transform, "AppIcons");
            if (container == null || container.childCount == 0)
            {
                if (!_failureLogged) ModLog.Warning("The home screen has no app icons to copy; the TP icon is not added.");
                _failureLogged = true;
                return null;
            }

            // Copy a vanilla icon (the first one) so the new one looks, scales and navigates like the rest.
            GameObject icon = UnityEngine.Object.Instantiate(container.GetChild(0).gameObject, container);
            icon.name = "TeleportToPropertiesIcon";

            Text label = icon.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.text = "Teleport";

            Image face = icon.transform.Find("Mask/Image")?.GetComponent<Image>();
            if (face != null)
            {
                face.sprite = null;
                face.color = new Color(0.12f, 0.38f, 0.62f, 1f);
                AddFaceText(face.transform, label != null ? label.font : null);
            }

            Button button = icon.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            GamePhoneAccess.OnClick(button, OpenList);

            // Joins the home screen's own controller navigation, so the stick can land on the icon.
            UIPanel panel = home.GetComponentInChildren<UIPanel>(true);
            UISelectable selectable = icon.GetComponent<UISelectable>();
            if (panel != null && selectable != null) panel.AddSelectable(selectable);
            else ModLog.Warning("The TP icon could not join the home screen's controller navigation; use the mouse or the hotkeys.");

            return icon;
        }

        private static void AddFaceText(Transform face, Font font)
        {
            var go = new GameObject("TP");
            go.layer = face.gameObject.layer;
            go.transform.SetParent(face, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = go.AddComponent<Text>();
            if (font != null) text.font = font;
            text.text = "TP";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 44;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private static void OpenList()
        {
            List<Place> places = GameAccess.OwnedPlaces();
            if (places.Count == 0)
            {
                Toast.Show("You own no properties yet");
                return;
            }

            var names = new List<string>(places.Count);
            var codes = new List<string>(places.Count);
            foreach (Place place in places)
            {
                names.Add(place.IsBusiness ? place.Name + "  (business)" : place.Name);
                codes.Add(place.Code);
            }

            int selected = Math.Max(0, Selection.Resolve(codes, Travel.PickedCode));
            GamePhoneAccess.OpenList(names, selected, index =>
            {
                if (index < 0 || index >= places.Count) return;
                // Close the phone first so the player is not left looking at it when they arrive.
                GamePhoneAccess.CloseMenu();
                Travel.Go(places[index]);
            });
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendant(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
