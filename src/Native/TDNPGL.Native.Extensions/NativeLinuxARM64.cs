﻿using System.Runtime.InteropServices;
using TDNPGL.Core.Math;

namespace TDNPGL.Native.Extensions
{
    internal static class NativeLinuxARM64
    {
        public static bool IsPointOverNative(AABB aabb, Vec2f point) =>
            AABB_IsPointOver(
            point.X,
            point.Y,
            aabb.min.X,
            aabb.min.Y,
            aabb.max.X,
            aabb.max.Y) == 1;

        static NativeLinuxARM64()
        {
        }
        [DllImport("arm64\\libtdnpgl.so", EntryPoint = "AABB_IsPointOver", CallingConvention = CallingConvention.Cdecl)]
        private static extern int AABB_IsPointOver(float x, float y, float minx, float miny, float maxx, float maxy);
    }
}
