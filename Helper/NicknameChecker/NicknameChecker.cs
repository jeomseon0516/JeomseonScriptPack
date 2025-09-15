using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Jeomseon.Helper
{
    public static class NicknameChecker
    {
        /// <summary>
        /// .. 닉네임이 올바른지 체크하고 warningMessage를 콜백으로 넘겨줍니다
        /// </summary>
        /// <param name="nickname"> .. 올바른지 검사할 닉네임 </param>
        /// <param name="warningCall"> .. 올바르지 않은 닉네임일시 넘겨줄 콜백 메세지 </param>
        /// <returns></returns>
        public static bool CheckNickname(string nickname, Action<string> warningCall)
            => checkNormalizedNickname(nickname.Contains(" "), "닉네임에는 공백을 포함할 수 없습니다", warningCall) &&
               checkNormalizedNickname(nickname.Length is < 2 or > 20, "닉네임의 길이는 2자 보다 크고 20자보다 작아야합니다", warningCall) &&
               checkNormalizedNickname(new Regex(@"[~!@\#$%^&*\()\=+|\\/:;?""<>']").IsMatch(nickname),
                "닉네임에 ~!@\\#$%^&*\\()\\=+|\\\\/:;?\"\"<> 등의 특수문자는 포함 할 수 없습니다", warningCall);

        private static bool checkNormalizedNickname(bool condition, string warningMesssage, Action<string> warningCall)
        {
            if (!condition) return true;
#if DEBUG
            Debug.LogWarning(warningMesssage);
#endif
            warningCall.Invoke(warningMesssage);
            return false;
        }
    }
}
