using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.UI;

namespace ConfigurableAtm
{
    /// <summary>
    /// Every read of or call into the game that is the same for both builds goes through here. This
    /// file holds the runtime differences between Mono and IL2CPP, so the shared patches stay identical.
    /// </summary>
    internal static class GameAccess
    {
#if !IL2CPP
        // Protected field on Mono; the IL2CPP proxy exposes it as a public property.
        private static readonly AccessTools.FieldRef<ATMInterface, RectTransform> LimitContainerRef =
            AccessTools.FieldRefAccess<ATMInterface, RectTransform>("depositLimitContainer");

        private static readonly AccessTools.FieldRef<ATMInterface, List<Button>> AmountButtonsRef =
            AccessTools.FieldRefAccess<ATMInterface, List<Button>>("amountButtons");
#endif

        /// <summary>
        /// True once an ATM screen has started. Until then there is nothing to relabel, and the
        /// game's Unity side may not be ready to be searched.
        /// </summary>
        public static bool ScreenStarted { get; set; }

        /// <summary>
        /// Writes the configured amounts into the game's button amounts, in place. The game reads
        /// them for each button's label (once, when the ATM screen starts) and on every click.
        /// </summary>
        public static void ApplyPresets(int[] presets)
        {
            var amounts = ATMInterface.amounts;
            int count = System.Math.Min(amounts.Length, presets.Length);
            for (int i = 0; i < count; i++) amounts[i] = presets[i];
        }

        /// <summary>
        /// Writes the configured amounts into the game's and relabels every ATM's buttons, for a
        /// change made while the game runs (the game labels them only as the screen starts). The last
        /// button is left alone: the game sets its label each time the screen opens.
        /// </summary>
        public static void RefreshPresets(int[] presets)
        {
            if (!ScreenStarted) return;

            ApplyPresets(presets);
            foreach (ATMInterface atm in Object.FindObjectsOfType<ATMInterface>())
            {
#if IL2CPP
                var buttons = atm.amountButtons;
#else
                var buttons = AmountButtonsRef(atm);
#endif
                for (int i = 0; i < buttons.Count - 1 && i < presets.Length; i++)
                    buttons[i].transform.Find("Text").GetComponent<Text>().text = MoneyManager.FormatAmount(presets[i]);
            }
        }

        /// <summary>Hides or shows the "spent / limit" readout in the ATM's top bar.</summary>
        public static void SetLimitDisplayVisible(ATMInterface atm, bool visible)
        {
#if IL2CPP
            RectTransform container = atm.depositLimitContainer;
#else
            RectTransform container = LimitContainerRef(atm);
#endif
            if (container != null) container.gameObject.SetActive(visible);
        }
    }
}
