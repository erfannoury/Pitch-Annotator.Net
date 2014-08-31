using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace PitchAnnotator
{
    /// <summary>
    /// This class implements the Liang-Barsky line clipping algorithm to clip lines to the image rectangle
    /// The algorithm is adapted from this article: http://hinjang.com/articles/04.html#eight
    /// References:
    /// Liang, Y.D., and Barsky, B., A New Concept and Method for Line Clipping," ACM Transactions on Graphics, 3(1):1-22, January 1984
    /// Liang, Y.D., B.A., Barsky, and M. Slater, Some Improvements to a Parametric Line Clipping Algorithm, CSD-92-688, Computer Science Division, University of California, Berkeley, 1992
    /// </summary>
    class LineClipper
    {
        private double xmin;
        private double ymin;
        private double xmax;
        private double ymax;
        private double tE;
        private double tL;
        public LineClipper(double w, double h)
        {
            this.xmin = 0;
            this.xmax = w;
            this.ymin = 0;
            this.ymax = h;
        }

        private bool isApproxZero(double x)
        {
            return (x < 10e-5) && (x > -10e-5);
        }

        private bool isPointInside(double x, double y)
        {
            return (x >= xmin && x <= xmax &&
                    y >= ymin && y <= ymax);
        }

        /// <summary>
        /// Clip the line to the bounds of the rectangle that has been created
        /// </summary>
        public void Clip(Line line)
        {
            double dx, dy;
            dx = line.X2 - line.X1;
            dy = line.Y2 - line.Y1;
            if (isApproxZero(dx) && isApproxZero(dy) && isPointInside(line.X1, line.Y1))
                return;

            tE = 0;
            tL = 1;
            if (clipT(xmin - line.X1, dx) &&
                clipT(line.X1 - xmax, -dx) &&
                clipT(ymin - line.Y1, dy) &&
                clipT(line.Y1 - ymax, -dy))
            {
                if (tL < 1)
                {
                    line.X2 = line.X1 + tL * dx;
                    line.Y2 = line.Y1 + tL * dy;
                }
                if (tE > 0)
                {
                    line.X1 += tE * dx;
                    line.Y1 += tE * dy;
                }
            }

        }

        private bool clipT(double num, double denum)
        {
            double t;

            if (isApproxZero(denum))
                return (num <= 0);

            t = num / denum;
            if (denum > 0)
            {
                if (t > tL)
                    return false;
                if (t > tE)
                    tE = t;
            }
            else
            {
                if (t < tE)
                    return false;
                if (t < tL)
                    tL = t;
            }
            return true;
        }
    }
}
