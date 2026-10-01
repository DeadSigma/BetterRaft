
namespace BetterRaft
{
    public enum EquippedTankType
    {
        None,
        OxygenBottle,
        OxygenCanister,
        OxygenTank
    }

    public static class OxygenTankManager
    {

        private const float BOTTLE_BONUS_OXYGEN = 200f; 
        private const float BOTTLE_REGEN_RATE = 1.5f; 

        private const float CANISTER_BONUS_OXYGEN = 500f;
        private const float CANISTER_REGEN_RATE = 2.5f;

        private const float TANK_BONUS_OXYGEN = 1200f;
        private const float TANK_REGEN_RATE = 4.0f;
        // -----------------------------------------


        public static EquippedTankType CurrentTank { get; private set; } = EquippedTankType.None;
        private static float currentBonusMaxOxygen = 0f;
        private static float currentOxygenRegenPerSecond = 0f;

        public static void ActivateTank(Item_Base equippedItem)
        {
            if (equippedItem == null)
            {
                DeactivateTank();
                return;
            }

            EquippedTankType detectedTank = EquippedTankType.None;
            switch (equippedItem.UniqueName)
            {
                case "OxygenBottle":
                    detectedTank = EquippedTankType.OxygenBottle;
                    break;
                case "Oxygen_Canister":
                    detectedTank = EquippedTankType.OxygenCanister;
                    break;
                case "Oxygen_Tank":
                    detectedTank = EquippedTankType.OxygenTank;
                    break;
            }

            CurrentTank = detectedTank;

            switch (CurrentTank)
            {
                case EquippedTankType.OxygenBottle:
                    currentBonusMaxOxygen = BOTTLE_BONUS_OXYGEN;
                    currentOxygenRegenPerSecond = BOTTLE_REGEN_RATE;
                    break;
                case EquippedTankType.OxygenCanister:
                    currentBonusMaxOxygen = CANISTER_BONUS_OXYGEN;
                    currentOxygenRegenPerSecond = CANISTER_REGEN_RATE;
                    break;
                case EquippedTankType.OxygenTank:
                    currentBonusMaxOxygen = TANK_BONUS_OXYGEN;
                    currentOxygenRegenPerSecond = TANK_REGEN_RATE;
                    break;
                default:
                    DeactivateTank();
                    break;
            }
        }

        public static void DeactivateTank()
        {
            CurrentTank = EquippedTankType.None;
            currentBonusMaxOxygen = 0f;
            currentOxygenRegenPerSecond = 0f;
        }

        public static float GetBonusMaxOxygen()
        {
            return currentBonusMaxOxygen;
        }

        public static float GetOxygenRegenRate()
        {
            return currentOxygenRegenPerSecond;
        }
    }
}