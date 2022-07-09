using System;
using System.Collections.Generic;
using System.Numerics;

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
        public int log2Multiplier;
        // assume upgrading one at a time in a balanced way
        public enum Progress { One = -2, Two, All }
        public Progress progress = Progress.All;

        public PlayerMultiplier()
        {
            log2Multiplier = 0;
        }

        public double Multiplier() { return Math.Pow(2, log2Multiplier); }

        public PlayerMultiplier(PlayerMultiplier old)
        {
            log2Multiplier = old.log2Multiplier;
            progress = old.progress;
        }

        public static bool operator ==(PlayerMultiplier a, PlayerMultiplier b) { return a.log2Multiplier == b.log2Multiplier && a.progress == b.progress; }
        public static bool operator !=(PlayerMultiplier a, PlayerMultiplier b) { return a.log2Multiplier != b.log2Multiplier || a.progress != b.progress; }
        public override bool Equals(object obj)
        {
            if (obj is PlayerMultiplier)
            {
                return log2Multiplier == (obj as PlayerMultiplier).log2Multiplier && progress == (obj as PlayerMultiplier).progress;
            }
            return false;
        }
        public override int GetHashCode() { return log2Multiplier * 3 + (int)progress; }
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

    public enum TransformationLabel { None, BuffNoob, Guardian, Shadow, Void, FDragon, SciBorg, Ocean, Warrior, ELord, Thunder, MDragon,
        Werewolf, Minotaur, Gryphon, Phoenix, Yeti, Hydra, Reaper, DragonRuler
    }

    struct Transformation
    {
        public double cost;
        public double powerMultiplier;
        public double coinMultiplier;
        public bool obtainedByFusion;

        public Transformation(double c, double pm, double cm) // transf.s you can buy
        {
            cost = c;
            powerMultiplier = pm;
            coinMultiplier = cm;
            obtainedByFusion = false;
        }

        public Transformation(double pm) // transf.s obtained by fusion
        {
            cost = double.PositiveInfinity;
            powerMultiplier = pm;
            coinMultiplier = 1;
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
            if (transf == TransformationLabel.Guardian) { rps *= 1.25; }
            return rps;
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

    class StatsUpGoal : PlayerGoal
    {
        public override string ToString() { return "Increase stats"; }
        public override BigInteger TimeUntilExecute(Player plr) { return plr.SecondsUntilStatsUp(out _); }
        public override bool Execute(Player plr) { return plr.StatsUp(); }
        public override string ViewResult(Player plr) { return plr.StatsInfo(); }
    }

    // This makes paths a lot shorter: process to combine StatsUpGoals
    class MultiStatsUpGoal : PlayerGoal
    {
        public int multiple;
        public override string ToString() { return "Increase stats x" + multiple.ToString(); }
        public override BigInteger TimeUntilExecute(Player plr) {
            Player fakePlr = new Player(plr);
            BigInteger totalTime = 0;
            for (int i = 0; i < multiple; ++i)
            {
                BigInteger subTime = fakePlr.SecondsUntilStatsUp(out _);
                if (subTime > MAX_GOAL_TIME) { return subTime; }
                totalTime += subTime;
                bool check = fakePlr.StatsUp();
                if (!check) { return BigInteger.Pow(10, 100); }
            }
            return totalTime;
        }
        public override bool Execute(Player plr) {
            for (int i = 0; i < multiple; ++i)
            {
                bool sub = plr.StatsUp();
                if (!sub) { return false; }
            }
            return true;
        }
        public override string ViewResult(Player plr) { return plr.StatsInfo(); }
        public MultiStatsUpGoal(int m) { multiple = m; }
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

    class Player
    {
        public BigInteger seconds = 0; // How long the player was in the game

        public double coins = 100;
        public double power = 0;
        // power normal multiplier
        public PlayerMultiplier mainMultiplier = new PlayerMultiplier();
        public RankLabel rank = RankLabel.F;
        public HashSet<TransformationLabel> transformations = new HashSet<TransformationLabel>() { TransformationLabel.None };
        public FusionLabel fusion = FusionLabel.None;

        public Player()
        {
            // collect initial chest reward instantly, all future rewards will be averaged
            coins += ChestRewardPerSecond(TransformationLabel.None) * (double)Chest.CHEST_TIME;
        }

        // Make sure to copy any new fields that you add later, like gems and items
        public Player(Player old)
        {
            seconds = old.seconds;
            coins = old.coins;
            power = old.power;
            mainMultiplier = new PlayerMultiplier(old.mainMultiplier);
            rank = old.rank;
            transformations = new HashSet<TransformationLabel>(old.transformations);
            fusion = old.fusion;
        } 

        // Splice point player is similar enough to another player
        public bool IsSimilar(Player other)
        {
            bool rtf = (rank == other.rank) && (fusion == other.fusion) && transformations.SetEquals(other.transformations);
            bool m = (mainMultiplier == other.mainMultiplier);
            return rtf && m;
        }

        public double PowerMultiplier(TransformationLabel transf)
        {
            double rm = GameInfo.rankInfo[rank].powerMultiplier;
            double tm = GameInfo.transfInfo[transf].powerMultiplier;
            double fm = GameInfo.fusionInfo[fusion].powerMultiplier;
            return mainMultiplier.Multiplier() * rm * tm * fm;
        }

        public double CoinMultiplier(TransformationLabel transf)
        {
            double rm = GameInfo.rankInfo[rank].coinMultiplier;
            double tm = GameInfo.transfInfo[transf].coinMultiplier;
            return rm * tm;
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

        public void Play(BigInteger secs, TransformationLabel transf)
        {
            seconds += secs;
            coins += (double)secs * ((GameInfo.baseCoinsPerSecond * CoinMultiplier(transf)) + ChestRewardPerSecond(transf));
            power += (double)secs * PowerMultiplier(transf);
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

        // return true = success
        public bool StatsUp()
        {
            int l2 = mainMultiplier.log2Multiplier;
            if (l2 >= GameInfo.plrMulCosts.Length) { return false; }
            TransformationLabel bestTransf;
            Play(SecondsUntilStatsUp(out bestTransf), bestTransf);
            coins -= GameInfo.plrMulCosts[l2];
            PlayerMultiplier next = new PlayerMultiplier(mainMultiplier);
            if (next.progress == PlayerMultiplier.Progress.All)
            {
                next.progress = PlayerMultiplier.Progress.One;
                ++next.log2Multiplier;
            }
            else if (next.progress == PlayerMultiplier.Progress.One) { next.progress = PlayerMultiplier.Progress.Two; }
            else { next.progress = PlayerMultiplier.Progress.All; }
            mainMultiplier = next;
            return true;
        }

        public BigInteger SecondsUntilStatsUp(out TransformationLabel bestTransf)
        {
            bestTransf = TransformationLabel.None;
            int l2 = mainMultiplier.log2Multiplier;
            if (l2 >= GameInfo.plrMulCosts.Length) { return BigInteger.Pow(10, 100); }
            double neededCoins = 0;
            if (mainMultiplier.progress == PlayerMultiplier.Progress.All) { neededCoins = GameInfo.plrMulCosts[l2]; }
            else { neededCoins = GameInfo.plrMulCosts[l2 - 1]; }
            return SecondsUntilCoins(neededCoins, out bestTransf);
        }

        public string StatsInfo()
        {
            return "Player multiplier is " + mainMultiplier.Multiplier().ToString() + " for " + mainMultiplier.progress.ToString().ToLower() + " of the stats";
        }
        
        // return true = success
        public bool RankUp()
        {
            if (rank == RankLabel.Omni) { return false; }
            TransformationLabel bestTransf;
            Play(SecondsUntilRankUp(out bestTransf), bestTransf);
            power = 0;
            mainMultiplier = new PlayerMultiplier();
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
            power = 0;
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
            { TransformationLabel.None, new Transformation(0, 1, 1) },
            { TransformationLabel.BuffNoob, new Transformation(25000, 1.2, 1) },
            { TransformationLabel.Guardian, new Transformation(250000, 1, 1.1) },
            { TransformationLabel.Shadow, new Transformation(1e6, 3, 1) },
            { TransformationLabel.Void, new Transformation(1.5e6, 5, 1) },
            { TransformationLabel.FDragon, new Transformation(2e6, 4, 1.15) },
            { TransformationLabel.SciBorg, new Transformation(2e6, 5.5, 1) },
            { TransformationLabel.Ocean, new Transformation(2.5e6, 7, 1) },
            { TransformationLabel.Warrior, new Transformation(2.5e6, 7, 1.1) },
            { TransformationLabel.ELord, new Transformation(3e6, 10, 1.125) },
            { TransformationLabel.Thunder, new Transformation(4e6, 12, 1.075) },
            { TransformationLabel.MDragon, new Transformation(5e6, 13.5, 1.135) },

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

        static GameInfo()
        {
            plrGoals = new List<PlayerGoal>();
            plrGoals.Add(new StatsUpGoal());
            plrGoals.Add(new RankUpGoal());
            plrGoals.Add(new FusionGoal());
            foreach (var kv in transfInfo)
            {
                if (kv.Key == TransformationLabel.None || kv.Value.obtainedByFusion) { continue; }
                plrGoals.Add(new TransformationGoal(kv.Key));
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
        Player initPlr;
        PathEndCondition end;
        List<PlayerGoal> path;
        public BigInteger completionTime;

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
                    while (i + multiple < p.Count && p[i + multiple] is StatsUpGoal) { ++multiple; }
                    if (multiple == 1) { path.Add(p[i]); }
                    else { path.Add(new MultiStatsUpGoal(multiple)); }
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
            Console.WriteLine("Total time (s): " + plr.seconds);
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
                chances[i] = Math.Sqrt(1.0 / (double)goals[i].TimeUntilExecute(plr));
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
        // This is a fine initial path, and should be in the starting pool.
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
                    if (aStates[ai].IsSimilar(bStates[bi])) { points.Add(ai, bi); }
                }
            }
            return points;
        }

        // take two paths and create the two spliced versions, if possible (else just return the same two paths)
        public static Tuple<PlayerPath, PlayerPath> Splice(PlayerPath a, PlayerPath b)
        {
            if (a.path.Count < 3 || b.path.Count < 3) { return new Tuple<PlayerPath, PlayerPath>(a, b); }
            Dictionary<int, int> splicePoints = SplicePoints(a, b);
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
        public PlayerPath Simulate(Player plr, PathEndCondition end)
        {
            PlayerPath[] pool = new PlayerPath[poolSize];
            pool[0] = PlayerPath.GreedyPath(plr, end);
            for (int i = 1; i < poolSize; ++i) { pool[i] = PlayerPath.RandomPath(plr, end); }
            for (int i = 0; i < generations; ++i)
            {
                if (i % 10 == 0) { Console.WriteLine("Generation " + i.ToString() + "/" + generations.ToString()); }
                PlayerPath[] newPool = new PlayerPath[poolSize];

                for (int l = 0; l < poolSize; l += 2)
                {
                    double[] chances = new double[poolSize];
                    double sum = 0;
                    for (int k = 0; k < poolSize; ++k)
                    {
                        chances[k] = Math.Sqrt(1.0 / (double)pool[k].completionTime);
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
                for (int k = poolSize - 1; k >= poolSize - keepRandom; --k) { pool[k] = PlayerPath.RandomPath(plr, end); }
                pool = newPool;
            }
            return pool[0];
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Player plr = new Player();
            plr.mainMultiplier = new PlayerMultiplier();
            plr.mainMultiplier.log2Multiplier = 22;
            plr.mainMultiplier.progress = PlayerMultiplier.Progress.All;
            plr.rank = RankLabel.Y;
            plr.fusion = FusionLabel.Phoenix;
            plr.transformations = new HashSet<TransformationLabel>()
            {
                TransformationLabel.BuffNoob, TransformationLabel.ELord
            };
            PlayerPath greedy = PlayerPath.GreedyPath(plr, new EndPathByRank(RankLabel.Omni));
            PlayerPath winner = (new GeneticSimulator()).Simulate(plr, new EndPathByRank(RankLabel.Omni));
            Console.WriteLine("Greedy solution");
            greedy.Print();
            Console.WriteLine("Genetic solution");
            winner.Print();

            Console.ReadKey();
        }
    }
}
