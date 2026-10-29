using UnityEngine;

namespace SakakuPuzzle
{
    [CreateAssetMenu(
        fileName = "LiveServiceAssets",
        menuName = "差覚パズル/Services/Live Service Assets")]
    public sealed class LiveServiceAssets : ScriptableObject
    {
        [SerializeField] TextAsset _unityroomHmacKey;

        public TextAsset UnityroomHmacKey => _unityroomHmacKey;
    }
}
