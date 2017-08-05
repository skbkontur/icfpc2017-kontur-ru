﻿using lib;
using lib.Ai;
using lib.Scores.Simple;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace worker
{
    

    public class DummyAIExperiment : IExperiment
    {
        public IEnumerable<Tuple<PlayerWithParams, long>> Play(Task task)
        {
            var players = task.Players;
            var gamers = players
                .Select(player => new DummyAi(player.Params["Param"]) { Name = player.Name })
                .Cast<IAi>()
                .ToList();

            var gameSimulator = new GameSimulatorRunner(new SimpleScoreCalculator());

            var results = gameSimulator.SimulateGame(
                gamers, MapLoader.LoadMapByName("sample.json").Map);

            var report = results.Select(result => Tuple.Create(
                players.Where(player => player.Name == result.Gamer.Name).Single(),
                result.Score))
                .ToList();
            return report;
        }
    }
}