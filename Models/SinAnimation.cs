namespace Roulette.Models {
    public static class SinAnimation {
        public static int SineInOut(float t, float totaltime, int min, int max) {
            var delta = max - min;
            return (int)(-delta / 2 * (Math.Cos(t * Math.PI / totaltime) - 1) + min);
        }
    }
}
