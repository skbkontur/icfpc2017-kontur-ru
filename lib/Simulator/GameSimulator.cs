using System;
using System.Collections.Generic;
using System.Linq;
using lib.Ai;
using lib.StateImpl;
using lib.Structures;
using MoreLinq;

namespace lib
{
    public class GameSimulator : ISimulator
    {
        private Map map;
        private readonly Settings settings;
        private List<Tuple<IAi, State, IServices>> punters;
        private int currentPunter = 0;
        private readonly List<Move> moves;
        private int turnsAmount;
        private Move[] turnMoves;

        public IList<Future[]> Futures { get; }

        public GameSimulator(Map map, Settings settings)
        {
            this.map = map;
            this.settings = settings;
            moves = new List<Move>();
            Futures = new List<Future[]>();
        }

        public void StartGame(List<IAi> gamers)
        {
            turnMoves = gamers.Select((_, i) => Move.Pass(i)).ToArray();
            punters = gamers.Select((g, i) => Tuple.Create(g, new State
            {
                map = map,
                punter = i,
                punters = gamers.Count,
                settings = settings
            }, (IServices)new Services())).ToList();
            for (int i = 0; i < punters.Count; i++)
            {
                var ai = punters[i].Item1;
                var state = punters[i].Item2;
                var services = punters[i].Item3;
                var setupDecision = ai.Setup(state, services);
                Futures.Add(ValidateFutures(setupDecision.futures));
                state.aiSetupDecision = new AiInfoSetupDecision
                {
                    name = ai.Name,
                    version = ai.Version,
                    futures = setupDecision.futures,
                    reason = setupDecision.reason
                };
            }

            turnsAmount = map.Rivers.Length;
        }

        public GameState NextMove()
        {
            if (turnsAmount <= 0)
                return new GameState(map, moves.TakeLast(punters.Count).ToList(), true);

            var ai = punters[currentPunter].Item1;
            var state = punters[currentPunter].Item2;
            var services = punters[currentPunter].Item3;
            state.map = map;
            state.turns.Add(new TurnState{moves = turnMoves.ToArray(), aiMoveDecision = state.lastAiMoveDecision});
            services.ApplyNextState(state);
            var moveDecision = ai.GetNextMove(state, services);
            state.lastAiMoveDecision = new AiInfoMoveDecision
            {
                name = ai.Name,
                version = ai.Version,
                move = moveDecision.move,
                reason = moveDecision.reason
            };

            map = map.ApplyMove(moveDecision.move);
            turnMoves[currentPunter] = moveDecision.move;
            moves.Add(moveDecision.move);
            currentPunter = (currentPunter + 1) % punters.Count;
            turnsAmount--;
            return new GameState(map, moves.TakeLast(punters.Count).ToList(), false);
        }

        private Future[] ValidateFutures(Future[] futures)
            => futures
                .Where(e => map.Mines.Contains(e.source) && !map.Mines.Contains(e.target))
                .GroupBy(e => e.source)
                .Select(e => e.Last())
                .ToArray();
    }
}