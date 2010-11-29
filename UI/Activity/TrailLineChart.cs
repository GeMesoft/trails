/*
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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

using ZoneFiveSoftware.Common.Data;
using ZoneFiveSoftware.Common.Data.Fitness;
using ZoneFiveSoftware.Common.Visuals;
using ZoneFiveSoftware.Common.Visuals.Chart;

#if ST_2_1
//SaveImage
using ZoneFiveSoftware.SportTracks.UI.Forms;
using TrailsPlugin.Data;
#else
using ZoneFiveSoftware.Common.Visuals.Fitness;
using ZoneFiveSoftware.Common.Visuals.Forms;
#endif

namespace TrailsPlugin.UI.Activity {
	public partial class TrailLineChart : UserControl {
        private Data.TrailResult m_refTrailResult = null;
        private IList<Data.TrailResult> m_trailResults = new List<Data.TrailResult>();
        private XAxisValue m_XAxisReferential = XAxisValue.Time;
        private LineChartTypes m_YAxisReferential = LineChartTypes.Speed;
        private IList<LineChartTypes> m_YAxisReferential_right = null;//temp
        private Color m_ChartFillColor = Color.WhiteSmoke;
        private Color m_ChartLineColor = Color.LightSkyBlue;
        private Color m_ChartSelectedColor = Color.AliceBlue;
        private ITheme m_visualTheme;
        private ActivityDetailPageControl m_page = null;
        private MultiChartsControl m_multiple = null;
        private SingleChartsControl m_single = null;

        public TrailLineChart()
        {
            InitializeComponent();
            InitControls();
        }

        void InitControls()
        {
#if ST_2_1
            this.MainChart.Margin = 0;
#else
            this.MainChart.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
#endif
            MainChart.YAxis.SmartZoom = true;
            copyChartMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.DocumentCopy16;
            copyChartMenuItem.Visible = false;
#if !ST_2_1
            saveImageMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Save16;
#endif
            //selectChartsMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Table16;
            selectChartsMenuItem.Visible = false;
#if !ST_2_1
//            this.listSettingsMenuItem.Click += new System.EventHandler(this.listSettingsToolStripMenuItem_Click);
#else
//            //No listSetting dialog in ST2
//            if (this.contextMenu.Items.Contains(this.listSettingsMenuItem))
//            {
//                this.contextMenu.Items.Remove(this.listSettingsMenuItem);
//            }
#endif
            fitToWindowMenuItem.Image = Properties.Resources.ZoomToContent;
        }

        public void SetControl(ActivityDetailPageControl page, MultiChartsControl multiple)
        {
            m_page = page;
            m_multiple = multiple;
        }
        public void SetControl(ActivityDetailPageControl page, SingleChartsControl single)
        {
            m_page = page;
            m_single = single;
        }

        public void ThemeChanged(ITheme visualTheme)
        {
            m_visualTheme = visualTheme;
            MainChart.ThemeChanged(visualTheme);
            ButtonPanel.ThemeChanged(visualTheme);
            ButtonPanel.BackColor = visualTheme.Window;
        }

        public void UICultureChanged(CultureInfo culture)
        {
            copyChartMenuItem.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionCopy;
#if ST_2_1
            saveImageMenuItem.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionSave;
#else
            saveImageMenuItem.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionSaveImage;
#endif
            fitToWindowMenuItem.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionRefresh;
            SetupAxes();
        }

        public enum XAxisValue
        {
			Time,
			Distance
		}
        //No simple way to dynamically translate enum
        //The integer (raw) value is stored as defaults too
        public static string XAxisValueString(XAxisValue XAxisReferential)
        {
            string xAxisLabel="";
            switch (XAxisReferential)
            {
                case XAxisValue.Distance:
                    {
                        xAxisLabel = CommonResources.Text.LabelDistance;
                        break;
                    }
                case XAxisValue.Time:
                    {
                        xAxisLabel = CommonResources.Text.LabelTime;
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }
            return xAxisLabel;
        }
        public enum LineChartTypes {
			Cadence,
			Elevation,
			HeartRateBPM,
			HeartRatePercentMax,
			Power,
			Grade,
			Speed,
			Pace,
            SpeedPace,
            TimeDiff,
            DistDiff
		}
        public static string LineChartTypesString(LineChartTypes YAxisReferential)
        {
            string yAxisLabel="";
			switch (YAxisReferential) {
				case LineChartTypes.Cadence: {
						yAxisLabel = CommonResources.Text.LabelCadence;
						break;
					}
				case LineChartTypes.Elevation: {
						yAxisLabel = CommonResources.Text.LabelElevation;
						break;
					}
				case LineChartTypes.HeartRateBPM: {
						yAxisLabel = CommonResources.Text.LabelHeartRate;
						break;
					}
				case LineChartTypes.HeartRatePercentMax: {
						yAxisLabel = CommonResources.Text.LabelHeartRate;
						break;
					}
				case LineChartTypes.Power: {
						yAxisLabel = CommonResources.Text.LabelPower;
						break;
					}
				case LineChartTypes.Speed: {
						yAxisLabel = CommonResources.Text.LabelSpeed;
						break;
					}
				case LineChartTypes.Pace: {
						yAxisLabel = CommonResources.Text.LabelPace;
						break;
					}
                case LineChartTypes.SpeedPace:
                    {
                        yAxisLabel = CommonResources.Text.LabelSpeed + CommonResources.Text.LabelPace;
                        break;
                    }
                case LineChartTypes.Grade:
                    {
                        yAxisLabel = CommonResources.Text.LabelGrade;
                        break;
                    }
                case LineChartTypes.TimeDiff:
                    {
                        yAxisLabel = CommonResources.Text.LabelTime;
                        break;
                    }
                case LineChartTypes.DistDiff:
                    {
                        yAxisLabel = CommonResources.Text.LabelDistance;
                        break;
                    }
                default:
                    {
						Debug.Assert(false);
						break;
					}
            }
            return yAxisLabel;
        }
         
		private void SaveImageButton_Click(object sender, EventArgs e) {
#if ST_2_1
            SaveImage dlg = new SaveImage();
#else
            SaveImageDialog dlg = new SaveImageDialog();
#endif
            dlg.ThemeChanged(m_visualTheme);
            dlg.FileName = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar + "Trails";
            dlg.ImageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
			if (dlg.ShowDialog() == DialogResult.OK) {
				Size imgSize = dlg.CustomImageSize;

#if ST_2_1
                if (dlg.ImageSize != SaveImage.ImageSizeType.Custom)
#else
                if (dlg.ImageSize != SaveImageDialog.ImageSizeType.Custom)
#endif
                {
					imgSize = dlg.ImageSizes[dlg.ImageSize];
				}
				MainChart.SaveImage(imgSize, dlg.FileName, dlg.ImageFormat);
			}

			MainChart.Focus();
		}

        private void ZoomOutButton_Click(object sender, EventArgs e)
        {
            MainChart.ZoomOut();
            MainChart.Focus();
        }
        private void ZoomInButton_Click(object sender, EventArgs e)
        {
            MainChart.ZoomIn();
            MainChart.Focus();
        }

        private void ZoomToContentButton_Click(object sender, EventArgs e)
        {
			this.ZoomToData();
		}

 		public void ZoomToData() {
			MainChart.AutozoomToData(true);
			MainChart.Refresh();
		}

        void copyChartMenuItem_Click(object sender, EventArgs e)
        {
            //Not visible menu item
            //MainChart.CopyTextToClipboard(true, System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator);
        }

        void MainChart_SelectData(object sender, ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataEventArgs e)
        {
            if (e != null && e.DataSeries != null && m_page != null)
            {
                int i = -1;
                for (int j = 0; j < MainChart.DataSeries.Count; j++)
                {
                    if (e.DataSeries.Equals(MainChart.DataSeries[j]))
                    {
                        i = j;
                        break;
                    }
                }
                if(i>=0)
                {
                    IList<float[]> regions;
                    e.DataSeries.GetSelectedRegions(out regions);
                    if (m_multiple != null)
                    {
                        m_multiple.SetSelectedRange(regions);
                    }
                    if (m_single != null)
                    {
                        m_single.SetSelectedRange(regions);
                    }

                    IList<Data.TrailResultMarked> results = new List<Data.TrailResultMarked>();
                    if (XAxisReferential == XAxisValue.Time)
                    {
                        IValueRangeSeries<DateTime> t = new ValueRangeSeries<DateTime>();
                        foreach (float[] at in regions)
                        {
                            t.Add(new ValueRange<DateTime>(
                                m_trailResults[i].FirstTime.AddSeconds(at[0]),
                                m_trailResults[i].FirstTime.AddSeconds(at[1])));
                        }
                        results.Add(new Data.TrailResultMarked(m_trailResults[i], t));
                    }
                    else
                    {
                        IValueRangeSeries<double> t = new ValueRangeSeries<double>();
                        foreach (float[] at in regions)
                        {
                            t.Add(new ValueRange<double>(
                                m_trailResults[i].FirstDist + Utils.Units.SetDistance(at[0], m_trailResults[i].Activity),
                                m_trailResults[i].FirstDist + Utils.Units.SetDistance(at[1], m_trailResults[i].Activity)));
                        }
                        results.Add(new Data.TrailResultMarked(m_trailResults[i], t));
                    }
                    m_page.MarkTrack(results);

                    this.MainChart.SelectData -= new ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataHandler(MainChart_SelectData);
                    for (int j = 0; j < MainChart.DataSeries.Count; j++)
                    {
                        if (i != j)
                        {
                            MainChart.DataSeries[j].ClearSelectedRegions();
                            foreach (float[] at in regions)
                            {
                                //MainChart.DataSeries[j].AddSelecedRegion(at[0],at[1]);
                            }
                        }
                    }
                    this.MainChart.SelectData += new ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataHandler(MainChart_SelectData);
                }
            }
        }
        public void SetSelectedRange(IList<float[]> regions)
        {
            if (regions != null && regions.Count > 0)
            {
                this.MainChart.SelectData -= new ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataHandler(MainChart_SelectData);
                foreach (ChartDataSeries s in MainChart.DataSeries)
                {
                    //TODO: full select
                    s.SetSelectedRange(regions[0][0], regions[0][1]);
                }
                this.MainChart.SelectData += new ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataHandler(MainChart_SelectData);
            }
        }

        public void SetSelected(IList<IItemTrackSelectionInfo> asel)
        {
            if (MainChart != null && MainChart.DataSeries != null &&
                    MainChart.DataSeries.Count > 0 &&
                m_trailResults.Count>0)
            {
                Data.TrailsItemTrackSelectionInfo sel = new Data.TrailsItemTrackSelectionInfo();
                foreach (IItemTrackSelectionInfo trm in asel)
                {
                    sel.Union(trm);
                }
                float x1 = float.MaxValue, x2 = float.MinValue;
                sel = sel.FirstSelection();

                for (int i = 0; i < m_trailResults.Count; i++)
                {
                    //TODO: AddSelecedRegion
                    //Currently only one region can be selected
                    if (sel.MarkedTimes != null && sel.MarkedTimes.Count > 0)
                    {
                        if (XAxisReferential == XAxisValue.Time)
                        {
                            x1 = (float)(sel.MarkedTimes[0].Lower.Subtract(m_trailResults[i].FirstTime).TotalSeconds);
                            x2 = (float)(sel.MarkedTimes[sel.MarkedTimes.Count - 1].Upper.Subtract(m_trailResults[i].FirstTime).TotalSeconds);
                        }
                        else
                        {
                            double t1, t2;
                            t1 = m_trailResults[i].getDistAt(sel.MarkedTimes[0].Lower);
                            t2 = m_trailResults[i].getDistAt(sel.MarkedTimes[sel.MarkedTimes.Count - 1].Upper);
                            x1 = Utils.Units.GetDistance(t1, m_trailResults[i].Activity);
                            x2 = Utils.Units.GetDistance(t2, m_trailResults[i].Activity);
                        }
                    }
                    else if (sel.MarkedDistances != null && sel.MarkedDistances.Count > 0)
                    {
                        double t1, t2;
                        t1 = sel.MarkedDistances[0].Lower;
                        t2 = sel.MarkedDistances[sel.MarkedDistances.Count - 1].Upper;
                        if (XAxisReferential == XAxisValue.Time)
                        {
                            x1 = (float)(m_trailResults[i].getTimeAt(t1).Subtract(m_trailResults[i].FirstTime).TotalSeconds);
                            x2 = (float)(m_trailResults[i].getTimeAt(t2).Subtract(m_trailResults[i].FirstTime).TotalSeconds);
                        }
                        else
                        {
                            x1 = Utils.Units.GetDistance(t1 - m_trailResults[i].FirstDist, m_trailResults[i].Activity);
                            x2 = Utils.Units.GetDistance(t2 - m_trailResults[i].FirstDist, m_trailResults[i].Activity);
                        }
                    }

                    //Ignore ranges outside current range and malformed scales
                    if (x1 < MainChart.XAxis.MaxOriginFarValue &&
                        MainChart.XAxis.MinOriginValue > float.MinValue &&
                        x2 > MainChart.XAxis.MinOriginValue &&
                        MainChart.XAxis.MaxOriginFarValue < float.MaxValue)
                    {
                        x1 = Math.Max(x1, (float)MainChart.XAxis.MinOriginValue);
                        x2 = Math.Min(x2, (float)MainChart.XAxis.MaxOriginFarValue);
                        MainChart.DataSeries[i].SetSelectedRange(x1, x2);
                    }
                }
            }
        }

        private void SetupDataSeries()
        {
			MainChart.DataSeries.Clear();
            MainChart.Parent.Parent.Hide();

                // Add main data.  We must use 2 separate data series to overcome the display
                //  bug in fill mode.  The main data series is normally rendered but the copy
                //  is set in Line mode to be displayed over the fill

            for (int i = 0; i < m_trailResults.Count; i++)
            {
                INumericTimeDataSeries graphPoints = GetSmoothedActivityTrack(m_trailResults[i]);

                if (graphPoints.Count > 1)
                {
                    MainChart.Parent.Parent.Show();
                    Color chartFillColor = ChartFillColor;
                    Color chartLineColor = ChartLineColor;
                    Color chartSelectedColor = ChartSelectedColor;
                    if (m_trailResults.Count > 1)
                    {
                        chartFillColor = m_trailResults[i].TrailColor;
                        chartLineColor = chartFillColor;
                        chartSelectedColor = chartFillColor;
                    }

                    ChartDataSeries dataFill = null;
                    ChartDataSeries dataLine = new ChartDataSeries(MainChart, MainChart.YAxis);

                    if (m_trailResults.Count == 1)
                    {
                        dataFill = new ChartDataSeries(MainChart, MainChart.YAxis);
                        MainChart.DataSeries.Add(dataFill);

                        dataFill.ChartType = ChartDataSeries.Type.Fill;
                        dataFill.FillColor = chartFillColor;
                        dataFill.LineColor = chartLineColor;
                        dataFill.SelectedColor = chartSelectedColor;
                        dataFill.LineWidth = 2;

                        MainChart.XAxis.Markers.Clear();
                    }
                    MainChart.DataSeries.Add(dataLine);

                    dataLine.ChartType = ChartDataSeries.Type.Line;
                    dataLine.LineColor = chartLineColor;
                    dataLine.SelectedColor = chartSelectedColor;

                    Image icon =
#if ST_2_1
                        CommonResources.Images.Information16;
#else
 new Bitmap(TrailsPlugin.CommonIcons.fileCircle(11, 11));
#endif
                    if (XAxisReferential == XAxisValue.Time)
                    {
                        foreach (ITimeValueEntry<float> entry in graphPoints)
                        {
                            if (null != dataFill)
                            {
                                dataFill.Points.Add(entry.ElapsedSeconds, new PointF(entry.ElapsedSeconds, entry.Value));
                            }
                            dataLine.Points.Add(entry.ElapsedSeconds, new PointF(entry.ElapsedSeconds, entry.Value));
                        }
                        if (m_refTrailResult != null && m_refTrailResult.Equals(m_trailResults[i]))
                        {
                            foreach (DateTime t in m_trailResults[i].TimeTrailPoints)
                            {
                                AxisMarker a = new AxisMarker(t.Subtract(m_trailResults[i].FirstTime).TotalSeconds, icon);
                                a.Line1Style = System.Drawing.Drawing2D.DashStyle.Solid;
                                a.Line1Color = Color.Black;
                                MainChart.XAxis.Markers.Add(a);
                            }
                        }
                    }
                    else
                    {
                        IDistanceDataTrack distanceTrack = m_trailResults[i].DistanceMetersTrack;

                        //Debug.Assert(distanceTrack.Count == graphPoints.Count);

                        for (int j = 0; j < distanceTrack.Count; ++j)
                        {
                            float distanceValue = Utils.Units.GetLength(distanceTrack[j].Value, m_trailResults[i].Category.DistanceUnits);
                            if (j < graphPoints.Count)
                            {
                                ITimeValueEntry<float> entry = graphPoints[j];

                                ///Debug.Assert(distanceTrack[j].ElapsedSeconds == entry.ElapsedSeconds);

                                if (null != dataFill)
                                {
                                    dataFill.Points.Add(entry.ElapsedSeconds, new PointF(distanceValue, entry.Value));

                                }
                                dataLine.Points.Add(entry.ElapsedSeconds, new PointF(distanceValue, entry.Value));
                            }
                        }
                        if (m_refTrailResult != null && m_refTrailResult.Equals(m_trailResults[i]))
                        {
                            foreach (double t in m_trailResults[i].DistanceTrailPoints)
                            {
                                AxisMarker a = new AxisMarker(Utils.Units.GetDistance(t, m_trailResults[i].Activity), icon);
                                a.Line1Style = System.Drawing.Drawing2D.DashStyle.Solid;
                                a.Line1Color = Color.Black;
                                MainChart.XAxis.Markers.Add(a);
                            }
                        }
                    }
                }
            }
			ZoomToData();
		}

        private void SetupAxes()
        {
            IActivity activity = null;
            if (m_refTrailResult != null)
            {
                activity = m_refTrailResult.Activity;
            }
            // X axis
            switch (XAxisReferential)
            {
                case XAxisValue.Distance:
                    {
                        MainChart.XAxis.Formatter = new Formatter.General();
                        MainChart.XAxis.Label = CommonResources.Text.LabelDistance + " (" +
                                                Utils.Units.GetDistanceLabel(activity) + ")";
                        break;
                    }
                case XAxisValue.Time:
                    {

                        MainChart.XAxis.Formatter = new Formatter.SecondsToTime();
                        MainChart.XAxis.Label = CommonResources.Text.LabelTime;
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }

            // Y axis
            MainChart.YAxis.Formatter = new Formatter.General();
            switch (YAxisReferential)
            {
                case LineChartTypes.Cadence:
                    {
                        MainChart.YAxis.Label = CommonResources.Text.LabelCadence + " (" +
                                                CommonResources.Text.LabelRPM + ")";
                        break;
                    }
                case LineChartTypes.Grade:
                    {
                        MainChart.YAxis.Formatter = new Percent100();
                        MainChart.YAxis.Label = CommonResources.Text.LabelGrade + " (%)";
                        break;
                    }
                case LineChartTypes.Elevation:
                    {
                        MainChart.YAxis.Label = CommonResources.Text.LabelElevation + " (" +
                                                   Utils.Units.GetElevationLabel(activity) + ")";
                        break;
                    }
                case LineChartTypes.HeartRateBPM:
                    {
                        MainChart.YAxis.Label = CommonResources.Text.LabelHeartRate + " (" +
                                                CommonResources.Text.LabelBPM + ")";
                        break;
                    }
                case LineChartTypes.HeartRatePercentMax:
                    {
                        MainChart.YAxis.Label = CommonResources.Text.LabelHeartRate + " (" +
                                                CommonResources.Text.LabelPercentOfMax + ")";
                        break;
                    }
                case LineChartTypes.Power:
                    {
                        MainChart.YAxis.Label = CommonResources.Text.LabelPower + " (" +
                                                CommonResources.Text.LabelWatts + ")";
                        break;
                    }
                case LineChartTypes.Speed:
                    {
                        MainChart.YAxis.Label = CommonResources.Text.LabelSpeed + " (" +
                                                Utils.Units.GetSpeedLabel(activity) + ")";
                        break;
                    }
                case LineChartTypes.Pace:
                    {
                        MainChart.YAxis.Formatter = new Formatter.SecondsToTime();
                        MainChart.YAxis.Label = CommonResources.Text.LabelPace + " (" +
                                                Utils.Units.GetPaceLabel(activity) + ")";
                        break;
                    }
                case LineChartTypes.TimeDiff:
                    {
                        MainChart.YAxis.Formatter = new Formatter.SecondsToTime();
                        MainChart.YAxis.Label = CommonResources.Text.LabelTime;
                        break;
                    }
                case LineChartTypes.DistDiff:
                    {

                        MainChart.YAxis.Formatter = new Formatter.General();
                        MainChart.YAxis.Label = CommonResources.Text.LabelDistance + " (" +
                                                Utils.Units.GetDistanceLabel(activity) + ")";
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }
        }

		private INumericTimeDataSeries GetSmoothedActivityTrack(Data.TrailResult result) {
			// Fail safe
			INumericTimeDataSeries track = new NumericTimeDataSeries();

			switch (YAxisReferential) {
				case LineChartTypes.Cadence: {
						track = result.CadencePerMinuteTrack;
						break;
					}
				case LineChartTypes.Elevation: {
						INumericTimeDataSeries tempResult = result.ElevationMetersTrack;

						// Value is in meters so convert to the right unit
						track = new NumericTimeDataSeries();
						foreach (ITimeValueEntry<float> entry in tempResult) {
                            float temp = Utils.Units.GetElevation(entry.Value, result.Activity); 

							track.Add(tempResult.EntryDateTime(entry), (float)temp);
						}
						break;
					}
				case LineChartTypes.HeartRateBPM: {
						track = result.HeartRatePerMinuteTrack;
						break;
					}
				/*
								case LineChartTypes.HeartRatePercentMax: {
										track = new NumericTimeDataSeries();

										IAthleteInfoEntry lastAthleteEntry = PluginMain.GetApplication().Logbook.Athlete.InfoEntries.LastEntryAsOfDate(Activity.StartTime);

										// Value is in BPM so convert to the % max HR if we have the info
										if (!float.IsNaN(lastAthleteEntry.MaximumHeartRatePerMinute)) {
											INumericTimeDataSeries tempResult = activityInfo.SmoothedHeartRateTrack;

											foreach (ITimeValueEntry<float> entry in tempResult) {
												double temp = (entry.Value / lastAthleteEntry.MaximumHeartRatePerMinute) * 100;

												track.Add(tempResult.EntryDateTime(entry), (float)temp);
											}
										}
										break;
									}
				*/
				case LineChartTypes.Power: {
						track = result.PowerWattsTrack;
						break;
					}
				case LineChartTypes.Grade: {
						track = result.GradeTrack;
						break;
					}

				case LineChartTypes.Speed: {
						INumericTimeDataSeries tempResult = result.SpeedTrack;

						track = new NumericTimeDataSeries();
						foreach (ITimeValueEntry<float> entry in tempResult) {
							track.Add(tempResult.EntryDateTime(entry), entry.Value);
						}
						break;
					}

                case LineChartTypes.Pace:
                    {
                        INumericTimeDataSeries tempResult = result.PaceTrack;

                        track = new NumericTimeDataSeries();
                        foreach (ITimeValueEntry<float> entry in tempResult)
                        {
                            track.Add(tempResult.EntryDateTime(entry), entry.Value);
                        }
                        break;
                    }

                case LineChartTypes.TimeDiff:
                    {
                        INumericTimeDataSeries tempResult = result.PaceTrack;

                        track = new NumericTimeDataSeries();
                        foreach (ITimeValueEntry<float> entry in tempResult)
                        {
                            track.Add(tempResult.EntryDateTime(entry), entry.Value);
                        }
                        break;
                    }
                case LineChartTypes.DistDiff:
                    {
                        INumericTimeDataSeries tempResult = result.SpeedTrack;

                        track = new NumericTimeDataSeries();
                        foreach (ITimeValueEntry<float> entry in tempResult)
                        {
                            track.Add(tempResult.EntryDateTime(entry), entry.Value);
                        }
                        break;
                    }

                default:
                    {
						Debug.Assert(false);
						break;
					}

			}

			return track;
		}

		[DisplayName("X Axis value")]
		public XAxisValue XAxisReferential {
			get { return m_XAxisReferential; }
			set {
				m_XAxisReferential = value;
			}
		}

        [DisplayName("Y Axis value")]
        public LineChartTypes YAxisReferential
        {
            get { return m_YAxisReferential; }
            set
            {
                m_YAxisReferential = value;
            }
        }

        [DisplayName("Y Axis value, right")]
        public IList<LineChartTypes> YAxisReferential_right
        {
            get { return m_YAxisReferential_right; }
            set
            {
                m_YAxisReferential_right = value;
            }
        }

        public Color ChartFillColor
        {
			get { return m_ChartFillColor; }
			set {
				if (m_ChartFillColor != value) {
					m_ChartFillColor = value;

					foreach (ChartDataSeries dataSerie in MainChart.DataSeries) {
						dataSerie.FillColor = ChartFillColor;
					}
				}
			}
		}

		public Color ChartLineColor {
			get { return m_ChartLineColor; }
			set {
				if (ChartLineColor != value) {
					m_ChartLineColor = value;

					foreach (ChartDataSeries dataSerie in MainChart.DataSeries) {
						dataSerie.LineColor = ChartLineColor;
					}
				}
			}
		}

		public Color ChartSelectedColor {
			get { return m_ChartSelectedColor; }
			set {
				if (ChartSelectedColor != value) {
					m_ChartSelectedColor = value;

					foreach (ChartDataSeries dataSerie in MainChart.DataSeries) {
						dataSerie.SelectedColor = ChartSelectedColor;
					}
				}
			}
		}

		[Browsable(false)]
        public Data.TrailResult ReferenceTrailResult
        {
            get
            {
                return m_refTrailResult;
            }
            set
            {
                if (m_refTrailResult != value)
                {
                    m_refTrailResult = value;
                    SetupAxes();
                    SetupDataSeries();
                }
            }
        }
        public IList<Data.TrailResult> TrailResults
        {
            get
            {
                return m_trailResults;
            }
            set
            {
                if (m_trailResults != value)
                {
                    if (value == null)
                    {
                        m_trailResults = new List<Data.TrailResult>();
                    }
                    else
                    {
                        m_trailResults = value;
                    }
                    SetupAxes();
                    SetupDataSeries();
                }
            }
        }

        public bool ShowChartToolBar
        {
            set
            {
                   this.chartTablePanel.RowStyles[0].Height = value ? 25 : 0;
            }
        }

		public bool BeginUpdate() {
			return MainChart.BeginUpdate();
		}

		public void EndUpdate() {
			MainChart.EndUpdate();
		}
	}
}
