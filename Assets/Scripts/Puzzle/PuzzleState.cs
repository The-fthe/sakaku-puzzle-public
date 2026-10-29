using System;
using System.Collections.Generic;

namespace SakakuPuzzle
{
    public enum Direction
    {
        Up,
        Right,
        Down,
        Left
    }

    public enum TileCorner
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    public readonly struct RouteStep
    {
        public int TileIndex { get; }
        public TileCorner EntryCorner { get; }
        public TileCorner ExitCorner { get; }

        public RouteStep(
            int tileIndex,
            TileCorner entryCorner,
            TileCorner exitCorner)
        {
            TileIndex = tileIndex;
            EntryCorner = entryCorner;
            ExitCorner = exitCorner;
        }
    }

    public readonly struct SolvedJourney
    {
        public int GoalIndex { get; }
        public IReadOnlyList<RouteStep> Steps { get; }

        public SolvedJourney(
            int goalIndex,
            IReadOnlyList<RouteStep> steps)
        {
            GoalIndex = goalIndex;
            Steps = steps;
        }
    }

    public readonly struct PuzzleGoal
    {
        public string Label { get; }
        public int TileIndex { get; }
        public TileCorner Corner { get; }

        public PuzzleGoal(string label, int tileIndex, TileCorner corner)
        {
            Label = label;
            TileIndex = tileIndex;
            Corner = corner;
        }
    }

    public enum RouteTermination
    {
        ReachedGoal,
        ReachedWrongBoundary,
        ClosedNetwork
    }

    public readonly struct RouteEvaluation
    {
        public RouteTermination Termination { get; }
        public IReadOnlyList<RouteStep> Steps { get; }
        public int ReachedGoalCount { get; }
        public int TotalGoalCount { get; }
        public bool ReachedGoal =>
            Termination == RouteTermination.ReachedGoal;

        public RouteEvaluation(
            RouteTermination termination,
            IReadOnlyList<RouteStep> steps,
            int reachedGoalCount,
            int totalGoalCount)
        {
            Termination = termination;
            Steps = steps;
            ReachedGoalCount = reachedGoalCount;
            TotalGoalCount = totalGoalCount;
        }
    }

    public sealed class PuzzleDefinition
    {
        public int Width { get; }
        public int Height { get; }
        public int Version { get; }
        public int InitialState { get; }
        public int SolutionState { get; }
        public int MinimumSteps { get; }
        public int Seed { get; }
        public int StartIndex { get; }
        public TileCorner StartCorner { get; }
        public IReadOnlyList<PuzzleGoal> Goals { get; }

        public int TileCount => Width * Height;
        public int VertexCount => (Width + 1) * (Height + 1);
        public int GoalIndex => Goals[0].TileIndex;

        PuzzleDefinition(
            int width,
            int height,
            int version,
            int initialState,
            int solutionState,
            int minimumSteps,
            int seed,
            int startIndex,
            TileCorner startCorner,
            IReadOnlyList<PuzzleGoal> goals)
        {
            Width = width;
            Height = height;
            Version = version;
            InitialState = initialState;
            SolutionState = solutionState;
            MinimumSteps = minimumSteps;
            Seed = seed;
            StartIndex = startIndex;
            StartCorner = startCorner;
            Goals = goals;
        }

        #region Stage Generation

        public static PuzzleDefinition CreateStage()
        {
            return CreateStage(1, 208015862);
        }

        public static PuzzleDefinition CreateStage(int version, int seed)
        {
            const int WIDTH = 5;
            const int HEIGHT = 5;
            const int START_INDEX = 20;
            const TileCorner START_CORNER = TileCorner.BottomLeft;
            const int SOLUTION_STATE = 25405621;

            var temporaryVersion = Math.Clamp(version, 1, 4);
            var temporaryGoalCount = temporaryVersion;
            var temporaryMinimumSteps = temporaryGoalCount * 2;
            var temporaryGoalVertices = new[] { 6, 32, 17, 34 };
            var temporaryRequiredTiles = new[]
            {
                5, 10, 15, 20, 22, 17, 11, 14
            };
            var temporaryInitialState = SOLUTION_STATE;
            for (var temporaryIndex = 0;
                 temporaryIndex < temporaryMinimumSteps;
                 temporaryIndex++)
            {
                temporaryInitialState ^=
                    1 << temporaryRequiredTiles[temporaryIndex];
            }

            var temporaryGoals = CreateGoals(
                WIDTH,
                HEIGHT,
                new ArraySegment<int>(
                    temporaryGoalVertices,
                    0,
                    temporaryGoalCount));
            return new PuzzleDefinition(
                WIDTH,
                HEIGHT,
                temporaryVersion,
                temporaryInitialState,
                SOLUTION_STATE,
                temporaryMinimumSteps,
                seed,
                START_INDEX,
                START_CORNER,
                temporaryGoals);
        }

        static List<PuzzleGoal> CreateGoals(
            int width,
            int height,
            IReadOnlyList<int> vertices)
        {
            var temporaryGoals = new List<PuzzleGoal>(vertices.Count);
            for (var temporaryIndex = 0;
                 temporaryIndex < vertices.Count;
                 temporaryIndex++)
            {
                GetBoundaryEndpoint(
                    width,
                    height,
                    vertices[temporaryIndex],
                    out var temporaryTileIndex,
                    out var temporaryCorner);
                temporaryGoals.Add(
                    new PuzzleGoal(
                        (temporaryIndex + 1).ToString(),
                        temporaryTileIndex,
                        temporaryCorner));
            }

            return temporaryGoals;
        }

        static void GetBoundaryEndpoint(
            int width,
            int height,
            int vertex,
            out int tileIndex,
            out TileCorner corner)
        {
            var temporaryX = vertex % (width + 1);
            var temporaryY = vertex / (width + 1);

            if (temporaryY == 0)
            {
                if (temporaryX == width)
                {
                    tileIndex = width - 1;
                    corner = TileCorner.TopRight;
                    return;
                }

                tileIndex = temporaryX;
                corner = TileCorner.TopLeft;
                return;
            }

            if (temporaryY == height)
            {
                if (temporaryX == width)
                {
                    tileIndex = (height - 1) * width + width - 1;
                    corner = TileCorner.BottomRight;
                    return;
                }

                tileIndex = (height - 1) * width + temporaryX;
                corner = TileCorner.BottomLeft;
                return;
            }

            if (temporaryX == 0)
            {
                tileIndex = temporaryY * width;
                corner = TileCorner.TopLeft;
                return;
            }

            tileIndex = temporaryY * width + width - 1;
            corner = TileCorner.TopRight;
        }

        static bool TryFindSolvedCandidateCombination(
            PuzzleDefinition definition,
            int encodedState,
            int remainingDepth,
            int nextCandidate,
            int flipMask,
            IReadOnlyList<int> candidateTiles,
            List<int> result)
        {
            if (remainingDepth == 0)
            {
                if (!IsSolvedState(definition, encodedState ^ flipMask))
                {
                    return false;
                }

                if (result != null)
                {
                    result.Clear();
                    foreach (var temporaryTile in candidateTiles)
                    {
                        if ((flipMask & (1 << temporaryTile)) != 0)
                        {
                            result.Add(temporaryTile);
                        }
                    }
                }

                return true;
            }

            for (var temporaryCandidate = nextCandidate;
                 temporaryCandidate <=
                 candidateTiles.Count - remainingDepth;
                 temporaryCandidate++)
            {
                var temporaryTile = candidateTiles[temporaryCandidate];
                if (TryFindSolvedCandidateCombination(
                    definition,
                    encodedState,
                    remainingDepth - 1,
                    temporaryCandidate + 1,
                    flipMask ^ (1 << temporaryTile),
                    candidateTiles,
                    result))
                {
                    return true;
                }
            }

            return false;
        }

        static void Shuffle<T>(IList<T> values, Random random)
        {
            for (var temporaryIndex = values.Count - 1;
                 temporaryIndex > 0;
                 temporaryIndex--)
            {
                var temporarySwapIndex =
                    random.Next(temporaryIndex + 1);
                (values[temporaryIndex], values[temporarySwapIndex]) =
                    (values[temporarySwapIndex], values[temporaryIndex]);
            }
        }

        #endregion

        #region Shared Graph Helpers

        internal static bool IsSolvedState(
            PuzzleDefinition definition,
            int encodedState)
        {
            var temporaryStart = GetVertexId(
                definition.Width,
                definition.StartIndex,
                definition.StartCorner);
            var temporarySearch = SearchConnectedVertices(
                definition.Width,
                definition.Height,
                encodedState,
                temporaryStart);
            foreach (var temporaryGoal in definition.Goals)
            {
                var temporaryGoalVertex = GetVertexId(
                    definition.Width,
                    temporaryGoal.TileIndex,
                    temporaryGoal.Corner);
                if (!temporarySearch.Visited[temporaryGoalVertex])
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TryFindSolvedCombination(
            PuzzleDefinition definition,
            int encodedState,
            int remainingDepth,
            int nextTile,
            int flipMask,
            List<int> result)
        {
            if (remainingDepth == 0)
            {
                if (!IsSolvedState(definition, encodedState ^ flipMask))
                {
                    return false;
                }

                if (result != null)
                {
                    result.Clear();
                    for (var temporaryTile = 0;
                         temporaryTile < definition.TileCount;
                         temporaryTile++)
                    {
                        if ((flipMask & (1 << temporaryTile)) != 0)
                        {
                            result.Add(temporaryTile);
                        }
                    }
                }

                return true;
            }

            for (var temporaryTile = nextTile;
                 temporaryTile <= definition.TileCount - remainingDepth;
                 temporaryTile++)
            {
                if (TryFindSolvedCombination(
                    definition,
                    encodedState,
                    remainingDepth - 1,
                    temporaryTile + 1,
                    flipMask ^ (1 << temporaryTile),
                    result))
                {
                    return true;
                }
            }

            return false;
        }

        internal static VertexSearch SearchConnectedVertices(
            int width,
            int height,
            int encodedState,
            int startVertex)
        {
            var temporaryEdges =
                BuildRoadGraph(width, height, encodedState);
            var temporarySearch =
                new VertexSearch((width + 1) * (height + 1));
            var temporaryQueue = new Queue<int>();
            temporarySearch.Visited[startVertex] = true;
            temporaryQueue.Enqueue(startVertex);

            while (temporaryQueue.Count > 0)
            {
                var temporaryVertex = temporaryQueue.Dequeue();
                foreach (var temporaryEdge in temporaryEdges[temporaryVertex])
                {
                    if (temporarySearch.Visited[temporaryEdge.ToVertex])
                    {
                        continue;
                    }

                    temporarySearch.Visited[temporaryEdge.ToVertex] = true;
                    temporarySearch.Distance[temporaryEdge.ToVertex] =
                        temporarySearch.Distance[temporaryVertex] + 1;
                    temporarySearch.HasPrevious[temporaryEdge.ToVertex] = true;
                    temporarySearch.Previous[temporaryEdge.ToVertex] =
                        new SearchPrevious(
                            temporaryVertex,
                            temporaryEdge.TileIndex,
                            temporaryEdge.EntryCorner,
                            temporaryEdge.ExitCorner);
                    temporaryQueue.Enqueue(temporaryEdge.ToVertex);
                }
            }

            return temporarySearch;
        }

        internal static List<SearchEdge>[] BuildRoadGraph(
            int width,
            int height,
            int encodedState)
        {
            var temporaryEdges =
                new List<SearchEdge>[(width + 1) * (height + 1)];
            for (var temporaryIndex = 0;
                 temporaryIndex < temporaryEdges.Length;
                 temporaryIndex++)
            {
                temporaryEdges[temporaryIndex] = new List<SearchEdge>();
            }

            for (var temporaryTileIndex = 0;
                 temporaryTileIndex < width * height;
                 temporaryTileIndex++)
            {
                var temporaryAlternate =
                    (encodedState & (1 << temporaryTileIndex)) != 0;
                var temporaryFirstCorner = temporaryAlternate
                    ? TileCorner.TopLeft
                    : TileCorner.BottomLeft;
                var temporarySecondCorner = temporaryAlternate
                    ? TileCorner.BottomRight
                    : TileCorner.TopRight;
                var temporaryFirstVertex = GetVertexId(
                    width,
                    temporaryTileIndex,
                    temporaryFirstCorner);
                var temporarySecondVertex = GetVertexId(
                    width,
                    temporaryTileIndex,
                    temporarySecondCorner);
                temporaryEdges[temporaryFirstVertex].Add(
                    new SearchEdge(
                        temporarySecondVertex,
                        temporaryTileIndex,
                        temporaryFirstCorner,
                        temporarySecondCorner));
                temporaryEdges[temporarySecondVertex].Add(
                    new SearchEdge(
                        temporaryFirstVertex,
                        temporaryTileIndex,
                        temporarySecondCorner,
                        temporaryFirstCorner));
            }

            return temporaryEdges;
        }

        public static int GetVertexId(
            int width,
            int tileIndex,
            TileCorner corner)
        {
            var temporaryRow = tileIndex / width;
            var temporaryColumn = tileIndex % width;
            var temporaryX =
                corner is TileCorner.TopRight or TileCorner.BottomRight
                    ? temporaryColumn + 1
                    : temporaryColumn;
            var temporaryY =
                corner is TileCorner.BottomLeft or TileCorner.BottomRight
                    ? temporaryRow + 1
                    : temporaryRow;
            return temporaryY * (width + 1) + temporaryX;
        }

        internal static List<RouteStep> BuildRoute(
            IReadOnlyList<SearchPrevious> previous,
            IReadOnlyList<bool> hasPrevious,
            int startVertex,
            int endVertex)
        {
            var temporaryReverse = new List<RouteStep>();
            var temporaryVertex = endVertex;
            while (temporaryVertex != startVertex &&
                   hasPrevious[temporaryVertex])
            {
                var temporaryPrevious = previous[temporaryVertex];
                temporaryReverse.Add(
                    new RouteStep(
                        temporaryPrevious.TileIndex,
                        temporaryPrevious.EntryCorner,
                        temporaryPrevious.ExitCorner));
                temporaryVertex = temporaryPrevious.FromVertex;
            }

            temporaryReverse.Reverse();
            return temporaryReverse;
        }

        internal static List<RouteStep>
            BuildStraightestShortestRoute(
                int width,
                int height,
                int encodedState,
                int startVertex,
                int endVertex)
        {
            const int DIRECTION_COUNT = 5;
            const int NO_DIRECTION = 0;

            var temporaryEdges =
                BuildRoadGraph(width, height, encodedState);
            var temporaryStateCount =
                (width + 1) *
                (height + 1) *
                DIRECTION_COUNT;
            var temporaryDistances = new int[temporaryStateCount];
            var temporaryTurns = new int[temporaryStateCount];
            var temporaryPreviousStates =
                new int[temporaryStateCount];
            var temporaryPreviousSteps =
                new RouteStep[temporaryStateCount];
            var temporaryHasPrevious =
                new bool[temporaryStateCount];
            Array.Fill(temporaryDistances, int.MaxValue);
            Array.Fill(temporaryTurns, int.MaxValue);
            Array.Fill(temporaryPreviousStates, -1);

            var temporaryStartState =
                startVertex * DIRECTION_COUNT + NO_DIRECTION;
            var temporaryQueue = new Queue<int>();
            temporaryDistances[temporaryStartState] = 0;
            temporaryTurns[temporaryStartState] = 0;
            temporaryQueue.Enqueue(temporaryStartState);

            while (temporaryQueue.Count > 0)
            {
                var temporaryState = temporaryQueue.Dequeue();
                var temporaryVertex =
                    temporaryState / DIRECTION_COUNT;
                var temporaryPreviousDirection =
                    temporaryState % DIRECTION_COUNT;

                foreach (var temporaryEdge in
                         temporaryEdges[temporaryVertex])
                {
                    var temporaryDirection = GetTravelDirection(
                        width,
                        temporaryVertex,
                        temporaryEdge.ToVertex);
                    var temporaryNextState =
                        temporaryEdge.ToVertex *
                        DIRECTION_COUNT +
                        temporaryDirection;
                    var temporaryNextDistance =
                        temporaryDistances[temporaryState] + 1;
                    var temporaryNextTurns =
                        temporaryTurns[temporaryState] +
                        (temporaryPreviousDirection != NO_DIRECTION &&
                         temporaryPreviousDirection !=
                         temporaryDirection
                            ? 1
                            : 0);
                    if (temporaryNextDistance >
                        temporaryDistances[temporaryNextState] ||
                        (temporaryNextDistance ==
                         temporaryDistances[temporaryNextState] &&
                         temporaryNextTurns >=
                         temporaryTurns[temporaryNextState]))
                    {
                        continue;
                    }

                    temporaryDistances[temporaryNextState] =
                        temporaryNextDistance;
                    temporaryTurns[temporaryNextState] =
                        temporaryNextTurns;
                    temporaryPreviousStates[temporaryNextState] =
                        temporaryState;
                    temporaryPreviousSteps[temporaryNextState] =
                        new RouteStep(
                            temporaryEdge.TileIndex,
                            temporaryEdge.EntryCorner,
                            temporaryEdge.ExitCorner);
                    temporaryHasPrevious[temporaryNextState] = true;
                    temporaryQueue.Enqueue(temporaryNextState);
                }
            }

            var temporaryBestEndState = -1;
            for (var temporaryDirection = 1;
                 temporaryDirection < DIRECTION_COUNT;
                 temporaryDirection++)
            {
                var temporaryEndState =
                    endVertex *
                    DIRECTION_COUNT +
                    temporaryDirection;
                if (!temporaryHasPrevious[temporaryEndState])
                {
                    continue;
                }

                if (temporaryBestEndState < 0 ||
                    temporaryDistances[temporaryEndState] <
                    temporaryDistances[temporaryBestEndState] ||
                    (temporaryDistances[temporaryEndState] ==
                     temporaryDistances[temporaryBestEndState] &&
                     temporaryTurns[temporaryEndState] <
                     temporaryTurns[temporaryBestEndState]))
                {
                    temporaryBestEndState = temporaryEndState;
                }
            }

            var temporaryReverse = new List<RouteStep>();
            var temporaryCurrentState = temporaryBestEndState;
            while (temporaryCurrentState >= 0 &&
                   temporaryCurrentState != temporaryStartState &&
                   temporaryHasPrevious[temporaryCurrentState])
            {
                temporaryReverse.Add(
                    temporaryPreviousSteps[temporaryCurrentState]);
                temporaryCurrentState =
                    temporaryPreviousStates[temporaryCurrentState];
            }

            temporaryReverse.Reverse();
            return temporaryReverse;
        }

        static int GetTravelDirection(
            int width,
            int fromVertex,
            int toVertex)
        {
            var temporaryRowWidth = width + 1;
            var temporaryFromX = fromVertex % temporaryRowWidth;
            var temporaryFromY = fromVertex / temporaryRowWidth;
            var temporaryToX = toVertex % temporaryRowWidth;
            var temporaryToY = toVertex / temporaryRowWidth;
            var temporaryRight = temporaryToX > temporaryFromX;
            var temporaryDown = temporaryToY > temporaryFromY;
            if (temporaryRight)
            {
                return temporaryDown ? 3 : 1;
            }

            return temporaryDown ? 4 : 2;
        }

        #endregion

        internal readonly struct SearchEdge
        {
            public int ToVertex { get; }
            public int TileIndex { get; }
            public TileCorner EntryCorner { get; }
            public TileCorner ExitCorner { get; }

            public SearchEdge(
                int toVertex,
                int tileIndex,
                TileCorner entryCorner,
                TileCorner exitCorner)
            {
                ToVertex = toVertex;
                TileIndex = tileIndex;
                EntryCorner = entryCorner;
                ExitCorner = exitCorner;
            }
        }

        internal readonly struct SearchPrevious
        {
            public int FromVertex { get; }
            public int TileIndex { get; }
            public TileCorner EntryCorner { get; }
            public TileCorner ExitCorner { get; }

            public SearchPrevious(
                int fromVertex,
                int tileIndex,
                TileCorner entryCorner,
                TileCorner exitCorner)
            {
                FromVertex = fromVertex;
                TileIndex = tileIndex;
                EntryCorner = entryCorner;
                ExitCorner = exitCorner;
            }
        }

        internal sealed class VertexSearch
        {
            public bool[] Visited { get; }
            public int[] Distance { get; }
            public SearchPrevious[] Previous { get; }
            public bool[] HasPrevious { get; }

            public VertexSearch(int vertexCount)
            {
                Visited = new bool[vertexCount];
                Distance = new int[vertexCount];
                Previous = new SearchPrevious[vertexCount];
                HasPrevious = new bool[vertexCount];
            }
        }
    }

    public sealed class PuzzleState
    {
        readonly PuzzleDefinition _definition;
        readonly int[] _rotationCounts;

        public int EncodedState { get; private set; }
        public int Steps { get; private set; }
        public bool IsSolved =>
            PuzzleDefinition.IsSolvedState(_definition, EncodedState);

        public PuzzleState(PuzzleDefinition definition)
        {
            _definition = definition;
            _rotationCounts = new int[_definition.TileCount];
            Reset();
        }

        #region Public API

        public bool Rotate(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= _definition.TileCount)
            {
                return false;
            }

            EncodedState ^= 1 << tileIndex;
            _rotationCounts[tileIndex]++;
            Steps++;
            return true;
        }

        public void Reset()
        {
            EncodedState = _definition.InitialState;
            Steps = 0;
            Array.Clear(_rotationCounts, 0, _rotationCounts.Length);
        }

        public int GetRotationCount(int tileIndex)
        {
            return tileIndex >= 0 && tileIndex < _rotationCounts.Length
                ? _rotationCounts[tileIndex]
                : 0;
        }

        public int CountRecordedRotations()
        {
            var temporaryTotal = 0;
            foreach (var temporaryCount in _rotationCounts)
            {
                temporaryTotal += temporaryCount;
            }

            return temporaryTotal;
        }

        public bool IsAlternateOrientation(int tileIndex)
        {
            return (EncodedState & (1 << tileIndex)) != 0;
        }

        public List<int> GetReachableTileIndices()
        {
            var temporaryStartVertex =
                PuzzleDefinition.GetVertexId(
                    _definition.Width,
                    _definition.StartIndex,
                    _definition.StartCorner);
            var temporarySearch =
                PuzzleDefinition.SearchConnectedVertices(
                    _definition.Width,
                    _definition.Height,
                    EncodedState,
                    temporaryStartVertex);
            var temporaryEdges =
                PuzzleDefinition.BuildRoadGraph(
                    _definition.Width,
                    _definition.Height,
                    EncodedState);
            var temporaryReachableTiles =
                new HashSet<int>();

            for (var temporaryVertex = 0;
                 temporaryVertex < temporarySearch.Visited.Length;
                 temporaryVertex++)
            {
                if (!temporarySearch.Visited[temporaryVertex])
                {
                    continue;
                }

                foreach (var temporaryEdge in
                         temporaryEdges[temporaryVertex])
                {
                    if (temporarySearch.Visited[
                            temporaryEdge.ToVertex])
                    {
                        temporaryReachableTiles.Add(
                            temporaryEdge.TileIndex);
                    }
                }
            }

            var temporaryResult =
                new List<int>(temporaryReachableTiles);
            temporaryResult.Sort();
            return temporaryResult;
        }

        public List<int> FindShortestSolution()
        {
            if (IsSolved)
            {
                return new List<int>();
            }

            var temporaryKnownSolution = new List<int>();
            for (var temporaryTile = 0;
                 temporaryTile < _definition.TileCount;
                 temporaryTile++)
            {
                var temporaryDifference =
                    (EncodedState ^ _definition.SolutionState) &
                    (1 << temporaryTile);
                if (temporaryDifference != 0)
                {
                    temporaryKnownSolution.Add(temporaryTile);
                }
            }

            return temporaryKnownSolution;
        }

        public RouteEvaluation TraceCurrentRoute()
        {
            var temporaryStart = PuzzleDefinition.GetVertexId(
                _definition.Width,
                _definition.StartIndex,
                _definition.StartCorner);
            var temporarySearch =
                PuzzleDefinition.SearchConnectedVertices(
                    _definition.Width,
                    _definition.Height,
                    EncodedState,
                    temporaryStart);
            var temporaryReachedGoals = 0;
            var temporarySteps = new List<RouteStep>();
            var temporaryGoalVertices = new HashSet<int>();

            foreach (var temporaryGoal in _definition.Goals)
            {
                var temporaryGoalVertex = PuzzleDefinition.GetVertexId(
                    _definition.Width,
                    temporaryGoal.TileIndex,
                    temporaryGoal.Corner);
                temporaryGoalVertices.Add(temporaryGoalVertex);
                if (!temporarySearch.Visited[temporaryGoalVertex])
                {
                    continue;
                }

                temporaryReachedGoals++;
                temporarySteps.AddRange(
                    PuzzleDefinition.BuildRoute(
                        temporarySearch.Previous,
                        temporarySearch.HasPrevious,
                        temporaryStart,
                        temporaryGoalVertex));
            }

            if (temporaryReachedGoals == _definition.Goals.Count)
            {
                return new RouteEvaluation(
                    RouteTermination.ReachedGoal,
                    temporarySteps,
                    temporaryReachedGoals,
                    _definition.Goals.Count);
            }

            var temporaryWrongBoundary = -1;
            var temporaryFarthest = temporaryStart;
            for (var temporaryVertex = 0;
                 temporaryVertex < _definition.VertexCount;
                 temporaryVertex++)
            {
                if (!temporarySearch.Visited[temporaryVertex])
                {
                    continue;
                }

                if (temporarySearch.Distance[temporaryVertex] >
                    temporarySearch.Distance[temporaryFarthest])
                {
                    temporaryFarthest = temporaryVertex;
                }

                if (temporaryVertex != temporaryStart &&
                    IsBoundaryVertex(temporaryVertex) &&
                    !temporaryGoalVertices.Contains(temporaryVertex) &&
                    temporaryWrongBoundary < 0)
                {
                    temporaryWrongBoundary = temporaryVertex;
                }
            }

            if (temporarySteps.Count == 0)
            {
                var temporaryEnd = temporaryWrongBoundary >= 0
                    ? temporaryWrongBoundary
                    : temporaryFarthest;
                temporarySteps.AddRange(
                    PuzzleDefinition.BuildRoute(
                        temporarySearch.Previous,
                        temporarySearch.HasPrevious,
                        temporaryStart,
                        temporaryEnd));
            }

            return new RouteEvaluation(
                temporaryWrongBoundary >= 0
                    ? RouteTermination.ReachedWrongBoundary
                    : RouteTermination.ClosedNetwork,
                temporarySteps,
                temporaryReachedGoals,
                _definition.Goals.Count);
        }

        public List<RouteStep> FindSolvedRouteSteps()
        {
            var temporaryRoute = new List<RouteStep>();
            foreach (var temporaryJourney in
                     FindSolvedJourneysByNearestGoal())
            {
                temporaryRoute.AddRange(temporaryJourney.Steps);
            }

            return temporaryRoute;
        }

        public List<SolvedJourney> FindSolvedJourneysByNearestGoal()
        {
            var temporaryJourneys = new List<SolvedJourney>();
            if (!IsSolved)
            {
                return temporaryJourneys;
            }

            var temporaryCurrentVertex = PuzzleDefinition.GetVertexId(
                _definition.Width,
                _definition.StartIndex,
                _definition.StartCorner);
            var temporaryRemainingGoalIndices = new List<int>();
            for (var temporaryIndex = 0;
                 temporaryIndex < _definition.Goals.Count;
                 temporaryIndex++)
            {
                temporaryRemainingGoalIndices.Add(temporaryIndex);
            }

            while (temporaryRemainingGoalIndices.Count > 0)
            {
                var temporarySearch =
                    PuzzleDefinition.SearchConnectedVertices(
                        _definition.Width,
                        _definition.Height,
                        EncodedState,
                        temporaryCurrentVertex);

                var temporaryNearestGoalIndex = -1;
                var temporaryNearestGoalVertex = -1;
                var temporaryNearestDistance = int.MaxValue;
                foreach (var temporaryGoalIndex in
                         temporaryRemainingGoalIndices)
                {
                    var temporaryGoal =
                        _definition.Goals[temporaryGoalIndex];
                    var temporaryGoalVertex =
                        PuzzleDefinition.GetVertexId(
                            _definition.Width,
                            temporaryGoal.TileIndex,
                            temporaryGoal.Corner);
                    if (!temporarySearch.Visited[temporaryGoalVertex])
                    {
                        continue;
                    }

                    var temporaryDistance =
                        temporarySearch.Distance[temporaryGoalVertex];
                    if (temporaryDistance >= temporaryNearestDistance)
                    {
                        continue;
                    }

                    temporaryNearestDistance = temporaryDistance;
                    temporaryNearestGoalIndex = temporaryGoalIndex;
                    temporaryNearestGoalVertex = temporaryGoalVertex;
                }

                if (temporaryNearestGoalIndex < 0)
                {
                    break;
                }

                temporaryJourneys.Add(
                    new SolvedJourney(
                        temporaryNearestGoalIndex,
                        PuzzleDefinition
                            .BuildStraightestShortestRoute(
                            _definition.Width,
                            _definition.Height,
                            EncodedState,
                            temporaryCurrentVertex,
                            temporaryNearestGoalVertex)));
                temporaryCurrentVertex = temporaryNearestGoalVertex;
                temporaryRemainingGoalIndices.Remove(
                    temporaryNearestGoalIndex);
            }

            return temporaryJourneys;
        }

        #endregion

        #region Helpers

        bool IsBoundaryVertex(int vertexId)
        {
            var temporaryX = vertexId % (_definition.Width + 1);
            var temporaryY = vertexId / (_definition.Width + 1);
            return temporaryX == 0 ||
                   temporaryY == 0 ||
                   temporaryX == _definition.Width ||
                   temporaryY == _definition.Height;
        }

        #endregion
    }
}
