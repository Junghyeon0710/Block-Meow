using UnityEngine;

namespace BlockMeow
{
    public enum CatPattern { None, Tabby, Patch, Mask }
    public enum CatRarity { Common, Rare, Legendary }

    /// <summary>The partner cat's power, charged by clearing lines and fired by tapping the cat.</summary>
    public enum SkillType { Punch, RowSweep, ColStamp, Knead, Yarn, FishParty, Nap, LuckyBell }

    public sealed class CatDef
    {
        public string Name;
        public Color Fur, PatternColor;
        public CatPattern Pattern;
        public string Accessory; // sprite name or null
        public CatRarity Rarity;
        public SkillType Skill;
    }

    /// <summary>The 24 collectible cats. Each one brings a skill into every game.</summary>
    public static class Cats
    {
        static CatDef Cat(string name, CatRarity r, SkillType skill, string fur, CatPattern p = CatPattern.None, string pat = "#000000", string acc = null)
            => new CatDef { Name = name, Rarity = r, Skill = skill, Fur = Pal.Hex(fur), Pattern = p, PatternColor = Pal.Hex(pat), Accessory = acc };

        const CatRarity C = CatRarity.Common, R = CatRarity.Rare, L = CatRarity.Legendary;

        public static readonly CatDef[] All =
        {
            Cat("치즈", C, SkillType.RowSweep, "#FFB35C", CatPattern.Tabby, "#E8862A"),
            Cat("모찌", C, SkillType.Yarn, "#FFF8F0", acc: "acc_bow"),
            Cat("까망이", R, SkillType.Punch, "#3B3448", acc: "acc_star"),
            Cat("구름", C, SkillType.ColStamp, "#B9B9CC", CatPattern.Tabby, "#8B8BA3"),
            Cat("라떼", C, SkillType.Punch, "#E9CBA2", CatPattern.Patch, "#8B5A3C"),
            Cat("나비", R, SkillType.FishParty, "#FFF4E6", CatPattern.Patch, "#FF9F43", "acc_flower"),
            Cat("호떡", C, SkillType.Knead, "#A0673C", CatPattern.Tabby, "#6E4426", "acc_leaf"),
            Cat("쿠키", C, SkillType.FishParty, "#F5DEB3", CatPattern.Patch, "#5A3E2B", "acc_glasses"),
            Cat("보리", R, SkillType.Knead, "#F3E5D0", CatPattern.Mask, "#5B4033"),
            Cat("콩이", C, SkillType.Nap, "#9DB4C8", acc: "acc_phones"),
            Cat("두부", C, SkillType.LuckyBell, "#FFFFFF", CatPattern.Patch, "#2E2A36"),
            Cat("망고", R, SkillType.LuckyBell, "#FFC94A", CatPattern.Tabby, "#F08C1D", "acc_hat"),
            Cat("달이", R, SkillType.Nap, "#D9DCE8", CatPattern.Tabby, "#A7AABB", "acc_star"),
            Cat("루루", C, SkillType.Nap, "#FFE4EC", acc: "acc_flower"),
            Cat("코코", L, SkillType.Knead, "#6B4226", acc: "acc_crown"),
            Cat("미미", R, SkillType.ColStamp, "#CFCFDA", CatPattern.Mask, "#6D6A80", "acc_bow"),
            Cat("토토", C, SkillType.RowSweep, "#FF9F43", CatPattern.Patch, "#FFFFFF", "acc_leaf"),
            Cat("하늘", R, SkillType.RowSweep, "#86AED1", acc: "acc_glasses"),
            Cat("솜이", C, SkillType.ColStamp, "#FFFBF5", CatPattern.Tabby, "#E9DCCB", "acc_hat"),
            Cat("감자", C, SkillType.Punch, "#D2A06B", CatPattern.Tabby, "#A86F3B", "acc_phones"),
            Cat("레오", L, SkillType.Punch, "#F0B43C", CatPattern.Tabby, "#C77B12", "acc_crown"),
            Cat("별이", L, SkillType.FishParty, "#2B2735", CatPattern.Patch, "#FFFFFF", "acc_star"),
            Cat("젤리", R, SkillType.Yarn, "#9FF0D6", acc: "acc_flower"),
            Cat("왕냥이", L, SkillType.LuckyBell, "#FFF1C1", CatPattern.Mask, "#D4A017", "acc_crown"),
        };

        public static CatDef Get(int i) => All[Mathf.Clamp(i, 0, All.Length - 1)];

        public static readonly string[] RarityName = { "일반", "희귀", "전설" };
        public static readonly Color[] RarityColor = { Pal.Hex("#CBD3E6"), Pal.Blue, Pal.Yellow };
        public const int MaxLevel = 5;
    }

    /// <summary>Skill names, texts and tuning. Strong versions unlock with level and rarity.</summary>
    public static class CatSkills
    {
        public static readonly string[] Name = { "냥냥 펀치", "꼬리 휩쓸기", "발도장 콕콕", "꾹꾹이", "털뭉치 놀이", "생선 파티", "낮잠", "행운의 방울" };

        public static readonly string[] Icon = { "sk_punch", "sk_row", "sk_col", "sk_knead", "sk_yarn", "sk_fish", "sk_nap", "sk_bell" };

        static readonly string[] Weak =
        {
            "누른 곳의 3x3 블록을 날려요",
            "가장 꽉 찬 가로줄의 빈칸을 채워 터뜨려요",
            "가장 꽉 찬 세로줄의 빈칸을 발도장으로 채워 터뜨려요",
            "블록을 2칸까지 아래로 꾹꾹 눌러요. 줄이 맞으면 터져요",
            "조각 3개를 1~2칸짜리 작은 조각으로 바꿔요",
            "가장 많은 색의 블록을 모두 먹어요",
            "6번 놓는 동안 콤보가 끊기지 않고 점수 2배",
            "줄을 바로 완성할 수 있는 조각 3개를 줘요"
        };

        static readonly string[] Strong =
        {
            "누른 곳 주변 13칸을 날려요",
            "가장 꽉 찬 가로줄 2개를 채워 터뜨려요",
            "가장 꽉 찬 세로줄 2개를 채워 터뜨려요",
            "블록을 바닥까지 누르고, 생긴 줄 점수 2배",
            "조각 3개를 줄을 바로 채우는 작은 조각으로 바꿔요",
            "가장 많은 색 2가지를 모두 먹어요",
            "9번 놓는 동안 콤보 유지, 점수 2배",
            "가장 많은 줄을 지울 수 있는 조각 3개를 줘요"
        };

        static int RarityBonus(CatRarity r) => r == CatRarity.Legendary ? 2 : r == CatRarity.Rare ? 1 : 0;

        /// <summary>Strong version from tier 4 (level + rarity bonus).</summary>
        public static bool IsStrong(CatRarity r, int level) => level + RarityBonus(r) >= 4;

        /// <summary>Level at which the skill turns strong: common 4, rare 3, legendary 2.</summary>
        public static int StrongLevel(CatRarity r) => Mathf.Max(1, 4 - RarityBonus(r));

        /// <summary>Extra lines per skill: board-wide skills cost more, the score-only nap less.</summary>
        static readonly int[] SkillCost = { 0, 0, 0, 3, 2, 0, -1, 0 };

        /// <summary>Each use in the same game makes the cat a little more tired: +2 lines per use, so no game runs forever.</summary>
        public const int TiredStep = 2, MaxTiredUses = 50;

        /// <summary>Lines to clear before the first use in a game.</summary>
        public static int GaugeNeed(CatRarity r, int level, SkillType s)
        {
            int baseNeed = r == CatRarity.Legendary ? 6 : r == CatRarity.Rare ? 7 : 8;
            return baseNeed + SkillCost[(int)s] - (level >= 3 ? 1 : 0) - (level >= 5 ? 1 : 0);
        }

        public static string Describe(SkillType s, CatRarity r, int level) => IsStrong(r, level) ? Strong[(int)s] : Weak[(int)s];
        public static string DescribeWeak(SkillType s) => Weak[(int)s];
        public static string DescribeStrong(SkillType s) => Strong[(int)s];
    }
}
