﻿/*
Copyright (C) 2009 Brendan Doherty
Copyright (C) 2011-2013 Gerhard Olsson

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
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;

using ZoneFiveSoftware.Common.Data;
using ZoneFiveSoftware.Common.Data.GPS;
using ZoneFiveSoftware.Common.Data.Fitness;
using ZoneFiveSoftware.Common.Data.Measurement;
using ZoneFiveSoftware.Common.Visuals.Fitness;
using ITrailExport;
using TrailsPlugin.Utils;
using GpsRunningPlugin.Util;

namespace TrailsPlugin.Data
{
    public class PositionParentTrailResult : ParentTrailResult
    {
        //Normal TrailResult
        public PositionParentTrailResult(ActivityTrail activityTrail, int order, TrailResultInfo indexes, float distDiff, bool reverse)
            : base(activityTrail, order, indexes, distDiff)
        {
            this.m_reverse = reverse;
        }
    }

    public class SplitsParentTrailResult : ParentTrailResult
    {
        public SplitsParentTrailResult(ActivityTrail activityTrail, int order, TrailResultInfo indexes) :
            base(activityTrail, order, indexes, 0)
        {
        }
    }

    public class HighScoreParentTrailResult : ParentTrailResult
    {
        public HighScoreParentTrailResult(ActivityTrail activityTrail, int order, TrailResultInfo indexes, string toolTip)
            : base(activityTrail, order, indexes, 0)
        {
            this.m_toolTip = toolTip;
        }
    }

    public class ParentTrailResult : TrailResult
    {
        protected ParentTrailResult(ActivityTrail activityTrail, int order, TrailResultInfo indexes, float distDiff) :
            base(activityTrail, order, indexes, distDiff)
        {
        }

        public bool OverlapParent = true;

        public IList<ChildTrailResult> getChildren()
        {
            IList<ChildTrailResult> splits = new List<ChildTrailResult>();
            if (this.m_subResultInfo.Count > 1)
            {
                int i; //start time index
                int subChildIndex = 1; //Swim sub index
                for (i = 0; i < m_subResultInfo.Count - 1; i++)
                {
                    if (m_subResultInfo.Points[i].Time != DateTime.MinValue &&
                                (!this.Trail.IsSplits || !Settings.RestIsPause || m_subResultInfo.Points[i].Required))
                    {
                        int j; //end time index
                        for (j = i + 1; j < m_subResultInfo.Points.Count; j++)
                        {
                            if (m_subResultInfo.Points[j].Time != DateTime.MinValue)
                            {
                                break;
                            }
                        }
                        if (this.m_subResultInfo.Count > i &&
                            this.m_subResultInfo.Count > j)
                        {
                            if (m_subResultInfo.Points[j].Time != DateTime.MinValue)
                            {
                                TrailResultInfo indexes = m_subResultInfo.CopyToChild(i, j);
                                int childIndex = i + 1;
                                if (m_subResultInfo.Points[i].Order >= 0)
                                {
                                    childIndex = m_subResultInfo.Points[i].Order;
                                }
                                ChildTrailResult ctr = new NormalChildTrailResult(this, childIndex, indexes);
                                //Note: paused results may be added, no limit for childresults
                                TimeSpan duration = ZoneFiveSoftware.Common.Data.Algorithm.DateTimeRangeSeries.TimeNotPaused(ctr.StartTime, ctr.EndTime, this.Pauses);
                                if (duration > TimeSpan.FromSeconds(1))
                                {
                                    splits.Add(ctr);
                                    if (m_subResultInfo.Points[i].SubPoints.Count > 1)
                                    {
                                        TrailResultInfo indexes2 = m_subResultInfo.Copy();
                                        for (int k = 0; k < m_subResultInfo.Points[i].SubPoints.Count - 1; k++)
                                        {
                                            indexes2.Points.Clear();
                                            indexes2.Points.Add(m_subResultInfo.Points[i].SubPoints[k]);
                                            indexes2.Points.Add(m_subResultInfo.Points[i].SubPoints[k + 1]);
                                            ChildTrailResult sub = new SubChildTrailResult(ctr, subChildIndex++, indexes2);
                                        }
                                    }
                                }
                            }
                        }
                        i = j - 1;//Next index to try
                    }
                }
                if (Data.Settings.ShowPausesAsResults)
                {
                    int maxChildIndex = splits.Count - 1;
                    foreach (IValueRange<DateTime> v in this.Pauses)
                    {
                        TrailResultInfo t = new TrailResultInfo(this.m_subResultInfo.Activity, this.m_subResultInfo.Reverse);
                        DateTime lower = v.Lower;
                        if(lower == DateTime.MinValue)
                        {
                            lower = m_subResultInfo.Points[0].Time;
                        }
                        DateTime upper = v.Upper;
                        if (upper == DateTime.MaxValue)
                        {
                            upper = m_subResultInfo.Points[m_subResultInfo.Points.Count-1].Time;
                        }
                        TimeSpan duration = upper - lower;
                        if(duration < TimeSpan.FromSeconds(2) && 
                            (this.StartTime - lower < TimeSpan.FromSeconds(1) ||
                            upper - this.EndTime < TimeSpan.FromSeconds(1)))
                        {
                            continue;
                        }
                        t.Points.Add(new TrailResultPoint(new TrailGPSLocation("Pause", false), lower, duration));
                        t.Points.Add(new TrailResultPoint(new TrailGPSLocation("Pause", false), upper, TimeSpan.Zero));
                        PausedChildTrailResult tr = new PausedChildTrailResult(this, -1, t);
                        for(int j = 0; j <= maxChildIndex; j++)
                        {
                            if (j == maxChildIndex || lower < splits[j].StartTime || 
                                splits[j].StartTime <= lower && lower < splits[j+1].StartTime)
                            {
                                tr.RelatedChildResult = splits[j];
                                break;
                            }
                        }
                        splits.Add(tr);
                    }
                }
                TimeSpan sp = TimeSpan.Zero;
                bool ok = true;
                foreach (ChildTrailResult ctr in splits)
                {
                    if (ctr.DurationIsNull())
                    {
                        ok = false;
                        break;
                    }
                    if (!(ctr is PausedChildTrailResult))
                    {
                        sp = sp.Add(ctr.Duration);
                    }
                }
                if (ok)
                {
                    this.m_duration = sp;
                }
            }
            //Only one subresult is not shown, add pool length to main result
            if (splits.Count <= 1 && this.SubResultInfo.Points.Count > 0)
            {
                this.m_PoolLengthInfo = this.SubResultInfo.Points[0].PoolLengthInfo;
            }
            m_childrenResults = splits;
            return splits;
        }

        //should be temporary, to get (possible) children that are a part of the parent
        internal IList<ChildTrailResult> m_childrenResults = new List<ChildTrailResult>();
        internal void RemoveChildren(TrailResultWrapper tr)
        {
            if (this.m_childrenResults != null
                && tr.Result is ChildTrailResult)
            {
                ChildTrailResult ctr = tr.Result as ChildTrailResult;
                if (this.m_childrenResults.Contains(ctr))
                {
                    this.m_childrenResults.Remove(ctr);
                }
            }
        }
    }
}
