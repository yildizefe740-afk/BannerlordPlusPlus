using HarmonyLib;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    public static class KingdomsUI
    {
        private static readonly uint SettlementLightBlue =
            new Color(0.58f, 0.78f, 1f, 1f).ToUnsignedInteger();

        // Snapshot of relation state. Getters read this without performing a scan.
        private class Snapshot
        {
            public Kingdom Player;
            public int Day = -1;
            // Display in blue: allies, vassals, and marches.
            public readonly HashSet<Kingdom> Highlighted = new HashSet<Kingdom>();
            // Use the player's banner colors: vassals and marches only.
            public readonly HashSet<Kingdom> FollowBanner = new HashSet<Kingdom>();
            // Created banners, to avoid generating a new Banner on every call.
            public readonly ConcurrentDictionary<Kingdom, Banner> Banners =
                new ConcurrentDictionary<Kingdom, Banner>();
        }

        private static readonly Snapshot Empty = new Snapshot();
        private static volatile Snapshot _snap = Empty;
        private static volatile bool _dirty = true;
        private static readonly object _lock = new object();

        [System.ThreadStatic] private static bool _building;

        /// <summary>
        /// Call when subjects or alliances change (AddSubject, EndSubject, OnKingdomDestroyed, alliance formed/broken).
        /// If not called, refreshes by the next day at the latest or when the player changes kingdoms.
        /// </summary>
        public static void Invalidate()
        {
            _dirty = true;
        }

        private static Snapshot Current()
        {
            if (Campaign.Current == null)
                return Empty;

            Snapshot s = _snap;
            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
            int day = (int)CampaignTime.Now.ToDays;

            if (!_dirty && s.Player == playerKingdom && s.Day == day)
                return s;

            // If re-entered during a build, use the previous snapshot to avoid an infinite loop.
            if (_building)
                return s;

            lock (_lock)
            {
                s = _snap;
                if (!_dirty && s.Player == playerKingdom && s.Day == day)
                    return s;

                _building = true;
                try
                {
                    _dirty = false;
                    s = Build(playerKingdom, day);
                    _snap = s; // Reference replacement is atomic; readers see either the old or new snapshot.
                }
                finally
                {
                    _building = false;
                }
            }
            return s;
        }

        private static Snapshot Build(Kingdom playerKingdom, int day)
        {
            var s = new Snapshot { Player = playerKingdom, Day = day };
            if (playerKingdom == null)
                return s;

            foreach (Kingdom k in Kingdom.All.ToList())
            {
                if (k == null || k == playerKingdom || k.IsEliminated)
                    continue;

                if (playerKingdom.AlliedKingdoms != null &&
                    playerKingdom.AlliedKingdoms.Any(allied => allied == k))
                {
                    s.Highlighted.Add(k);
                }

                SubjectAttributes subject = SubjectBehavior.GetSubjectBetween(playerKingdom, k);
                if (subject != null &&
                    subject.SeniorKingdom == playerKingdom &&
                    subject.JuniorKingdom == k &&
                    (subject.Type == SubjectType.Vassal || subject.Type == SubjectType.March))
                {
                    s.Highlighted.Add(k);
                    s.FollowBanner.Add(k);
                }
            }
            return s;
        }

        [HarmonyPatch(typeof(Kingdom), "get_Color")]
        public static class KingdomColorPatch
        {
            public static bool Prefix(Kingdom __instance, ref uint __result)
            {
                if (!Current().Highlighted.Contains(__instance))
                    return true;

                __result = SettlementLightBlue;
                return false;
            }
        }

        [HarmonyPatch(typeof(Kingdom), "get_Color2")]
        public static class KingdomColor2Patch
        {
            public static bool Prefix(Kingdom __instance, ref uint __result)
            {
                if (!Current().Highlighted.Contains(__instance))
                    return true;

                __result = SettlementLightBlue;
                return false;
            }
        }

        [HarmonyPatch(typeof(Kingdom), "get_PrimaryBannerColor")]
        public static class KingdomPrimaryBannerColorPatch
        {
            public static bool Prefix(Kingdom __instance, ref uint __result)
            {
                Snapshot s = Current();
                if (!s.FollowBanner.Contains(__instance))
                    return true;

                __result = s.Player.PrimaryBannerColor;
                return false;
            }
        }

        [HarmonyPatch(typeof(Kingdom), "get_SecondaryBannerColor")]
        public static class KingdomSecondaryBannerColorPatch
        {
            public static bool Prefix(Kingdom __instance, ref uint __result)
            {
                Snapshot s = Current();
                if (!s.FollowBanner.Contains(__instance))
                    return true;

                __result = s.Player.SecondaryBannerColor;
                return false;
            }
        }

        [HarmonyPatch(typeof(Kingdom), "get_Banner")]
        public static class KingdomBannerPatch
        {
            public static void Postfix(Kingdom __instance, ref Banner __result)
            {
                if (__result == null)
                    return;

                Snapshot s = Current();
                if (!s.FollowBanner.Contains(__instance))
                    return;

                if (s.Banners.TryGetValue(__instance, out Banner cached))
                {
                    __result = cached;
                    return;
                }

                Banner created = new Banner(
                    __result,
                    s.Player.PrimaryBannerColor,
                    s.Player.SecondaryBannerColor);

                s.Banners[__instance] = created;
                __result = created;
            }
        }
    }
}