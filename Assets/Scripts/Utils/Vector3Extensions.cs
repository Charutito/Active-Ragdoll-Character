using UnityEngine;

namespace Utils
{
    public static class VectorExtensions
    {
        public static Vector3 XYToX0Z(this Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }
        
        public static Vector3 To0YZ(this Vector3 v)
        {
            return new Vector3(0, v.y, v.z);
        }

        public static Vector3 ToX0Z(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.z);
        }

        public static Vector3 ToXY0(this Vector3 v)
        {
            return new Vector3(v.x, v.y, 0);
        }

        public static Vector3 ModifyX(this Vector3 v, float x)
        {
            return new Vector3(x, v.y, v.z);
        }
        public static Vector3 ModifyY(this Vector3 v, float y)
        {
            return new Vector3(v.x, y, v.z);
        }
        public static Vector3 ModifyZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }
        public static Vector3 To0Y0(this Vector3 v)
        {
            return new Vector3(0, v.y, 0);
        }
        public static Vector3 XYToXZ(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.y);
        }
        
        
    }
}