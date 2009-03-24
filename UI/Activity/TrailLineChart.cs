/******************************************************************************

    This file is part of TrailsPlugin.

    TrailsPlugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    TrailsPlugin is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with TrailsPlugin.  If not, see <http://www.gnu.org/licenses/>.
******************************************************************************/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.Generic;
using ZoneFiveSoftware.Common.Data;
using ZoneFiveSoftware.Common.Data.Fitness;
using ZoneFiveSoftware.Common.Visuals;
using ZoneFiveSoftware.Common.Visuals.Chart;
using ZoneFiveSoftware.Common.Data.Measurement;
using ZoneFiveSoftware.SportTracks.UI.Forms;

namespace TrailsPlugin.UI.Activity {
	public partial class TrailLineChart : UserControl {
		public enum XAxisValue {
			Time,
			Distance
		}

		public enum LineChartTypes {
			Cadence,
			Elevation,
			HeartRateBPM,
			HeartRatePercentMax,
			Power,
			Grade,
			Speed,
			Pace
		}

		private Data.TrailResult m_trailResult = null;
		private XAxisValue m_XAxisReferential = XAxisValue.Time;
		private LineChartTypes m_YAxisReferential = LineChartTypes.Speed;
		private Color m_ChartFillColor = Color.WhiteSmoke;
		private Color m_ChartLineColor = Color.LightSkyBlue;
		private Color m_ChartSelectedColor = Color.AliceBlue;
		private IActivity m_activity = null;

		public TrailLineChart() {
			InitializeComponent();

			MainChart.YAxis.SmartZoom = true;

			SaveImageButton.CenterImage = CommonResources.Images.Save16;
			ZoomInButton.CenterImage = CommonResources.Images.ZoomIn16;
			ZoomOutButton.CenterImage = CommonResources.Images.ZoomOut16;
		}

		private void SaveImageButton_Click(object sender, EventArgs e) {
			SaveImage dlg = new SaveImage();

			if (dlg.ShowDialog() == DialogResult.OK) {
				Size imgSize = dlg.CustomImageSize;

				if (dlg.ImageSize != SaveImage.ImageSizeType.Custom) {
					imgSize = dlg.ImageSizes[dlg.ImageSize];
				}

				MainChart.SaveImage(imgSize, dlg.FileName, dlg.ImageFormat);
			}

			MainChart.Focus();
		}

		private void ZoomOutButton_Click(object sender, EventArgs e) {
			MainChart.ZoomOut();
			MainChart.Focus();
		}

		private void ZoomInButton_Click(object sender, EventArgs e) {
			MainChart.ZoomIn();
			MainChart.Focus();
		}

		public void ThemeChanged(ITheme visualTheme) {
			MainChart.ThemeChanged(visualTheme);
			ButtonPanel.ThemeChanged(visualTheme);
			ButtonPanel.BackColor = visualTheme.Window;
		}

		public void UICultureChanged(CultureInfo culture) {
			SetupAxes();
		}

		public void ZoomToData() {
			MainChart.AutozoomToData(true);
			MainChart.Refresh();
		}

		public string GetViewLabel() {
			string xAxisLabel = string.Empty;
			string yAxisLabel = string.Empty;

			// X axis
			switch (XAxisReferential) {
				case XAxisValue.Distance: {
						xAxisLabel = CommonResources.Text.LabelDistance;
						break;
					}
				case XAxisValue.Time: {
						xAxisLabel = CommonResources.Text.LabelTime;
						break;
					}
				default: {
						Debug.Assert(false);
						break;
					}
			}

			// Y axis
			MainChart.YAxis.Formatter = new Formatter.General();
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
				case LineChartTypes.Grade: {
						yAxisLabel = CommonResources.Text.LabelGrade;
						break;
					}
				default: {
						Debug.Assert(false);
						break;
					}
			}

			return yAxisLabel + " / " + xAxisLabel;
		}

		private void SetupDataSeries() {
			MainChart.DataSeries.Clear();


			// Add main data.  We must use 2 seperate data series to overcome the display
			//  bug in fill mode.  The main data series is normally rendered but the copy
			//  is set in Line mode to be displayed over the fill

			if (m_trailResult != null) {
				ChartDataSeries mainData = new ChartDataSeries(MainChart, MainChart.YAxis);
				ChartDataSeries mainDataCopy = new ChartDataSeries(MainChart, MainChart.YAxis);

				MainChart.DataSeries.Add(mainData);
				MainChart.DataSeries.Add(mainDataCopy);

				mainData.ChartType = ChartDataSeries.Type.Fill;
				mainData.FillColor = ChartFillColor;
				mainData.LineColor = ChartLineColor;
				mainData.SelectedColor = ChartSelectedColor;
				mainData.LineWidth = 2;

				mainDataCopy.ChartType = ChartDataSeries.Type.Line;
				mainDataCopy.LineColor = ChartLineColor;
				mainDataCopy.SelectedColor = ChartSelectedColor;

				INumericTimeDataSeries graphPoints = GetSmoothedActivityTrack(m_trailResult);

				if (graphPoints.Count > 0) {
					if (XAxisReferential == XAxisValue.Time) {
						foreach (ITimeValueEntry<float> entry in graphPoints) {
							mainData.Points.Add(entry.ElapsedSeconds, new PointF(entry.ElapsedSeconds, entry.Value));
							mainDataCopy.Points.Add(entry.ElapsedSeconds, new PointF(entry.ElapsedSeconds, entry.Value));
						}
					} else {
						IDistanceDataTrack distanceTrack = m_trailResult.DistanceMetersTrack;

						//Debug.Assert(distanceTrack.Count == graphPoints.Count);

						for (int i = 0; i < distanceTrack.Count; ++i) {
							float distanceValue = (float)Length.Convert(distanceTrack[i].Value, Length.Units.Meter, m_trailResult.Category.DistanceUnits);
							if (i < graphPoints.Count) {
								ITimeValueEntry<float> entry = graphPoints[i];

								///Debug.Assert(distanceTrack[i].ElapsedSeconds == entry.ElapsedSeconds);

								mainData.Points.Add(entry.ElapsedSeconds, new PointF(distanceValue, entry.Value));
								mainDataCopy.Points.Add(entry.ElapsedSeconds, new PointF(distanceValue, entry.Value));
							}
						}
					}
				}
			}

			ZoomToData();
		}

		private void SetupAxes() {
			// X axis
			switch (XAxisReferential) {
				case XAxisValue.Distance: {
						if (PluginMain.GetApplication() != null) {
							Length.Units distanceUnit = PluginMain.GetApplication().SystemPreferences.DistanceUnits;

							if (m_activity!=null && m_activity.Category != null) {
								distanceUnit = m_activity.Category.DistanceUnits;
							}

							MainChart.XAxis.Formatter = new Formatter.General();
							MainChart.XAxis.Label = CommonResources.Text.LabelDistance + " (" +
													Length.Label(distanceUnit) + ")";
						}
						break;
					}
				case XAxisValue.Time: {

						MainChart.XAxis.Formatter = new Formatter.SecondsToTime();
						MainChart.XAxis.Label = CommonResources.Text.LabelTime;
						break;
					}
				default: {
						Debug.Assert(false);
						break;
					}
			}

			// Y axis
			MainChart.YAxis.Formatter = new Formatter.General();
			switch (YAxisReferential) {
				case LineChartTypes.Cadence: {
						MainChart.YAxis.Label = CommonResources.Text.LabelCadence + " (" +
												CommonResources.Text.LabelRPM + ")";
						break;
					}
				case LineChartTypes.Grade: {
						MainChart.YAxis.Formatter = new Percent100();
						MainChart.YAxis.Label = CommonResources.Text.LabelGrade + " (%)";
						break;
					}
				case LineChartTypes.Elevation: {
						if (PluginMain.GetApplication() != null) {
							Length.Units elevationUnit = PluginMain.GetApplication().SystemPreferences.ElevationUnits;

							if (m_activity != null) {
								elevationUnit = m_activity.Category.ElevationUnits;
							}

							MainChart.YAxis.Label = CommonResources.Text.LabelElevation + " (" +
													Length.Label(elevationUnit) + ")";
						}
						break;
					}
				case LineChartTypes.HeartRateBPM: {
						MainChart.YAxis.Label = CommonResources.Text.LabelHeartRate + " (" +
												CommonResources.Text.LabelBPM + ")";
						break;
					}
				case LineChartTypes.HeartRatePercentMax: {
						MainChart.YAxis.Label = CommonResources.Text.LabelHeartRate + " (" +
												CommonResources.Text.LabelPercentOfMax + ")";
						break;
					}
				case LineChartTypes.Power: {
						MainChart.YAxis.Label = CommonResources.Text.LabelPower + " (" +
												CommonResources.Text.LabelWatts + ")";
						break;
					}
				case LineChartTypes.Speed: {
						MainChart.YAxis.Label = CommonResources.Text.LabelSpeed + " (" +
												Utils.Units.GetSpeedUnitLabelForActivity(m_activity) + ")";
						break;
					}
				case LineChartTypes.Pace: {
						MainChart.YAxis.Formatter = new Formatter.SecondsToTime();
						MainChart.YAxis.Label = CommonResources.Text.LabelPace + " (" +
												Utils.Units.GetPaceUnitLabelForActivity(m_activity) + ")";
						break;
					}
				default: {
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
							double temp = Length.Convert(entry.Value, Length.Units.Meter, m_activity.Category.ElevationUnits);

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

						// Value is in m/sec so convert to the right unit 
						track = new NumericTimeDataSeries();
						foreach (ITimeValueEntry<float> entry in tempResult) {
							track.Add(tempResult.EntryDateTime(entry), entry.Value);
						}
						break;
					}

				case LineChartTypes.Pace: {
						INumericTimeDataSeries tempResult = result.PaceTrack;

						// Value is in m/sec so convert to the right unit and to pace

						track = new NumericTimeDataSeries();
						foreach (ITimeValueEntry<float> entry in tempResult) {
							track.Add(tempResult.EntryDateTime(entry), entry.Value);
						}
						break;
					}
				default: {
						Debug.Assert(false);
						break;
					}

			}

			return track;
		}

		[Browsable(false)]
		public Data.TrailResult TrailResult {
			get {
				return m_trailResult;
			}
			set {
				if (m_trailResult != value) {
					m_trailResult = value;
					//if (m_trailResult != null) {
						SetupAxes();
						SetupDataSeries();
						ZoomToData();
					//}
				}
			}
		}

		[DisplayName("X Axis value")]
		public XAxisValue XAxisReferential {
			get { return m_XAxisReferential; }
			set {
				m_XAxisReferential = value;
				SetupAxes();
				SetupDataSeries();
			}
		}

		[DisplayName("Y Axis value")]
		public LineChartTypes YAxisReferential {
			get { return m_YAxisReferential; }
			set {
				m_YAxisReferential = value;
				SetupAxes();
				SetupDataSeries();
			}
		}

		public Color ChartFillColor {
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

		public IActivity Activity {
			set {
				m_activity = value;
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
