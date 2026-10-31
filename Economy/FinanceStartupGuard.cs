using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BannerlordPlusPlus
{
    internal static class FinanceStartupGuard
    {
        private static readonly string HarmonyId = "com.bannerlordplusplus.patch";
        private static MethodInfo _updatePlayerInfoMethod;
        private static object _deferredInstance;
        private static bool _deferredForceUpdate;
        private static double _campaignStartDay;
        private static bool _campaignStarted;

        public static void ApplyPatch()
        {
            Type mapInfoType = AccessTools.TypeByName(
                "TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar.MapInfoVM");
            if (mapInfoType == null)
                throw new TypeLoadException(
                    "Could not find TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar.MapInfoVM.");

            _updatePlayerInfoMethod = AccessTools.Method(
                mapInfoType,
                "UpdatePlayerInfo",
                new[] { typeof(bool) });
            if (_updatePlayerInfoMethod == null)
                throw new MissingMethodException(mapInfoType.FullName, "UpdatePlayerInfo");

            var harmony = new Harmony(HarmonyId);
            harmony.Patch(
                _updatePlayerInfoMethod,
                prefix: new HarmonyMethod(
                    typeof(FinanceStartupGuard),
                    nameof(UpdatePlayerInfoPrefix)));
        }

        public static void BeginCampaign()
        {
            _campaignStartDay = CampaignTime.Now.ToDays;
            _campaignStarted = true;
            _deferredInstance = null;
            _deferredForceUpdate = false;
        }

        public static bool UpdatePlayerInfoPrefix(object __instance, bool __0)
        {
            if (TreasuryBehavior.EnsureDelayedPatchesApplied(refreshDeferredUpdate: false))
            {
                if (ReferenceEquals(_deferredInstance, __instance))
                {
                    _deferredInstance = null;
                    _deferredForceUpdate = false;
                }

                return true;
            }

            _deferredInstance = __instance;
            _deferredForceUpdate = __0;
            return false;
        }

        public static void RefreshDeferredUpdate()
        {
            object instance = _deferredInstance;
            if (instance == null)
                return;

            bool forceUpdate = _deferredForceUpdate;
            _deferredInstance = null;
            _deferredForceUpdate = false;
            _updatePlayerInfoMethod.Invoke(instance, new object[] { forceUpdate });
        }

        public static bool IsFinanceReady
        {
            get
            {
                if (!_campaignStarted ||
                    Campaign.Current == null ||
                    CampaignTime.Now.ToDays - _campaignStartDay < 1.0)
                {
                    return false;
                }

                return Game.Current?.GameTextManager != null;
            }
        }
    }
}
