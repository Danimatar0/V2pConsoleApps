using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Models
{
    public class Line
    {
        public double a { get; set; }
        public double b { get; set; }
        public double c { get; set; }
        public string equation { get; set; }
        public Line(Point3D p1, Point3D p2)
        {
            a = p2.Y - p1.Y;
            b = p1.X - p2.X;
            c = a * (p1.X) + b * (p1.Y);

            equation = b < 0 ? $"{a}x - {b}y = {c}" : $"{a}x + {b}y = {c}";
        }

        public Line(double a, double b, double c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            equation = b < 0 ? $"{a}x - {b}y = {c}" : $"{a}x + {b}y = {c}";
        }

        public string ToString()
        {
            return $"{a},{b},{c}";
        }
        public string LineFromPints(Point3D p1, Point3D p2)
        {
            return $"{a},{b},{c}";
        }
    }

    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public string ToString()
        {
            return $"[{X},{Y},{Z}]";
        }
    }
}
