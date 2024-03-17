using UnityEngine;
using Unity.Mathematics;
using System;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Resources
{
    public struct OpaqueSphere
    {
        public float3 position;
        public float radius;
        public int materialFlag;
    };
    struct OpaqueMaterial
    {
        public float3 color;
        public float3 specularColor;
        public float brightness;
        public float smoothness;
    };
    public class Utils
    {
        public static Vector2 GetMouseWorldPos(int Width, int Height)
        {
            Vector3 MousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x , Input.mousePosition.y , -Camera.main.transform.position.z));
            Vector2 MouseWorldPos = new(((MousePos.x - Width/2) * 0.55f + Width) / 2, ((MousePos.y - Height/2) * 0.55f + Height) / 2);

            return MouseWorldPos;
        }

        public static bool2 GetMousePressed()
        {
            bool LMousePressed = Input.GetMouseButton(0);
            bool RMousePressed = Input.GetMouseButton(1);

            bool2 MousePressed = new bool2(LMousePressed, RMousePressed);

            return MousePressed;
        }

        public static int GetThreadGroupsNums(int threadsNum, int threadSize)
        {
            int threadGroupsNum = (int)Math.Ceiling((float)threadsNum / threadSize);
            return threadGroupsNum;
        }

        public static int2 GetThreadGroupsNumsXY(int2 threadsNum, int threadSize)
        {
            int threadGroupsNumX = GetThreadGroupsNums(threadsNum.x, threadSize);
            int threadGroupsNumY = GetThreadGroupsNums(threadsNum.y, threadSize);
            return new(threadGroupsNumX, threadGroupsNumY);
        }
    }

    public class Func
    {
        public static float[] DegreesToRadians(float[] degreesArray)
        {
            float[] radiansArray = new float[degreesArray.Length];
            for (int i = 0; i < degreesArray.Length; i++)
            {
                radiansArray[i] = degreesArray[i] * Mathf.Deg2Rad;
            }
            return radiansArray;
        }
    }
}