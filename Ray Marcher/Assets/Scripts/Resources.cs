using UnityEngine;
using Unity.Mathematics;
using System;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Resources
{
    // Shader input structs
    public struct TriObject
    {
        public float3 pos;
        public float3 rot;
        public float3 lastRot;
        public float containedRadius;
        public int triStart;
        public int triEnd;
    };
    public struct Tri // Triangle
    {
        public float3 vA;
        public float3 vB;
        public float3 vC;
        public float3 normal;
        public int materialKey;
        public int parentKey;
    };
    public struct Sphere
    {
        public float3 pos;
        public float radius;
        public int materialKey;
    };
    public struct Material2
    {
        public float3 color;
        public float3 specularColor;
        public float brightness;
        public float smoothness;
    };
    public static class Utils
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

        public static int GetThreadGroupsNum(int threadsNum, int threadSize)
        {
            int threadGroupsNum = (int)Math.Ceiling((float)threadsNum / threadSize);
            return threadGroupsNum;
        }
        public static int2 GetThreadGroupsNum(int2 threadsNum, int threadSize)
        {
            int threadGroupsNumX = GetThreadGroupsNum(threadsNum.x, threadSize);
            int threadGroupsNumY = GetThreadGroupsNum(threadsNum.y, threadSize);
            return new(threadGroupsNumX, threadGroupsNumY);
        }
        public static int3 GetThreadGroupsNum(int3 threadsNum, int threadSize)
        {
            int threadGroupsNumX = GetThreadGroupsNum(threadsNum.x, threadSize);
            int threadGroupsNumY = GetThreadGroupsNum(threadsNum.y, threadSize);
            int threadGroupsNumZ = GetThreadGroupsNum(threadsNum.z, threadSize);
            return new(threadGroupsNumX, threadGroupsNumY, threadGroupsNumZ);
        }
    }

    public static class Func
    {
        public static void Log2(ref int a, bool doCeil = false)
        {
            double logValue = Math.Log(a, 2);
            a = doCeil ? (int)Math.Ceiling(logValue) : (int)logValue;
        }
        public static int Log2(int a, bool doCeil = false)
        {
            double logValue = Math.Log(a, 2);
            return doCeil ? (int)Math.Ceiling(logValue) : (int)logValue;
        }
        public static int Pow2(int a)
        {
            double powValue = Mathf.Pow(2, a);
            return (int)powValue;
        }
        public static int RandInt(int min, int max)
        {
            return UnityEngine.Random.Range(min, max+1);
        }
        public static int NextPow2(int a)
        {
            int nextPow2 = 1;
            while (nextPow2 < a)
            {
                nextPow2 *= 2;
            }
            return nextPow2;
        }
        public static void NextPow2(ref int a)
        {
            int nextPow2 = 1;
            while (nextPow2 < a)
            {
                nextPow2 *= 2;
            }
            a = nextPow2;
        }
        public static int NextLog2(int a)
        {
            return Log2(NextPow2(a));
        }
        public static void NextLog2(ref int a)
        {
            a = Log2(NextPow2(a));
        }
    }
}