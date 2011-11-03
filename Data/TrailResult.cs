﻿/*
Copyright (C) 2009 Brendan Doherty
Copyright (C) 2011 Gerhard Olsson

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
using ZoneFiveSoftware.Common.Visuals.Fitness;
using ITrailExport;
using GpsRunningPlugin.Util;

namespace TrailsPlugin.Data
{
    public class TrailResult : ITrailResult, IComparable
    {
        private ActivityTrail m_activityTrail;
        private IActivity m_activity;
        private int m_order;
        private string m_name;
        private bool m_reverse;

        private TrailResult m_parentResult;
        private IList<TrailResult> m_childrenResults;
        private TrailResultInfo m_childrenInfo;

        private DateTime? m_startTime;
        private DateTime? m_endTime;
        private float m_startDistance = float.NaN;
        private float m_totalDistDiff; //to give quality of results
        private Color m_trailColor = getColor(nextTrailColor++);
        private string m_toolTip;
        //Temporary?
        public static bool m_diffOnDateTime = false;

        private IValueRangeSeries<DateTime> m_pauses;
        private IDistanceDataTrack m_distanceMetersTrack;
        private IDistanceDataTrack m_activityDistanceMetersTrack;
        //private INumericTimeDataSeries m_elevationMetersTrack;
        //private INumericTimeDataSeries m_cadencePerMinuteTrack;
        //private INumericTimeDataSeries m_heartRatePerMinuteTrack;
        //private INumericTimeDataSeries m_powerWattsTrack;
        //private INumericTimeDataSeries m_speedTrack;
        //private INumericTimeDataSeries m_gradeTrack;

        //Converted tracks, to display format, with smoothing
        //Should be in a separate class
        private TrailResult m_cacheTrackRef;
        private IDistanceDataTrack m_distanceMetersTrack0;
        private INumericTimeDataSeries m_cadencePerMinuteTrack0;
        private INumericTimeDataSeries m_elevationMetersTrack0;
        private INumericTimeDataSeries m_gradeTrack0;
        private INumericTimeDataSeries m_heartRatePerMinuteTrack0;
        private INumericTimeDataSeries m_powerWattsTrack0;
        private INumericTimeDataSeries m_speedTrack0;
        private INumericTimeDataSeries m_paceTrack0;
        private INumericTimeDataSeries m_DiffTimeTrack0;
        private INumericTimeDataSeries m_DiffDistTrack0;
        IList<double> m_trailPointTime0;
        IList<double> m_trailPointDist0;

        private IGPSRoute m_gpsTrack;
        private IList<IList<IGPSPoint>> m_gpsPoints;

        //common for all - UR uses activities and this is a cache for resultList too
        //This could well be moved to another cache
        private static IActivity m_cacheTrackActivity;
        private static IDictionary<IActivity, IItemTrackSelectionInfo[]> m_commonStretches;
        private static ActivityInfoOptions m_TrailActivityInfoOptions;

        public static ActivityInfoOptions TrailActivityInfoOptions
        {
            get
            {
                if (m_TrailActivityInfoOptions == null)
                {
                    m_TrailActivityInfoOptions = new ActivityInfoOptions(true);
                }
                return m_TrailActivityInfoOptions;
            }
            set
            {
                m_TrailActivityInfoOptions = value;
            }
        }

        //reverse not used yet
        public TrailResult(ActivityTrail activityTrail, int order, TrailResultInfo indexes, float distDiff, bool reverse)
            : this(activityTrail, null, order, indexes, distDiff)
        {
            this.m_reverse = reverse;
        }
        public TrailResult(ActivityTrail activityTrail, int order, TrailResultInfo indexes, float distDiff)
            : this(activityTrail, null, order, indexes, distDiff)
        {
        }

        public TrailResult(ActivityTrail activityTrail, int order, TrailResultInfo indexes, float distDiff, string toolTip)
            : this(activityTrail, null, order, indexes, distDiff)
        {
            m_toolTip = toolTip;
        }

        private TrailResult(ActivityTrail activityTrail, TrailResult par, int order, TrailResultInfo indexes, float distDiff)
        {
            createTrailResult(activityTrail, par, order, indexes, distDiff);
        }

        //Create from splits
        public TrailResult(ActivityTrail activityTrail, Trail trail, IActivity activity, int order)
        {
            TrailResultInfo indexes;
            Data.Trail.TrailGpsPointsFromSplits(activity, out indexes);
            createTrailResult(activityTrail, null, order, indexes, float.MaxValue);
        }

        private void createTrailResult(ActivityTrail activityTrail, TrailResult par, int order, TrailResultInfo indexes, float distDiff)
        {
            m_activityTrail = activityTrail;
            m_parentResult = par;
            m_activity = indexes.Activity;
            m_order = order;
            m_name = indexes.Name;
            m_childrenInfo = indexes.Copy();
            m_totalDistDiff = distDiff;

            if (par == null)
            {
                if (!s_activities.ContainsKey(m_activity))
                {
                    s_activities.Add(m_activity, new trActivityInfo());
                    //aActivities[m_activity].activityColor = getColor(nextActivityColor++);
                }
                s_activities[m_activity].res.Add(this);
            }
            else
            {
                if (par.m_childrenResults == null)
                {
                    par.m_childrenResults = new List<TrailResult>();
                }
                par.m_childrenResults.Add(this);
            }
        }

        public IList<TrailResult> getSplits()
        {
            IList<TrailResult> splits = new List<TrailResult>();
            if (this.m_childrenInfo.Count > 1)
            {
                int i; //start time index
                for (i = 0; i < m_childrenInfo.Count - 1; i++)
                {
                    if (m_childrenInfo.Points[i].Time != DateTime.MinValue)
                    {
                        int j; //end time index
                        for (j = i + 1; j < m_childrenInfo.Points.Count; j++)
                        {
                            if (m_childrenInfo.Points[j].Time != DateTime.MinValue)
                            {
                                break;
                            }
                        }
                        if (this.m_childrenInfo.Count > i &&
                            this.m_childrenInfo.Count > j)
                        {
                            if (m_childrenInfo.Points[j].Time != DateTime.MinValue)
                            {
                                TrailResultInfo t = m_childrenInfo.CopySlice(i, j);
                                TrailResult tr = new TrailResult(m_activityTrail, this, i + 1, t, m_totalDistDiff);
                                //if (aActivities.Count > 1)
                                //{
                                //    nextTrailColor--;
                                //    tr.m_trailColor = this.m_trailColor;
                                //}
                                splits.Add(tr);
                            }
                        }
                        i = j - 1;//Next index to try
                    }
                }
            }
            return splits;
        }

        /**********************************************************/
        public IActivity Activity
        {
            get { return m_activity; }
        }
        //Distance error used to sort results
        public float DistDiff
        {
            get { return (float)(m_totalDistDiff / Math.Pow(m_childrenInfo.Count, 1.5)); }
        }
        public int Order
        {
            get
            {
                return m_order;
            }
        }
        public bool Reverse
        {
            get
            {
                return m_reverse;
            }
        }
        /****************************************************************/
        public TimeSpan Duration
        {
            get
            {
                return ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.TimeNotPaused(
                   StartDateTime, EndDateTime, Pauses);
            }
        }
        public double Distance
        {
            get
            {
                if (DistanceMetersTrack == null || DistanceMetersTrack.Count == 0)
                {
                    return 0;
                }
                return DistanceMetersTrack.Max;
            }
        }

        public TimeSpan StartTime
        {
            get
            {
                return StartDateTime.ToLocalTime().TimeOfDay;
            }
        }
        public TimeSpan EndTime
        {
            get
            {
                return EndDateTime.ToLocalTime().TimeOfDay;
            }
        }

        //Start/end time must match the Distance track
        public DateTime StartDateTime
        {
            get
            {
                if (m_startTime == null)
                {
                    DateTime startTime = m_childrenInfo.Points[0].Time;
                    DateTime endTime = m_childrenInfo.Points[m_childrenInfo.Points.Count - 1].Time;
                    m_startTime = getFirstUnpausedTime(startTime, Pauses, true);
                    if (endTime.CompareTo((DateTime)m_startTime) <= 0)
                    {
                        //Trail (or subtrail) is completely paused. Use all
                        m_startTime = startTime;
                    }
                    if (m_startTime == DateTime.MinValue)
                    {
                        m_startTime = Info.ActualTrackStart;
                        if (m_startTime == DateTime.MinValue)
                        {
                            m_startTime = this.Activity.StartTime;
                        }
                    }
                }
                return (DateTime)m_startTime;
            }
        }
        public DateTime EndDateTime
        {
            get
            {
                if (m_endTime == null)
                {
                    DateTime startTime = m_childrenInfo.Points[0].Time;
                    DateTime endTime = m_childrenInfo.Points[m_childrenInfo.Points.Count - 1].Time;
                    m_endTime = getFirstUnpausedTime(endTime, Pauses, false);
                    if (startTime.CompareTo((DateTime)m_endTime) >= 0)
                    {
                        //Trail (or subtrail) is completely paused. Use all
                        m_endTime = endTime;
                    }
                    if (m_endTime == DateTime.MinValue)
                    {
                        m_endTime = Info.ActualTrackEnd;
                        if (m_endTime == DateTime.MinValue)
                        {
                            m_endTime = Info.EndTime;
                        }
                    }
                }
                return (DateTime)m_endTime;
            }
        }

        //All of result including pauses/stopped
        //This is how FilteredStatistics want the info
        public IValueRangeSeries<DateTime> getSelInfo()
        {
            IValueRangeSeries<DateTime> t = new ValueRangeSeries<DateTime>();
            t.Add(new ValueRange<DateTime>(this.StartDateTime, this.EndDateTime));
            return t;
        }

        public double StartDist
        {
            get
            {
                if (Settings.StartDistOffsetFromStartPoint &&
                    this.Activity.GPSRoute != null && this.Activity.GPSRoute.Count > 0)
                {
                    //Get offset from start point, regardless if there is a pause
                    DateTime startTime = StartDateTime;
                    int i = -1;
                    for (int k = 0; k < this.TrailPointDateTime.Count; k++)
                    {
                        if (this.TrailPointDateTime[k] > DateTime.MinValue)
                        {
                            //The used start time for the point, disregarding pauses
                            startTime = getFirstUnpausedTime(this.TrailPointDateTime[k], new ValueRangeSeries<DateTime>(), true);
                            i = k;
                            break;
                        }
                    }
                    //Alternative, get time on track
                    //startDistance = this.ActivityDistanceMetersTrack.GetInterpolatedValue(StartDateTime).Value -
                    //    this.ActivityDistanceMetersTrack.GetInterpolatedValue(startTime).Value;
                    float startDistance = -1000; //Negative to see it in list
                    if (i >= 0 && i < this.m_activityTrail.Trail.TrailLocations.Count)
                    {
                        ITimeValueEntry<IGPSPoint> entry = this.Activity.GPSRoute.GetInterpolatedValue(StartDateTime);
                        if (entry != null)
                        {
                            startDistance = this.m_activityTrail.Trail.TrailLocations[i].DistanceMetersToPoint(entry.Value);
                        }
                    }
                    return startDistance;
                }
                else
                {
                    if (float.IsNaN(m_startDistance))
                    {
                        getDistanceTrack();
                        if (float.IsNaN(m_startDistance))
                        {
                            m_startDistance = 0;
                        }
                    }
                }
                return m_startDistance;
            }
        }


        /*************************************************/
        //DateTime vs elapsed result/activity, distance result/activity conversions
        //The correct tracks must be in DistanceMetersTrack and ActivityDistanceMetersTrack

        //Get result time and distance from activity references
        public float getDistResult(DateTime t)
        {
            return getDistFromTrackTime(DistanceMetersTrack, t);
        }
        public float getDistResultFromDistActivity(double t)
        {
            return getDistFromTrackTime(DistanceMetersTrack, getDateTimeFromDistActivity(t));
        }
        public DateTime getDateTimeFromDistActivity(double t)
        {
            return getDateTimeFromTrackDist(ActivityDistanceMetersTrack, (float)t);
        }
        public DateTime getDateTimeFromDistResult(double t)
        {
            DateTime time = getDateTimeFromTrackDist(DistanceMetersTrack, (float)t);
            return adjustTimeToLimits(time);
        }
        //public float getDistActivityFromDistResult(double t)
        //{
        //    return getDistFromTrackTime(ActivityDistanceMetersTrack, getDateTimeFromDistResult(t));
        //}
        //public double getDistResultFromUnpausedDistActivity(double t)
        //{
        //    return getDistResult(getDateTimeFromUnpausedDistActivity(t));
        //}
        //public DateTime getDateTimeFromUnpausedDistActivity(double t)
        //{
        //    return ActivityUnpausedDistanceMetersTrack.GetTimeAtDistanceMeters(t);
        //}

        //Elapsed vs DateTime for result
        public double getElapsedResult(DateTime d)
        {
            return getElapsedTimeSpanResult(d).TotalSeconds;
        }

        private TimeSpan getElapsedTimeSpanResult(DateTime entryTime)
        {
            TimeSpan elapsed = ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.TimeNotPaused(
                this.DistanceMetersTrack.StartTime, entryTime, Pauses);
            return elapsed;
        }

        //(Distance) Result to activity 
        public DateTime getDateTimeFromElapsedResult(float t)
        {
            DateTime time = ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.AddTimeAndPauses(DistanceMetersTrack.StartTime, TimeSpan.FromSeconds(t), Pauses);
            return adjustTimeToLimits(time);
        }
        public DateTime getDateTimeFromElapsedActivity(float t)
        {
            return ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.AddTimeAndPauses(ActivityDistanceMetersTrack.StartTime, TimeSpan.FromSeconds(t), Pauses);
        }

        private DateTime adjustTimeToLimits(DateTime time)
        {
            if (time < this.StartDateTime)
            {
                time = StartDateTime;
            }
            if (time > this.EndDateTime)
            {
                time = EndDateTime;
            }
            return time;
        }

        /**********************************/
        //Static methods

        private static float getDistFromTrackTime(IDistanceDataTrack distTrack, DateTime t)
        {
            int status;
            return getDistFromTrackTime(distTrack, t, out status);
        }
        private static float getDistFromTrackTime(IDistanceDataTrack distTrack, DateTime t, out int status)
        {
            //Ignore malformed activities and selection outside the result
            float res = 0;
            status = 1;
            ITimeValueEntry<float> entry = distTrack.GetInterpolatedValue(t);
            if (entry != null)
            {
                res = entry.Value;
                status = 0;
            }
            else if (distTrack.Count > 0 && t >= distTrack.StartTime.AddSeconds(2))
            {
                //Seem to be out of bounds
                //Any time after start is handled as end, as pauses may complicate calcs (default is 0, i.e. before)
                res = distTrack[distTrack.Count - 1].Value;
            }
            return res;
        }

        public static DateTime getDateTimeFromTrackDist(IDistanceDataTrack distTrack, float t)
        {
            DateTime res = DateTime.MinValue;
            if (t >= distTrack.Max && distTrack.Count > 0)
            {
                res = distTrack.StartTime.AddSeconds(distTrack.TotalElapsedSeconds);
            }
            else if (t <= distTrack.Min)
            {
                res = distTrack.StartTime;
            }
            else
            {
                try
                {
                    res = distTrack.GetTimeAtDistanceMeters(t);
                }
                catch { }
            }
            return res;
        }

        //Chart and Result must have the same understanding of Distance
        public static double DistanceConvertTo(double t, TrailResult refRes)
        {
            IActivity activity = refRes == null ? null : refRes.Activity;
            return UnitUtil.Distance.ConvertTo(t, activity);
        }
        public static double DistanceConvertFrom(double t, TrailResult refRes)
        {
            IActivity activity = refRes == null ? null : refRes.Activity;
            return UnitUtil.Distance.ConvertFrom(t, activity);
        }

        /***************************************************/

        private static DateTime getFirstUnpausedTime(DateTime time, IValueRangeSeries<DateTime> pauses, bool next)
        {
            if (ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(time, pauses))
            {
                foreach (IValueRange<DateTime> pause in pauses)
                {
                    if (time.CompareTo(pause.Lower) >= 0 &&
                        time.CompareTo(pause.Upper) <= 0)
                    {
                        if (next)
                        {
                            time = (pause.Upper).Add(TimeSpan.FromSeconds(1));
                        }
                        else if (pause.Lower > DateTime.MinValue)
                        {
                            time = (pause.Lower).Add(TimeSpan.FromSeconds(-1));
                        }
                        break;
                    }
                }
            }
            return time;
        }
        bool isIncludeStoppedCategory(IActivityCategory category)
        {
            if (category == null || Settings.ExcludeStoppedCategory == null || Settings.ExcludeStoppedCategory == "")
            {
                return true;
            }
            else
            {
                String[] values = Settings.ExcludeStoppedCategory.Split(';');
                foreach (String name in values)
                {
                    if (name.Contains(category.Name))
                    {
                        return false;
                    }
                }
            }
            return isIncludeStoppedCategory(category.Parent);
        }
        private bool? m_includeStopped;
        private bool IncludeStopped
        {
            get
            {
                if (m_includeStopped == null)
                {
#if ST_2_1
            // If UseEnteredData is set, exclude Stopped
            if (info.Activity.UseEnteredData == false && info.Time.Equals(info.ActualTrackTime))
            {
                m_includeStopped = true;
            }
#else
                    m_includeStopped = TrailsPlugin.Plugin.GetApplication().SystemPreferences.AnalysisSettings.IncludeStopped;
#endif
                    if ((bool)m_includeStopped)
                    {
                        m_includeStopped = isIncludeStoppedCategory(this.Activity.Category);
                    }
                }
                return (bool)m_includeStopped;
            }
        }

        /*********************************************/

        public IValueRangeSeries<DateTime> Pauses
        {
            get
            {
                if (ParentResult != null)
                {
                    //Note: Assumes that subtrails are a part of parent - could be changed
                    return ParentResult.Pauses;
                }
                if (m_pauses == null)
                {
                    m_pauses = new ValueRangeSeries<DateTime>();
                    IValueRangeSeries<DateTime> actPause;
                    actPause = Info.NonMovingTimes;
                    foreach (ValueRange<DateTime> t in actPause)
                    {
                        m_pauses.Add(t);
                    }

                    if (Settings.RestIsPause)
                    {
                        //Check for active laps first
                        bool isActive = false;
                        foreach (ILapInfo lap in Activity.Laps)
                        {
                            if (!lap.Rest)
                            {
                                isActive = true;
                                break;
                            }
                        }
                        if (isActive)
                        {
                            for (int i = 0; i < Activity.Laps.Count; i++)
                            {
                                ILapInfo lap = Activity.Laps[i];
                                if (lap.Rest)
                                {
                                    DateTime upper;
                                    if (i < Activity.Laps.Count - 1)
                                    {
                                        upper = Activity.Laps[i + 1].StartTime;
                                        if (!Activity.Laps[i + 1].Rest)
                                        {
                                            upper -= TimeSpan.FromSeconds(1);
                                        }
                                    }
                                    else
                                    {
                                        upper = Info.EndTime;
                                    }
                                    m_pauses.Add(new ValueRange<DateTime>(lap.StartTime, upper));
                                }
                            }
                        }
                    }
                    if (Settings.RestIsPause)
                    {
                        IList<TrailGPSLocation> trailLocations = m_activityTrail.Trail.TrailLocations;
                        if (m_activityTrail.Trail.IsSplits)
                        {
                            trailLocations = Trail.TrailGpsPointsFromSplits(this.m_activity);
                        }
                        for (int i = 0; i < this.TrailPointDateTime.Count - 1; i++)
                        {
                            if (i < trailLocations.Count &&
                                !trailLocations[i].Required &&
                                this.TrailPointDateTime[i] > DateTime.MinValue)
                            {
                                DateTime lower = this.TrailPointDateTime[i];
                                DateTime upper = this.EndDateTime;
                                while (i < this.TrailPointDateTime.Count &&
                                    i < trailLocations.Count &&
                                    (!trailLocations[i].Required ||
                                    this.TrailPointDateTime[i] == DateTime.MinValue))
                                {
                                    i++;
                                }
                                if (i < this.TrailPointDateTime.Count &&
                                    this.TrailPointDateTime[i] > DateTime.MinValue)
                                {
                                    upper = this.TrailPointDateTime[i];
                                }
                                m_pauses.Add(new ValueRange<DateTime>(lower, upper));
                            }
                        }
                    }
                    //if (ParentResult != null)
                    //{
                    //    if (this.ParentResult.StartDateTime_0 < this.StartDateTime_0)
                    //    {
                    //        m_pauses.Add(new ValueRange<DateTime>(this.ParentResult.StartDateTime_0, this.StartDateTime_0));
                    //    }
                    //    if (this.EndDateTime_0 < this.ParentResult.EndDateTime_0)
                    //    {
                    //        m_pauses.Add(new ValueRange<DateTime>(this.EndDateTime_0, this.ParentResult.EndDateTime_0));
                    //    }
                    //}
                }
                return m_pauses;
            }
        }

        /*********************************************/
        //Distance

        private void getDistanceTrack()
        {
            if (null == m_distanceMetersTrack)
            {
                m_distanceMetersTrack = new DistanceDataTrack();
                m_distanceMetersTrack.AllowMultipleAtSameTime = false;
                if (this.ParentResult != null)
                {
                    m_activityDistanceMetersTrack = this.ParentResult.ActivityDistanceMetersTrack;
                    //m_distanceMetersTrack could be created from parent too (if second rounding is disregarded),
                    //but it is simpler and not heavier to use same code-path as parent
                }
                else
                {
                    m_activityDistanceMetersTrack = Info.MovingDistanceMetersTrack;
                    m_activityDistanceMetersTrack.AllowMultipleAtSameTime = false;
                    if (m_activityDistanceMetersTrack != null)
                    {
                        //insert points at borders in m_activityDistanceMetersTrack
                        m_activityDistanceMetersTrack = (DistanceDataTrack)(new InsertValues<float>(this, m_activityDistanceMetersTrack, m_activityDistanceMetersTrack)).
                            insertValues();
                    }
                }
                if (m_activityDistanceMetersTrack != null)
                {
                    int i = 0;
                    float distance = 0;
                    //Start(end) should always be inserted - it the base for StartTime in track
                    m_distanceMetersTrack.Add(StartDateTime, distance);
                    m_startDistance = TrailResult.getDistFromTrackTime(m_activityDistanceMetersTrack, StartDateTime);
                    float prevDist = m_startDistance;
                    int oldElapsed = 0;

                    while (i < m_activityDistanceMetersTrack.Count &&
                        StartDateTime.AddSeconds(1) > m_activityDistanceMetersTrack.EntryDateTime(m_activityDistanceMetersTrack[i]))
                    {
                        i++;
                    }

                    while (i < m_activityDistanceMetersTrack.Count &&
                        EndDateTime > m_activityDistanceMetersTrack.EntryDateTime(m_activityDistanceMetersTrack[i]))
                    {
                        ITimeValueEntry<float> timeValue = m_activityDistanceMetersTrack[i];
                        uint elapsed = timeValue.ElapsedSeconds;
                        DateTime time = m_activityDistanceMetersTrack.EntryDateTime(timeValue);
                        if (elapsed > oldElapsed &&
                            !ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(time, Pauses))
                        {
                            float actDist = timeValue.Value;
                            if (!float.IsNaN(prevDist))
                            {
                                //TODO: Get the offsets at boundaries, to minimize cumulative errors
                                //There are points at borders though
                                distance += actDist - prevDist;
                            }
                            m_distanceMetersTrack.Add(time, distance);
                            prevDist = actDist;
                            oldElapsed = (int)elapsed;
                        }
                        else
                        {
                            prevDist = float.NaN;
                        }
                        i++;
                    }
                    //Set real last distance, even if elapsedSec is not matching
                    if (!float.IsNaN(prevDist))
                    {
                        distance += TrailResult.getDistFromTrackTime(this.m_activityDistanceMetersTrack, EndDateTime) - prevDist;
                        m_distanceMetersTrack.Add(EndDateTime, distance);
                    }
                }
            }
        }

        //Distance track for trail, adjusted with pauses
        public IDistanceDataTrack DistanceMetersTrack
        {
            get
            {
                getDistanceTrack();
                return m_distanceMetersTrack;
            }
        }

        //Distance track as ST sees it. Used in conversions
        public IDistanceDataTrack ActivityDistanceMetersTrack
        {
            get
            {
                getDistanceTrack();
                return m_activityDistanceMetersTrack;
            }
        }

        //The Activity related tracks were previously cached. Kept as an uniform method to get
        //The tracks, if the Info cache handling is changed
        private INumericTimeDataSeries CadencePerMinuteTrack
        {
            get
            {
                return Info.SmoothedCadenceTrack;
            }
        }

        private INumericTimeDataSeries ElevationMetersTrack
        {
            get
            {
                return Info.SmoothedElevationTrack;
            }
        }
        private INumericTimeDataSeries GradeTrack
        {
            get
            {
                return Info.SmoothedGradeTrack;
            }
        }

        private INumericTimeDataSeries HeartRatePerMinuteTrack
        {
            get
            {
                return Info.SmoothedHeartRateTrack;
            }
        }

        private INumericTimeDataSeries PowerWattsTrack
        {
            get
            {
                return Info.SmoothedPowerTrack;
            }
        }

        private INumericTimeDataSeries SpeedTrack
        {
            get
            {
                return Info.SmoothedSpeedTrack;
            }
        }

        public IList<DateTime> TrailPointDateTime
        {
            get
            {
                return m_childrenInfo.CopyTime();
            }
        }

        /*************************************************/
        public TrailResult ParentResult
        {
            get
            {
                return m_parentResult;
            }
        }

        public String Name
        {
            get
            {
                return m_name;
            }
        }
        public string ToolTip
        {
            get
            {
                if (m_toolTip != null)
                {
                    return m_toolTip;
                }
                else
                {
                    string s = string.Format("{0} {1} {2}", Activity.StartTime.ToLocalTime(), Activity.Name, Activity.Notes.Substring(0, Math.Min(Activity.Notes.Length, 40)));
                    if (m_diffOnDateTime)
                    {
                        s += " " + Activity.Metadata.Source;
                    }
                    return s;
                }
            }
        }

        public float AvgCadence
        {
            get
            {
                INumericTimeDataSeries track = CadencePerMinuteTrack0(m_cacheTrackRef);
                if (track == null || track.Count == 0)
                {
                    return Info.AverageCadence;
                }
                return track.Avg;
            }
        }
        public float AvgHR
        {
            get
            {
                INumericTimeDataSeries track = HeartRatePerMinuteTrack0(m_cacheTrackRef);
                if (track == null || track.Count == 0)
                {
                    return Info.AverageHeartRate;
                }
                return track.Avg;
            }
        }
        public float MaxHR
        {
            get
            {
                INumericTimeDataSeries track = HeartRatePerMinuteTrack0(m_cacheTrackRef);
                if (track == null || track.Count == 0)
                {
                    return Info.MaximumHeartRate;
                }
                return track.Max;
            }
        }
        public float AvgPower
        {
            get
            {
                float result;
                INumericTimeDataSeries track = PowerWattsTrack0(m_cacheTrackRef);
                if (track == null || track.Count == 0)
                {
                    result = Info.AveragePower;
                }
                else
                {
                    result = track.Avg;
                }
                return (float)UnitUtil.Power.ConvertTo(result, m_cacheTrackRef.Activity);
            }
        }
        public float AvgGrade
        {
            get
            {
                return GradeTrack0(m_cacheTrackRef).Avg;
            }
        }
        public float AvgSpeed
        {
            get
            {
                return (float)(this.Distance / this.Duration.TotalSeconds);
            }
        }
        public float FastestSpeed
        {
            get
            {
                checkCacheRef(m_cacheTrackRef);
                return (float)UnitUtil.Speed.ConvertTo(this.SpeedTrack0(m_cacheTrackRef).Max, m_cacheTrackRef.Activity);
            }
        }
        //Smoothing could differ speed/pace, why this is separate
        public double FastestPace
        {
            get
            {
                checkCacheRef(m_cacheTrackRef);
                return UnitUtil.Pace.ConvertTo(this.PaceTrack0(m_cacheTrackRef).Min, m_cacheTrackRef.Activity);
            }
        }
        public double ElevChg
        {
            get
            {
                float value = 0;
                INumericTimeDataSeries elevationTrack = ElevationMetersTrack0(m_cacheTrackRef);
                if (elevationTrack != null && elevationTrack.Count > 1)
                {
                    value = (float)UnitUtil.Elevation.ConvertTo(
                        elevationTrack[elevationTrack.Count - 1].Value -
                        elevationTrack[0].Value,
                        m_cacheTrackRef.Activity);
                }
                return value;
            }
        }

        public IActivityCategory Category
        {
            get
            {
                return m_activity.Category;
            }
        }

        /**************************************************************************/
        //Reset values used in calculations
        public void Clear(bool onlyDisplay)
        {
            m_distanceMetersTrack0 = null;
            m_elevationMetersTrack0 = null;
            m_gradeTrack0 = null;
            m_heartRatePerMinuteTrack0 = null;
            m_powerWattsTrack0 = null;
            m_cadencePerMinuteTrack0 = null;
            m_speedTrack0 = null;
            m_paceTrack0 = null;
            m_DiffTimeTrack0 = null;
            m_DiffDistTrack0 = null;
            m_trailPointTime0 = null;
            m_trailPointDist0 = null;

            //smoothing control
            //m_TrailActivityInfoOptions = null;

            if (!onlyDisplay)
            {
                m_startTime = null;
                m_endTime = null;
                m_includeStopped = null;

                m_distanceMetersTrack = null;
                m_gpsPoints = null;
                m_gpsTrack = null;

                if (ParentResult == null)
                {
                    m_activityDistanceMetersTrack = null;
                    m_ActivityInfo = null;
                    m_pauses = null;
                }
            }
        }

        private bool checkCacheRef(TrailResult refRes)
        {
            if (refRes == null || refRes != m_cacheTrackRef)
            {
                //Clear cache where ref (possibly null) has been used
                Clear(true);
                m_cacheTrackRef = refRes;
                if (m_cacheTrackRef == null)
                {
                    //A reference is needed to set for instance display format
                    m_cacheTrackRef = this;
                }

                return true;
            }
            return false;
        }

        private static bool checkCacheAct(IActivity refRes)
        {
            if (refRes == null || refRes != m_cacheTrackActivity)
            {
                m_cacheTrackActivity = refRes;
                m_commonStretches = null;

                return true;
            }
            return false;
        }

        internal class InsertValues<T>
        {
            TrailResult result;
            ITimeDataSeries<T> track;
            ITimeDataSeries<T> source;

            public InsertValues(TrailResult t, ITimeDataSeries<T> track, ITimeDataSeries<T> source)
            {
                this.result = t;
                this.track = track;
                this.source = source;
            }
            public void insertValues(DateTime atime)
            {
                if (atime >= result.StartDateTime && atime <= result.EndDateTime &&
                    !ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(atime, result.Pauses))
                {
                    ITimeValueEntry<T> interpolatedP = this.source.GetInterpolatedValue(atime);
                    if (interpolatedP != null)
                    {
                        try
                        {
                            this.track.Add(atime, interpolatedP.Value);
                        }
                        catch { }
                    }
                }
            }
            public ITimeDataSeries<T> insertValues()
            {
                //Insert points around pauses and points
                //This is needed to get the track match the cut-up activity
                //(otherwise for instance start point need to be added)

                //Bug in ST 3.0.4205 not resorting
                int noPoints = track.Count;

                //start/end should be included from points, but prepare for changes...
                insertValues(result.StartDateTime);
                insertValues(result.EndDateTime);

                foreach (IValueRange<DateTime> p in result.Pauses)
                {
                    if (p.Lower > DateTime.MinValue)
                    {
                        insertValues(p.Lower.AddSeconds(-1));
                    }
                    insertValues(p.Upper.AddSeconds(1));
                }
                foreach (TrailResultPoint t in result.m_childrenInfo.Points)
                {
                    DateTime time = t.Time;
                    if (time > DateTime.MinValue)
                    {
                        insertValues(time.Subtract(TimeSpan.FromSeconds(1)));
                        insertValues(time);
                    }
                }

                if (noPoints > 0)
                {
                    //AllowMultipleAtSameTime=false does not work in 3.0.4205
                    bool reSort = false;
                    for (int i = noPoints; i < track.Count; i++)
                    {
                        if (track[noPoints - 1].ElapsedSeconds > track[i].ElapsedSeconds)
                        {
                            reSort = true;
                            break;
                        }
                    }
                    if (reSort)
                    {
                        SortedDictionary<uint, ITimeValueEntry<T>> dic = new SortedDictionary<uint, ITimeValueEntry<T>>();
                        foreach (ITimeValueEntry<T> g in track)
                        {
                            if (!dic.ContainsKey(g.ElapsedSeconds))
                            {
                                dic.Add(g.ElapsedSeconds, g);
                            }
                        }
                        DateTime startTime = track.StartTime;
                        track.Clear();
                        foreach (KeyValuePair<uint, ITimeValueEntry<T>> g in dic)
                        {
                            track.Add(startTime.AddSeconds(g.Value.ElapsedSeconds), g.Value.Value);
                        }
                    }
                }
                //Return the original track, the typecasting must work
                return this.track;
            }
        }

        //copy the relevant part to a new track
        private INumericTimeDataSeries copyTrailTrack(INumericTimeDataSeries source)
        {
            return copySmoothTrack(source, true, 0, new Convert(ConvertNone), m_cacheTrackRef);
        }

        //Convert to/from internal format to display format
        public delegate double Convert(double value, IActivity activity);
        //Empty definition, when no conversion needed
        public static double ConvertNone(double p, IActivity activity)
        {
            return p;
        }

        //Copy a track and apply smoothing
        public INumericTimeDataSeries copySmoothTrack(INumericTimeDataSeries source, bool insert, int smooth, Convert convert, TrailResult refRes)
        {
            IActivity refActivity = null;
            if (refRes != null)
            {
                refActivity = refRes.Activity;
            }
            INumericTimeDataSeries track = new NumericTimeDataSeries();
            track.AllowMultipleAtSameTime = false;
            if (source != null)
            {
                if (insert)
                {
                    //Insert values around borders, to limit effects when track is chopped
                    //Do this before other additions, so start is StartTime for track
                    track = (INumericTimeDataSeries)(new InsertValues<float>(this, track, source)).insertValues();
                }
                int oldElapsed = int.MinValue;
                foreach (ITimeValueEntry<float> t in source)
                {
                    DateTime time = source.EntryDateTime(t);
                    if (StartDateTime <= time && time <= EndDateTime &&
                        !ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(time, Pauses))
                    {
                        uint elapsed = t.ElapsedSeconds;
                        if (elapsed > oldElapsed)
                        {
                            track.Add(time, (float)convert(t.Value, refActivity));
                            oldElapsed = (int)elapsed;
                        }
                    }
                    if (time > EndDateTime)
                    {
                        break;
                    }
                }
                if (smooth > 0)
                {
                    float min; float max;
                    track = ZoneFiveSoftware.Common.Data.Algorithm.NumericTimeDataSeries.Smooth(track, smooth, out min, out max);
                }
            }
            return track;
        }


        public IDistanceDataTrack DistanceMetersTrack0(TrailResult refRes)
        {
            checkCacheRef(refRes);
            if (m_distanceMetersTrack0 == null)
            {
                //Just copy the track. No smoothing or inserting of values
                m_distanceMetersTrack0 = new DistanceDataTrack();
                foreach (ITimeValueEntry<float> entry in this.DistanceMetersTrack)
                {
                    float val = (float)UnitUtil.Distance.ConvertFrom(entry.Value, m_cacheTrackRef.Activity);
                    if (!float.IsNaN(val))
                    {
                        DateTime time = DistanceMetersTrack.EntryDateTime(entry);
                        m_distanceMetersTrack0.Add(time, val);
                    }
                }
            }
            return m_distanceMetersTrack0;
        }

        public INumericTimeDataSeries ElevationMetersTrack0(TrailResult refRes)
        {
            checkCacheRef(refRes);
            if (m_elevationMetersTrack0 == null)
            {
                m_elevationMetersTrack0 = copySmoothTrack(this.ElevationMetersTrack, true, TrailActivityInfoOptions.ElevationSmoothingSeconds,
                    new Convert(UnitUtil.Elevation.ConvertFrom), refRes);
            }
            return m_elevationMetersTrack0;
        }

        public INumericTimeDataSeries GradeTrack0(TrailResult refRes)
        {
            //checkCacheRef(refRes);
            if (m_gradeTrack0 == null)
            {
                m_gradeTrack0 = copySmoothTrack(this.GradeTrack, true, TrailActivityInfoOptions.ElevationSmoothingSeconds,
                    new Convert(UnitUtil.Grade.ConvertFrom), refRes);
            }
            return m_gradeTrack0;
        }

        public INumericTimeDataSeries CadencePerMinuteTrack0(TrailResult refRes)
        {
            //checkCacheRef(refRes);
            if (m_cadencePerMinuteTrack0 == null)
            {
                m_cadencePerMinuteTrack0 = copySmoothTrack(this.CadencePerMinuteTrack, true, TrailActivityInfoOptions.CadenceSmoothingSeconds,
                    new Convert(UnitUtil.Cadence.ConvertFrom), refRes);
            }
            return m_cadencePerMinuteTrack0;
        }

        public INumericTimeDataSeries HeartRatePerMinuteTrack0(TrailResult refRes)
        {
            //checkCacheRef(refRes);
            if (m_heartRatePerMinuteTrack0 == null)
            {
                m_heartRatePerMinuteTrack0 = copySmoothTrack(this.HeartRatePerMinuteTrack, true, TrailActivityInfoOptions.HeartRateSmoothingSeconds,
                    new Convert(UnitUtil.HeartRate.ConvertFrom), refRes);
            }
            return m_heartRatePerMinuteTrack0;
        }

        public INumericTimeDataSeries PowerWattsTrack0(TrailResult refRes)
        {
            //checkCacheRef(refRes);
            if (m_powerWattsTrack0 == null)
            {
                m_powerWattsTrack0 = copySmoothTrack(this.PowerWattsTrack, true, TrailActivityInfoOptions.PowerSmoothingSeconds,
                    new Convert(UnitUtil.Power.ConvertFrom), refRes);
            }
            return m_powerWattsTrack0;
        }

        public INumericTimeDataSeries SpeedTrack0(TrailResult refRes)
        {
            checkCacheRef(refRes);
            if (m_speedTrack0 == null)
            {
                m_speedTrack0 = copySmoothTrack(this.SpeedTrack, false, TrailActivityInfoOptions.SpeedSmoothingSeconds,
                                new Convert(UnitUtil.Speed.ConvertFrom), refRes);
            }
            return m_speedTrack0;
        }

        public INumericTimeDataSeries PaceTrack0(TrailResult refRes)
        {
            checkCacheRef(refRes);
            if (m_paceTrack0 == null)
            {
                m_paceTrack0 = copySmoothTrack(this.SpeedTrack, false, TrailActivityInfoOptions.SpeedSmoothingSeconds,
                                new Convert(UnitUtil.Pace.ConvertFrom), refRes);
            }
            return m_paceTrack0;
        }

        /********************************************************************/

        private TrailResult getRefSub(TrailResult parRes)
        {
            TrailResult res = parRes;
            if (this.ParentResult != null && parRes != null && parRes.m_childrenResults != null)
            {
                //This is a subsplit, get the subsplit related to the ref
                foreach (TrailResult tr in parRes.m_childrenResults)
                {
                    if (this.m_order == tr.Order)
                    {
                        res = tr;
                        break;
                    }
                }
            }
            return res;
        }

        /********************************************************************/

        public INumericTimeDataSeries DiffTimeTrack0(TrailResult refRes)
        {
            checkCacheRef(refRes);
            if (m_DiffTimeTrack0 == null)
            {
                m_DiffTimeTrack0 = new NumericTimeDataSeries();
                TrailResult trRef = getRefSub(m_cacheTrackRef);
                if (this.DistanceMetersTrack.Count > 0 && trRef != null)
                {
                    int oldElapsed = int.MinValue;
                    float lastValue = 0;
                    int dateTrailPointIndex = -1;
                    float refOffset = 0;
                    float refPrevElapsedSec = 0;
                    float diffOffset = 0;

                    bool prevCommonStreches = false;
                    IValueRangeSeries<DateTime> commonStretches = null;
                    if (Settings.DiffUsingCommonStretches && trRef.Activity != null)
                    {
                        //TODO: Use reference stretch too
                        commonStretches = CommonStretches(trRef.Activity, new List<IActivity> { this.Activity }, null)[this.Activity][0].MarkedTimes;
                        m_DiffTimeTrack0.Add(StartDateTime, 0);
                    }

                    foreach (ITimeValueEntry<float> t in DistanceMetersTrack)
                    {
                        uint elapsed = t.ElapsedSeconds;
                        if (elapsed > oldElapsed)
                        {
                            DateTime d1 = this.DistanceMetersTrack.EntryDateTime(t);
                            if (!ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(d1, Pauses))
                            {
                                while (Settings.ResyncDiffAtTrailPoints &&
                                    this.TrailPointDateTime.Count == trRef.TrailPointDateTime.Count && //Splits etc
                                    (dateTrailPointIndex == -1 ||
                                    dateTrailPointIndex < this.TrailPointDateTime.Count - 1 &&
                                    d1 > this.TrailPointDateTime[dateTrailPointIndex + 1]))
                                {
                                    dateTrailPointIndex++;
                                    if (dateTrailPointIndex < this.TrailPointDateTime.Count - 1 &&
                                        dateTrailPointIndex < trRef.TrailPointDateTime.Count - 1 &&
                                        this.TrailPointDateTime[dateTrailPointIndex] > DateTime.MinValue &&
                                        trRef.TrailPointDateTime[dateTrailPointIndex] > DateTime.MinValue)
                                    {
                                        refOffset = trRef.getDistResult(trRef.TrailPointDateTime[dateTrailPointIndex]) -
                                           this.getDistResult(this.TrailPointDateTime[dateTrailPointIndex]);
                                        if (Settings.AdjustResyncDiffAtTrailPoints)
                                        {
                                            //diffdist over time will normally "jump" at each trail point
                                            //I.e. if the reference is behind, the distance suddenly gained must be subtracted
                                            //Note: ST only uses int for time, use double anyway
                                            float oldElapsedSec = oldElapsed > 0 ? oldElapsed : 0;
                                            float refElapsedP = (float)trRef.getElapsedResult(trRef.TrailPointDateTime[dateTrailPointIndex]);
                                            float elapsedP = (float)this.getElapsedResult(this.TrailPointDateTime[dateTrailPointIndex]);
                                            diffOffset += (refElapsedP - refPrevElapsedSec - (elapsedP - oldElapsed));
                                        }
                                    }
                                }

                                if (Settings.DiffUsingCommonStretches &&
                                    //IsPaused is in the series here...
                                !ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(d1, commonStretches))
                                {
                                    prevCommonStreches = true;
                                }
                                else
                                {
                                    //elapsed (entry) is elapsed in the series, not elapsed seconds....
                                    float elapsedSec = (float)this.getElapsedResult(d1);
                                    float? refElapsedSec = null;
                                    if (trRef == this)
                                    {
                                        ////"inconsistency" from getDateTimeFromTrack() can happen if the ref stands still, getDateTimeFromTrack returns first elapsed
                                        //refElapsedSec = elapsedSec;
                                        //get diff from average
                                        refElapsedSec = this.getDistResult(d1) / this.AvgSpeed;
                                    }
                                    else
                                    {
                                        if (t.Value + refOffset <= trRef.DistanceMetersTrack.Max)
                                        {
                                            DateTime d2 = TrailResult.getDateTimeFromTrackDist(trRef.DistanceMetersTrack, t.Value + refOffset);
                                            refElapsedSec = (float)trRef.getElapsedResult(d2);
                                        }
                                    }
                                    if (refElapsedSec != null)
                                    {
                                        if (Settings.DiffUsingCommonStretches && prevCommonStreches)
                                        {
                                            diffOffset = elapsedSec - (float)refElapsedSec;
                                            prevCommonStreches = false;
                                        }
                                        lastValue = (float)refElapsedSec - elapsedSec + diffOffset;
                                        m_DiffTimeTrack0.Add(d1, lastValue);
                                        oldElapsed = (int)elapsed;
                                        refPrevElapsedSec = (float)refElapsedSec;
                                    }
                                }
                            }
                        }
                    }
                    //Add a point last in the track, to show the complete dist in the chart
                    //Alternative use lastvalue
                    dateTrailPointIndex = this.TrailPointDateTime.Count - 1;
                    if (trRef != this &&
                        !ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(EndDateTime, Pauses) &&
                        (Settings.ResyncDiffAtTrailPoints || this.m_activityTrail.Trail.IsReference || !this.m_activityTrail.Trail.Generated) &&
                        m_cacheTrackRef == trRef && //Otherwise will cache be cleared for splits...
                        this.TrailPointDateTime.Count == trRef.TrailPointDateTime.Count && //Splits etc
                        dateTrailPointIndex > 0 &&
                        dateTrailPointIndex == trRef.TrailPointDateTime.Count - 1 &&
                                this.TrailPointDateTime[dateTrailPointIndex] > DateTime.MinValue &&
                                trRef.TrailPointDateTime[dateTrailPointIndex] > DateTime.MinValue)
                    {
                        lastValue = (float)(trRef.TrailPointTime0(trRef)[dateTrailPointIndex] - this.TrailPointTime0(trRef)[dateTrailPointIndex] + diffOffset);
                        m_DiffTimeTrack0.Add(EndDateTime, lastValue);
                    }
                }
            }
            return m_DiffTimeTrack0;
        }

        private ITimeValueEntry<float> getValueEntryOffset(ITimeValueEntry<float> t, int refOffset)
        {
            uint refElapsed = t.ElapsedSeconds;
            if (refElapsed > -refOffset)
            {
                refElapsed = (uint)(refElapsed + refOffset);
            }
            return new TimeValueEntry<float>(refElapsed, t.Value);
        }

        private enum DiffMode { ActivityStart, AbsoluteTime } //TODO:, TimeOfDay }
        public INumericTimeDataSeries DiffDistTrack0(TrailResult refRes)
        {
            checkCacheRef(refRes);
            if (m_DiffDistTrack0 == null)
            {
                m_DiffDistTrack0 = new NumericTimeDataSeries();
                TrailResult trRef = getRefSub(m_cacheTrackRef);
                if (this.DistanceMetersTrack.Count > 0 && trRef != null)
                {
                    int oldElapsed = int.MinValue;
                    float lastValue = 0;
                    int dateTrailPointIndex = -1;
                    int refOffset = 0;
                    float diffOffset = 0;
                    double prevDist = 0;
                    double prevRefDist = 0;

                    bool prevCommonStreches = false;
                    IValueRangeSeries<DateTime> commonStretches = null;
                    if (Settings.DiffUsingCommonStretches && trRef.Activity != null)
                    {
                        commonStretches = CommonStretches(trRef.Activity, new List<IActivity> { this.Activity }, null)[this.Activity][0].MarkedTimes;
                        m_DiffDistTrack0.Add(StartDateTime, 0);
                    }
                    DiffMode diffMode = DiffMode.ActivityStart;
                    if (m_diffOnDateTime && (
                        this.StartDateTime >= trRef.StartDateTime && this.StartDateTime <= trRef.EndDateTime ||
                        this.EndDateTime >= trRef.StartDateTime && this.EndDateTime <= trRef.EndDateTime))
                    {
                        //TODO: Implement
                        diffMode = DiffMode.AbsoluteTime;
                    }
                    foreach (ITimeValueEntry<float> t in this.DistanceMetersTrack)
                    {
                        uint elapsed = t.ElapsedSeconds;
                        if (elapsed > oldElapsed)
                        {
                            DateTime d1 = DistanceMetersTrack.EntryDateTime(t);
                            if (!ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(d1, Pauses))
                            {
                                while (diffMode == DiffMode.ActivityStart && Settings.ResyncDiffAtTrailPoints &&
                                    this.TrailPointDateTime.Count == trRef.TrailPointDateTime.Count && //Splits etc
                                    (dateTrailPointIndex == -1 ||
                                    dateTrailPointIndex < this.TrailPointDateTime.Count - 1 &&
                                    d1 > this.TrailPointDateTime[dateTrailPointIndex + 1]))
                                {
                                    dateTrailPointIndex++;
                                    if (dateTrailPointIndex < this.TrailPointDateTime.Count - 1 &&
                                        dateTrailPointIndex < trRef.TrailPointDateTime.Count - 1 &&
                                        this.TrailPointDateTime[dateTrailPointIndex] > DateTime.MinValue &&
                                        trRef.TrailPointDateTime[dateTrailPointIndex] > DateTime.MinValue)
                                    {
                                        refOffset = (int)(trRef.getElapsedResult(trRef.TrailPointDateTime[dateTrailPointIndex]) -
                                           this.getElapsedResult(this.TrailPointDateTime[dateTrailPointIndex]));
                                        //TODO: Configure, explain (or remove)
                                        if (Settings.AdjustResyncDiffAtTrailPoints)
                                        {
                                            //diffdist over time will normally "jump" at each trail point
                                            //I.e. if the reference is behind, the distance suddenly gained must be subtracted
                                            int status2;
                                            double refDistP = getDistFromTrackTime(trRef.DistanceMetersTrack,
                                                trRef.TrailPointDateTime[dateTrailPointIndex], out status2);
                                            if (status2 == 0)
                                            {
                                                double distP = getDistFromTrackTime(this.DistanceMetersTrack,
                                                    this.TrailPointDateTime[dateTrailPointIndex], out status2);
                                                if (status2 == 0)
                                                {
                                                    diffOffset += (float)(refDistP - prevRefDist - (distP - prevDist));
                                                }
                                            }
                                        }
                                    }
                                }

                                if (Settings.DiffUsingCommonStretches &&
                                    //IsPaused is in the series here...
                                !ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(d1, commonStretches))
                                {
                                    prevCommonStreches = true;
                                }
                                else
                                {
                                    if (t.ElapsedSeconds + refOffset <= trRef.DistanceMetersTrack.TotalElapsedSeconds)
                                    {
                                        int status;
                                        double refDist;
                                        if (this == trRef)
                                        {
                                            //get diff from average
                                            refDist = this.getElapsedResult(d1) * this.AvgSpeed;
                                            status = 0;
                                        }
                                        else
                                        {
                                            DateTime d2;
                                            if (diffMode == DiffMode.ActivityStart)
                                            {
                                                d2 = trRef.DistanceMetersTrack.EntryDateTime(getValueEntryOffset(t, refOffset));
                                            }
                                            else
                                            {
                                                d2 = d1;
                                            }
                                            refDist = getDistFromTrackTime(trRef.DistanceMetersTrack, d2, out status);
                                        }
                                        if (status == 0)
                                        {
                                            if (Settings.DiffUsingCommonStretches && prevCommonStreches)
                                            {
                                                diffOffset = (float)refDist - t.Value;
                                                prevCommonStreches = false;
                                            }
                                            //Only add if valid estimation
                                            double diff = t.Value - refDist + diffOffset;
                                            lastValue = (float)UnitUtil.Distance.ConvertFrom(diff, trRef.Activity);
                                            m_DiffDistTrack0.Add(d1, lastValue);
                                            oldElapsed = (int)elapsed;
                                            prevDist = t.Value;
                                            prevRefDist = refDist;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //Add a point last in the track, to show the complete dist in the chart
                    //Alternatively use lastvalue
                    dateTrailPointIndex = this.TrailPointDateTime.Count - 1;
                    if (trRef != this &&
                        !ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(EndDateTime, Pauses) &&
                        (Settings.ResyncDiffAtTrailPoints || this.m_activityTrail.Trail.IsReference || !this.m_activityTrail.Trail.Generated) &&
                        m_cacheTrackRef == trRef && //Otherwise will cache be cleared for splits...
                        this.TrailPointDateTime.Count == trRef.TrailPointDateTime.Count && //Splits etc
                        dateTrailPointIndex > 0 &&
                        dateTrailPointIndex == trRef.TrailPointDateTime.Count - 1 &&
                                this.TrailPointDateTime[dateTrailPointIndex] > DateTime.MinValue &&
                                trRef.TrailPointDateTime[dateTrailPointIndex] > DateTime.MinValue)
                    {
                        lastValue = (float)(trRef.TrailPointDist0(trRef)[dateTrailPointIndex] - this.TrailPointDist0(trRef)[dateTrailPointIndex] + diffOffset);
                        m_DiffDistTrack0.Add(EndDateTime, lastValue);
                    }
                }
            }
            return m_DiffDistTrack0;
        }


        /********************************************************************/
        IList<double> m_trailPointTimeOffset;
        IList<double> m_trailPointDistOffset;
        public IList<double> TrailPointTimeOffset0(TrailResult refRes)
        {
            TrailPointTime0(refRes);
            return m_trailPointTimeOffset;
        }
        public IList<double> TrailPointDistOffset01(TrailResult refRes)
        {
            TrailPointDist0(refRes);
            IList<double> trailPointDistOffset0 = new List<double>();
            foreach (float f in m_trailPointDistOffset)
            {
                trailPointDistOffset0.Add(UnitUtil.Distance.ConvertFrom(f, m_cacheTrackRef.Activity));
            }

            return trailPointDistOffset0;
        }
        public IList<double> TrailPointTime0(TrailResult refRes)
        {
            //checkCacheRef(refRes);
            if (m_trailPointTime0 == null || m_trailPointTimeOffset == null)
            {
                m_trailPointTime0 = new List<double>();
                m_trailPointTimeOffset = new List<double>();
                for (int k = 0; k < this.TrailPointDateTime.Count; k++)
                {
                    double val, val1;
                    if (this.TrailPointDateTime[k] > DateTime.MinValue)
                    {
                        //The used start time for the point
                        DateTime t1 = getFirstUnpausedTime(this.TrailPointDateTime[k], Pauses, true);
                        val = ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.TimeNotPaused(
                                StartDateTime, t1, Pauses).TotalSeconds;
                        //Offset time from detected to actual start
                        //NonMovingTimes here instead?
                        val1 = ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.TimeNotPaused(
                                this.TrailPointDateTime[k], t1, Activity.TimerPauses).TotalSeconds;
                    }
                    else
                    {
                        if (k > 0)
                        {
                            val = m_trailPointTime0[m_trailPointTime0.Count - 1];
                        }
                        else
                        {
                            val = 0;
                        }
                        val1 = 0;
                    }
                    m_trailPointTime0.Add(val);
                    m_trailPointTimeOffset.Add(val1);
                }
                for (int k = 0; k < m_trailPointTimeOffset.Count - 1; k++)
                {
                    if (m_trailPointTimeOffset[k] > m_trailPointTime0[k + 1] - m_trailPointTime0[k])
                    {
                        m_trailPointTimeOffset[k] = 0;
                    }
                }
            }
            return m_trailPointTime0;
        }

        public IList<double> TrailPointDist0(TrailResult refRes)
        {
            checkCacheRef(refRes);
            if (m_trailPointDist0 == null || m_trailPointDistOffset == null)
            {
                m_trailPointDist0 = new List<double>();
                m_trailPointDistOffset = new List<double>();
                for (int k = 0; k < TrailPointDateTime.Count; k++)
                {
                    double val = 0, val1 = 0;
                    if (TrailPointDateTime[k] > DateTime.MinValue)
                    {
                        ITimeValueEntry<float> interpolatedP = DistanceMetersTrack0(m_cacheTrackRef).GetInterpolatedValue(getFirstUnpausedTime(this.TrailPointDateTime[k], Pauses, true));
                        if (interpolatedP != null)
                        {
                            val = interpolatedP.Value;
                        }
                        else if (k > 0)
                        {
                            val = m_trailPointDist0[k - 1];
                        }

                        //Use "ST" distance track to get offset from detected start to used start
                        interpolatedP = ActivityDistanceMetersTrack.GetInterpolatedValue(getFirstUnpausedTime(this.TrailPointDateTime[k], Pauses, true));
                        ITimeValueEntry<float> interpolatedP2 = ActivityDistanceMetersTrack.GetInterpolatedValue(this.TrailPointDateTime[k]);
                        if (interpolatedP != null && interpolatedP2 != null)
                        {
                            val1 = interpolatedP.Value - interpolatedP2.Value;
                        }
                    }
                    else
                    {
                        if (k > 0)
                        {
                            val = m_trailPointDist0[k - 1];
                        }
                    }
                    m_trailPointDist0.Add(val);
                    m_trailPointDistOffset.Add(val1);
                }
                for (int k = 0; k < m_trailPointDistOffset.Count; k++)
                {
                    if (k < m_trailPointDistOffset.Count - 1 && m_trailPointDistOffset[k] > m_trailPointDist0[k + 1] - m_trailPointDist0[k])
                    {
                        m_trailPointDistOffset[k] = 0;
                    }
                }
            }
            return m_trailPointDist0;
        }

        /****************************************************************/
        private IGPSRoute getGps()
        {
            IGPSRoute gpsTrack = new GPSRoute();
            gpsTrack.AllowMultipleAtSameTime = false;
            if (m_activity.GPSRoute != null && m_activity.GPSRoute.Count > 0 &&
                StartDateTime != DateTime.MinValue && EndDateTime != DateTime.MinValue)
            {
                //Insert values at borders in m_gpsTrack
                gpsTrack = (GPSRoute)(new InsertValues<IGPSPoint>(this, gpsTrack, m_activity.GPSRoute)).insertValues();

                int i = 0;
                while (i < m_activity.GPSRoute.Count)
                {
                    DateTime time = m_activity.GPSRoute.EntryDateTime(m_activity.GPSRoute[i]);
                    if (this.StartDateTime.AddSeconds(1) <= time)
                    {
                        break;
                    }
                    i++;
                }
                while (i < m_activity.GPSRoute.Count)
                {
                    DateTime time = m_activity.GPSRoute.EntryDateTime(m_activity.GPSRoute[i]);
                    if (time >= this.EndDateTime)
                    {
                        break;
                    }

                    if (!ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.IsPaused(time, Pauses))
                    {
                        IGPSPoint point = m_activity.GPSRoute[i].Value;
                        gpsTrack.Add(time, point);
                    }
                    i++;
                }
            }
            return gpsTrack;
        }

        public IGPSRoute GPSRoute
        {
            get
            {
                if (m_gpsTrack == null)
                {
                    m_gpsTrack = getGps();
                    m_gpsPoints = null;
                }
                return m_gpsTrack;
            }
        }

        public IList<IList<IGPSPoint>> GpsPoints()
        {
            if (m_gpsPoints == null)
            {
                IValueRangeSeries<DateTime> t = new ValueRangeSeries<DateTime>();
                t.Add(new ValueRange<DateTime>(StartDateTime, EndDateTime));
                m_gpsPoints = this.GpsPoints(t);
            }
            return m_gpsPoints;
        }

        public IList<IList<IGPSPoint>> GpsPoints(Data.TrailsItemTrackSelectionInfo t)
        {
            //Before calling, the MarkedTimes should be set
            if (t.MarkedTimes != null && t.MarkedTimes.Count > 0)
            {
                if (t.MarkedTimes.Count == 1 &&
                    t.MarkedTimes[0].Lower <= StartDateTime &&
                    t.MarkedTimes[0].Upper >= EndDateTime)
                {
                    //Use cache
                    return this.GpsPoints();
                }
                return this.GpsPoints(t.MarkedTimes);
            }
            return new List<IList<IGPSPoint>>();
        }
        private IList<IList<IGPSPoint>> GpsPoints(IValueRangeSeries<DateTime> t)
        {
            return TrailsItemTrackSelectionInfo.GpsPoints(this.GPSRoute, this.Pauses, t);
        }

        /*************************************************/
        #region Color
        private static int nextTrailColor = 1;
        //private static int nextActivityColor = 1;

        //public Color ActivityColor
        //{
        //    get
        //    {
        //        trActivityInfo t = new trActivityInfo();
        //        aActivities.TryGetValue(this.m_activity, out t);
        //        if (t == null)
        //        {
        //            return Color.Brown;
        //        }
        //        return t.activityColor;
        //    }
        //}

        private bool m_colorOverridden = false;
        public Color TrailColor
        {
            get
            {
                if (!m_colorOverridden && s_activities.Count > 1 && this.ParentResult != null)
                {
                    return this.ParentResult.m_trailColor;
                }
                else
                {
                    return this.m_trailColor;
                }
            }
            set
            {
                m_colorOverridden = true;
                m_trailColor = value;
            }
        }

        private static Color getColor(int color)
        {
            switch (color % 10)
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

        /********************************************************************/
        public static IDictionary<IActivity, IItemTrackSelectionInfo[]> CommonStretches(IActivity refAct, IList<IActivity> acts, System.Windows.Forms.ProgressBar progressBar)
        {
            checkCacheAct(refAct);
            if (null == m_commonStretches)
            {
                m_commonStretches = new Dictionary<IActivity, IItemTrackSelectionInfo[]>();
            }
            IList<IActivity> acts2 = new List<IActivity>();
            foreach (IActivity act in acts)
            {
                if (!m_commonStretches.ContainsKey(act))
                {
                    acts2.Add(act);
                }
            }
            if (acts2.Count > 0)
            {
                IDictionary<IActivity, IItemTrackSelectionInfo[]> commonStretches =
                    TrailsPlugin.Integration.UniqueRoutes.GetCommonStretchesForActivity(refAct, acts2, null);
                foreach (IActivity act in acts2)
                {
                    m_commonStretches.Add(act, commonStretches[act]);
                }
            }
            //Return all...
            return m_commonStretches;
        }

        ActivityInfo m_ActivityInfo = null;
        ActivityInfo Info
        {
            get
            {
                //Caching is not needed, done by ST
                //return ActivityInfoCache.Instance.GetInfo(this.Activity);
                //Custom InfoCache, to control smoothing
                if (this.ParentResult != null)
                {
                    return this.ParentResult.Info;
                }
                if (m_ActivityInfo == null)
                {
                    ActivityInfoCache c = new ActivityInfoCache();
                    ActivityInfoOptions t = new ActivityInfoOptions(false, IncludeStopped);
                    c.Options = t;
                    IActivity activity = this.Activity;
                    //if (this.Pauses != activity.TimerPauses)
                    //{
                    //    IActivity activity2 = Plugin.GetApplication().Logbook.Activities.AddCopy(activity);
                    //    activity = activity2;
                    //    activity.TimerPauses.Clear();
                    //    foreach (IValueRange<DateTime> p in this.Pauses)
                    //    {
                    //        activity.TimerPauses.Add(p);
                    //    }
                    //    activity.Category = Plugin.GetApplication().Logbook.ActivityCategories[1];
                    //    if (activity.Category.SubCategories.Count > 0)
                    //    {
                    //        activity.Category = activity.Category.SubCategories[0];
                    //    }
                    //}
                    m_ActivityInfo = c.GetInfo(activity);
                }
                return m_ActivityInfo;
            }
        }

        private class trActivityInfo
        {
            public IList<TrailResult> res = new List<TrailResult>();
            public Color activityColor = getColor(0);
        }

        private static IDictionary<IActivity, trActivityInfo> s_activities = new Dictionary<IActivity, trActivityInfo>();
        //public static IList<TrailResult> TrailResultList(IActivity activity)
        //{
        //    trActivityInfo t = new trActivityInfo();
        //    s_activities.TryGetValue(activity, out t);
        //    return t.res;
        //}
        public static void Reset()
        {
            nextTrailColor = 1;
            //nextActivityColor = 1;
            s_activities.Clear();
        }
        #endregion

        //Create a copy of this result as an activity
        //Incomplete right now, used to create trails from results
        public IActivity CopyToActivity()
        {
            IActivity activity = Plugin.GetApplication().Logbook.Activities.Add(this.StartDateTime);
            activity.Category = this.Activity.Category;
            activity.GPSRoute = this.GPSRoute;
            activity.TimeZoneUtcOffset = this.Activity.TimeZoneUtcOffset;
            activity.TimerPauses.Clear();
            foreach (IValueRange<DateTime> t in this.Pauses)
            {
                activity.TimerPauses.Add(t);
            }
            return activity;
        }

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
            get { return (float)UnitUtil.Pace.ConvertFrom(AvgSpeed); }
        }

        float ITrailResult.AvgPower
        {
            get { return AvgPower; }
        }

        float ITrailResult.AvgSpeed
        {
            get { return (float)UnitUtil.Speed.ConvertFrom(AvgSpeed); }
        }

        INumericTimeDataSeries ITrailResult.CadencePerMinuteTrack
        {
            get { return CadencePerMinuteTrack0(m_cacheTrackRef); }
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
            get { return UnitUtil.Distance.ToString(Distance, ""); }
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
            get { return (ElevChg > 0 ? "+" : "") + UnitUtil.Elevation.ToString(ElevChg, ""); }
        }

        INumericTimeDataSeries ITrailResult.ElevationMetersTrack
        {
            get { return ElevationMetersTrack0(m_cacheTrackRef); }
        }

        TimeSpan ITrailResult.EndTime
        {
            get { return EndTime; }
        }

        double ITrailResult.FastestPace
        {
            get
            {
                checkCacheRef(m_cacheTrackRef);
                return UnitUtil.Pace.ConvertFrom(FastestPace, m_cacheTrackRef.Activity);
            }
        }

        float ITrailResult.FastestSpeed
        {
            get
            {
                checkCacheRef(m_cacheTrackRef);
                return (float)UnitUtil.Speed.ConvertFrom(FastestSpeed, m_cacheTrackRef.Activity);
            }
        }

        INumericTimeDataSeries ITrailResult.GradeTrack
        {
            get
            {
                return copySmoothTrack(this.GradeTrack, true, TrailActivityInfoOptions.ElevationSmoothingSeconds,
                    new Convert(UnitUtil.Grade.ConvertTo), m_cacheTrackRef);
            }
        }

        INumericTimeDataSeries ITrailResult.HeartRatePerMinuteTrack
        {
            get { return HeartRatePerMinuteTrack0(m_cacheTrackRef); }
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
            get { return PaceTrack0(m_cacheTrackRef); }
        }

        INumericTimeDataSeries ITrailResult.PowerWattsTrack
        {
            get { return PowerWattsTrack0(m_cacheTrackRef); }
        }

        INumericTimeDataSeries ITrailResult.SpeedTrack
        {
            get { return SpeedTrack0(m_cacheTrackRef); }
        }

        TimeSpan ITrailResult.StartTime
        {
            get { return StartTime; }
        }

        #endregion

        #region IComparable<Product> Members

        public int CompareTo(object obj)
        {
            if (obj != null && obj is TrailResult)
            {
                TrailResult other = obj as TrailResult;
                return TrailResultColumnIds.Compare(this, other);
            }
            else
            {
                return this.ToString().CompareTo(obj.ToString());
            }
        }
        public int CompareTo(TrailResult other)
        {
            return TrailResultColumnIds.Compare(this, other);
        }
        #endregion

        public override string ToString()
        {
            return (this.m_startTime != null ? m_startTime.ToString() : "") + " " + this.TrailPointDateTime[0].ToShortTimeString() + " " + this.TrailPointDateTime.Count;
        }
    }
}
