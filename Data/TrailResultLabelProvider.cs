﻿/*
Copyright (C) 2010 Gerhard Olsson

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
using System.Collections.Generic;
using ZoneFiveSoftware.Common.Visuals;
using System.Drawing;
using GpsRunningPlugin.Util;

namespace TrailsPlugin.Data
{
    class TrailResultLabelProvider : TreeList.DefaultLabelProvider
    {

        private bool m_multiple = false;
        private Controller.TrailController m_controller;

        public Controller.TrailController Controller
        {
            set { m_controller = value; }
        }


        public bool MultipleActivities
        {
            set { m_multiple = value; }
        }

        #region ILabelProvider Members

        public override Image GetImage(object element, TreeList.Column column)
        {
            Data.TrailResult row = TrailsPlugin.UI.Activity.ResultListControl.getTrailResultRow(element);

            if (column.Id == "Color")
            {
                Bitmap image = new Bitmap(column.Width, 15);
                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        image.SetPixel(x, y, row.TrailColor);
                    }
                }
                return image;
            }
            else
            {
                return base.GetImage(row.Activity, column);
            }
        }

        public override string GetText(object element, TreeList.Column column)
        {
            Data.TrailResult row = TrailsPlugin.UI.Activity.ResultListControl.getTrailResultRow(element);

            switch (column.Id)
            {
                case TrailResultColumnIds.Order:
                    return row.Order.ToString();
                case TrailResultColumnIds.Color:
                    return null;
                case TrailResultColumnIds.StartTime:
                    if (row.Activity == null) return null;
                    string date = "";
                    if (m_multiple)
                    {
                        date = row.StartTime.ToLocalTime().ToShortDateString() + " ";
                    }
                    return date + row.StartTime.ToLocalTime().ToString("T");
                case TrailResultColumnIds.StartDistance:
                    return UnitUtil.Distance.ToString(row.StartDist, "");
                case TrailResultColumnIds.EndTime:
                    if (row.Activity == null) return null;
                    return row.EndTime.ToLocalTime().ToString("T");
                case TrailResultColumnIds.Duration:
                    return UnitUtil.Time.ToString(row.Duration, "");
                case TrailResultColumnIds.Distance:
                    return UnitUtil.Distance.ToString(row.Distance, m_controller.ReferenceActivity, "");
                case TrailResultColumnIds.AvgCadence:
                    return UnitUtil.Cadence.ToString(row.AvgCadence);
                case TrailResultColumnIds.AvgHR:
                    return UnitUtil.HeartRate.ToString(row.AvgHR);
                case TrailResultColumnIds.MaxHR:
                    return UnitUtil.HeartRate.ToString(row.MaxHR);
                case TrailResultColumnIds.Ascent:
                    return UnitUtil.Elevation.ToString(row.Ascent);
                case TrailResultColumnIds.Descent:
                    return UnitUtil.Elevation.ToString(row.Descent);
                case TrailResultColumnIds.ElevChg:
                    return (row.ElevChg > 0 ? "+" : "") + UnitUtil.Elevation.ToString(row.ElevChg, "");
                case TrailResultColumnIds.AvgPower:
                    return UnitUtil.Power.ToString(row.AvgPower);
                case TrailResultColumnIds.AscAvgGrade:
                    return (row.AscAvgGrade).ToString("0.0%");
                case TrailResultColumnIds.DescAvgGrade:
                    return (row.DescAvgGrade).ToString("0.0%");
                case TrailResultColumnIds.AvgSpeed:
                    return UnitUtil.Speed.ToString(row.AvgSpeed, m_controller.ReferenceActivity, "");
                case TrailResultColumnIds.FastestSpeed:
                    return UnitUtil.Speed.ToString(row.FastestSpeed, m_controller.ReferenceActivity, "");
                case TrailResultColumnIds.AvgPace:
                    return UnitUtil.Pace.ToString(row.AvgSpeed, m_controller.ReferenceActivity, "");
                case TrailResultColumnIds.FastestPace:
                    return UnitUtil.Pace.ToString(row.FastestSpeed, m_controller.ReferenceActivity, "");
                case TrailResultColumnIds.AvgSpeedPace:
                    return UnitUtil.PaceOrSpeed.ToString(row.AvgSpeed, m_controller.ReferenceActivity, "");
                case TrailResultColumnIds.FastestSpeedPace:
                    return UnitUtil.PaceOrSpeed.ToString(row.FastestSpeed, m_controller.ReferenceActivity, "");
                case TrailResultColumnIds.Name:
                    return row.Name;
                default:
                    if (row.Activity == null) return null;
                    return base.GetText(row.Activity, column);
            }
        }

        #endregion
    }
}
