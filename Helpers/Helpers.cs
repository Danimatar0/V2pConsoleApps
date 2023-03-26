namespace Helpers
{
    public static class Helpers
    {
        public static Tuple<double,double,double> GetRandomCoordinate(double minX, double maxX, double minY, double maxY, double minZ, double maxZ)
        {
            Random rand = new Random();

            // Generate random 3D coordinates within the specified range
            double x = rand.NextDouble() * (maxX - minX) + minX;
            double y = rand.NextDouble() * (maxY - minY) + minY;
            double z = rand.NextDouble() * (maxZ - minZ) + minZ;

            Console.WriteLine($"Random 3D coordinates: ({x}, {y}, {z})");

            return new Tuple<double, double, double> (x,y,z);
        }

        public static string RandomString(Random rnd, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[rnd.Next(s.Length)]).ToArray());
        }

    }
}