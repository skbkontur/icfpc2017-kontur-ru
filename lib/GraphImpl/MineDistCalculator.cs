﻿using System;
using System.Collections.Generic;
using System.Linq;
using lib.StateImpl;
using NUnit.Framework;
using Shouldly;
#pragma warning disable 618

namespace lib.GraphImpl
{
    public class MineDistanceInfo
    {
        public MineDistanceInfo(int distance, int prevSiteId)
        {
            Distance = distance;
            PrevSiteId = prevSiteId;
        }

        public int Distance;
        public int PrevSiteId;
    }

    public class MineDistCalculator : IService
    {
        public Impl impl;

        public class ServiceState
        {
            public Dictionary<int, Dictionary<int, MineDistanceInfo>> distFromMines;
        }

        public class Impl
        {
            private readonly Graph graph;
            public readonly Dictionary<int, Dictionary<int, MineDistanceInfo>> distFromMines;

            public Impl(Graph graph, Dictionary<int, Dictionary<int, MineDistanceInfo>> distFromMines)
            {
                this.graph = graph;
                this.distFromMines = distFromMines;
            }

            public Impl(Graph graph)
            {
                this.graph = graph;

                distFromMines = new Dictionary<int, Dictionary<int, MineDistanceInfo>>();
                foreach (var vertex in graph.Vertexes.Values)
                {
                    if (vertex.IsMine)
                    {
                        var dists = CalcDist(vertex.Id);
                        distFromMines.Add(vertex.Id, dists);
                    }
                }
            }

            public int GetDist(int mineId, int vertexId)
            {
                if (!distFromMines.ContainsKey(mineId))
                    throw new InvalidOperationException();
                if (!distFromMines[mineId].ContainsKey(vertexId))
                    return -1;
                return distFromMines[mineId][vertexId].Distance;
            }

            public IEnumerable<int> GetReversedPath(int mineId, int vertexId)
            {
                var current = vertexId;
                while (current >= 0)
                {
                    var info = GetInfo(mineId, current);
                    if (info == null) yield break;
                    yield return current;
                    current = info.PrevSiteId;
                }
            }

            public MineDistanceInfo GetInfo(int mineId, int vertexId)
            {
                if (!distFromMines.ContainsKey(mineId))
                    throw new InvalidOperationException();
                if (!distFromMines[mineId].ContainsKey(vertexId))
                    return null;
                return distFromMines[mineId][vertexId];
            }

            //(v, dist)

            private Dictionary<int, MineDistanceInfo> CalcDist(int start)
            {
                var dist = new Dictionary<int, MineDistanceInfo>();
                var queue = new Queue<int>();

                dist[start] = new MineDistanceInfo(0, -1);
                queue.Enqueue(start);

                while (queue.Any())
                {
                    int v = queue.Dequeue();
                    foreach (var edge in graph.Vertexes[v].Edges)
                    {
                        int u = edge.To;
                        if (dist.ContainsKey(u))
                            continue;
                        dist.Add(u, new MineDistanceInfo(dist[v].Distance + 1, v));
                        queue.Enqueue(u);
                    }
                }

                return dist;
            }
        }

        public void Setup(State state, IServices services)
        {
            var graph = services.Get<GraphService>(state).Graph;
            impl = new Impl(graph);
            state.mdc = new ServiceState { distFromMines = impl.distFromMines };
        }

        public void ApplyNextState(State state, IServices services)
        {
            var graph = services.Get<GraphService>(state).Graph;
            impl = impl ?? new Impl(graph, state.mdc.distFromMines);
        }

        public int GetDist(int mineId, int vertexId)
        {
            return impl.GetDist(mineId, vertexId);
        }

        public IEnumerable<int> GetReversedPath(int mineId, int vertexId)
        {
            return impl.GetReversedPath(mineId, vertexId);
        }
    }

    [TestFixture]
    public class MineDistCalculator_Tests
    {

        [Test]
        public void TestDisjoint()
        {
            var graph = new Graph();

            graph.AddVertex(1, true);
            graph.AddVertex(2);

            var calculator = new MineDistCalculator.Impl(graph);
            Assert.AreEqual(-1, calculator.GetDist(1, 2));
        }

        [Test]
        public void TestGraphWithTwoMines()
        {
            var graph = new Graph();

            graph.AddVertex(1);
            graph.AddVertex(2);
            graph.AddVertex(3, true);
            graph.AddVertex(4, true);
            graph.AddVertex(5);
            graph.AddVertex(100);

            graph.AddEdge(1, 4);
            graph.AddEdge(1, 2);
            graph.AddEdge(1, 3);
            graph.AddEdge(2, 3);
            graph.AddEdge(3, 5);

            var calculator = new MineDistCalculator.Impl(graph);

            Assert.AreEqual(1, calculator.GetDist(3, 1));
            Assert.AreEqual(1, calculator.GetDist(3, 2));
            Assert.AreEqual(0, calculator.GetDist(3, 3));
            Assert.AreEqual(2, calculator.GetDist(3, 4));
            Assert.AreEqual(1, calculator.GetDist(3, 5));

            Assert.AreEqual(1, calculator.GetDist(4, 1));
            Assert.AreEqual(2, calculator.GetDist(4, 2));
            Assert.AreEqual(2, calculator.GetDist(4, 3));
            Assert.AreEqual(0, calculator.GetDist(4, 4));
            Assert.AreEqual(3, calculator.GetDist(4, 5));
            calculator.GetReversedPath(4, 5).ShouldBe(new[] { 5, 3, 1, 4 });
            calculator.GetReversedPath(3, 2).ShouldBe(new[] { 2, 3 });
            calculator.GetReversedPath(3, 3).ShouldBe(new[] { 3 });
            calculator.GetReversedPath(4, 100).ShouldBeEmpty();
        }
    }
}