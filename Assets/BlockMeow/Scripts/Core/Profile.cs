using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BlockMeow
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public long coins = 300;
        public int[] boosters = { 2, 2, 2 }; // hammer, refresh, rotate
        public int bestClassic;
        public int adventureLevel = 1; // next level to play
        public List<int> cats = new List<int> { 0 };
        public int mascot;
        public int theme;
        public bool[] themesOwned = { true, false, false, false, false, false, false };
        public int[] themeAds = new int[7];

        public int loginIndex, lastLoginDay = -1;
        public int missionDay = -1;
        public int[] missionIds = new int[3];
        public int[] missionProgress = new int[3];
        public bool[] missionClaimed = new bool[3];
        public bool missionBonus;
        public int spinDay = -1;
        public bool freeSpinUsed;
        public int bonusSpins;
        public int dailyLastDone = -1, dailyStreak;
        public List<int> dailyDays = new List<int>();
        public int adDay = -1;
        public int[] adCounts = new int[Ads.SlotCount];

        // cats: level per cat (0 = not owned), daily free cat box, one-game trial cat
        public int[] catLevel = new int[24];
        public int catBoxFreeDay = -1, catBoxesOpened;
        public int trialCat = -1;
        // friend challenges: the seed of the best classic game lets the player send it as a challenge
        public int bestSeed = -1;
        public int challengesPlayed, challengesWon;

        public bool sfx = true, music = true, vibration = true;
        public int gamesPlayed, bestCombo, totalGems;
        public long totalLines;
        public bool tutorialDone, skillTipShown;
        public long lastInterstitial, lastRewarded;
        public int gamesSinceInterstitial;

        // suspended classic game
        public bool hasSaved;
        public int[] savedColors = new int[64];
        public int[] savedGems = new int[64];
        public int[] savedTray = { -1, -1, -1 };
        public int[] savedTrayColor = new int[3];
        public int savedScore, savedCombo, savedMiss, savedSeed, savedGauge, savedSkillUses, savedPlaced;
    }

    public struct Reward
    {
        public long Coins;
        public int Hammer, Refresh, Rotate, Spins;
        public bool IsEmpty => Coins == 0 && Hammer == 0 && Refresh == 0 && Rotate == 0 && Spins == 0;
        public static Reward C(long c) => new Reward { Coins = c };
    }

    public enum Booster { Hammer, Refresh, Rotate }
    public enum MissionKind { PlayClassic, Lines, ClassicScore, Combo3, AdventureClear, UseBooster, Gems, DailyWin, UseSkill }

    public static class Profile
    {
        public static SaveData D { get; private set; }
        public static event Action Changed;

        static bool _dirty;
        static float _lastSave;
        static string FilePath => Path.Combine(Application.persistentDataPath, "blockmeow.json");

        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public static int Today => (int)(DateTime.Now.Date - new DateTime(2020, 1, 1)).TotalDays;
        public static DateTime DayToDate(int day) => new DateTime(2020, 1, 1).AddDays(day);

        public static readonly string[] MissionText =
        {
            "클래식 2판 플레이", "줄 20개 지우기", "클래식에서 1,500점 넘기", "콤보 3 만들기",
            "모험 레벨 1개 클리어", "부스터 1회 사용", "보석 15개 모으기", "오늘의 도전 성공", "냥이 스킬 3번 쓰기"
        };
        public static readonly int[] MissionTarget = { 2, 20, 1, 1, 1, 1, 15, 1, 3 };
        public static readonly int[] MissionCoins = { 50, 60, 80, 60, 80, 40, 60, 100, 60 };

        public static readonly Reward[] LoginRewards =
        {
            Reward.C(100), new Reward { Hammer = 1 }, Reward.C(150), new Reward { Refresh = 2 },
            Reward.C(200), new Reward { Rotate = 2 }, new Reward { Coins = 500, Spins = 1 }
        };

        // ------------------------------------------------------------------ persistence

        /// <summary>Play mode without a domain reload keeps statics: never carry a profile over from the last session.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            D = null;
            _dirty = false;
            _lastSave = 0f;
            Changed = null;
        }

        public static void Load()
        {
            D = null;
            try
            {
                if (File.Exists(FilePath)) D = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e) { Debug.LogWarning("[Profile] load failed: " + e.Message); D = null; }
            if (D == null) D = new SaveData();
            Sanitize();
            CheckDaily();
        }

        static void Sanitize()
        {
            if (D.boosters == null || D.boosters.Length != 3) D.boosters = new[] { 2, 2, 2 };
            if (D.cats == null || D.cats.Count == 0) D.cats = new List<int> { 0 };
            if (D.themesOwned == null || D.themesOwned.Length != Themes.All.Length) D.themesOwned = new bool[Themes.All.Length];
            D.themesOwned[0] = true;
            if (D.themeAds == null || D.themeAds.Length != Themes.All.Length) D.themeAds = new int[Themes.All.Length];
            if (D.missionIds == null || D.missionIds.Length != 3) { D.missionIds = new int[3]; D.missionDay = -1; }
            if (D.missionProgress == null || D.missionProgress.Length != 3) D.missionProgress = new int[3];
            if (D.missionClaimed == null || D.missionClaimed.Length != 3) D.missionClaimed = new bool[3];
            if (D.adCounts == null || D.adCounts.Length != Ads.SlotCount) D.adCounts = new int[Ads.SlotCount];
            if (D.catLevel == null || D.catLevel.Length != Cats.All.Length) D.catLevel = new int[Cats.All.Length];
            foreach (int c in D.cats) if (c >= 0 && c < D.catLevel.Length && D.catLevel[c] < 1) D.catLevel[c] = 1;
            if (D.trialCat >= Cats.All.Length) D.trialCat = -1;
            if (D.dailyDays == null) D.dailyDays = new List<int>();
            if (D.savedColors == null || D.savedColors.Length != 64) { D.savedColors = new int[64]; D.hasSaved = false; }
            if (D.savedGems == null || D.savedGems.Length != 64) { D.savedGems = new int[64]; D.hasSaved = false; }
            if (D.savedTray == null || D.savedTray.Length != 3) { D.savedTray = new[] { -1, -1, -1 }; D.hasSaved = false; }
            if (D.savedTrayColor == null || D.savedTrayColor.Length != 3) D.savedTrayColor = new int[3];
            D.theme = Mathf.Clamp(D.theme, 0, Themes.All.Length - 1);
            if (!D.themesOwned[D.theme]) D.theme = 0;
            if (!D.cats.Contains(D.mascot)) D.mascot = D.cats[0];
        }

        public static void Save()
        {
            if (D == null) return;
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(D));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
                _dirty = false;
                _lastSave = Time.unscaledTime;
            }
            catch (Exception e) { Debug.LogWarning("[Profile] save failed: " + e.Message); }
        }

        public static void MarkDirty() { _dirty = true; Changed?.Invoke(); }

        public static void Tick()
        {
            if (_dirty && Time.unscaledTime - _lastSave > 1.5f) Save();
        }

        public static void ResetAll()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { /* ignored */ }
            D = new SaveData();
            Sanitize();
            CheckDaily();
            Save();
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------ currency & items

        public static void AddCoins(long v) { D.coins += v; MarkDirty(); }

        public static bool SpendCoins(long v)
        {
            if (D.coins < v) return false;
            D.coins -= v;
            MarkDirty();
            return true;
        }

        public static int Boosters(Booster b) => D.boosters[(int)b];
        public static void AddBooster(Booster b, int n) { D.boosters[(int)b] += n; MarkDirty(); }

        public static bool UseBooster(Booster b)
        {
            if (D.boosters[(int)b] <= 0) return false;
            D.boosters[(int)b]--;
            MissionAdd(MissionKind.UseBooster);
            MarkDirty();
            return true;
        }

        public static readonly int[] BoosterPrice = { 150, 100, 120 };
        public static readonly string[] BoosterName = { "망치", "새로고침", "회전" };
        public static readonly string[] BoosterIcon = { "ic_hammer", "ic_refresh", "ic_rotate" };
        public static readonly string[] BoosterDesc = { "블록 한 칸을 부숴요", "남은 조각을 새로 뽑아요", "조각을 90도씩 돌려요" };

        public static void Grant(Reward r)
        {
            D.coins += r.Coins;
            D.boosters[0] += r.Hammer;
            D.boosters[1] += r.Refresh;
            D.boosters[2] += r.Rotate;
            D.bonusSpins += r.Spins;
            MarkDirty();
        }

        // ------------------------------------------------------------------ themes & cats

        public static bool ThemeOwned(int i) => D.themesOwned[i];

        public static bool BuyTheme(int i)
        {
            if (ThemeOwned(i)) return true;
            if (!SpendCoins(Themes.All[i].CoinPrice)) return false;
            D.themesOwned[i] = true;
            MarkDirty();
            return true;
        }

        /// <summary>Counts one watched ad towards a theme; returns true when the theme unlocks.</summary>
        public static bool ThemeAdWatched(int i)
        {
            D.themeAds[i]++;
            if (D.themeAds[i] >= Themes.All[i].AdPrice) D.themesOwned[i] = true;
            MarkDirty();
            return D.themesOwned[i];
        }

        public static void SetTheme(int i)
        {
            if (!ThemeOwned(i)) return;
            D.theme = i;
            MarkDirty();
        }

        public static bool CatUnlocked(int i) => D.cats.Contains(i);
        public static int CatLevel(int i) => i >= 0 && i < D.catLevel.Length ? Mathf.Max(CatUnlocked(i) ? 1 : 0, D.catLevel[i]) : 0;

        /// <summary>The cat that plays the next game: a trial cat if one was granted, otherwise the chosen partner.</summary>
        public static int PartnerCat => D.trialCat >= 0 ? D.trialCat : D.mascot;

        public static void UnlockCat(int i)
        {
            if (CatUnlocked(i)) return;
            D.cats.Add(i);
            D.catLevel[i] = Mathf.Max(1, D.catLevel[i]);
            MarkDirty();
        }

        public const int CatBoxCoins = 800, CatMaxCopyCoins = 300;
        public static readonly float[] CatBoxOdds = { 70f, 25f, 5f };

        public struct CatBoxResult { public int Cat; public bool IsNew, LevelUp; public int Level; public long Coins; }

        public static bool CatBoxFreeAvailable => D.catBoxFreeDay != Today;

        /// <summary>Uses today's free cat box; false when it was already taken.</summary>
        public static bool TakeFreeCatBox()
        {
            if (!CatBoxFreeAvailable) return false;
            D.catBoxFreeDay = Today;
            MarkDirty();
            return true;
        }

        public static void SetPartner(int cat)
        {
            if (!CatUnlocked(cat)) return;
            D.mascot = cat;
            MarkDirty();
        }

        /// <summary>The first boxes always bring a cat the player does not have yet.</summary>
        public const int GuaranteedNewBoxes = 3;

        /// <summary>Opens a cat box: a random cat by rarity odds.</summary>
        public static CatBoxResult OpenCatBox()
        {
            float roll = UnityEngine.Random.value * 100f;
            var rarity = roll < CatBoxOdds[2] ? CatRarity.Legendary : roll < CatBoxOdds[2] + CatBoxOdds[1] ? CatRarity.Rare : CatRarity.Common;
            var pool = new List<int>();
            bool onlyNew = D.catBoxesOpened < GuaranteedNewBoxes;
            for (int i = 0; i < Cats.All.Length; i++)
                if (Cats.All[i].Rarity == rarity && (!onlyNew || !CatUnlocked(i))) pool.Add(i);
            if (pool.Count == 0)
                for (int i = 0; i < Cats.All.Length; i++) if (Cats.All[i].Rarity == rarity) pool.Add(i);
            D.catBoxesOpened++;
            return GiveCat(pool[UnityEngine.Random.Range(0, pool.Count)]);
        }

        /// <summary>A new cat, a level-up for a duplicate, or coins when it is already at max level.</summary>
        public static CatBoxResult GiveCat(int cat)
        {
            var r = new CatBoxResult { Cat = cat };
            if (!CatUnlocked(cat)) { UnlockCat(cat); r.IsNew = true; r.Level = 1; }
            else if (D.catLevel[cat] < Cats.MaxLevel) { D.catLevel[cat]++; r.LevelUp = true; r.Level = D.catLevel[cat]; }
            else { r.Level = Cats.MaxLevel; r.Coins = CatMaxCopyCoins; D.coins += CatMaxCopyCoins; }
            MarkDirty();
            return r;
        }

        /// <summary>Cat rewarded for clearing adventure level n (every 5 levels), or -1.</summary>
        public static int CatForLevel(int n) => n % 5 == 0 && n / 5 < Cats.All.Length ? n / 5 : -1;

        // ------------------------------------------------------------------ daily systems

        public static void CheckDaily()
        {
            int today = Today;
            if (D.missionDay != today)
            {
                D.missionDay = today;
                var rng = new System.Random(today * 977 + 5);
                var pool = new List<int>();
                for (int i = 0; i < MissionText.Length; i++) pool.Add(i);
                for (int i = 0; i < 3; i++)
                {
                    int k = rng.Next(pool.Count);
                    D.missionIds[i] = pool[k];
                    pool.RemoveAt(k);
                    D.missionProgress[i] = 0;
                    D.missionClaimed[i] = false;
                }
                D.missionBonus = false;
                MarkDirty();
            }
            if (D.spinDay != today) { D.spinDay = today; D.freeSpinUsed = false; MarkDirty(); }
            if (D.adDay != today) { D.adDay = today; for (int i = 0; i < D.adCounts.Length; i++) D.adCounts[i] = 0; MarkDirty(); }
            // a missed day breaks the daily-challenge streak
            if (D.dailyLastDone >= 0 && today - D.dailyLastDone > 1 && D.dailyStreak != 0) { D.dailyStreak = 0; MarkDirty(); }
        }

        public static bool LoginAvailable => D.lastLoginDay != Today;

        public static Reward ClaimLogin()
        {
            var r = LoginRewards[D.loginIndex % LoginRewards.Length];
            D.loginIndex = (D.loginIndex + 1) % LoginRewards.Length;
            D.lastLoginDay = Today;
            Grant(r);
            return r;
        }

        public static void MissionAdd(MissionKind kind, int amount = 1)
        {
            CheckDaily();
            for (int i = 0; i < 3; i++)
            {
                int id = D.missionIds[i];
                if (id != (int)kind) continue;
                D.missionProgress[i] = Mathf.Min(MissionTarget[id], D.missionProgress[i] + amount);
                MarkDirty();
            }
        }

        public static bool MissionDone(int slot) => D.missionProgress[slot] >= MissionTarget[D.missionIds[slot]];

        public static bool AnyMissionReady()
        {
            CheckDaily();
            bool all = true;
            for (int i = 0; i < 3; i++)
            {
                if (MissionDone(i) && !D.missionClaimed[i]) return true;
                if (!D.missionClaimed[i]) all = false;
            }
            return all && !D.missionBonus;
        }

        public static long ClaimMission(int slot)
        {
            if (!MissionDone(slot) || D.missionClaimed[slot]) return 0;
            D.missionClaimed[slot] = true;
            long c = MissionCoins[D.missionIds[slot]];
            AddCoins(c);
            return c;
        }

        public static bool ClaimMissionBonus()
        {
            for (int i = 0; i < 3; i++) if (!D.missionClaimed[i]) return false;
            if (D.missionBonus) return false;
            D.missionBonus = true;
            D.bonusSpins++;
            MarkDirty();
            return true;
        }

        public static bool SpinAvailable => !D.freeSpinUsed || D.bonusSpins > 0;

        public static bool DailyDoneToday => D.dailyLastDone == Today;

        public static long CompleteDaily()
        {
            int today = Today;
            if (D.dailyLastDone == today) return 0;
            D.dailyStreak = D.dailyLastDone == today - 1 ? D.dailyStreak + 1 : 1;
            D.dailyLastDone = today;
            if (!D.dailyDays.Contains(today)) D.dailyDays.Add(today);
            if (D.dailyDays.Count > 120) D.dailyDays.RemoveAt(0);
            long coins = 150 + Mathf.Min(150, D.dailyStreak * 10);
            if (D.dailyStreak % 7 == 0) { coins += 300; D.boosters[(int)Booster.Rotate]++; }
            AddCoins(coins);
            MissionAdd(MissionKind.DailyWin);
            return coins;
        }

        // ------------------------------------------------------------------ ads bookkeeping

        public static int AdCount(AdPlacement p) { CheckDaily(); return D.adCounts[(int)p]; }

        public static void AdWatched(AdPlacement p)
        {
            CheckDaily();
            D.adCounts[(int)p]++;
            D.lastRewarded = Now;
            MarkDirty();
        }
    }
}
