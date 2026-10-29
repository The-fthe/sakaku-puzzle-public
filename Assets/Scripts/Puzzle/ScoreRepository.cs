using System.Collections.Generic;

namespace SakakuPuzzle
{
    public readonly struct ScoreEntry
    {
        public string PlayerName { get; }
        public int Steps { get; }

        public ScoreEntry(string playerName, int steps)
        {
            PlayerName = playerName;
            Steps = steps;
        }
    }

    public interface IScoreRepository
    {
        void Submit(ScoreEntry scoreEntry);
        IReadOnlyList<ScoreEntry> GetTopScores();
    }

    public sealed class InMemoryScoreRepository : IScoreRepository
    {
        readonly List<ScoreEntry> _scores = new()
        {
            new ScoreEntry("モリ", 4),
            new ScoreEntry("ソラ", 5),
            new ScoreEntry("ナミ", 6)
        };

        public void Submit(ScoreEntry scoreEntry)
        {
            _scores.Add(scoreEntry);
            _scores.Sort((left, right) => left.Steps.CompareTo(right.Steps));
        }

        public IReadOnlyList<ScoreEntry> GetTopScores()
        {
            return _scores;
        }
    }
}
