using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskStatsServer.Extensions
{
    static class ArrayExtension
    {
        /// <summary>
        /// 写js写的
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="arr">原数组</param>
        /// <param name="startIndex">起始偏移（含）</param>
        /// <param name="endIndex">结束偏移（不含）</param>
        /// <returns>原数组的切片的复制</returns>
        public static T[] Slice<T>(this T[] arr, int startIndex, int endIndex = int.MaxValue)
        {
            endIndex = Math.Min(endIndex, arr.Length);
            startIndex = Math.Max(startIndex, 0);

            if (startIndex >= endIndex)
            {
                return new T[0];
            }

            T[] result = new T[endIndex - startIndex];
            for (int i = startIndex, cursor = 0; i < endIndex; ++i, ++cursor)
            {
                result[cursor] = arr[i];
            }
            return result;
        }
    }
}
