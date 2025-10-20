using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenTrader.Indicators
{
    public enum ExtremaType { Peak, Trough };

    public enum DistanceType { Euclidean, Perpendicular, Vertical };

    public struct Extrema 
    {
        public int position;
        public ExtremaType extremaType;
    }

    public class Point {
        public double x; public double y;

        public Point(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public Point((double x, double y) point)
        {
            x = point.x;
            y = point.y;
        }
    }
    ;

    /// <summary>
    /// A PointsLine is a line, defined by being between two points.
    /// </summary>
    public class PointsLine
    {
        public double slope;
        public double intercept;
        public Point p0, pN;

        public PointsLine(double x1, double y1, double x2, double y2)
        {
            p0 = new Point(x1, y1);
            pN = new Point(x2, y2);
            slope = (x2 - x1) / (y2 - y1);
            intercept = y2 - slope * x2;
        }

        public PointsLine((double x, double y) point1, (double x, double y) point2)
        {
            p0 = new Point(point1.x, point1.y);
            pN = new Point(point2.x, point2.y);
            slope = (point2.x - point1.x) / (point2.y - point1.y);
            intercept = point2.y - slope * point2.y;
        }

        public PointsLine((int x, double y) point1, (int x, double y) point2)
        {
            p0 = new Point(point1.x, point1.y);
            pN = new Point(point2.x, point2.y);
            slope = ((double) point2.x - (double) point1.x) / (point2.y - point1.y);
            intercept = point2.y - slope * point2.y;
        }

        public double Y(double x)
        {
            return x * slope + intercept;
        }

        /// <summary>
        /// Caculate the distance of a point from the line
        /// </summary>
        /// <param name="distanceType">How to calculate the distance</param>
        /// <param name="point">The point</param>
        /// <returns></returns>
        public double Distance(DistanceType distanceType, (int x, double y) point )
        {
            switch(distanceType)
            {
                case DistanceType.Perpendicular:
                    // Point is on a line that is perpendicular. Finds the distance between the two lines.
                    double intercept = point.y - slope * point.x;
                    return Math.Abs(intercept - this.intercept) / Math.Sqrt(slope * slope + 1);
                case DistanceType.Vertical:
                    // The vertical distance between the two points
                    double y = Y(point.x);
                    return Math.Abs(y - point.y);
                case DistanceType.Euclidean:
                    // The point is on a line that is at right angles to the line. Calculates the length of the line at right angles.
                    return Math.Sqrt(Math.Pow(point.x - p0.x, 2) + Math.Pow(point.y - p0.y, 2)) + Math.Sqrt(Math.Pow(point.x - pN.x, 2) + Math.Pow(point.y - pN.y, 2));
            }
            return 0;
        }
    }

    public class Patterns
    {
        /// <summary>
        /// Find nPips inflection points in a data array using a distance type calculation
        /// </summary>
        /// <param name="data"></param>
        /// <param name="nPips">No of inflection points to find</param>
        /// <param name="distType">Euclidean, perpendicular or vertical</param>
        /// <returns>List of inflection points</returns>
        /// 

        double[]? ema9;
        double[]? sma3;
        double[] data;

        public Patterns(double[] data)
        {
            this.data = data;
            sma3 = data.SmaSeries(3);
            ema9 = data.EmaSeries(9);
        }

        static public List<(int x, double y)> FindPips (double[] data,int nPips, DistanceType distType)
        {
            var pips = new List<(int x, double y)>();

            if (data.Length == 0)
                return pips;

            // The first point is the first pip
            pips.Add((0, data[0]));
            if (data.Length == 1)
                return pips;

            // The last piont is the last pip
            pips.Add((data.Length-1, data[^1]));

            for( int i=0; i<nPips; i++ )
            {
                // Initialise the index and distance to zero
                (int x, double distance) distance = (0,0);


                for ( int p=0;p<pips.Count-1;p++)
                {
                    // Set up the line between the two pips
                    var line = new PointsLine(pips[p], pips[p + 1]);

                    // Now look at each pip between the two pips
                    for(int j=pips[p].x;j<pips[p+1].x;j++)
                    {
                        // Work out the distance for each data point
                        (int x, double distance) tryDistance = (j, line.Distance(distType, (j, data[j])));

                        if( tryDistance.distance > distance.distance )
                        {
                            distance = tryDistance;
                        }
                    }
                }

                // Add in the point with the largest distance
                pips.Add((distance.x, data[distance.x]));
                // And sort by the index
                pips.Sort((p, q) => p.x.CompareTo(q.x));
            }

            return pips;
        }

        public static List<(string name, int[] bars)> DoubleBottom(double[] data, int w = 8, int lookBack = 60)
        {
            var results = new List<(string, int[])>();

            var extrema = FindExtrema(data, w);
            var patterns = FindExtremaPattern(extrema, new ExtremaType[] { ExtremaType.Peak, ExtremaType.Trough, ExtremaType.Peak, ExtremaType.Trough });
            
            
            foreach (var i in patterns)
            {
                var p1 = extrema[i].position;
                var t1 = extrema[i + 1].position;
                var p2 = extrema[i + 2].position;
                var t2 = extrema[i + 3].position;

                // bottoms should be between two to eight weeks apart
                if (t2 - t1 > 40 || t2 - t1 < 10)
                    continue;

                // rise between bottoms should be at least 10%
                if (data[p2] < data[t1] * 1.1 && data[p2] < data[t2] * 1.1)
                    continue;

                var pips = FindPips(data[(p1 - lookBack)..p1], 2, DistanceType.Euclidean);

                // Price should trend downwards and not drift below the bottom
                if (data[p1] >= pips[2].y || pips[2].y <= pips[1].y)
                    continue;

                // Confirmation price: The highest price between the two bottoms
                var confirmation = p2;
                for (int j=t1; j<=t2;)
                {
                    if (data[j] > data[confirmation])
                        confirmation = j;
                }

                for (int j = t2 + 1; j < data.Length; j++)
                {
                    // If price drops below the right bottom before confirmation then look elsewhere
                    if (data[j] < data[t2])
                        break;

                    if (data[j] > data[confirmation])
                    {
                        results.Add(("doublebottom", new int[] { pips[2].x + p1 - lookBack, p1, t1, confirmation, t2, j }));
                        break;
                    }
                }

            }

            return results;
        }

        public static List<(string name,int[] bars)> Pennant3(double[] data, int w=3, int lookBack=60)
        {
            var results = new List<(string,int[])>();

            var extrema = FindExtrema(data, w);
            // Find all the patterns in the data
            var patterns = FindExtremaPattern(extrema, new ExtremaType[] { ExtremaType.Peak, ExtremaType.Trough, ExtremaType.Peak, ExtremaType.Trough });

            foreach (var i in patterns)
            {

                var p1 = extrema[i].position;
                var t2 = extrema[i + 3].position;
                if (t2 - p1 > 15)
                    continue;

                if (p1 - lookBack < 0)
                    continue;

                // Get the pips leading up to the data
                var pips = FindPips(data[(p1-lookBack)..p1],2,DistanceType.Euclidean);

                if (data[p1] <= pips[2].y)
                    continue;



                var t1 = extrema[i + 1].position;
                var p2 = extrema[i + 2].position;
                

                if (data[t1] > data[p1] || data[t2] > data[p2])
                    continue;

                var topLine = new PointsLine(p1, data[p1], p2, data[p2]);
                var bottomLine = new PointsLine(t1, data[t1], t2, data[t2]);

                var check = new double[] { data[p1], data[t1], data[p1], data[t2] };

                if (topLine.slope - bottomLine.slope != 0)
                {
                    var lastX = (bottomLine.intercept - topLine.intercept) / (topLine.slope - bottomLine.slope);

                    for (int j = t2 + 1; j <= lastX && j < data.Length && j < t2 + 15; j++)
                    {
                        var topY = topLine.Y(j);
                        
                        if (data[j] > topY)
                        {
                            // break out up
                            results.Add(("pennant+", new int[] { pips[2].x+p1-lookBack, p1, t1, p2, t2, j }));
                            break;
                        }
                        else 
                        {
                            var bottomY = bottomLine.Y(j);
                            if (data[j] < bottomY)
                            {
                                // break out down
                                results.Add(("pennant-", new int[] { pips[2].x+p1-lookBack, p1, t1, p2, t2, j }));
                            }
                        }
                    }
                }
            }

            return results;
        }



        public static List<(string name, int[] bars)> HeadShouldersTop(double[] data, int w=5, int lookBack=60)
        {
            var results = new List<(string name, int[] bars)>();

            var extrema = FindExtrema(data, w);
            var patterns = FindExtremaPattern(extrema, new ExtremaType[] {
                ExtremaType.Peak, // Left shoulder
                ExtremaType.Trough, 
                ExtremaType.Peak, // Head
                ExtremaType.Trough, 
                ExtremaType.Peak // Right shoulder
            });
            
            foreach(var i in patterns)
            {
                var p0 = extrema[i].position; // First peak, the left shoulder
                var t0 = extrema[i + 1].position; // First trough
                var p1 = extrema[i + 2].position; // Second peak, the head
                var t1 = extrema[i + 3].position; // Second trough
                var p2 = extrema[i + 4].position; // Third peak, the right shoulder

                // Get the pips leading up to the data
                var pips = FindPips(data[(p0 - lookBack)..p0], 2, DistanceType.Euclidean);

                // Price must be trending up
                if (data[p0] <= pips[2].y)
                    continue;

                // For debugging purposes
                var check = new double[] { pips[2].y, data[p0], data[t0], data[p1], data[t1], data[p2] };

                // Condition 1: The head must be higher than the shoulders: 
                // P2 > max(p1,p3)
                if ( ! (data[p1] > Math.Max(data[p0], data[p2])) )
                    continue;



                // Condition 5: Penetration
                // the pattern is confirmed when price breaches downwards the neckline ( 5.6 ) for the first time.
                // The time required for this confirmation, t B , must be no longer than the time interval between the two shoulders 

                var slope = (data[t1]  - data[t0] ) / (t1 - t0);
                var intercept = data[t1] - slope * t1;

                for(int b=p2+1; b<data.Length; b++)
                {
                    if (b - p2 > p2 - p0)
                        break; // The time required for this confirmation, t B , must be no longer than the time interval between the two shoulders 

                    if (data[b] >= data[p2])
                        break; // Price has risen above the shoulder

                    if ( data[b] <= slope*b+intercept)
                    {
                        // the pattern is confirmed when price breaches downwards the neckline ( 5.6 ) for the first time.
                        results.Add(("HeadShouldersTop", new int[] {pips[2].x + p0 - lookBack, p0,t0,p1,t1,p2 ,b}));
                        break;
                    }
                }
            }

            return results;
        }

        public static List<int> FindExtremaPattern(List<Extrema> source, ExtremaType[] pattern)
        {
            var result = new List<int>();
            var sourceCount = source.Count;
            var patternLength = pattern.Length;

            // starting at the length of the pattern, through to the end of the source
            for (int i = 0; i <= sourceCount - patternLength; i++)
            {
                bool found = true;
                // check against the pattern
                for (int j = 0; j < patternLength; j++)
                {
                    if (source[i +  j].extremaType != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    var check = new List<Extrema>();
                    for (int j=0; j<patternLength; j++)
                    {
                        check.Add(source[i + j]);
                    }
                    result.Add(i);
                }
            }

            return result;
        }

        public static List<Extrema> FindExtrema(double[] data, int w) // RW
        {
            int l = data.Length;

            var extrema = new List<Extrema>();

            for (int i = w; i < l - w; i++)
            {
                var left = data[(i - w)..(i )];
                var right = data[(i + 1)..(i + 1 + w)];
                if ( data[i] > left.Max() && data[i] > right.Max() )
                {
                    extrema.Add(new Extrema() { position = i, extremaType=ExtremaType.Peak });
                }

                if ( data[i] < left.Min() && data[i] < right.Min() )
                {
                    extrema.Add(new Extrema() { position = i, extremaType = ExtremaType.Trough });
                }
            }

            return extrema;
        }

        object patternLock = new object();
        public void FindPatterns(double[] data)
        {
            var result = new List<(string name, int[] bars)>();
            Parallel.For(0, 2, i => 
            { 
                switch(i)
                {
                    case 0:
                        var pennants = Pennant3(data);
                        lock(patternLock)
                        {
                            result.AddRange(pennants);
                        }
                        break;
                    case 1:
                        var bottoms = DoubleBottom(data);
                        lock (patternLock)
                        {
                            result.AddRange(bottoms);
                        }
                        break;
                }
            });
        }
    }
}