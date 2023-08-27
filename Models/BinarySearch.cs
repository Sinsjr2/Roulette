namespace Roulette.Models {
    /// <summary>
    /// 2分探索を行う関数を持っています。
    /// </summary>
    public static class BinarySearch {

        public static int LowerBound<T>(this IReadOnlyList<T> xs, T v, Func<T, T, int> compare) {
            return BoundCore(xs, v, compare, -1);
        }

        public static int UpperBound<T>(this IReadOnlyList<T> xs, T v, Func<T, T, int> compare) {
            return BoundCore(xs, v, compare, 0);
        }

        static int BoundCore<T>(IReadOnlyList<T> xs, T v, Func<T, T, int> compare, int boundValue) {
            // TODO 空の配列の場合のエラーチェックを行う かそれとも0をかえすか
            var l = 0;
            var r = xs.Count - 1;
            while (l <= r) {
                var mid = l + ((r - l) >> 1);
                var res = compare(xs[mid], v);
                if (res <= boundValue) {
                    l = mid + 1;
                } else {
                    r = mid - 1;
                }
            }
            return l;
        }
    }
}
