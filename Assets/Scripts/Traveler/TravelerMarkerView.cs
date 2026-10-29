using UnityEngine;
using UnityEngine.UI;

namespace SakakuPuzzle
{
    [DisallowMultipleComponent]
    public sealed class TravelerMarkerView : MonoBehaviour
    {
        static readonly int MovingAnimatorId =
            Animator.StringToHash("Moving");
        static readonly int ArriveAnimatorId =
            Animator.StringToHash("Arrive");

        [SerializeField] Image _image;
        [SerializeField] Animator _animator;

        public RectTransform RectTransform =>
            (RectTransform)transform;
        public Animator Animator => _animator;

        #region Configuration

        public void Configure(PuzzleUiTheme theme)
        {
            if (_image != null)
            {
                _image.sprite = theme.TravelerSprite;
                _image.color = Color.white;
                _image.raycastTarget = false;
            }

            if (_animator != null)
            {
                _animator.runtimeAnimatorController =
                    theme.TravelerController;
                _animator.applyRootMotion = false;
                _animator.updateMode =
                    AnimatorUpdateMode.UnscaledTime;
            }
        }

        public void PlayArrival()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(ArriveAnimatorId);
            }
        }

        public void FaceDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            RectTransform.localEulerAngles = new Vector3(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg -
                90f);
        }

        #endregion
    }
}
