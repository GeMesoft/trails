﻿/*
Copyright (C) 2009 Brendan Doherty

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.Collections.Generic;

using ZoneFiveSoftware.Common.Data;
using ZoneFiveSoftware.Common.Data.GPS;
using ZoneFiveSoftware.Common.Data.Fitness;
using ZoneFiveSoftware.Common.Data.Measurement;
using ITrailExport;

namespace TrailsPlugin.Data {
    public class TrailResult : ITrailResult, IComparable
    {
		private IActivity m_activity;
		private int m_order;
		private INumericTimeDataSeries m_cadencePerMinuteTrack;
		private INumericTimeDataSeries m_heartRatePerMinuteTrack;
		private IDistanceDataTrack m_distanceMetersTrack = null;
		private INumericTimeDataSeries m_elevationMetersTrack;
		private INumericTimeDataSeries m_powerWattsTrack;
        private INumericTimeDataSeries m_speedTrack;
        private INumericTimeDataSeries m_gradeTrack;
        private IGPSRoute m_gpsTrack;
        private IList<IGPSPoint> m_gpsPoints;
        private INumericTimeDataSeries m_paceTrack;
        private int m_startIndex;
		private int m_endIndex;
        private DateTime m_startTime;
        private float m_startDistance;
        private float m_distDiff; //to give quality of results
        private IList<int> m_indexes = new List<int>();
        private int m_trailColor = nextTrailColor++;

        public TrailResult(IActivity activity, int order, IList<int> indexes, float distDiff)
        {
            m_activity = activity;
			m_order = order;
			m_startIndex = indexes[0];
            m_endIndex = indexes[indexes.Count-1];
            foreach (int i in indexes)
            {
                m_indexes.Add(i);
            }
            m_distDiff = distDiff;

            m_startTime = m_activity.StartTime.AddSeconds(m_activity.GPSRoute[m_startIndex].ElapsedSeconds);
            m_startDistance = m_activity.GPSRoute.GetDistanceMetersTrack()[m_startIndex].Value;
            if (!aActivities.ContainsKey(m_activity))
            {
                aActivities.Add(m_activity, new trActivityInfo());
                aActivities[m_activity].activityColor = nextActivityColor++;
            }
            aActivities[m_activity].res.Add(this);
        }

        public IActivity Activity
        {
            get { return m_activity; }
        }
        public float DistDiff
        {
            get { return m_distDiff; }
        }
        public int Order
        {
			get {
				return m_order;
			}
		}

        public TimeSpan StartTime
        {
            get
            {
                return m_startTime.ToLocalTime().TimeOfDay;
            }
        }
        public TimeSpan EndTime
        {
            get
            {
                return m_activity.GPSRoute.EntryDateTime(m_activity.GPSRoute[m_endIndex]).ToLocalTime().TimeOfDay;
            }
        }
        public DateTime FirstTime
        {
            get
            {
                return m_startTime;
            }
        }
        public DateTime LastTime
        {
            get
            {
                return m_activity.GPSRoute.EntryDateTime(m_activity.GPSRoute[m_endIndex]);
            }
        }
        public double FirstDist
        {
            get
            {
                return m_startDistance;
            }
        }
        public double LastDist
        {
            get
            {
                return m_activity.GPSRoute.GetDistanceMetersTrack()[m_endIndex].Value;
            }
        }
        public double getDistAt(DateTime t)
        {
            //Ignore malformed activities
            double res = 0;
            try
            {
                res = m_activity.GPSRoute.GetDistanceMetersTrack().GetInterpolatedValue(t).Value - FirstDist;
            }
            catch { }
            return res;
        }
        public DateTime getTimeAt(double t)
        {
            DateTime res = FirstTime;
            try 
            {
                res = m_activity.GPSRoute.GetDistanceMetersTrack().GetTimeAtDistanceMeters(t);
            }
            catch { }
            return res;
        }
        public IList<DateTime> TimeTrailPoints
        {
            get
            {
                IList<DateTime> results = new List<DateTime>();
                foreach (int i in m_indexes)
                {
                    results.Add(m_activity.GPSRoute.EntryDateTime(m_activity.GPSRoute[i]));
                }
                return results;
            }
        }
        public IList<double> DistanceTrailPoints
        {
            get
            {
                IList<double> results = new List<double>();
                foreach (int i in m_indexes)
                {
                    results.Add(m_activity.GPSRoute.GetDistanceMetersTrack()[i].Value-m_startDistance);
                }
                return results;
            }
        }
        public TimeSpan Duration
        {
			get {
				return TimeSpan.FromSeconds(
					m_activity.GPSRoute[m_endIndex].ElapsedSeconds
					- m_activity.GPSRoute[m_startIndex].ElapsedSeconds
				);
			}
		}
		public string Distance {
			get {
				float distance = 0;
				for (int i = m_startIndex; i < m_endIndex; i++) {
					distance += m_activity.GPSRoute[i].Value.DistanceMetersToPoint(
						m_activity.GPSRoute[i + 1].Value
					);
				}
                return Utils.Units.DistanceToString(distance, "");
			}
		}

		public float AvgCadence {
			get {
				return CadencePerMinuteTrack.Avg;
			}
		}
		public float AvgHR {
			get {
				return HeartRatePerMinuteTrack.Avg;
			}
		}
		public float MaxHR {
			get {
				return HeartRatePerMinuteTrack.Max;
			}
		}
		public float AvgPower {
			get {
				return PowerWattsTrack.Avg;
			}
		}
		public float AvgGrade {
			get {
				return GradeTrack.Avg;
			}
		}
		public float AvgSpeed {
			get {
				return this.SpeedTrack.Avg;
			}
		}
		public float FastestSpeed {
			get {
				return this.SpeedTrack.Max;
			}
		}
		public double AvgPace {
			get {
				return this.PaceTrack.Avg;
			}
		}
		public double FastestPace {
			get {
				return this.PaceTrack.Min;
			}
		}
        public string ElevChg
        {
			get {
				float value = m_activity.GPSRoute[m_endIndex].Value.ElevationMeters - m_activity.GPSRoute[m_startIndex].Value.ElevationMeters;
				return (value > 0 ? "+" : "") + Utils.Units.ElevationToString(value, "");
			}
		}
		public IDistanceDataTrack DistanceMetersTrack 
        {
            get{
                //Note: Must have the same indexes as the GPS track....
                if (null == m_distanceMetersTrack)
                {
                    m_distanceMetersTrack = new DistanceDataTrack();
                    IDistanceDataTrack track = m_activity.GPSRoute.GetDistanceMetersTrack();
                    if (track != null)
                    {
                        float startDistance = track[m_startIndex].Value;
                        for (int i = m_startIndex; i <= m_endIndex; i++)
                        {
                            ITimeValueEntry<float> value = track[i];
                            m_distanceMetersTrack.Add(
                                m_startTime.AddSeconds(value.ElapsedSeconds),
                                value.Value - startDistance
                            );
                        }
                    }
                }
                return m_distanceMetersTrack;
			}
		}
		public INumericTimeDataSeries ElevationMetersTrack {
			get {
				if (m_elevationMetersTrack == null) {
					ActivityInfo activityInfo = ActivityInfoCache.Instance.GetInfo(m_activity);
					m_elevationMetersTrack = this.copyTrailTrack(activityInfo.SmoothedElevationTrack);
				}
				return m_elevationMetersTrack;
			}
		}

		public IActivityCategory Category {
			get {
				return m_activity.Category;
			}
		}

        public INumericTimeDataSeries copyTrailTrack(INumericTimeDataSeries source)
        {
            INumericTimeDataSeries track = new NumericTimeDataSeries();
            if (source != null)
            {
                for (int i = 0; i < this.DistanceMetersTrack.Count; i++)
                {
                    DateTime time = m_startTime.AddSeconds(this.DistanceMetersTrack[i].ElapsedSeconds);
                    ITimeValueEntry<float> value = source.GetInterpolatedValue(time);
                    if (value != null)
                    {
                        track.Add(time, value.Value);
                    }
                }
            }
            return track;
        }

		public INumericTimeDataSeries CadencePerMinuteTrack {
			get {
				if (m_cadencePerMinuteTrack == null) {
					ActivityInfo activityInfo = ActivityInfoCache.Instance.GetInfo(m_activity);
					m_cadencePerMinuteTrack = this.copyTrailTrack(activityInfo.SmoothedCadenceTrack);
				}
				return m_cadencePerMinuteTrack;
			}
		}
		public INumericTimeDataSeries HeartRatePerMinuteTrack {
			get {
				if (m_heartRatePerMinuteTrack == null) {
					ActivityInfo activityInfo = ActivityInfoCache.Instance.GetInfo(m_activity);
					m_heartRatePerMinuteTrack = this.copyTrailTrack(activityInfo.SmoothedHeartRateTrack);
				}
				return m_heartRatePerMinuteTrack;
			}
		}
		public INumericTimeDataSeries PowerWattsTrack {
			get {
				if (m_powerWattsTrack == null) {
					ActivityInfo activityInfo = ActivityInfoCache.Instance.GetInfo(m_activity);
					m_powerWattsTrack = this.copyTrailTrack(activityInfo.SmoothedPowerTrack);
				}
				return m_powerWattsTrack;
			}
		}
		public INumericTimeDataSeries SpeedTrack {
			get {
				if (m_speedTrack == null) {
					m_speedTrack = new NumericTimeDataSeries();
					ActivityInfo activityInfo = ActivityInfoCache.Instance.GetInfo(m_activity);
					for (int i = 0; i < this.DistanceMetersTrack.Count; i++) {
						DateTime time = m_startTime.AddSeconds(this.DistanceMetersTrack[i].ElapsedSeconds);
						ITimeValueEntry<float> value = activityInfo.SmoothedSpeedTrack.GetInterpolatedValue(time);
						if (value != null) {
                            float speed = Utils.Units.GetSpeed(value.Value, m_activity, Speed.Units.Speed);
                            m_speedTrack.Add(time, speed);
						}
					}
				}
				return m_speedTrack;
			}
		}
		public INumericTimeDataSeries PaceTrack {
			get {
				if (m_paceTrack == null) {
                    //PaceTrack could share a common base track (in m/s) with SpeedTrack,
                    //and be converted to pace/speed units when referenced
                    m_paceTrack = new NumericTimeDataSeries();
                    ActivityInfo activityInfo = ActivityInfoCache.Instance.GetInfo(m_activity);
                    for (int i = 0; i < this.DistanceMetersTrack.Count; i++)
                    {
                        DateTime time = m_startTime.AddSeconds(this.DistanceMetersTrack[i].ElapsedSeconds);
                        ITimeValueEntry<float> value = activityInfo.SmoothedSpeedTrack.GetInterpolatedValue(time);
                        if (value != null)
                        {
                            float pace = Utils.Units.GetSpeed(value.Value, m_activity, Speed.Units.Pace);
                            if (pace != float.NaN)
                            {
                                m_paceTrack.Add(time, pace);
                            }
                        }
                    }
				}
				return m_paceTrack;
			}
		}
        public INumericTimeDataSeries GradeTrack
        {
            get
            {
                if (m_gradeTrack == null)
                {
                    ActivityInfo activityInfo = ActivityInfoCache.Instance.GetInfo(m_activity);
                    m_gradeTrack = this.copyTrailTrack(activityInfo.SmoothedGradeTrack);
                }
                return m_gradeTrack;
            }
        }
        private void getGps()
        {
            m_gpsTrack = new GPSRoute();
            m_gpsPoints = new List<IGPSPoint>();
            for (int i = m_startIndex; i <= m_endIndex; i++)
            {
                m_gpsPoints.Add(m_activity.GPSRoute[i].Value);
                m_gpsTrack.Add(m_activity.GPSRoute.EntryDateTime(m_activity.GPSRoute[i]), m_activity.GPSRoute[i].Value);
            }
        }
        public IGPSRoute GpsTrack
        {
            get
            {
                if (m_gpsTrack == null)
                {
                    getGps();
                }
                return m_gpsTrack;
            }
        }
        public IList<IGPSPoint> GpsPoints()
        {
            if (m_gpsPoints == null)
            {
                getGps();
            }
            return m_gpsPoints;
        }
        public IList<IGPSPoint> GpsPoints(Data.TrailsItemTrackSelectionInfo t)
        {
            if (t.MarkedTimes != null && t.MarkedTimes.Count > 0)
            {
                return GpsPoints(t.MarkedTimes);
            }
            else if (t.MarkedDistances != null && t.MarkedDistances.Count > 0)
            {
                return GpsPoints(t.MarkedDistances);
            }
            return new List<IGPSPoint>();
        }
        private IList<IGPSPoint> GpsPoints(IValueRangeSeries<DateTime> t)
        {
            IGPSRoute gpsTrack = this.GpsTrack;
            IList<IGPSPoint> result = new List<IGPSPoint>();

            foreach (IValueRange<DateTime> r in t)
            {
                int i = m_startIndex;
                while (i <= m_endIndex &&
                    0 < r.Lower.CompareTo(m_activity.GPSRoute.EntryDateTime(m_activity.GPSRoute[i])))
                {
                    i++;
                }
                while (i <= m_endIndex &&
                    0 <= r.Upper.CompareTo(m_activity.GPSRoute.EntryDateTime(m_activity.GPSRoute[i])))
                {
                    result.Add(m_activity.GPSRoute[i].Value);
                    i++;
                }
            }

            return result;
        }
        private IList<IGPSPoint> GpsPoints(IValueRangeSeries<double> t)
        {
            IGPSRoute gpsTrack = this.GpsTrack;
            IList<IGPSPoint> result = new List<IGPSPoint>();

            foreach (IValueRange<double> r in t)
            {
                int i = m_startIndex;
                while (i <= m_endIndex &&
                    r.Lower > DistanceMetersTrack[i - m_startIndex].Value)
                {
                    i++;
                }
                while (i <= m_endIndex &&
                    r.Upper >= DistanceMetersTrack[i - m_startIndex].Value)
                {
                    result.Add(m_activity.GPSRoute[i].Value);
                    i++;
                }
            }

            return result;
        }

        /*************************************************/
        #region Color
        private static int nextTrailColor = 1;
        private static int nextActivityColor = 1;

        public Color ActivityColor
        {
            get
            {
                trActivityInfo t = new trActivityInfo();
                aActivities.TryGetValue(this.m_activity, out t);
                return getColor(t.activityColor);
            }
        }

        public Color TrailColor
        {
            get
            {
                return getColor(this.m_trailColor);
            }
        }

        private Color getColor(int color)
        {
            switch (color%10)
            {
                case 0: return Color.Blue;
                case 1: return Color.Red;
                case 2: return Color.Green;
                case 3: return Color.Orange;
                case 4: return Color.Plum;
                case 5: return Color.HotPink;
                case 6: return Color.Gold;
                case 7: return Color.Silver;
                case 8: return Color.YellowGreen;
                case 9: return Color.Turquoise;
            }
            return Color.Black;
        }

        //private Color newColor()
        //{
        //    int color = nextIndex;
        //    nextIndex = (nextIndex + 1) % 10;
        //    return getColor(color);
        //}
        #endregion

        #region Activity caches
        private class trActivityInfo
        {
            public IList<TrailResult> res = new List<TrailResult>();
            public int activityColor = 0;
        }
        private static IDictionary<IActivity, trActivityInfo> aActivities = new Dictionary<IActivity, trActivityInfo>();
        public static IList<TrailResult> TrailResultList(IActivity activity)
        {
            trActivityInfo t = new trActivityInfo();
            aActivities.TryGetValue(activity, out t);
            return t.res;
        }
        #endregion

        #region Implementation of ITrailResult

        float ITrailResult.AvgCadence
        {
            get { return AvgCadence; }
        }

        float ITrailResult.AvgGrade
        {
            get { return AvgGrade; }
        }

        float ITrailResult.AvgHR
        {
            get { return AvgHR; }
        }

        double ITrailResult.AvgPace
        {
            get { return AvgPace; }
        }

        float ITrailResult.AvgPower
        {
            get { return AvgPower; }
        }

        float ITrailResult.AvgSpeed
        {
            get { return AvgSpeed; }
        }

        INumericTimeDataSeries ITrailResult.CadencePerMinuteTrack
        {
            get { return CadencePerMinuteTrack; }
        }

        IActivityCategory ITrailResult.Category
        {
            get { return Category; }
        }

        INumericTimeDataSeries ITrailResult.CopyTrailTrack(INumericTimeDataSeries source)
        {
            return copyTrailTrack(source);
        }

        string ITrailResult.Distance
        {
            get { return Distance; }
        }

        IDistanceDataTrack ITrailResult.DistanceMetersTrack
        {
            get { return DistanceMetersTrack; }
        }

        TimeSpan ITrailResult.Duration
        {
            get { return Duration; }
        }

        string ITrailResult.ElevChg
        {
            get { return ElevChg; }
        }

        INumericTimeDataSeries ITrailResult.ElevationMetersTrack
        {
            get { return ElevationMetersTrack; }
        }

        TimeSpan ITrailResult.EndTime
        {
            get { return EndTime; }
        }

        double ITrailResult.FastestPace
        {
            get { return FastestPace; }
        }

        float ITrailResult.FastestSpeed
        {
            get { return FastestSpeed; }
        }

        INumericTimeDataSeries ITrailResult.GradeTrack
        {
            get { return GradeTrack; }
        }

        INumericTimeDataSeries ITrailResult.HeartRatePerMinuteTrack
        {
            get { return HeartRatePerMinuteTrack; }
        }

        float ITrailResult.MaxHR
        {
            get { return MaxHR; }
        }

        int ITrailResult.Order
        {
            get { return Order; }
        }

        INumericTimeDataSeries ITrailResult.PaceTrack
        {
            get { return PaceTrack; }
        }

        INumericTimeDataSeries ITrailResult.PowerWattsTrack
        {
            get { return PowerWattsTrack; }
        }

        INumericTimeDataSeries ITrailResult.SpeedTrack
        {
            get { return SpeedTrack; }
        }

        TimeSpan ITrailResult.StartTime
        {
            get { return StartTime; }
        }

        #endregion

        #region IComparable<Product> Members

        public int CompareTo(object obj)
        {
            int result = 1;
            if (obj != null && obj is TrailResult)
            {
                TrailResult other = obj as TrailResult;
                result = TrailResultColumnIds.Compare(this, other);
            }
            return result;
        }
        public int CompareTo(TrailResult other)
        {
            return TrailResultColumnIds.Compare(this, other);
        }

        #endregion
    }
}
