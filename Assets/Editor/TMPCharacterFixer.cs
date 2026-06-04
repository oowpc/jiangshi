using TMPro;
using UnityEditor;
using UnityEngine;

namespace Jiangshi.Editor
{
    public static class TMPCharacterFixer
    {
        private const string RequiredChars = ""
            + "血清原液已取得任务完成正在返回基地操作者失联未"
            + "实验记录代号计划月采集体共例分失控密码"
            + "获得了一份按查看基地指挥医疗兵陈静未知信号"
            + "检测到走廊方向异常生物质信号僵尸来源疑似旧收容"
            + "需要一名进入调查寻找投放消退操"
            + "通讯器关闭打开进行中接近它们更快了庞然大物出现最终冲击"
            + "战场通讯记录药液调查员已消失无人生还";

        public static void AddCharactersToAllFonts()
        {
            var fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            var updated = 0;

            foreach (var guid in fontGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fontAsset == null || fontAsset.sourceFontFile == null) continue;

                var unicodes = new uint[RequiredChars.Length];
                for (var i = 0; i < RequiredChars.Length; i++)
                    unicodes[i] = RequiredChars[i];

                if (fontAsset.TryAddCharacters(unicodes, out var missing))
                {
                    EditorUtility.SetDirty(fontAsset);
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"TMP characters added: {updated} font(s) updated.");
        }
    }
}
