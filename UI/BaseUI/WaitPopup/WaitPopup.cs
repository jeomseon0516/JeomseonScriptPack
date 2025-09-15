using System.Collections;
using UnityEngine;
using TMPro;
using Jeomseon.Coroutine;

namespace Jeomseon.UI.Components
{
    public sealed class WaitPopup : BaseUI
    {
        [Header("Wait Text")]
        [SerializeField]
        private TMP_Text _waitText;

        protected override void EnableUI()
            => StartCoroutine(iEWaitEvent());

        private IEnumerator iEWaitEvent()
        {
            string getDotFromCount(int count) => (count % 3) switch
            {
                0 => ".",
                1 => "..",
                2 => "...",
                _ => string.Empty
            };

            int count = 0;

            while (true)
            {
                yield return CoroutineHelper.WaitForSeconds(1f);
                count++;
                _waitText.text = $"Wait.. {getDotFromCount(count)}";
            }
        }
    }
}
