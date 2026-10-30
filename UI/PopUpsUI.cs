using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    public static class PupUpsUI
    {
        /// <summary>
        /// Shows the player a popup asking whether to join a war.
        /// </summary>
        /// <param name="callingKingdom">The allied kingdom requesting support.</param>
        /// <param name="targetKingdom">The kingdom involved in the war.</param>
        /// <param name="isDefensive">Whether the call is defensive or offensive.</param>
        /// <param name="onAccepted">The action to run when the player accepts.</param>
        /// <param name="onRejected">The action to run when the player declines.</param>
        public static void ShowCallToWarPopup(
            Kingdom callingKingdom,
            Kingdom targetKingdom,
            bool isDefensive,
            Action onAccepted,
            Action onRejected)
        {
            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
            if (playerKingdom == null) return;
            if (callingKingdom == null || targetKingdom == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Cannot show the war call: a kingdom is missing.",
                    Colors.Red));
                return;
            }

            string warTypeTitle = isDefensive ? "Defensive War Call!" : "Offensive War Call!";
            string warTypeText = isDefensive
                ? $"Our ally {callingKingdom.Name} has been attacked by {targetKingdom.Name}!"
                : $"Our ally {callingKingdom.Name} is launching an offensive campaign against {targetKingdom.Name}!";

            string detailedMessage = $"{warTypeText}\n\nDo you wish to answer the call to arms and join them in this war?";

            InformationManager.ShowInquiry(new InquiryData(
                warTypeTitle,
                detailedMessage,
                true,
                true,
                "Yes, we march!",
                "No, decline",
                () => onAccepted?.Invoke(),
                () => onRejected?.Invoke()
            ));
        }
    }
}