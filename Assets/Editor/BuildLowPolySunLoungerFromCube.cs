using UnityEditor;
using UnityEngine;

public static class BuildLowPolySunLoungerFromCube
{
    [MenuItem("Tools/GameLab/Convert Selected Cube To Low Poly Sun Lounger")]
    private static void ConvertSelectedCube()
    {
        GameObject target = Selection.activeGameObject;

        if (target == null)
        {
            Debug.LogError("Hierarchy에서 Cube를 선택한 뒤 실행하세요.");
            return;
        }

        target.name = "LowPoly_SunLounger";

        Debug.Log("메뉴 연결 성공. 선택한 Cube 이름을 LowPoly_SunLounger로 바꿨습니다.");
    }
}
