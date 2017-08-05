using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace lib.Replays
{
    [TestFixture]
    [Explicit]
    public class ReplayRepoTests
    {
        [Test]
        public void SaveReplay_ShouldSave()
        {
            var repo = new ReplayRepo(true);
            var meta = CreateReplayMeta();
            var map = MapLoader.LoadMap(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\maps\oxford2-sparse.json")).Map;
            var sw1 = Stopwatch.StartNew();
            JsonConvert.SerializeObject(map).CalculateMd5();
            Console.WriteLine(sw1.Elapsed);
            var data = new ReplayData(map, Enumerable.Range(0, map.Rivers.Length).Select(i => new ClaimMove(0, i, i + 1)).ToArray(), new Future[0]);
            var sw = Stopwatch.StartNew();
            repo.SaveReplay(meta, data);
            Console.WriteLine(sw.Elapsed);
            sw.Restart();
            var savedData = repo.GetData(meta.DataId);
            Console.WriteLine(sw.Elapsed);
            Assert.NotNull(savedData);
        }

        private static ReplayMeta CreateReplayMeta()
        {
            var meta = new ReplayMeta(
                DateTime.UtcNow,
                "player",
                0,
                1,
                new[]
                {
                    new ScoreModel
                    {
                        Punter = 0,
                        Score = 42
                    }
                }
            );
            return meta;
        }

        [Test, Explicit]
        public void GetRecentMetas_Should()
        {
            var repo = new ReplayRepo(true);
            
            var metas = repo.GetRecentMetas();
            
            Assert.That(metas[0].Timestamp > metas[1].Timestamp);
        }
    }
}