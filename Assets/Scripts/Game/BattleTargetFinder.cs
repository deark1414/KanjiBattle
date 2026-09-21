using System.Collections.Generic;

// 旧呼び出し元との互換性を保つための委譲層。
// 対象選択の実装は TargetingService に一本化する。
public static class BattleTargetFinder
{
    public static BattleCharacter FindAdjacentEnemy(BattleManager bm, BattleCharacter self) =>
        TargetingService.FindAdjacentEnemy(bm, self);

    public static BattleCharacter FindAdjacentAlly(BattleManager bm, BattleCharacter self) =>
        TargetingService.FindAdjacentAlly(bm, self);

    public static BattleCharacter FindNearestEnemy(BattleManager bm, BattleCharacter self, List<BattleCharacter> candidates) =>
        TargetingService.FindNearestEnemy(bm, self, candidates);

    public static BattleCharacter FindNearestAlly(BattleManager bm, BattleCharacter self, List<BattleCharacter> candidates) =>
        TargetingService.FindNearestAlly(bm, self, candidates);

    public static BattleCharacter FindSwordTarget(BattleManager bm, BattleCharacter self) =>
        TargetingService.FindSwordTarget(bm, self);

    public static BattleCharacter FindArrowTarget(BattleManager bm, BattleCharacter self) =>
        TargetingService.FindArrowTarget(bm, self);

    public static BattleCharacter FindSpearTarget(BattleManager bm, BattleCharacter self) =>
        TargetingService.FindSpearTarget(bm, self);

    public static BattleCharacter FindStoneTarget(BattleManager bm, BattleCharacter self) =>
        TargetingService.FindStoneTarget(bm, self);

    public static BattleCharacter FindGunTarget(BattleManager bm, BattleCharacter self) =>
        TargetingService.FindGunTarget(bm, self);

    public static BattleCharacter FindHorseChargeTarget(
        BattleManager bm,
        BattleCharacter self,
        List<BattleCharacter> candidates) =>
        TargetingService.FindHorseChargeTarget(bm, self, candidates);
}
