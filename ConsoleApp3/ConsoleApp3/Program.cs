using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;

namespace SuperPowerSolver
{
    struct Area
    {
        public double requiredPower;
        public double multiplier;

        public Area(double rp, double m)
        {
            requiredPower = rp;
            multiplier = m;
        }
    }

    class PlayerMultiplier
    {
        // The highest amount a level can be differ from another.
        // BALANCE_TOLERANCE = 6 would mean, for example, we can't upgrade 2^22 strength until we first upgrade 2^16 endurance
        // (strength is 6 levels above endurance and would become 7 levels above if we upgraded again.)
        // This must be at least 1 or no upgrade will ever be allowed!
        public const int BALANCE_TOLERANCE = 6;

        public Dictionary<StatLabel, int> levels = new Dictionary<StatLabel, int>()
        {
            { StatLabel.Endurance, 0 }, { StatLabel.Strength, 0 }, { StatLabel.Psychic, 0 },
        };
        // assume upgrading one at a time in a balanced way

        public PlayerMultiplier() { }

        // would it remain balanced if we upgrade this?
        // this function is used to disallow the upgrade if it upsets the balance.
        public bool WouldItRemainBalanced(StatLabel next)
        {
            Dictionary<StatLabel, int> fakeLevels = new Dictionary<StatLabel, int>(levels);
            ++fakeLevels[next];
            if (Math.Abs(fakeLevels[StatLabel.Endurance] - fakeLevels[StatLabel.Strength]) > BALANCE_TOLERANCE) { return false; }
            if (Math.Abs(fakeLevels[StatLabel.Psychic] - fakeLevels[StatLabel.Strength]) > BALANCE_TOLERANCE) { return false; }
            if (Math.Abs(fakeLevels[StatLabel.Psychic] - fakeLevels[StatLabel.Endurance]) > BALANCE_TOLERANCE) { return false; }
            return true;
        }

        public double Multiplier(StatLabel which) {
            return Math.Pow(2, levels[which]);
        }

        public PlayerMultiplier(PlayerMultiplier old)
        {
            levels = new Dictionary<StatLabel, int>(old.levels);
        }

        public static bool operator ==(PlayerMultiplier a, PlayerMultiplier b) { return a.Equals(b); }
        public static bool operator !=(PlayerMultiplier a, PlayerMultiplier b) { return !a.Equals(b); }
        public override bool Equals(object obj)
        {
            if (obj is PlayerMultiplier)
            {
                PlayerMultiplier other = obj as PlayerMultiplier;
                return levels[StatLabel.Endurance] == other.levels[StatLabel.Endurance]
                    && levels[StatLabel.Strength] == other.levels[StatLabel.Strength]
                    && levels[StatLabel.Psychic] == other.levels[StatLabel.Psychic];
            }
            return false;
        }
        public override int GetHashCode() { return levels[StatLabel.Endurance] + 1000 * levels[StatLabel.Strength] + 1000000 * levels[StatLabel.Psychic]; }
    }

    public enum RankLabel { F, E, D, C, B, A, S, SS, SSS, X, Y, Z, XYZ, Legend, Immortal, TimeRuler, UniRuler, MulRuler, Omni }

    struct Rank
    {
        public double requiredPower;
        public double powerMultiplier;
        public double coinMultiplier;

        public Rank(double r, double pm, double cm)
        {
            requiredPower = r;
            powerMultiplier = pm;
            coinMultiplier = cm;
        }
    }

    public enum TransformationLabel { None, BuffNoob, GoldGrd, Shadow, Void, FDragon, SciBorg, Ocean, GWarrior, ELord, Thunder, MDragon,
        Ninja, GemGrd, FKnight, AWarrior,
        Werewolf, Minotaur, Gryphon, Phoenix, Yeti, Hydra, Reaper, DragonRuler
    }

    struct Transformation
    {
        public double cost;
        public double powerMultiplier;
        public double coinMultiplier;
        public double gemMultiplier;
        public bool obtainedByFusion;

        public Transformation(double c, double pm, double cm, double gm) // transf.s you can buy
        {
            cost = c;
            powerMultiplier = pm;
            coinMultiplier = cm;
            gemMultiplier = gm;
            obtainedByFusion = false;
        }

        public Transformation(double pm) // transf.s obtained by fusion
        {
            cost = double.PositiveInfinity;
            powerMultiplier = pm;
            coinMultiplier = 1;
            gemMultiplier = 1;
            obtainedByFusion = true;
        }
    }

    public enum FusionLabel { None, Werewolf, Minotaur, Gryphon, Phoenix, Yeti, Hydra, Reaper, DragonRuler }

    struct Fusion
    {
        public RankLabel requiredRank;
        public double requiredPower;
        public double powerMultiplier;

        public Fusion(RankLabel rr, double rp, double pm)
        {
            requiredRank = rr;
            requiredPower = rp;
            powerMultiplier = pm;
        }
    }

    struct Chest
    {
        public RankLabel requiredRank;
        public FusionLabel requiredFusion;
        public double reward; // in coins
        public double rankBonus;

        public Chest(double r)
        {
            reward = r;
            requiredRank = RankLabel.F;
            requiredFusion = FusionLabel.None;
            rankBonus = 0;
        }

        public Chest(double r, RankLabel rr)
        {
            reward = r;
            requiredRank = rr;
            requiredFusion = FusionLabel.None;
            rankBonus = 0;
        }

        public Chest(double r, FusionLabel rf)
        {
            reward = r;
            requiredRank = RankLabel.F;
            requiredFusion = rf;
            rankBonus = 0;
        }

        public Chest(double r, RankLabel rr, FusionLabel rf)
        {
            reward = r;
            requiredRank = rr;
            requiredFusion = rf;
            rankBonus = 0;
        }

        public Chest(double r, double rb)
        {
            reward = r;
            requiredRank = RankLabel.F;
            requiredFusion = FusionLabel.None;
            rankBonus = rb;
        }

        public bool Available(Player plr)
        {
            return plr.rank >= requiredRank && plr.fusion >= requiredFusion;
        }

        // can be collected every 7 hours, we average out for the simplicity of calculations.
        public static BigInteger CHEST_TIME = 60 * 60 * 7;
        public double RewardPerSecond(Player plr, TransformationLabel transf)
        {
            double rps = (reward + ((int)plr.rank * rankBonus)) / (double)CHEST_TIME;
            if (transf == TransformationLabel.GoldGrd) { rps *= 1.25; }
            return rps;
        }
    }

    public enum Rarity { Common, Rare, Epic, Legendary, Celestial, God }

    class AncientItem
    {
        public string name;
        public double chance; // chance in the pack (need not be normalized, but should be)
        public double sellWorth; // in gems
        public double[] multipliers = new double[5];
        public StatLabel stat;
        public Rarity rarity;
        public ItemPack pack;

        public static bool operator ==(AncientItem a, AncientItem b) { return a.Equals(b); }
        public static bool operator !=(AncientItem a, AncientItem b) { return !a.Equals(b); }
        public override bool Equals(object obj)
        {
            if (obj is AncientItem) { return (obj as AncientItem).name == name; }
            return false;
        }
        public override int GetHashCode() { return name.GetHashCode(); }

        public double GetMultiplier(StatLabel outStat, double count)
        {
            if ((stat & outStat) == 0) { return 0; }
            if (count < 0) { throw new Exception("STOP DOING MATH"); }
            int[] countsForGrade = GameInfo.itemGradeRequirements[rarity];
            int grade = -1;
            for (int i = 0; i < 5; ++i) { if (count + 0.01 >= countsForGrade[i]) { grade = i; } }
            if (grade == -1) { return 0; }
            return multipliers[grade];
        }

        public double ExpectedTries() { return pack.ExpectedTries(this); }
        public double NormalizedChance() { pack.CalculateTotalChance(); return chance / pack.totalChance; }
    }

    class ItemPack
    {
        public string name;
        public double cost; // in gems
        public HashSet<AncientItem> items = new HashSet<AncientItem>();

        public double totalChance = -1;
        public void CalculateTotalChance()
        {
            if (totalChance < 0)
            {
                totalChance = 0;
                foreach (AncientItem item in items) { totalChance += item.chance; }
            }
        }

        // Geometric random variable: how many mean packs to open, to obtain the item once
        public double ExpectedTries(AncientItem item)
        {
            CalculateTotalChance();
            double rc = item.chance / totalChance;
            return Math.Ceiling(1.0 / rc);
        }

        public AncientItem PickItem()
        {
            if (items.Count == 0) { throw new Exception("Item pack named " + name +" shouldn't be empty"); }
            CalculateTotalChance();
            double r = GameInfo.rand.NextDouble() * totalChance;
            AncientItem chosen = null;
            double sumulus = 0;
            foreach (AncientItem item in items) {
                sumulus += item.chance;
                if (r < sumulus) { chosen = item; break; }
            }
            return chosen;
        }
    }

    abstract class PlayerGoal
    {
        // We don't want any goal to take longer than a week. If no goals are available, then the game objectively sucks!
        public static BigInteger MAX_GOAL_TIME = 60 * 60 * 24 * 7 * 10000000000;
        public abstract override string ToString();
        public abstract BigInteger TimeUntilExecute(Player plr);
        public bool CanExecute(Player plr) { return TimeUntilExecute(plr) <= MAX_GOAL_TIME; }
        public abstract bool Execute(Player plr);
        public abstract string ViewResult(Player plr);
    }

    // All is used for items that upgrade all stats
    public enum StatLabel { Endurance = 1, Strength = 2, Psychic = 4, All = 7 }

    class StatsUpGoal : PlayerGoal
    {
        public StatLabel which;
        public override string ToString() { return "Increase " + which.ToString() + " stat"; }
        public override BigInteger TimeUntilExecute(Player plr) { return plr.SecondsUntilStatsUp(which, out _); }
        public override bool Execute(Player plr) { return plr.StatsUp(which); }
        public override string ViewResult(Player plr) { return plr.StatsInfo(); }
        public StatsUpGoal(StatLabel w) { which = w; }
    }

    // This makes paths a lot shorter: process to combine StatsUpGoals
    class MultiStatsUpGoal : PlayerGoal
    {
        public StatLabel which;
        public int multiple;
        public override string ToString() { return "Increase " + which.ToString()  + " stat x" + multiple.ToString(); }
        public override BigInteger TimeUntilExecute(Player plr)
        {
            Player fakePlr = new Player(plr);
            BigInteger totalTime = 0;
            for (int i = 0; i < multiple; ++i)
            {
                BigInteger subTime = fakePlr.SecondsUntilStatsUp(which, out _);
                if (subTime > MAX_GOAL_TIME) { return subTime; }
                totalTime += subTime;
                bool check = fakePlr.StatsUp(which);
                if (!check) { return BigInteger.Pow(10, 100); }
            }
            return totalTime;
        }
        public override bool Execute(Player plr)
        {
            for (int i = 0; i < multiple; ++i)
            {
                bool sub = plr.StatsUp(which);
                if (!sub) { return false; }
            }
            return true;
        }
        public override string ViewResult(Player plr) { return plr.StatsInfo(); }
        public MultiStatsUpGoal(StatLabel w, int m) { which = w; multiple = m; }
    }

    class MultiRankUpGoal : PlayerGoal
    {
        public int multiple;
        public override string ToString() { return "Rank up x" + multiple.ToString(); }
        public override BigInteger TimeUntilExecute(Player plr)
        {
            Player fakePlr = new Player(plr);
            BigInteger totalTime = 0;
            for (int i = 0; i < multiple; ++i)
            {
                BigInteger subTime = fakePlr.SecondsUntilRankUp(out _);
                if (subTime > MAX_GOAL_TIME) { return subTime; }
                totalTime += subTime;
                bool check = fakePlr.RankUp();
                if (!check) { return BigInteger.Pow(10, 100); }
            }
            return totalTime;
        }
        public override bool Execute(Player plr)
        {
            for (int i = 0; i < multiple; ++i)
            {
                bool sub = plr.RankUp();
                if (!sub) { return false; }
            }
            return true;
        }
        public override string ViewResult(Player plr) { return plr.RankInfo(); }
        public MultiRankUpGoal(int m) { multiple = m; }
    }

    class RankUpGoal : PlayerGoal
    {
        public override string ToString() { return "Rank up"; }
        public override BigInteger TimeUntilExecute(Player plr) { return plr.SecondsUntilRankUp(out _); }
        public override bool Execute(Player plr) { return plr.RankUp(); }
        public override string ViewResult(Player plr) { return plr.RankInfo(); }
    }

    class TransformationGoal : PlayerGoal
    {
        public TransformationLabel target;
        public override string ToString() { return "Buy " + target.ToString() + " transformation"; }
        public override BigInteger TimeUntilExecute(Player plr) { return plr.SecondsUntilTransformation(target, out _); }
        public override bool Execute(Player plr) { return plr.BuyTransformation(target); }
        public override string ViewResult(Player plr) { return plr.TransformationInfo(target); }
        public TransformationGoal(TransformationLabel t) { target = t; }
    }

    class FusionGoal : PlayerGoal
    {
        public override string ToString() { return "Next fusion"; }
        public override BigInteger TimeUntilExecute(Player plr) { return plr.SecondsUntilNextFusion(out _); }
        public override bool Execute(Player plr) { return plr.FuseUp(); }
        public override string ViewResult(Player plr) { return plr.FusionInfo(); }
    }

    class OneItemPackGoal : PlayerGoal
    {
        public ItemPack pack;
        public override string ToString() { return "Buy " + pack.name + " pack"; }
        public override BigInteger TimeUntilExecute(Player plr) { return plr.SecondsUntilBuyPack(pack, 1, out _); }
        public override bool Execute(Player plr) { return plr.BuyPack(pack); }
        public override string ViewResult(Player plr) { return plr.ItemInfo(); }
        public OneItemPackGoal(ItemPack p) { pack = p; }
    }

    class AncientItemGoal : PlayerGoal
    {
        public AncientItem item;
        public override string ToString() { return "Obtain " + item.name + " by buying " + item.ExpectedTries().ToString("G7") + " " + item.pack.name + " packs"; }
        public override BigInteger TimeUntilExecute(Player plr) { return plr.SecondsUntilMeanItem(item, out _); }
        public override bool Execute(Player plr) { return plr.BuyPackForItem(item); }
        public override string ViewResult(Player plr) { return plr.ItemInfo(); }
        public AncientItemGoal(AncientItem i) { item = i; }
    }

    class Player
    {
        public BigInteger seconds = 0; // How long the player was in the game

        public double coins = 100;
        public double power = 0;
        public double gems = 0;
        // power normal multiplier
        public PlayerMultiplier mainMultiplier = new PlayerMultiplier();
        public RankLabel rank = RankLabel.F;
        public HashSet<TransformationLabel> transformations = new HashSet<TransformationLabel>() { TransformationLabel.None };
        public FusionLabel fusion = FusionLabel.None;
        public Dictionary<AncientItem, double> items = new Dictionary<AncientItem, double>();
        // this will save a lot of calculation time.
        // changed whenever an item is purchased.
        private Dictionary<StatLabel, double> multipliersFromItems = new Dictionary<StatLabel, double>()
        {
            { StatLabel.Endurance, 0 }, { StatLabel.Strength, 0 }, { StatLabel.Psychic, 0 }
        };
        private AncientItem mostRecentItem = null;
        public double totalItems = 0;

        public Player()
        {
            // collect initial chest reward instantly, all future rewards will be averaged
            coins += ChestRewardPerSecond(TransformationLabel.None) * (double)Chest.CHEST_TIME;
            // collect first daily mini-chest reward instantly
            gems += 47500;
        }

        // Make sure to copy any new fields that you add later, like gems and items
        public Player(Player old)
        {
            seconds = old.seconds;
            coins = old.coins;
            power = old.power;
            gems = old.gems;
            mainMultiplier = new PlayerMultiplier(old.mainMultiplier);
            rank = old.rank;
            transformations = new HashSet<TransformationLabel>(old.transformations);
            fusion = old.fusion;
            items = new Dictionary<AncientItem, double>(old.items);
        } 

        // Splice point player is similar enough to another player
        public bool IsSimilar(Player other)
        {
            bool rtf = (rank == other.rank) && (fusion == other.fusion) && transformations.SetEquals(other.transformations);
            bool m = (mainMultiplier == other.mainMultiplier);
            return rtf && m;
        }

        private double MultiplierByStat(StatLabel which)
        {
            return mainMultiplier.Multiplier(which) * (1 + multipliersFromItems[which]);
        }

        // Use mainMultiplier and items
        private double HighestMultiplierByStat()
        {
            return Math.Max(Math.Max(MultiplierByStat(StatLabel.Endurance), MultiplierByStat(StatLabel.Strength)), MultiplierByStat(StatLabel.Psychic));
        }

        public double PowerMultiplier(TransformationLabel transf)
        {
            double rm = GameInfo.rankInfo[rank].powerMultiplier;
            double tm = GameInfo.transfInfo[transf].powerMultiplier;
            double fm = GameInfo.fusionInfo[fusion].powerMultiplier;
            return HighestMultiplierByStat() * rm * tm * fm;
        }

        public double CoinMultiplier(TransformationLabel transf)
        {
            double rm = GameInfo.rankInfo[rank].coinMultiplier;
            double tm = GameInfo.transfInfo[transf].coinMultiplier;
            return rm * tm; // is it really stacking? need to find out
        }

        public double GemMultiplier(TransformationLabel transf)
        {
            double fm = 1 + (int)fusion;
            double tm = GameInfo.transfInfo[transf].gemMultiplier;
            return fm * tm; // is it really stacking? need to find out
        }

        public double ChestRewardPerSecond(TransformationLabel transf)
        {
            double tr = 0;
            foreach (Chest c in GameInfo.chests)
            {
                if (!c.Available(this)) { continue; }
                tr += c.RewardPerSecond(this, transf);
            }
            return tr;
        }

        public double MiniChestRewardPerSecond(TransformationLabel transf)
        {
            return (transf == TransformationLabel.GemGrd ? 1.25 : 1) * 47500.0 / (60 * 60 * 24);
        }

        public void Play(BigInteger secs, TransformationLabel transf)
        {
            seconds += secs;
            coins += (double)secs * ((GameInfo.baseCoinsPerSecond * CoinMultiplier(transf)) + ChestRewardPerSecond(transf));
            power += (double)secs * PowerMultiplier(transf);
            gems += (double)secs * ((GameInfo.baseGemsPerSecond * GemMultiplier(transf)) + MiniChestRewardPerSecond(transf) /*+ GameInfo.dailyOfferGemsPerSecond*/);
        }

        // get seconds until reaching a certain power, if the player doesn't upgrade, clicks every second, and takes advantage of all areas
        public BigInteger SecondsUntilPower(double target, out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            if (target <= power) { return 0; }
            // Be greedy and use the best transformation we have
            foreach (TransformationLabel t in transformations)
            {
                if (GameInfo.transfInfo[t].powerMultiplier > GameInfo.transfInfo[bestTransf].powerMultiplier) { bestTransf = t; }
            }
            Area[] areas = GameInfo.areas;
            int currArea = 0;
            while (currArea < areas.Length && areas[currArea].requiredPower < power) { ++currArea; }
            BigInteger runningSeconds = 0;
            double runningPower = power;
            double pmul = PowerMultiplier(bestTransf);
            while (currArea + 1 < areas.Length && areas[currArea + 1].requiredPower < target)
            {
                double subTargPower = areas[currArea + 1].requiredPower;
                runningSeconds += (BigInteger)Math.Ceiling((subTargPower - runningPower) / (areas[currArea].multiplier * pmul));
                runningPower = subTargPower;
                ++currArea;
            }
            runningSeconds += (BigInteger)Math.Ceiling((target - runningPower) / (areas[currArea].multiplier * pmul));
            return runningSeconds;
        }

        public BigInteger SecondsUntilCoins(double target, out TransformationLabel bestTransf)
        {
            // Be greedy and use the best transformation we have
            bestTransf = TransformationLabel.None;
            if (target <= coins) { return 0; }
            foreach (TransformationLabel t in transformations)
            {
                if (GameInfo.transfInfo[t].coinMultiplier > GameInfo.transfInfo[bestTransf].coinMultiplier) { bestTransf = t; }
            }
            double rate = ChestRewardPerSecond(bestTransf) + (GameInfo.baseCoinsPerSecond * CoinMultiplier(bestTransf));
            // Player only gets coins every minute, so round up to the next minute
            return (BigInteger)Math.Ceiling((target - coins) / (rate * 60.0)) * 60;
        }

        public BigInteger SecondsUntilGems(double target, out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            if (target <= gems) { return 0; }
            foreach (TransformationLabel t in transformations)
            {
                if (GameInfo.transfInfo[t].gemMultiplier > GameInfo.transfInfo[bestTransf].gemMultiplier) { bestTransf = t; }
            }
            double rate = /*GameInfo.dailyOfferGemsPerSecond +*/ MiniChestRewardPerSecond(bestTransf) + (GameInfo.baseGemsPerSecond * GemMultiplier(bestTransf));
            // Player only gets coins every minute, so round up to the next minute
            return (BigInteger)Math.Ceiling((target - gems) / (rate * 60.0)) * 60;
        }

        // return true = success
        public bool StatsUp(StatLabel which)
        {
            int l2 = mainMultiplier.levels[which];
            if (l2 >= GameInfo.plrMulCosts.Length) { return false; }
            if (!mainMultiplier.WouldItRemainBalanced(which)) { return false; }
            TransformationLabel bestTransf;
            Play(SecondsUntilStatsUp(which, out bestTransf), bestTransf);
            coins -= GameInfo.plrMulCosts[l2];
            PlayerMultiplier next = new PlayerMultiplier(mainMultiplier);
            ++next.levels[which];
            mainMultiplier = next;
            return true;
        }

        public BigInteger SecondsUntilStatsUp(StatLabel which, out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            int l2 = mainMultiplier.levels[which];
            if (l2 >= GameInfo.plrMulCosts.Length) { return BigInteger.Pow(10, 100); }
            if (!mainMultiplier.WouldItRemainBalanced(which)) { return BigInteger.Pow(10, 100); }
            double neededCoins = GameInfo.plrMulCosts[l2];
            return SecondsUntilCoins(neededCoins, out bestTransf);
        }

        public string StatsInfo()
        {
            return "(End " + mainMultiplier.Multiplier(StatLabel.Endurance)
                + ") (Str " + mainMultiplier.Multiplier(StatLabel.Strength)
                + ") (Psy " + mainMultiplier.Multiplier(StatLabel.Psychic) + ")";
        }
        
        // return true = success
        public bool RankUp()
        {
            if (rank == RankLabel.Omni) { return false; }
            TransformationLabel bestTransf;
            Play(SecondsUntilRankUp(out bestTransf), bestTransf);
            power = 0;
            rank = rank + 1;
            return true;
        }

        public BigInteger SecondsUntilRankUp(out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            if (rank == RankLabel.Omni) { return BigInteger.Pow(10, 100); }
            return SecondsUntilPower(GameInfo.rankInfo[rank + 1].requiredPower, out bestTransf);
        }

        public string RankInfo()
        {
            return "Player rank is " + rank.ToString();
        }

        public bool BuyTransformation(TransformationLabel target)
        {
            if (transformations.Contains(target)) { return false; }
            if (GameInfo.transfInfo[target].obtainedByFusion) { return false; }
            TransformationLabel bestTransf;
            Play(SecondsUntilTransformation(target, out bestTransf), bestTransf);
            coins -= GameInfo.transfInfo[target].cost;
            transformations.Add(target);
            return true;
        }

        public BigInteger SecondsUntilTransformation(TransformationLabel target, out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            if (transformations.Contains(target)) { return BigInteger.Pow(10, 100); }
            if (GameInfo.transfInfo[target].obtainedByFusion) { return BigInteger.Pow(10, 100); }
            return SecondsUntilCoins(GameInfo.transfInfo[target].cost, out bestTransf);
        }

        public string TransformationInfo(TransformationLabel search)
        {
            string s = transformations.Contains(search) ? " including " : " not including ";
            return "Player has " + (transformations.Count - 1).ToString() + " transformation(s)" + s + search.ToString();
        }

        public bool FuseUp()
        {
            if (!RankSufficientForNextFusion()) { return false; }
            TransformationLabel bestTransf;
            Play(SecondsUntilNextFusion(out bestTransf), bestTransf);
            rank = RankLabel.F;
            fusion = fusion + 1;
            transformations.Add(GameInfo.transfFromFusion[fusion]);
            return true;
        }

        private bool RankSufficientForNextFusion()
        {
            if (fusion == FusionLabel.DragonRuler) { return false; }
            return rank >= GameInfo.fusionInfo[fusion + 1].requiredRank;
        }

        public BigInteger SecondsUntilNextFusion(out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            if (!RankSufficientForNextFusion()) { return BigInteger.Pow(10, 100); }
            return SecondsUntilPower(GameInfo.fusionInfo[fusion + 1].requiredPower, out bestTransf);
        }

        public string FusionInfo()
        {
            return "Player fusion is " + fusion.ToString();
        }

        public bool BuyPack(ItemPack pack)
        {
            TransformationLabel bestTransf;
            Play(SecondsUntilBuyPack(pack, 1, out bestTransf), bestTransf);
            gems -= pack.cost;
            AncientItem item = pack.PickItem();
            mostRecentItem = item;
            Dictionary<StatLabel, double> newMFI = new Dictionary<StatLabel, double>(multipliersFromItems);
            if (!items.ContainsKey(item)) { items.Add(item, 0); }
            if (items.ContainsKey(item))
            foreach (var kv in multipliersFromItems)
            {
                double lastMul = item.GetMultiplier(kv.Key, items[item]);
                double newMul = item.GetMultiplier(kv.Key, items[item] + 1);
                newMFI[kv.Key] += (newMul - lastMul);
            }
            ++items[item];
            multipliersFromItems = newMFI;
            ++totalItems;
            return true;
        }

        // Average buy until obtain the item
        public bool BuyPackForItem(AncientItem target)
        {
            if (items.ContainsKey(target) && items[target] > 0.99) { return false; }
            TransformationLabel bestTransf;
            double amount = target.ExpectedTries();
            Play(SecondsUntilBuyPack(target.pack, amount, out bestTransf), bestTransf);
            gems -= target.pack.cost * amount;
            Dictionary<StatLabel, double> newMFI = new Dictionary<StatLabel, double>(multipliersFromItems);
            mostRecentItem = target;
            foreach (AncientItem item in target.pack.items)
            {
                if (!items.ContainsKey(item)) { items.Add(item, 0); }
                double copiesGotten = amount * item.NormalizedChance();
                foreach (var kv in multipliersFromItems)
                {
                    double lastMul = item.GetMultiplier(kv.Key, items[item]);
                    double newMul = item.GetMultiplier(kv.Key, items[item] + copiesGotten);
                    newMFI[kv.Key] += (newMul - lastMul);
                }
                items[item] += copiesGotten;
                totalItems += copiesGotten;
            }
            multipliersFromItems = newMFI;
            return true;
        }

        public BigInteger SecondsUntilMeanItem(AncientItem item, out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            if (items.ContainsKey(item) && items[item] > 0.99) { return BigInteger.Pow(10, 100); }
            double tries = item.ExpectedTries();
            return SecondsUntilBuyPack(item.pack, tries, out bestTransf);
        }

        // It takes 5 seconds to open a pack
        public BigInteger SecondsUntilBuyPack(ItemPack pack, double amount, out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            if (gems >= pack.cost * amount) { return (BigInteger)Math.Ceiling(5 * amount); }
            return SecondsUntilGems(pack.cost * amount, out bestTransf) + (BigInteger)Math.Ceiling(5 * amount);
        }

        public string ItemInfo()
        {
            if (items.Count == 0) { return "No items"; }
            // Unique items is not total items!
            string test = mostRecentItem.stat.ToString();
            return Math.Round(totalItems) + " items, Last one was " + mostRecentItem.name + " #" + items[mostRecentItem].ToString("G7")
                + "\n\t" + "Total item multipliers: (S " + multipliersFromItems[StatLabel.Strength].ToString("G7") 
                + ")(E " + multipliersFromItems[StatLabel.Endurance].ToString("G7")
                + ")(P " + multipliersFromItems[StatLabel.Psychic].ToString("G7") + ")";
        }

        public List<PlayerGoal> AvailableGoals()
        {
            List<PlayerGoal> ret = new List<PlayerGoal>();
            foreach (PlayerGoal g in GameInfo.plrGoals)
            {
                if (g.CanExecute(this)) { ret.Add(g); }
            }
            return ret;
        }
    }

    class GameInfo
    {
        public static Random rand = new Random();

        public static double baseCoinsPerSecond = 25.0 / 60.0;
        public static double baseGemsPerSecond = 500.0 / 60;
        // Oops! The offer changes every day!
        //public static double dailyOfferGemsPerSecond = 100000.0 / (60 * 60 * 24);

        public static double[] plrMulCosts = new double[] {100, 200, 500, 750,
        1000, 2000, 3000, 5000, 7500,
        10000, 15000, 20000, 25000, 35000,
        50000, 67500, 100000, 125000, 165000,
        220000, 300000, 400000, 550000, 800000,
        1100000, 1500000, 2000000, 3000000, 5000000,
        7500000, 8000000, 8500000, 9000000, 10000000,
        10250000, 10500000, 10750000, 11000000, 11250000,
        11500000, 11750000, 12000000, 12250000, 12500000,
        12750000, 13000000, 13500000, 13750000, 15000000,
        16500000, 18000000};

        public static Area[] areas = new Area[] {
            new Area(0, 1), new Area(100, 2), new Area(1000, 5), new Area(10000, 20), new Area(1e5, 100),
            new Area(5e6, 750), new Area(5e8, 10000), new Area(5e10, 1.2e5), new Area(3e13, 3e6), new Area(1e16, 1e8),
            new Area(2.5e19, 3e9), new Area(1e26, 9e10), new Area(5e30, 2.5e12), new Area(1e35, 7.5e13), new Area(6e39, 2.25e15),
            new Area(1.5e44, 7.5e16), new Area(1.5e50, 3e18), new Area(4e55, 3.3e18), new Area(3e58, 3.6e18), new Area(9e59, 3e20),
            new Area(6.3e64, 3e22), new Area(4e69, 7.5e24), new Area(8.2e75, 1e27)
        };

        public static Dictionary<RankLabel, Rank> rankInfo = new Dictionary<RankLabel, Rank>()
        {
            { RankLabel.F, new Rank(0, 1, 1) },
            { RankLabel.E, new Rank(1e6, 2, 2) },
            { RankLabel.D, new Rank(5e7, 4, 3) },
            { RankLabel.C, new Rank(1e10, 10, 4) },
            { RankLabel.B, new Rank(5e12, 35, 5) },
            { RankLabel.A, new Rank(1e15, 100, 6) },
            { RankLabel.S, new Rank(1e18, 500, 7) },
            { RankLabel.SS, new Rank(5e23, 2500, 8) },
            { RankLabel.SSS, new Rank(4e29, 10000, 9) },
            { RankLabel.X, new Rank(4e33, 100000, 10) },
            { RankLabel.Y, new Rank(4e38, 750000, 11) },
            { RankLabel.Z, new Rank(3e42, 9e6, 12) },
            { RankLabel.XYZ, new Rank(3e48, 1e8, 13) },
            { RankLabel.Legend, new Rank(1.3e54, 2e9, 14) },
            { RankLabel.Immortal, new Rank(9e55, 4e10, 15) },
            { RankLabel.TimeRuler, new Rank(8e57, 1e12, 15) },
            { RankLabel.UniRuler, new Rank(1.5e67, 1e14, 16) },
            { RankLabel.MulRuler, new Rank(2.7e72, 1e17, 16) },
            { RankLabel.Omni, new Rank(1e80, 1e21, 17) },
        };

        public static Dictionary<TransformationLabel, Transformation> transfInfo = new Dictionary<TransformationLabel, Transformation>()
        {
            { TransformationLabel.None, new Transformation(0, 1, 1, 1) },
            { TransformationLabel.BuffNoob, new Transformation(25000, 1.2, 1, 1.75) },
            { TransformationLabel.GoldGrd, new Transformation(250000, 1, 1.1, 1) },
            { TransformationLabel.Shadow, new Transformation(1e6, 3, 1, 4) },
            { TransformationLabel.Void, new Transformation(1.5e6, 5, 1, 7) },
            { TransformationLabel.FDragon, new Transformation(2e6, 4, 1.15, 6) },
            { TransformationLabel.SciBorg, new Transformation(2e6, 5.5, 1, 6) },
            { TransformationLabel.Ocean, new Transformation(2.5e6, 7, 1, 15) },
            { TransformationLabel.GWarrior, new Transformation(2.5e6, 7, 1.1, 7) },
            { TransformationLabel.ELord, new Transformation(3e6, 10, 1.125, 10) },
            { TransformationLabel.Thunder, new Transformation(4e6, 12, 1.075, 12) },
            { TransformationLabel.MDragon, new Transformation(5e6, 13.5, 1.135, 13.5) },

            { TransformationLabel.Ninja, new Transformation(5000, 1, 1, 1.25) },
            { TransformationLabel.GemGrd, new Transformation(250000, 1, 1, 3) },
            { TransformationLabel.FKnight, new Transformation(400000, 1, 1, 2.5) },
            { TransformationLabel.AWarrior, new Transformation(750000, 1, 1, 3.5) },

            { TransformationLabel.Werewolf, new Transformation(1.5) },
            { TransformationLabel.Minotaur, new Transformation(2) },
            { TransformationLabel.Gryphon, new Transformation(2.5) },
            { TransformationLabel.Phoenix, new Transformation(3.5) },
            { TransformationLabel.Yeti, new Transformation(4.5) },
            { TransformationLabel.Hydra, new Transformation(6) },
            { TransformationLabel.Reaper, new Transformation(12) },
            { TransformationLabel.DragonRuler, new Transformation(15) },
        };

        public static Dictionary<FusionLabel, Fusion> fusionInfo = new Dictionary<FusionLabel, Fusion>()
        {
            { FusionLabel.None, new Fusion(RankLabel.F, 0, 1) },
            { FusionLabel.Werewolf, new Fusion(RankLabel.S, 5e21, 25) },
            { FusionLabel.Minotaur, new Fusion(RankLabel.SS, 2e27, 500) },
            { FusionLabel.Gryphon, new Fusion(RankLabel.SSS, 3e32, 10000) },
            { FusionLabel.Phoenix, new Fusion(RankLabel.X, 7e36, 250000) },
            { FusionLabel.Yeti, new Fusion(RankLabel.Y, 5e41, 5e6) },
            { FusionLabel.Hydra, new Fusion(RankLabel.Z, 2e46, 1e8) },
            { FusionLabel.Reaper, new Fusion(RankLabel.XYZ, 1.5e52, 2e9) },
            { FusionLabel.DragonRuler, new Fusion(RankLabel.TimeRuler, 2.4e62, 2e11) },
        };

        public static Dictionary<FusionLabel, TransformationLabel> transfFromFusion = new Dictionary<FusionLabel, TransformationLabel>()
        {
            { FusionLabel.Werewolf, TransformationLabel.Werewolf },
            { FusionLabel.Minotaur, TransformationLabel.Minotaur },
            { FusionLabel.Gryphon, TransformationLabel.Gryphon },
            { FusionLabel.Phoenix, TransformationLabel.Phoenix },
            { FusionLabel.Yeti, TransformationLabel.Yeti },
            { FusionLabel.Hydra, TransformationLabel.Hydra },
            { FusionLabel.Reaper, TransformationLabel.Reaper },
            { FusionLabel.DragonRuler, TransformationLabel.DragonRuler },
        };

        public static Chest[] chests = new Chest[]
        {
            new Chest(1000), // Golden chest
            new Chest(2500), // Group chest (assume player is in group)
            // new Chest(2500), // Premium chest (assume player is premium)
            new Chest(5000, RankLabel.C), // Mirage
            new Chest(7500, RankLabel.A), // Deep sea
            new Chest(10000, RankLabel.SS), // Robot
            new Chest(15000, RankLabel.X), // Ninja
            new Chest(5000, 1000), // Sky chest (assume player can fly)
            new Chest(2500, FusionLabel.Gryphon), // Space chest (need legend rank?)
            new Chest(5000, FusionLabel.Gryphon), // Space group chest
            new Chest(6250, FusionLabel.Gryphon), // Corrupted forest
            new Chest(8750, FusionLabel.Yeti), // Glacial
            new Chest(15000, FusionLabel.Reaper), // Volcanic
        };

        public static List<PlayerGoal> plrGoals = null; // initialized in static constructor

        public static Dictionary<Rarity, int[]> itemGradeRequirements = new Dictionary<Rarity, int[]>()
        {
            { Rarity.Common, new int[5] { 1, 10, 20, 50, 100 } },
            { Rarity.Rare, new int[5] { 1, 10, 20, 40, 70 } },
            { Rarity.Epic, new int[5] { 1, 3, 10, 20, 40 } },
            { Rarity.Legendary, new int[5] { 1, 2, 5, 10, 20 } },
            { Rarity.Celestial, new int[5] { 1, 2, 4, 7, 10 } },
            { Rarity.God, new int[5] { 1, 2, 3, 4, 5 } },
        };

        public static List<ItemPack> itemPacks = null;

        static GameInfo()
        {
            // load items table
            itemPacks = new List<ItemPack>();
            Console.WriteLine("Loading item table...");
            // This file name assumes we run debug mode within Visual Studio. Fix later
            using (var rh = new StreamReader("..\\..\\AncientItems.csv"))
            {
                rh.ReadLine();
                rh.ReadLine();
                ItemPack currPack = null;
                StatLabel storedStat = StatLabel.All;
                double storedChance = -1;
                Rarity storedRarity = Rarity.Common;
                double storedSellWorth = 0;
                double[] storedMultipliers = new double[5] { 0, 0, 0, 0, 0 };
                while (!rh.EndOfStream)
                {
                    string[] cells = rh.ReadLine().Split(',');
                    if (cells[0] != "")
                    {
                        if (currPack != null) { itemPacks.Add(currPack); }
                        currPack = new ItemPack();
                        currPack.name = cells[0];
                        currPack.cost = double.Parse(cells[1]);
                        currPack.items = new HashSet<AncientItem>();
                    }
                    else
                    {
                        AncientItem newItem = new AncientItem();
                        newItem.name = cells[1];
                        switch (cells[2])
                        {
                            case "S": storedStat = StatLabel.Strength; break;
                            case "E": storedStat = StatLabel.Endurance; break;
                            case "P": storedStat = StatLabel.Psychic; break;
                            case "A": storedStat = StatLabel.All; break;
                            default: break;
                        }
                        newItem.stat = storedStat;
                        if (cells[3] != "") { storedChance = double.Parse(cells[3]); }
                        newItem.chance = storedChance;
                        switch (cells[4])
                        {
                            case "C": storedRarity = Rarity.Common; break;
                            case "R": storedRarity = Rarity.Rare; break;
                            case "E": storedRarity = Rarity.Epic; break;
                            case "L": storedRarity = Rarity.Legendary; break;
                            case "S": storedRarity = Rarity.Celestial; break;
                            case "G": storedRarity = Rarity.God; break;
                            default: break;
                        }
                        newItem.rarity = storedRarity;
                        if (cells[5] != "") { storedSellWorth = double.Parse(cells[5]); }
                        newItem.sellWorth = storedSellWorth;
                        for (int i = 0; i < 5; ++i)
                        {
                            if (cells[6 + i] != "") { storedMultipliers[i] = double.Parse(cells[6 + i]); }
                        }
                        newItem.multipliers = new double[5];
                        for (int i = 0; i < 5; ++i) { newItem.multipliers[i] = storedMultipliers[i]; }
                        newItem.pack = currPack;
                        currPack.items.Add(newItem);
                    }
                }
                if (currPack != null) { itemPacks.Add(currPack); }
            }
            Console.WriteLine("Item table loading is over.");

            // add goals
            plrGoals = new List<PlayerGoal>();
            plrGoals.Add(new StatsUpGoal(StatLabel.Endurance));
            plrGoals.Add(new StatsUpGoal(StatLabel.Psychic));
            plrGoals.Add(new StatsUpGoal(StatLabel.Strength));
            plrGoals.Add(new RankUpGoal());
            plrGoals.Add(new FusionGoal());
            foreach (var kv in transfInfo)
            {
                if (kv.Key == TransformationLabel.None || kv.Value.obtainedByFusion) { continue; }
                plrGoals.Add(new TransformationGoal(kv.Key));
            }
            // foreach (ItemPack pack in itemPacks) { plrGoals.Add(new OneItemPackGoal(pack)); }
            foreach (ItemPack pack in itemPacks)
            {
                foreach (AncientItem item in pack.items) { plrGoals.Add(new AncientItemGoal(item)); }
            }
        }
    }

    abstract class PathEndCondition
    {
        public abstract bool ConditionMet(Player plr);
    }

    class EndPathByRank : PathEndCondition
    {
        public RankLabel requiredRank;
        public override bool ConditionMet(Player plr) { return plr.rank >= requiredRank; }
        public EndPathByRank(RankLabel rr) { requiredRank = rr; }
    }

    // instructions to progress through the game
    class PlayerPath
    {
        public Player initPlr;
        public PathEndCondition end;
        public List<PlayerGoal> path;
        public BigInteger completionTime;

        // Damper so we don't buy individual packs forever, since it's so fast
        public const int PACK_RESISTANCE = 5;

        public PlayerPath(Player ip, PathEndCondition e, List<PlayerGoal> p)
        {
            initPlr = new Player(ip);
            end = e;
            path = new List<PlayerGoal>();
            // Compress path
            int i = 0;
            while (i < p.Count)
            {
                if (p[i] is StatsUpGoal)
                {
                    int multiple = 1;
                    while (i + multiple < p.Count && p[i + multiple] is StatsUpGoal
                        && (p[i] as StatsUpGoal).which == (p[i + multiple] as StatsUpGoal).which) { ++multiple; }
                    if (multiple == 1) { path.Add(p[i]); }
                    else { path.Add(new MultiStatsUpGoal((p[i] as StatsUpGoal).which, multiple)); }
                    i += multiple - 1;
                }
                else if (p[i] is RankUpGoal)
                {
                    int multiple = 1;
                    while (i + multiple < p.Count && p[i + multiple] is RankUpGoal) { ++multiple; }
                    if (multiple == 1) { path.Add(p[i]); }
                    else { path.Add(new MultiRankUpGoal(multiple)); }
                    i += multiple - 1;
                }
                else { path.Add(p[i]); }
                ++i;
            }

            // Validate path and calculate completion time
            Player fakePlr = new Player(initPlr);
            for (int k = 0; k < path.Count; ++k)
            {
                if (!path[k].Execute(fakePlr)) { throw new Exception("Path is invalid"); }
            }
            completionTime = BigInteger.Max(1, fakePlr.seconds);
        }

        public void Print()
        {
            Player plr = new Player(initPlr);
            Console.WriteLine("--------");
            int i = 1;
            foreach (PlayerGoal g in path)
            {
                Console.WriteLine("Goal " + i.ToString() + "/" + path.Count + ": " + g.ToString());
                Console.WriteLine("Time: " + g.TimeUntilExecute(plr));
                if (!g.CanExecute(plr)) { throw new Exception("Path is invalid"); }
                g.Execute(plr);
                Console.WriteLine("Result: " + g.ViewResult(plr));
                Console.WriteLine("--------");
                ++i;
            }
            Console.WriteLine("Total time: " + plr.seconds);
            Console.WriteLine("--------");
        }

        // choose a random goal weighted slightly by low completion time: quicker goals are slightly more likely to be chosen.
        private static PlayerGoal ChooseRandomNextGoal(Player plr)
        {
            List<PlayerGoal> goals = plr.AvailableGoals();
            if (goals.Count == 0) { return null; }
            if (goals.Count == 1) { return goals[0]; }
            double[] chances = new double[goals.Count];
            double sum = 0;
            for (int i = 0; i < goals.Count; ++i)
            {
                BigInteger t = goals[i].TimeUntilExecute(plr);
                // We always choose a 0 second goal if one is available
                if (t == 0) { return goals[i]; }
                
                if (goals[i] is OneItemPackGoal) { t *= PACK_RESISTANCE * ((BigInteger)plr.totalItems) + 1; }
                chances[i] = Math.Sqrt(1.0 / (double)t);
                sum += chances[i];
            }
            for (int i = 0; i < goals.Count; ++i) { chances[i] /= sum; }
            double sum2 = 0;
            double rand = GameInfo.rand.NextDouble();
            int j = 0;
            while (j < goals.Count - 1)
            {
                sum2 += chances[j];
                if (rand <= sum2) { break; }
                ++j;
            }
            return goals[j];
        }

        // This is probably a terrible path, but we need to start somewhere.
        public static PlayerPath RandomPath(Player init, PathEndCondition end)
        {
            Player plr = new Player(init);
            List<PlayerGoal> path = new List<PlayerGoal>();
            while (!end.ConditionMet(plr))
            {
                PlayerGoal goal = ChooseRandomNextGoal(plr);
                if (goal == null) { break; }
                path.Add(goal);
                goal.Execute(plr);
            }
            return new PlayerPath(init, end, path);
        }

        // The greedy algorithm always executes the goal that takes the least time.
        // Obsolete: it will keep buy the Basic pack forever!
        public static PlayerPath GreedyPath(Player init, PathEndCondition end)
        {
            Player plr = new Player(init);
            List<PlayerGoal> path = new List<PlayerGoal>();
            List<PlayerGoal> goals = plr.AvailableGoals();
            while (goals.Count > 0 && !end.ConditionMet(plr))
            {
                goals.Sort((a, b) => a.TimeUntilExecute(plr).CompareTo(b.TimeUntilExecute(plr)));
                path.Add(goals[0]);
                goals[0].Execute(plr);
                goals = plr.AvailableGoals();
            }
            return new PlayerPath(init, end, path);
        }

        // Key: a point, Value: b point
        private static Dictionary<int, int> SplicePoints(PlayerPath a, PlayerPath b)
        {
            List<Player> aStates = new List<Player>();
            List<Player> bStates = new List<Player>();
            for (int i = 0; i < a.path.Count; ++i)
            {
                aStates.Add(new Player((i == 0) ? a.initPlr : new Player(aStates[i - 1])));
                if (!a.path[i].Execute(aStates[i])) { throw new Exception("Path A is somehow invalid"); }
            }
            for (int i = 0; i < b.path.Count; ++i)
            {
                bStates.Add(new Player((i == 0) ? b.initPlr : new Player(bStates[i - 1])));
                if (!b.path[i].Execute(bStates[i])) { throw new Exception("Path B is somehow invalid"); }
            }

            Dictionary<int, int> points = new Dictionary<int, int>();
            for (int ai = 1; ai < a.path.Count - 1; ++ai)
            {
                for (int bi = 1; bi < b.path.Count - 1; ++bi)
                {
                    if (!points.ContainsKey(ai) && aStates[ai].IsSimilar(bStates[bi])) { points[ai] = bi; }
                }
            }
            return points;
        }

        // take two paths and create the two spliced versions, if possible (else just return the same two paths)
        public static Tuple<PlayerPath, PlayerPath> Splice(PlayerPath a, PlayerPath b)
        {
            if (a.path.Count < 3 || b.path.Count < 3) { return new Tuple<PlayerPath, PlayerPath>(a, b); }
            Dictionary<int, int> splicePoints;
            try { splicePoints = SplicePoints(a, b); }
            catch (Exception e) { Console.WriteLine(e.Message); return new Tuple<PlayerPath, PlayerPath>(a, b); }
            if (splicePoints.Count == 0) { return new Tuple<PlayerPath, PlayerPath>(a, b); }
            // Find the most central splice point to both
            int lowestSpliceScore = int.MaxValue;
            int spliceAPoint = -1;

            foreach (var kv in splicePoints)
            {
                int spliceScore = Math.Abs(kv.Key - a.path.Count / 2) + Math.Abs(kv.Value - b.path.Count / 2);
                if (spliceScore < lowestSpliceScore)
                {
                    spliceAPoint = kv.Key;
                    lowestSpliceScore = spliceScore;
                }
            }

            List<PlayerGoal> abpath = new List<PlayerGoal>();
            for (int i = 0; i < spliceAPoint; ++i) { abpath.Add(a.path[i]); }
            for (int i = splicePoints[spliceAPoint]; i < b.path.Count; ++i) { abpath.Add(b.path[i]); }
            List<PlayerGoal> bapath = new List<PlayerGoal>();
            for (int i = 0; i < splicePoints[spliceAPoint]; ++i) { bapath.Add(b.path[i]); }
            for (int i = spliceAPoint; i < a.path.Count; ++i) { bapath.Add(a.path[i]); }
            PlayerPath ab;
            PlayerPath ba;
            try
            {
                ab = new PlayerPath(a.initPlr, b.end, abpath);
                ba = new PlayerPath(b.initPlr, a.end, bapath);
            }
            catch
            {
                return new Tuple<PlayerPath, PlayerPath>(a, b);
            }
            return new Tuple<PlayerPath, PlayerPath>(ab, ba);
        }
    }

    class GeneticSimulator
    {
        public int poolSize = 150;
        public int keepRandom = 30;
        public int generations = 500;
        public double mutationRate = 0.1; // percent of paths where one node may be changed
        public double introduceOldRecordsRate = 0.3; // percent of random paths that are turned onto old records
        public PlayerPath Simulate(Player plr, PathEndCondition end)
        {

            PlayerPath[] pool = new PlayerPath[poolSize];
            for (int i = 0; i < poolSize; ++i) { pool[i] = PlayerPath.RandomPath(plr, end); }
            PlayerPath bestPath = pool[0];
            BigInteger bestPathTime = pool[0].completionTime;
            List<PlayerPath> oldBestPaths = new List<PlayerPath>();
            for (int i = 0; i < generations; ++i)
            {
                {
                    Console.WriteLine("Generation " + i.ToString() + "/" + generations.ToString());
                    Console.WriteLine("Current best time is " + bestPathTime);
                }
                PlayerPath[] newPool = new PlayerPath[poolSize];

                for (int l = 0; l < poolSize - keepRandom; l += 2)
                {
                    double[] chances = new double[poolSize];
                    double sum = 0;
                    for (int k = 0; k < poolSize; ++k)
                    {
                        chances[k] = Math.Pow(1.0 / (double)pool[k].completionTime, 0.5 + 0.5 * (i / (double)generations));
                        sum += chances[k];
                    }
                    for (int k = 0; k < poolSize; ++k) { chances[k] /= sum; }

                    // Choose first parent
                    double sum2 = 0.0;
                    double rand = GameInfo.rand.NextDouble();
                    int j = 0;
                    while (j < poolSize - 1)
                    {
                        sum2 += chances[j];
                        if (rand <= sum2) { break; }
                        ++j;
                    }
                    // Choose second parent
                    for (int k = 0; k < poolSize; ++k)
                    {
                        if (k == j) { continue; }
                        else { chances[k] /= (1.0 - chances[j]); }
                    }
                    chances[j] = 0.0;
                    sum2 = 0.0;
                    rand = GameInfo.rand.NextDouble();
                    int j2 = 0;
                    while (j2 < poolSize - 1)
                    {
                        sum2 += chances[j2];
                        if (rand <= sum2) { break; }
                        ++j2;
                    }

                    PlayerPath parentA = pool[j];
                    PlayerPath parentB = pool[j2];
                    Tuple<PlayerPath, PlayerPath> children = PlayerPath.Splice(parentA, parentB);
                    newPool[l] = children.Item1;
                    newPool[l + 1] = children.Item2;
                }
                for (int k = poolSize - 1; k >= poolSize - keepRandom; --k)
                {
                    // Start introducing old good solutions
                    if (GameInfo.rand.NextDouble() < introduceOldRecordsRate && oldBestPaths.Count > 10)
                    {
                        newPool[k] = oldBestPaths[GameInfo.rand.Next(oldBestPaths.Count - 5)];
                    }
                    else { newPool[k] = PlayerPath.RandomPath(plr, end); }
                }

                // Random mutations: insertion and deletion
                for (int k = 0; k < poolSize; ++k)
                {
                    if (GameInfo.rand.NextDouble() < mutationRate) { continue; }
                    if (newPool[k] == null) { throw new Exception("How?"); }
                    for (int l = 0; l < newPool[k].path.Count / 50; ++l)
                    {
                        int mutidx = GameInfo.rand.Next(newPool[k].path.Count);
                        List<PlayerGoal> mutp = new List<PlayerGoal>(newPool[k].path);
                        PlayerGoal mut = mutp[mutidx];
                        if (GameInfo.rand.Next(2) == 1) // deletion
                        {
                            if (mut is MultiStatsUpGoal && (mut as MultiStatsUpGoal).multiple > 1)
                            {
                                mutp[mutidx] = new MultiStatsUpGoal((mut as MultiStatsUpGoal).which, (mut as MultiStatsUpGoal).multiple - 1);
                            }
                            else if (!(mut is RankUpGoal) && !(mut is MultiRankUpGoal) && !(mut is FusionGoal))
                            {
                                mutp.RemoveAt(mutidx);
                            }
                        }
                        else // Insertion
                        {
                            if (mut is MultiStatsUpGoal)
                            {
                                mutp[mutidx] = new MultiStatsUpGoal((mut as MultiStatsUpGoal).which, (mut as MultiStatsUpGoal).multiple + 1);
                            }
                            else if (mut is StatsUpGoal) { mut = new MultiStatsUpGoal((mut as StatsUpGoal).which, 2); }
                            else
                            {
                                PlayerGoal insertGoal = GameInfo.plrGoals[GameInfo.rand.Next(GameInfo.plrGoals.Count)];
                                if (!(insertGoal is RankUpGoal) && !(mut is MultiRankUpGoal) && !(insertGoal is FusionGoal))
                                {
                                    mutp.Insert(mutidx, insertGoal);
                                }
                            }
                        }

                        try { newPool[k] = new PlayerPath(newPool[k].initPlr, newPool[k].end, mutp); }
                        catch { }
                    }
                }

                pool = newPool;

                for (int k = 0; k < poolSize; ++k)
                {
                    if (pool[k].completionTime < bestPathTime)
                    {
                        bestPathTime = pool[k].completionTime;
                        bestPath = pool[k];
                    }
                }
                oldBestPaths.Add(bestPath);
            }
            
            return bestPath;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Player plr = new Player();
            PlayerPath greedy = PlayerPath.GreedyPath(plr, new EndPathByRank(RankLabel.Omni));
            PlayerPath winner = (new GeneticSimulator()).Simulate(plr, new EndPathByRank(RankLabel.Omni));
            Console.WriteLine("Greedy solution");
            greedy.Print();
            Console.WriteLine();
            Console.WriteLine("Genetic solution");
            winner.Print();

            Console.ReadKey();
        }
    }
}
