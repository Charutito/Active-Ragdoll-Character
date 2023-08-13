using UnityEngine;

namespace Utils
{
    public static class QuaternionExtensions
    {
        public static Quaternion ModifyX(this Quaternion quaternion, float x)
        {
            return new Quaternion(x, quaternion.y, quaternion.z, quaternion.w);
        }

        public static Quaternion ModifyY(this Quaternion quaternion, float y)
        {
            return new Quaternion(quaternion.x, y, quaternion.z, quaternion.w);
        }

        public static Quaternion ModifyW(this Quaternion quaternion, float w)
        {
            return new Quaternion(quaternion.x, quaternion.y, quaternion.z, w);
        }

        public static Quaternion ModifyZ(this Quaternion quaternion, float z)
        {
            return new Quaternion(quaternion.x, quaternion.y, z, quaternion.w);
        }

        public static Quaternion DisplaceX(this Quaternion quaternion, float xDisplacement)
        {
            return quaternion.ModifyX(quaternion.x + xDisplacement);
        }

        public static Quaternion DisplaceY(this Quaternion quaternion, float yDisplacement)
        {
            return quaternion.ModifyY(quaternion.y + yDisplacement);
        }

        public static Quaternion DisplaceZ(this Quaternion quaternion, float zDisplacement)
        {
            return quaternion.ModifyZ(quaternion.z + zDisplacement);
        }

        public static Quaternion DisplaceW(this Quaternion quaternion, float wDisplacement)
        {
            return quaternion.ModifyW(quaternion.w + wDisplacement);
        }
    }
}