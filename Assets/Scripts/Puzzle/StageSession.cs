using System;
using System.Collections.Generic;
using UnityEngine;

namespace SakakuPuzzle
{
    public static class StageSession
    {
        const int MIN_STAGE = 1;
        const int MAX_STAGE = 4;

        public static int SelectedStage { get; private set; } = 1;
        public static bool IsRankingChallenge { get; private set; }
        public static bool IsAssisted { get; private set; }
        public static int CumulativeSteps { get; private set; }
        public static int ExpectedStage { get; private set; } = 1;
        public static int RestartCount { get; private set; }
        public static IReadOnlyList<int> StageSteps => StageStepValues;

        static readonly int[] StageStepValues = new int[MAX_STAGE];

        public static event Action Changed;

        public static void SelectStage(int stageNumber)
        {
            SelectedStage = Mathf.Clamp(
                stageNumber,
                MIN_STAGE,
                MAX_STAGE);

            if (SelectedStage == MIN_STAGE)
            {
                BeginRankingChallenge();
                return;
            }

            IsRankingChallenge = false;
            IsAssisted = false;
            CumulativeSteps = 0;
            ExpectedStage = SelectedStage;
            RestartCount = 0;
            Array.Clear(StageStepValues, 0, StageStepValues.Length);
            Changed?.Invoke();
        }

        public static void BeginRankingChallenge()
        {
            SelectedStage = MIN_STAGE;
            IsRankingChallenge = true;
            IsAssisted = false;
            CumulativeSteps = 0;
            ExpectedStage = MIN_STAGE;
            RestartCount = 0;
            Array.Clear(StageStepValues, 0, StageStepValues.Length);
            Changed?.Invoke();
        }

        public static bool CompleteStage(int stageNumber, int steps)
        {
            var temporaryStage = Mathf.Clamp(
                stageNumber,
                MIN_STAGE,
                MAX_STAGE);
            var temporarySteps = Mathf.Max(0, steps);

            if (!IsRankingChallenge ||
                temporaryStage != ExpectedStage)
            {
                IsRankingChallenge = false;
                Changed?.Invoke();
                return false;
            }

            CumulativeSteps += temporarySteps;
            StageStepValues[temporaryStage - 1] = temporarySteps;
            ExpectedStage++;
            Changed?.Invoke();
            return temporaryStage == MAX_STAGE;
        }

        public static void ContinueToNextStage()
        {
            if (ExpectedStage > MAX_STAGE)
            {
                return;
            }

            SelectedStage = ExpectedStage;
            Changed?.Invoke();
        }

        public static void MarkAssisted()
        {
            IsAssisted = true;
            Changed?.Invoke();
        }

        public static void RecordRestart()
        {
            RestartCount++;
            Changed?.Invoke();
        }

        public static void AbandonToStageSelect()
        {
            IsRankingChallenge = false;
            IsAssisted = false;
            CumulativeSteps = 0;
            ExpectedStage = MIN_STAGE;
            RestartCount = 0;
            Array.Clear(StageStepValues, 0, StageStepValues.Length);
            Changed?.Invoke();
        }

        public static bool CanSubmitRanking =>
            IsRankingChallenge &&
            !IsAssisted &&
            ExpectedStage > MAX_STAGE;
    }
}
