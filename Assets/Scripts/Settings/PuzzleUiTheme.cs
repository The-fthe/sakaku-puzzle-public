using TMPro;
using UnityEngine;

namespace SakakuPuzzle
{
    [CreateAssetMenu(
        fileName = "PuzzleUiTheme",
        menuName = "差覚パズル/UI/Theme")]
    public sealed class PuzzleUiTheme : ScriptableObject
    {
        #region Assets

        [SerializeField] TMP_FontAsset _displayFont;
        [SerializeField] Sprite _squareSprite;
        [SerializeField] Sprite _rectangleSprite;
        [SerializeField] Sprite _gameCursorSprite;
        [SerializeField] Sprite _travelerSprite;
        [SerializeField] Sprite _destinationSprite;
        [SerializeField] Sprite _stageNodeSprite;
        [SerializeField] Texture2D _landscapeBackdrop;
        [SerializeField] Texture2D _portraitBackdrop;
        [SerializeField] RuntimeAnimatorController _travelerController;
        [SerializeField] AudioClip _rotateClip;
        [SerializeField] AudioClip _goalClip;
        [SerializeField] AudioClip _clearClip;

        public TMP_FontAsset DisplayFont => _displayFont;
        public Sprite SquareSprite => _squareSprite;
        public Sprite RectangleSprite => _rectangleSprite;
        public Sprite GameCursorSprite => _gameCursorSprite;
        public Sprite TravelerSprite => _travelerSprite;
        public Sprite DestinationSprite => _destinationSprite;
        public Sprite StageNodeSprite => _stageNodeSprite;
        public Texture2D LandscapeBackdrop => _landscapeBackdrop;
        public Texture2D PortraitBackdrop => _portraitBackdrop;
        public RuntimeAnimatorController TravelerController =>
            _travelerController;
        public AudioClip RotateClip => _rotateClip;
        public AudioClip GoalClip => _goalClip;
        public AudioClip ClearClip => _clearClip;

        #endregion
    }
}
