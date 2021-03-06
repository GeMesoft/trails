/*
Copyright (C) 2009 Brendan Doherty
Copyright (C) 2010-2014 Gerhard Olsson

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
using TrailsPlugin.UI.MapLayers;
#endif
using TrailsPlugin.Data;
using TrailsPlugin.Utils;
using GpsRunningPlugin.Util;

namespace TrailsPlugin.UI.Activity {
    public partial class TrailLineChart : UserControl {
        private Data.TrailResult m_refTrailResult = null;
        private IList<Data.TrailResult> m_trailResults = new List<Data.TrailResult>();

        private XAxisValue m_XAxisReferential = XAxisValue.Time;
        private IList<LineChartTypes> m_ChartTypes = new List<LineChartTypes>();
        private IDictionary<LineChartTypes, IAxis> m_axisCharts = new Dictionary<LineChartTypes, IAxis>();
        private LineChartTypes m_lastSelectedType = LineChartTypes.Unknown;
        private IDictionary<LineChartTypes, bool> m_hasValues = null;

        private bool m_multipleCharts = false;
        private bool m_visible = false;
        private ITheme m_visualTheme;
        private ActivityDetailPageControl m_page;
        private MultiChartsControl m_multiple;
        //private bool m_selectDataHandler = true; //Event handler is enabled by default
        private bool refIsSelf = false;

        private bool m_CtrlPressed = false;
        private Point m_MouseDownLocation;
        private System.Drawing.Point m_cursorLocationAtMouseMove;

        //selecting in the chart
        private DateTime m_lastSelectingTime = DateTime.MinValue;
        private DateTime m_lastMarkingRouteTime = DateTime.MinValue;
        private bool m_endSelect = false;
        private float m_firstRangeSelected = float.NaN;
        private float[] m_prevSelectedRange = null;
        private IList<float[]> m_prevSelectedRegions = new List<float[]>(); //Not null
        private XAxisValue m_prevSelectedXAxis = XAxisValue.Time;
        private TrailResult m_prevSelectedResult;
        private int m_selectedDataSeries = -1;
 
        const int MaxSelectedSeries = 6;
        public static SyncGraphMode SyncGraph = SyncGraphMode.None;
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
#if !ST_2_1
            saveImageMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Save16;
#endif
        }

        public void SetControl(ActivityDetailPageControl page, MultiChartsControl multiple)
        {
            this.m_page = page;
            this.m_multiple = multiple;
        }

        public void ThemeChanged(ITheme visualTheme)
        {
            this.m_visualTheme = visualTheme;
            this.MainChart.ThemeChanged(visualTheme);
            this.ButtonPanel.ThemeChanged(visualTheme);
            this.ButtonPanel.BackColor = visualTheme.Window;
            this.chartContextMenu.Renderer = new ThemedContextMenuStripRenderer(visualTheme);
        }

        public void UICultureChanged(CultureInfo culture)
        {
            summaryListToolTip.SetToolTip(this.ZoomInButton, ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionZoomIn);
            summaryListToolTip.SetToolTip(this.ZoomOutButton, ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionZoomOut);
            summaryListToolTip.SetToolTip(this.ZoomToContentButton, Properties.Resources.UI_Chart_FitToWindow);
            summaryListToolTip.SetToolTip(this.SaveImageButton, ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionSaveImage);
            if (this.MultipleCharts)
            {
                summaryListToolTip.SetToolTip(this.MoreChartsButton, Properties.Resources.UI_Chart_SelectMoreCharts);
            }
            else
            {
                summaryListToolTip.SetToolTip(this.MoreChartsButton, Properties.Resources.UI_Chart_SelectMoreGraphs);
            }
            summaryListToolTip.SetToolTip(this.TrailPointsButton, Properties.Resources.TrailPointsControlLayer);
            //set smoothingLabel,smoothingPicker in SetupData:  setSmoothingPicker(GetSmooth());
            //summaryListToolTip.SetToolTip(this.smoothingPicker, Properties.Resources);

            copyChartMenuItem.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionCopy;
#if ST_2_1
            saveImageMenuItem.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionSave;
#else
            saveImageMenuItem.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionSaveImage;
#endif
            fitToWindowMenuItem.Text = Properties.Resources.UI_Chart_FitToWindow;
            moreChartsMenuItem.Text = Properties.Resources.UI_Chart_SelectMoreCharts;
            //set smoothingLabel,smoothingPicker in SetupData:  setSmoothingPicker(GetSmooth());
            SetupAxes();
        }

        private void PrecedeControl(Control a, Control b)
        {
            a.Location = new Point(b.Location.X - a.Size.Width - 3, a.Location.Y);
        }

        public bool ShowPage
        {
            get
            {
                return m_visible;
            }
            set
            {
                m_visible = value;
                if (value)
                {
                    SetupAxes();
                    SetupDataSeries();
                }
            }
        }

        /********************************************************************************/

        private void CopyCharts_Click(object sender, EventArgs e)
        {
            string fileName = Path.GetTempFileName();
            MainChart.SaveImage(MainChart.ChartDataRect.Size, fileName, System.Drawing.Imaging.ImageFormat.MemoryBmp);
            Clipboard.SetImage(new Bitmap(fileName));
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
            catch (Exception) { }
        }

        private void SaveImageButton_Click(object sender, EventArgs e)
        {
#if ST_2_1
            SaveImage dlg = new SaveImage();
#else
            SaveImageDialog dlg = new SaveImageDialog();
#endif
            dlg.ThemeChanged(m_visualTheme);
            dlg.FileName = Data.Settings.SaveChartImagePath + Path.DirectorySeparatorChar + "Trails";
            if (this.m_refTrailResult != null && !String.IsNullOrEmpty(this.m_refTrailResult.Trail.Name))
            {
                dlg.FileName += "-" + this.m_refTrailResult.Trail.Name;
            }
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
                Data.Settings.SaveChartImagePath = (new FileInfo(dlg.FileName)).DirectoryName;
            }

            MainChart.Focus();
        }

        private void ZoomOutButton_Click(object sender, EventArgs e)
        {
            MainChart.ZoomOut();
            //MainChart.Focus();
        }

        private void ZoomInButton_Click(object sender, EventArgs e)
        {
            MainChart.ZoomIn();
            //MainChart.Focus();
        }

        private void ZoomToContentButton_Click(object sender, EventArgs e)
        {
            MainChart.AutozoomToData(true);
            MainChart.Refresh();
            //MainChart.Focus();
        }

         public void ZoomToData()
        {
            MainChart.AutozoomToData(true);
            //MainChart.Refresh();
        }

        private void MoreCharts_Click(object sender, EventArgs e)
        {
            ListSettingsDialog dialog = new ListSettingsDialog();
            IList<IListColumnDefinition> cols;
            if (this.MultipleCharts)
            {
                cols = LineChartUtil.MultiCharts();
                dialog.SelectedColumns = LineChartUtil.LineChartType_strings(Data.Settings.MultiChartType);
                dialog.Text = Properties.Resources.UI_Chart_SelectChartsTitle;
                dialog.SelectedItemListLabel = Properties.Resources.UI_Chart_SelectedCharts;
                dialog.AddButtonLabel = Properties.Resources.UI_Chart_AddChart;
            }
            else
            {
                cols = LineChartUtil.MultiGraphs();
                //The selected cols are not really arrangable. The list could be sorted when presenting, but it is harder preventing reordering.
                dialog.SelectedColumns = LineChartUtil.LineChartType_strings(
                    LineChartUtil.SortMultiGraphType(Data.Settings.MultiGraphType));
                dialog.Text = Properties.Resources.UI_Chart_SelectGraphsTitle;
                dialog.SelectedItemListLabel = Properties.Resources.UI_Chart_SelectedGraphs;
                dialog.AddButtonLabel = Properties.Resources.UI_Chart_AddGraph;
            }
            dialog.AvailableColumns = cols;
            dialog.ThemeChanged(m_visualTheme);
            dialog.AllowFixedColumnSelect = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (this.MultipleCharts)
                {
                    Data.Settings.SetMultiChartType = ((List<string>)dialog.SelectedColumns).ToArray();
                }
                else
                {
                    Data.Settings.SetMultiGraphType = ((List<string>)dialog.SelectedColumns).ToArray();
                }
                this.m_multiple.RefreshChart();
            }
        }

        void TrailPoints_Click(object sender, EventArgs e)
        {
            Data.Settings.ShowTrailPointsOnChart = !Data.Settings.ShowTrailPointsOnChart;
            this.m_multiple.RefreshChart();
        }

        private void SmoothingPicker_LostFocus(object sender, EventArgs e)
        {
            //There should be a brief delay here
            if (null != SetSmooth((int)smoothingPicker.Value, false, false))
            {
                Controller.TrailController.Instance.Clear(true);
                m_page.RefreshChart();
            }
        }

        private void SetSmoothingPicker(int val)
        {
            if (LineChartUtil.IsDiffType(m_lastSelectedType))
            {
                this.smoothingPicker.Maximum = decimal.MaxValue;
                this.smoothingPicker.Minimum = decimal.MinValue;
                summaryListToolTip.SetToolTip(this.smoothingPicker, Properties.Resources.UI_Chart_PickerOffset);
            }
            else
            {
                //Some resonable, to keep size
                this.smoothingPicker.Maximum = 9999;
                this.smoothingPicker.Minimum = 0;
                summaryListToolTip.SetToolTip(this.smoothingPicker, Properties.Resources.UI_Chart_PickerSmoothing);
            }
            if (val > this.smoothingPicker.Maximum)
            {
                val = (int)this.smoothingPicker.Maximum;
            }
            this.smoothingPicker.Value = val;

            smoothingLabel.Text = LineChartUtil.ChartTypeString(m_lastSelectedType);
            PrecedeControl(smoothingLabel, smoothingPicker);
        }

        //Fires about every 33ms when selecting
        void MainChart_SelectingData(object sender, ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataEventArgs e)
        {
            //Let update rate depend on number of chart activities, less choppy update
            if (DateTime.Now.Subtract(this.m_lastSelectingTime).TotalMilliseconds >= Math.Min(500, 33*this.m_trailResults.Count))
            {
                this.BeginUpdate();
                this.m_lastSelectingTime = DateTime.Now;
                MainChart_SelectingData(this.m_selectedDataSeries, null, true, false);
                this.EndUpdate(false);
            }
        }

        //Fires before and after selection, also when just clicking
        void MainChart_SelectData(object sender, ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataEventArgs e)
        {
            float[] range = null;
            bool rangeIsValid = false;

            this.m_selectedDataSeries = -1;

            this.BeginUpdate();
            if (e != null && e.DataSeries != null)
            {
                range = new float[2];
                e.DataSeries.GetSelectedRange(out range[0], out range[1]);
                if (float.IsNaN(range[1]))
                {
                    //Sync, should not be needed
                    this.m_endSelect = false;
                    //Save first, to find if selecting to left/right
                    this.m_firstRangeSelected = range[0];
                }

                if (!this.m_endSelect)
                {
                    //Save start range, to shrink if changed
                    this.m_prevSelectedRange = range;
                }
                this.m_prevSelectedXAxis = this.m_XAxisReferential;

                //Get index for dataseries - relates to result
                for (int j = 0; j < MainChart.DataSeries.Count; j++)
                {
                    if (e.DataSeries.Equals(MainChart.DataSeries[j]))
                    {
                        rangeIsValid = !float.IsNaN(range[0]);
                        this.m_selectedDataSeries = j;
                        this.m_prevSelectedResult = TrailResults[this.SeriesIndexToResult(this.m_selectedDataSeries)];
                        break;
                    }
                }
            }
            else
            {
                this.m_endSelect = false;
                //Click out of charts - forget previous selection
                this.m_prevSelectedRange = null;
                m_prevSelectedRegions.Clear();
                if (this.m_MouseDownLocation == Point.Empty)
                {
                    //Just a point other than empty...
                    this.m_MouseDownLocation = new Point(1,1);
                }
            }

            //Clear if starting a new selection and ctrl is not pressed
            if (!this.m_endSelect)
            {
                if (this.m_MouseDownLocation != Point.Empty)
                {
                    this.m_multiple.ClearSelectedRegions(!rangeIsValid);
                    this.m_page.ClearCurrentSelectedOnRoute();
                }
                //Reset color on axis when start to select
                foreach (LineChartTypes chartType in m_ChartTypes)
                {
                    if (this.m_multipleCharts &&
                        this.m_selectedDataSeries >= 0 && e.DataSeries.ValueAxis == m_axisCharts[chartType])
                    {
                        e.DataSeries.ValueAxis.LabelColor = Color.Black;
                        this.m_lastSelectedType = chartType;
                    }
                    else if (m_axisCharts.Count > 0)
                    {
                        LineChartTypes axisType = LineChartUtil.ChartToAxis(chartType);
                        System.Diagnostics.Debug.Assert(m_axisCharts.ContainsKey(axisType), "no axis for " + axisType);
                        if (m_axisCharts.ContainsKey (axisType) && (axisType == chartType || !m_ChartTypes.Contains(axisType)))
                        {
                            m_axisCharts[axisType].LabelColor = ColorUtil.ChartColor[axisType].LineNormal;
                        }
                    }
                }
            }
            this.m_MouseDownLocation = Point.Empty;

            //Select in charts etc only with current series. Use range instead of e 
            if (rangeIsValid)
            {
                MainChart_SelectingData(this.m_selectedDataSeries, range, false, this.m_endSelect);
            }
            this.m_endSelect = !this.m_endSelect;
            this.EndUpdate(false);
        }

        void MainChart_SelectingData(int seriesIndex, float[] range, bool selecting, bool endSelect)
        {
            if (seriesIndex >= 0)
            {
                Debug.Assert(seriesIndex < this.MainChart.DataSeries.Count, "Incorrect index?");

                //Series must be added in order, so they can be resolved to result here
                TrailResult tr = m_trailResults[this.SeriesIndexToResult(seriesIndex)];

                this.MainChart.DataSeries[seriesIndex].GetSelectedRegions(out IList<float[]> regions);

                if (selecting && range == null)
                {
                    range = new float[2];
                    this.MainChart.DataSeries[seriesIndex].GetSelectedRange(out range[0], out range[1]);
                }

                //Find if a selection has decreased
                if (range != null && regions != null && this.m_prevSelectedRange != null &&
                    !float.IsNaN(range[0]) && !float.IsNaN(this.m_prevSelectedRange[0]))
                {
                    bool clearDecreased = false;
                    if (float.IsNaN(this.m_prevSelectedRange[1]))
                    {
                        //First selection, second not yet set, clicking in selected region: clear it
                        //m_prevSelectedRange and range should be the same
                        foreach (float[] r in regions)
                        {
                            if (r[0] <= range[0] && range[0] <  r[1] ||
                                r[0] <  range[0] && range[0] <= r[1])
                            {
                                r[0] = range[0];
                                if (!float.IsNaN(range[1]))
                                {
                                    r[1] = range[1];
                                }
                                else
                                {
                                    r[1] = range[0];
                                }
                                clearDecreased = true;
                            }
                        }
                    }
                    //TBD some check what of m_prevSelectedRange/range should be used
                    //Range was selected when clicking in the chart
                    else if (!float.IsNaN(range[1]) && 
                        (this.m_prevSelectedRange[0] < range[0] || 
                          range[1] < this.m_prevSelectedRange[1]))
                    //range decreasing from prev range, decrease region
                    {
                        foreach (float[] r in regions)
                        {
                            if (r[0] <= range[0] && range[1] <  r[1] ||
                                r[0] <  range[0] && range[1] <= r[1])
                            {
                                //Selection decreasing
                                r[0] = range[0];
                                r[1] = range[1];
                                clearDecreased = true;
                            }
                        }
                    }
                    if (clearDecreased)
                    {
                        this.m_multiple.ClearSelectedRegions(false);
                    }
                    else
                    {
                        //save the regions, when switching
                        m_prevSelectedRegions = new List<float[]>();
                        foreach (float[] r in regions)
                        {
                            m_prevSelectedRegions.Add(r);
                        }
                        //this.m_prevSelectedXAxis = this.m_XAxisReferential;
                    }
                }
                this.m_prevSelectedRange = range;
                this.m_prevSelectedResult = tr;

                bool markAll = (MainChart.DataSeries.Count <= MaxSelectedSeries);
                //Mark route track, but not chart
                //Decrease update rate some, this is choppy
                if (this.m_endSelect || DateTime.Now.Subtract(this.m_lastMarkingRouteTime).TotalMilliseconds >= 300)
                {
                    this.m_lastMarkingRouteTime = DateTime.Now;

                    IList<TrailResult> markResults = new List<TrailResult>();
                    //select all results on map if summary or "few"
                    if (tr is SummaryTrailResult || this.TrailResults.Count <= MaxSelectedSeries)
                    {
                        foreach (TrailResult tr2 in this.TrailResults)
                        {
                            markResults.Add(tr2);
                        }
                    }
                    else
                    {
                        //If not summary set only mark selected
                        markAll = false;
                        markResults.Add(tr);
                    }

                    //possibly mark the ST mapped activity on the map
                    if (markResults.Count <= MaxSelectedSeries && m_page.ViewSingleActivity() != null)
                    {
                        bool addReference = true;
                        foreach (TrailResult tr2 in markResults)
                        {
                            if (tr2.Activity == m_page.ViewSingleActivity())
                            {
                                addReference = false;
                                break;
                            }
                        }
                        if (addReference)
                        {
                            //find the results for the view activity
                            IList<TrailResultWrapper> allResults = Controller.TrailController.Instance.Results;
                            foreach (TrailResultWrapper tr2 in allResults)
                            {
                                if (tr2.Result.Activity == m_page.ViewSingleActivity())
                                {
                                    markResults.Add(tr2.Result);
                                }
                            }
                        }
                    }

                    IList<Data.TrailResultMarked> results = new List<Data.TrailResultMarked>();
                    foreach (TrailResult tr2 in markResults)
                    {
                        if (!(tr2 is SummaryTrailResult))
                        {
                            IValueRangeSeries<DateTime> t2 = TrackUtil.GetDateTimeFromChartResult(XAxisReferential == XAxisValue.Time, IsTrailPointOffset(tr2), tr2, this.ReferenceTrailResult, regions);
                            //Add ranges if single set, then it is a part of a new selection
                            if (!selecting && range != null)
                            {
                                if (float.IsNaN(range[1]) && !float.IsNaN(range[0]))
                                {
                                    DateTime time = TrackUtil.GetDateTimeFromChartResult(XAxisReferential == XAxisValue.Time, IsTrailPointOffset(tr2), tr2, this.ReferenceTrailResult, range[0]);
                                    //Add a one second duration, otherwise there will be a complicated shared/Marked times combination
                                    t2.Add(new ValueRange<DateTime>(time, time.AddSeconds(1)));
                                }
                            }
                            results.Add(new Data.TrailResultMarked(tr2, t2));
                        }
                    }

                    m_page.MarkTrack(results, false, true);
                }

                //Scroll list
                m_page.EnsureVisible(new List<Data.TrailResult> { tr }, false, false);

                int resultIndex;
                if (markAll)
                {
                    resultIndex = -1;
                }
                else
                {
                    resultIndex = SeriesIndexToResult(seriesIndex);
                }

                //regions/range is in (raw) chart format, do not offset/convert
                m_multiple.SetSelectedResultRange(resultIndex, regions, range);
                //this.MainChart.SelectData += new ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataHandler(MainChart_SelectData);
                //m_selectDataHandler = true;

                if (!selecting && endSelect && !(tr is SummaryTrailResult))
                {
                    this.ShowSpeedToolTip(tr, regions);
                }

                int val = GetSmooth();
                SetSmoothingPicker(val);
            }
        }

        public void ClearSelectedRegions(bool clearRange)
        {
            if (this.MainChart != null && this.MainChart.DataSeries != null)
            {
                foreach (ChartDataSeries c in this.MainChart.DataSeries)
                {
                    //Optimize clear range, as this is slow
                    if (clearRange)
                    {
                        float[] range = new float[2];
                        c.GetSelectedRange(out range[0], out range[1]);
                        if (!float.IsNaN(range[0]) || !float.IsNaN(range[1]))
                        {
                            c.SetSelectedRange(float.NaN, float.NaN);
                        }
                    }
                    c.ClearSelectedRegions();
               }
            }
        }

        public void UpdateSelectedResultRegions()
        {
            //Select same region/range as before, switch time/distance if needed
            if (this.m_selectedDataSeries < this.MainChart.DataSeries.Count && this.MainChart.DataSeries.Count > 0)
            {
                int index = this.SeriesIndexToResult(this.m_selectedDataSeries);
                TrailResult tr = TrailResults[index];
                if (this.m_prevSelectedXAxis != this.m_XAxisReferential)
                {
                    if (this.m_prevSelectedRegions != null)
                    {
                        foreach (float[] af in this.m_prevSelectedRegions)
                        {
                            TrackUtil.ChartResultConvert(this.m_prevSelectedXAxis == XAxisValue.Time, this.XAxisReferential == XAxisValue.Time,
                                IsTrailPointOffset(tr), tr, this.ReferenceTrailResult, af);
                        }
                    }
                    if (this.m_prevSelectedRange != null)
                    {
                        TrackUtil.ChartResultConvert(this.m_prevSelectedXAxis == XAxisValue.Time, this.XAxisReferential == XAxisValue.Time,
                            IsTrailPointOffset(tr), tr, this.ReferenceTrailResult, this.m_prevSelectedRange);
                    }
                    this.m_prevSelectedXAxis = this.m_XAxisReferential;
                }
                else if (/*this.m_prevSelectedResult != tr &&*/ 
                    this.m_prevSelectedResult != null && tr != null &&
                    this.m_prevSelectedResult.Activity != null && tr.Activity != null &&
                    this.m_prevSelectedResult.Activity == tr.Activity)
                {
                    //Result for same activity, same data: recalc
                    if (this.m_prevSelectedRange != null)
                    {
                        float[] t = new float[2] { this.m_prevSelectedRange[0], this.m_prevSelectedRange[1] };
                        TrackUtil.ChartResultConvert(this.XAxisReferential == XAxisValue.Time, this.XAxisReferential == XAxisValue.Time,
                                IsTrailPointOffset(tr), this.m_prevSelectedResult, tr, this.ReferenceTrailResult, t);
                        bool sameRange = false;
                        if (!float.IsNaN(t[0]) && !float.IsNaN(t[1]))
                        {
                            //There was an overlap, use it (otherwise keep the time/distance)
                            this.m_prevSelectedRange = t;
                            sameRange = true;
                        }
                        else if (!TrackUtil.AnyRangeOverlap(this.XAxisReferential == XAxisValue.Time,
                            IsTrailPointOffset(tr), tr, this.ReferenceTrailResult, this.m_prevSelectedRange))
                        {
                            this.m_prevSelectedRange[0] = float.NaN;
                            this.m_prevSelectedRange[1] = float.NaN;
                        }
                        if (this.m_prevSelectedRegions != null)
                        {
                            foreach (float[] af in this.m_prevSelectedRegions)
                            {
                                if (sameRange)
                                {
                                    TrackUtil.ChartResultConvert(this.XAxisReferential == XAxisValue.Time, this.XAxisReferential == XAxisValue.Time,
                                        IsTrailPointOffset(tr), this.m_prevSelectedResult, tr, this.ReferenceTrailResult, af);
                                }
                                else if (!TrackUtil.AnyRangeOverlap(this.XAxisReferential == XAxisValue.Time,
                                    IsTrailPointOffset(tr), tr, this.ReferenceTrailResult, af))
                                {
                                    af[0] = float.NaN;
                                    af[1] = float.NaN;
                                }
                            }
                        }
                        this.m_prevSelectedResult = tr;
                    }
                }
            }
        }

        public void SetSelectedResultRegions()
        {
            this.SetSelectedResultRegions(-1, this.m_prevSelectedRegions, this.m_prevSelectedRange);
        }

        //Mark the series for all or a specific result
        //Note: Clear should be done prior to the call, regions are added only
        public void SetSelectedResultRegions(int resultIndex, IList<float[]> regions, float[] range)
        {
            if (ShowPage)
            {
                //if (m_selectDataHandler)
                //{
                //    this.MainChart.SelectData -= new ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataHandler(MainChart_SelectData);
                //}
                if (resultIndex < 0)
                {
                    //Use recursion to set all series
                    for (int j = 0; j < MainChart.DataSeries.Count; j++)
                    {
                        this.SetSelectedResultRegions(j, regions, range);
                    }
                    return;
                }

                if (regions != null)
                {
                    foreach (float[] ax in regions)
                    {
                        //Ignore ranges outside current range and malformed scales
                        if (ax[0] < MainChart.XAxis.MaxOriginFarValue &&
                            MainChart.XAxis.MinOriginValue > float.MinValue &&
                            ax[1] > MainChart.XAxis.MinOriginValue &&
                            MainChart.XAxis.MaxOriginFarValue < float.MaxValue)
                        {
                            ax[0] = Math.Max(ax[0], (float)MainChart.XAxis.MinOriginValue);
                            ax[1] = Math.Min(ax[1], (float)MainChart.XAxis.MaxOriginFarValue);

                            foreach (int j in ResultIndexToSeries(resultIndex))
                            {
                                MainChart.DataSeries[j].AddSelecedRegion(ax[0], ax[1]);
                            }
                        }
                    }
                }

                if (range != null)
                {
                    //Ignore ranges outside current range and malformed scales
                    if (TrackUtil.AnyRangeOverlap(range, MainChart.XAxis.MinOriginValue, MainChart.XAxis.MaxOriginFarValue))
                    {
                        range[0] = Math.Max(range[0], (float)MainChart.XAxis.MinOriginValue);
                        if (!float.IsNaN(range[1]))
                        {
                            range[1] = Math.Min(range[1], (float)MainChart.XAxis.MaxOriginFarValue);
                        }
                        if (range[1] == range[0])
                        {
                            //"Single" selection on chart
                            range[1] = float.NaN;
                        }
                        foreach (int j in ResultIndexToSeries(resultIndex))
                        {
                            //Checking what is already set does not seem to improve much
                            //float[] range2 = new float[2];
                            //MainChart.DataSeries[j].GetSelectedRange(out range2[0], out range2[1]);
                            //if (range[0] != range2[0] || range[1] != range2[1])
                            {
                                //This is the slow part of this routine
                                MainChart.DataSeries[j].SetSelectedRange(range[0], range[1]);
                                MainChart.DataSeries[j].EnsureSelectedRangeVisible(); //Not working?
                            }
                        }
                    }
                }
                //if (m_selectDataHandler)
                //{
                //    this.MainChart.SelectData += new ZoneFiveSoftware.Common.Visuals.Chart.ChartBase.SelectDataHandler(MainChart_SelectData);
                //}
            }
        }

        private void SetSelectedResultRegions(TrailResultMarked trm, bool isRegion, ref bool toolTipShown)
        {
            //Set the matching time distance for the activity
            for (int resultIndex = 0; resultIndex < m_trailResults.Count; resultIndex++)
            {
                TrailResult tr = m_trailResults[resultIndex];
                if (trm.trailResult.Activity == tr.Activity || this.m_trailResults.Count == 1)
                {
                    if (tr is SummaryTrailResult)
                    {
                        tr = this.ReferenceTrailResult;
                    }
                    IList<float[]> regions = TrackUtil.GetChartResultFromActivity(XAxisReferential == XAxisValue.Time, IsTrailPointOffset(tr), tr, ReferenceTrailResult, trm.selInfo);
                    if (isRegion)
                    {
                        this.SetSelectedResultRegions(resultIndex, regions, null);
                        this.m_prevSelectedRegions = regions;
                        //this.m_prevSelectedXAxis = XAxisReferential;
                        if (!toolTipShown && trm.trailResult == tr)
                        {
                            //While more than one result may be shown, only one tooltip
                            this.ShowSpeedToolTip(tr, regions);
                            toolTipShown = true;
                        }
                    }
                    else
                    {
                        if (regions != null && regions.Count > 0)
                        {
                            this.SetSelectedResultRegions(resultIndex, null, regions[regions.Count - 1]);
                            this.m_prevSelectedRange = regions[regions.Count - 1];
                            this.m_prevSelectedXAxis = XAxisReferential;
                            this.m_prevSelectedResult = tr;
                        }
                    }
                }
            }
        }

        public void SetSelectedResultRegions(IList<TrailResultMarked> atr, TrailResultMarked markedRange)
        {
            if (ShowPage && MainChart != null && MainChart.DataSeries != null &&
                    MainChart.DataSeries.Count > 0 &&
                m_trailResults.Count > 0)
            {
                //The clear used to be controlled by (markedRange == null), but this path should be used from ActivityPage, that should have corrct status
                this.ClearSelectedRegions(true);
                bool toolTipShown = false;
                foreach (TrailResultMarked trm in atr)
                {
                    SetSelectedResultRegions(trm, true, ref toolTipShown);
                }

                if (markedRange != null)
                {
                    SetSelectedResultRegions(markedRange, false, ref toolTipShown);
                }
            }
        }

        private float GetSelectedInterval()
        {
            float res = 0;
            if (this.m_selectedDataSeries >= 0)
            {
                float[] range = new float[2];
                this.MainChart.DataSeries[this.m_selectedDataSeries].GetSelectedRange(out range[0], out range[1]);

                if (!float.IsNaN(range[0]) && !float.IsNaN(range[1]))
                {
                    res = range[1] - range[0];
                }
                if (!float.IsNaN(this.m_firstRangeSelected))
                {
                    if (this.m_firstRangeSelected > range[0])
                    {
                        res = -res;
                    }
                }
                if (res != 0)
                {
                    if (XAxisReferential != XAxisValue.Time)
                    {
                        //no conversion needed for time
                        res = (float)TrackUtil.DistanceConvertTo(res, this.ReferenceTrailResult);
                    }
                }
            }
            return res;
        }

        //Could use TrailResultMarked, but a selection of the track cannot be marked in multi mode
        public void EnsureVisible(IList<TrailResult> atr)
        {
            if (ShowPage)
            {
                foreach (TrailResult tr in atr)
                {
                    int resultIndex = -1;
                    this.ClearSelectedRegions(true);
                    for (int i = 0; i < MainChart.DataSeries.Count; i++)
                    {
                        if (m_trailResults[SeriesIndexToResult(i)].Equals(tr))
                        {
                            resultIndex = SeriesIndexToResult(i);
                            break;
                        }
                    }
                    foreach (int j in ResultIndexToSeries(resultIndex))
                    {
                        MainChart.DataSeries[j].AddSelecedRegion(
                                                MainChart.DataSeries[j].XMin, MainChart.DataSeries[j].XMax);
                    }
                }
                this.MainChart.Focus();
            }
        }

        //Find if the chart has any data
        public bool AnyData()
        {
            return m_axisCharts != null && m_axisCharts.Count>0;
        }

        public bool HasValues(LineChartTypes chartType)
        {
            if (m_hasValues == null)
            {
                m_hasValues = new Dictionary<LineChartTypes, bool>();
            }
            if(!m_hasValues.ContainsKey(chartType))
            {
                m_hasValues.Add(chartType, false);
                //Previous check when a diff to itself is not a value - enable replacing
                //if (!(m_trailResults == null || m_refTrailResult == null ||
                //    (yaxis == LineChartTypes.DiffTime || yaxis == LineChartTypes.DiffDist) &&
                //    m_trailResults.Count == 1 && m_trailResults[0] == m_refTrailResult))
                {
                    for (int i = 0; i < this.TrailResults.Count; i++)
                    {
                        TrailResult tr = this.TrailResults[i];
                        if (//TODO: check data for summary too
                            tr is SummaryTrailResult)
                        {
                            m_hasValues[chartType] = true;
                            break;
                        }
                        //The track is mostly cached in result, it is not much extra to request and drop it
                        INumericTimeDataSeries graphPoints = LineChartUtil.GetSmoothedActivityTrack(tr, chartType, ReferenceTrailResult);

                        if (graphPoints != null && graphPoints.Count > 1)
                        {
                            m_hasValues[chartType] = true;
                            break;
                        }
                    }
                }
            }
            return m_hasValues[chartType];
        }

        private int SeriesIndexToResult(int seriesIndex)
        {
            if (this.m_trailResults.Count == 0 || seriesIndex < 0)
            {
                return 0;
            }
            return seriesIndex % this.m_trailResults.Count;
        }

        private int[] ResultIndexToSeries(int resultIndex)
        {
            if (resultIndex < 0 || this.m_trailResults.Count <= 0)
            {
                //No data
                return new int[0];
            }
            int[] indexes = new int[MainChart.DataSeries.Count / this.m_trailResults.Count];
            for (int i = 0; i < indexes.Length; i++)
            {
                indexes[i] = this.SeriesIndexToResult(resultIndex) + i * this.m_trailResults.Count;
            }
            return indexes;
        }

        virtual protected void SetupDataSeries()
        {
            try
            {
                MainChart.DataSeries.Clear();
                MainChart.XAxis.Markers.Clear();
                if (m_visible)
                {
                    IList<TrailResult> chartResults = new List<TrailResult>();
                    foreach (TrailResult tr in m_trailResults)
                    {
                        chartResults.Add(tr);
                    }

                    //Special handling for summary, needs graphs for all results
                    bool summarySpecialColor = false;
                    SummaryTrailResult summaryResult = null;
                    foreach (TrailResult tr in m_trailResults)
                    {
                        if (tr is SummaryTrailResult)
                        {
                            if (summaryResult != null)
                            {
                                //total or average already selected, use one of them
                                continue;
                            }
                            summaryResult = (tr as SummaryTrailResult);
                            if (m_trailResults.Count == 2)
                            {
                                summarySpecialColor = true;
                            }
                            foreach (TrailResult tr2 in summaryResult.Results)
                            {
                                if (!m_trailResults.Contains(tr2))
                                {
                                    chartResults.Add(tr2);
                                }
                            }
                            break;
                        }
                    }

                    //Find if ReferenceTrailResult is in the results - needed when displaying data
                    TrailResult leftRefTr = null;
                    if (m_trailResults.Count > 0)
                    {
                        leftRefTr = m_trailResults[0];
                        for (int i = 0; i < m_trailResults.Count; i++)
                        {
                            if (m_trailResults[i] == ReferenceTrailResult)
                            {
                                leftRefTr = ReferenceTrailResult;
                                break;
                            }
                        }
                    }

                    float syncGraphOffsetSum = 0;
                    int syncGraphOffsetCount = 0;
                    LineChartTypes syncGraphOffsetChartType = LineChartTypes.Speed;
                    if (m_ChartTypes.Count > 0)
                    {
                        syncGraphOffsetChartType = m_ChartTypes[0];
                        if (syncGraphOffsetChartType != LineChartUtil.ChartToAxis(syncGraphOffsetChartType) && m_ChartTypes.Contains(LineChartUtil.ChartToAxis(syncGraphOffsetChartType)))
                        {
                            syncGraphOffsetChartType = LineChartUtil.ChartToAxis(syncGraphOffsetChartType);
                        }
                    }
                    //The ST standard order is to draw the Fill chart latest, so it covers others (Insert)
                    //The order in Trails paints left to right, so the fill graph is in the bottom (Add)
                    IList<LineChartTypes> chartTypes = m_ChartTypes;
                    if (m_trailResults.Count == 1)
                    {
                        chartTypes = new List<LineChartTypes>();
                        foreach (LineChartTypes chartType in m_ChartTypes)
                        {
                            chartTypes.Add(chartType);
                        }
                    }
                    foreach (LineChartTypes chartType in chartTypes)
                    {
                        LineChartTypes axisType = LineChartUtil.ChartToAxis(chartType);
                        if (!m_axisCharts.ContainsKey(axisType))
                        {
                            //Race condition?
                            return;
                        }
                        ChartDataSeries summaryDataLine = null;
                        IList<ChartDataSeries> summarySeries = new List<ChartDataSeries>();
                        INumericTimeDataSeries refGraphPoints = null;
                        LineChartTypes refChartType = chartType;

                        if (SyncGraph != SyncGraphMode.None)
                        {
                            if (chartType != LineChartUtil.ChartToAxis(chartType) && m_ChartTypes.Contains(LineChartUtil.ChartToAxis(chartType)))
                            {
                                refChartType = LineChartUtil.ChartToAxis(chartType);
                            }
                            if (refChartType == syncGraphOffsetChartType && ReferenceTrailResult != null)
                            {
                                refGraphPoints = LineChartUtil.GetSmoothedActivityTrack(ReferenceTrailResult, refChartType, ReferenceTrailResult);
                            }
                        }

                        //Note: If the add order changes, the dataseries to result lookup in MainChart_SelectData is affected too
                        for (int i = 0; i < chartResults.Count; i++)
                        {
                            TrailResult tr = chartResults[i];

                            ChartDataSeries dataLine = new ChartDataSeries(MainChart, m_axisCharts[chartType]);

                            //Add to the chart only if result is visible. "summary" results are only for calculation
                            if (m_trailResults.Contains(tr))
                            {
                                //Note: Add empty Dataseries even if no graphpoints. index must match results
                                MainChart.DataSeries.Add(dataLine);

                                //Update display only data
                                //It could be possible to add basis for dataseries in .Data, to only recalc the points. Not so much gain
                                dataLine.ValueAxisLabel = ChartDataSeries.ValueAxisLabelType.Average;

                                //Set colors
                                {
                                    ChartColors chartColor;
                                    //Color for the graph - keep standard color if only one result displayed
                                    if (m_trailResults.Count <= 1 || summarySpecialColor ||
                                        Data.Settings.OnlyReferenceRight && (m_axisCharts[chartType] is RightVerticalAxis))
                                    {
                                        chartColor = ColorUtil.ChartColor[chartType];
                                    }
                                    else
                                    {
                                        //TBD? other color for children (at least if only one selected)
                                        chartColor = tr.ResultColor;
                                    }

                                    dataLine.LineColor = chartColor.LineNormal;
                                    dataLine.FillColor = chartColor.FillNormal;
                                    dataLine.SelectedColor = chartColor.FillSelected; //The selected fill color only

                                    //Decrease alpha for many activities for fill (but not selected)
                                    if (m_trailResults.Count > 1)
                                    {
                                        int alpha = chartColor.FillNormal.A - m_trailResults.Count * 2;
                                        alpha = Math.Min(alpha, 0x77);
                                        alpha = Math.Max(alpha, 0x10);
                                        dataLine.FillColor = Color.FromArgb(alpha, chartColor.FillNormal.R, chartColor.FillNormal.G, chartColor.FillNormal.B);
                                    }
                                }

                                //Set chart type to Fill similar to ST for first result (not charttype), only summary if selected
                                if (m_ChartTypes[0] == chartType /*&& i==0/* || summaryResult == tr*/)
                                {
                                    dataLine.ChartType = ChartDataSeries.Type.Fill;
                                }
                                else
                                {
                                    dataLine.ChartType = ChartDataSeries.Type.Line;
                                }
                            }

                            if (tr is SummaryTrailResult)
                            {
                                //The data is calculated from the normal results
                                //If both total and average selected, only one of them is used
                                summaryDataLine = dataLine;
                                if (m_trailResults.Count > 1)
                                {
                                    dataLine.LineWidth *= 2;
                                }
                            }
                            else
                            {
                                INumericTimeDataSeries graphPoints;

                                //Hide right column graph in some situations
                                //Note that the results may be needed if only ref right also should show average...
                                if ((1 >= m_trailResults.Count ||
                                    !Data.Settings.OnlyReferenceRight ||
                                    !(m_axisCharts[chartType] is RightVerticalAxis) ||
                                    tr == leftRefTr))
                                {
                                    TrailResult refTr = ReferenceTrailResult;
                                    if (refIsSelf || null == ReferenceTrailResult)
                                    {
                                        refTr = tr;
                                    }
                                    graphPoints = LineChartUtil.GetSmoothedActivityTrack(tr, chartType, refTr);
                                }
                                else
                                {
                                    //No data
                                    graphPoints = new TrackUtil.NumericTimeDataSeries();
                                }

                                if (graphPoints.Count > 1)
                                {
                                    //Get the actual graph for all displayed
                                    float syncGraphOffset = GetDataLine(tr, graphPoints, dataLine, refGraphPoints);
                                    if (refChartType == LineChartUtil.ChartToAxis(chartType) && (refChartType != chartType || ReferenceTrailResult != tr))
                                    {
                                        syncGraphOffsetSum += syncGraphOffset;
                                        syncGraphOffsetCount++;
                                    }

                                    //Add as graph for summary
                                    if (dataLine.Points.Count > 1 && summaryResult != null &&
                                        summaryResult.Results.Contains(tr)//&&
                                                                          //Ignore ref for diff time/dist graphs
                                                                          //(pair.Key != LineChartTypes.DiffDist || pair.Key != LineChartTypes.DiffTime ||
                                            )
                                    {
                                        summarySeries.Add(dataLine);
                                    }
                                }
                            }
                        }
                        ////All results for this axis
                        //Create list summary from resulting datalines
                        if (summaryDataLine != null)
                        {
                            if (summarySeries.Count == 1)
                            {
                                //Cannot create a summary from one line, just copy the original
                                foreach (KeyValuePair<float, PointF> kv in summarySeries[0].Points)
                                {
                                    summaryDataLine.Points.Add(kv.Key, kv.Value);
                                }
                            }
                            else
                            {
                                //Only add if more than one one result
                                this.GetCategoryAverage(summaryDataLine, summarySeries);
                            }

                        }
                    }  //for all axis

                    //tooltip for "offset"
                    if (SyncGraph != SyncGraphMode.None && syncGraphOffsetCount > 0)
                    {
                        ShowGeneralToolTip(SyncGraph.ToString() + ": " + syncGraphOffsetSum / syncGraphOffsetCount); //TODO: Translate
                    }
                    if (this.m_axisCharts.Count > 0)
                    {
                        int val = GetSmooth();
                        SetSmoothingPicker(val);
                    }

                    ///////TrailPoints
                    Data.TrailResult trailPointResult = TrailPointResult();

                    if (Data.Settings.ShowTrailPointsOnChart && trailPointResult != null)
                    {
                        Image icon =
#if ST_2_1
                        CommonResources.Images.Information16;
#else
                        new Bitmap(TrailsPlugin.CommonIcons.fileCircle(11, 11, trailPointResult.ResultColor.LineNormal));
#endif
                        double oldElapsed = double.MinValue;
                        foreach (DateTime t in trailPointResult.TrailPointDateTime)
                        {
                            double elapsed;
                            if (XAxisReferential == XAxisValue.Time)
                            {
                                elapsed = trailPointResult.GetTimeResult(t);
                            }
                            else
                            {
                                elapsed = trailPointResult.GetDistResult(t);
                            }
                            elapsed += trailPointResult.GetXOffset(XAxisReferential == XAxisValue.Time, this.ReferenceTrailResult);
                            if (XAxisReferential != XAxisValue.Time)
                            {
                                //No ReSync for trailpoints
                                elapsed = TrackUtil.DistanceConvertFrom(elapsed, this.ReferenceTrailResult);
                            }
                            if (!double.IsNaN(elapsed) && elapsed > oldElapsed)
                            {
                                AxisMarker a = new AxisMarker(elapsed, icon)
                                {
                                    Line1Style = System.Drawing.Drawing2D.DashStyle.Solid,
                                    Line1Color = Color.Goldenrod
                                };
                                MainChart.XAxis.Markers.Add(a);
                            }
                        }
                    }
                }
                UpdateSelectedResultRegions();
            }
#pragma warning disable 0168
            catch (Exception e)
            { } //Exception when debugging
        }

        private TrailResult TrailPointResult()
        {
            Data.TrailResult trailPointResult = ReferenceTrailResult;

            if ((!Data.Settings.SyncChartAtTrailPoints && m_trailResults.Count == 1 ||
                 m_trailResults.Count > 0 && trailPointResult == null) &&
                 !(m_trailResults[0] is SummaryTrailResult))
            {
                trailPointResult = m_trailResults[0];
            }

            return trailPointResult;
        }

        private bool IsTrailPointOffset(TrailResult tr)
        {
            return Data.Settings.SyncChartAtTrailPoints && m_trailResults.Count > 1 && tr != TrailPointResult();
        }

        private float GetDataLine(TrailResult tr, INumericTimeDataSeries graphPoints, 
            ChartDataSeries dataLine, INumericTimeDataSeries refGraphPoints)
        {
            INumericTimeDataSeries dataPoints;
            //DataPoints for Distance can include more/less points than the points to graph
            //The is used both for pruning and extrapolating graphs
            DateTime graphStart = graphPoints.StartTime;
            DateTime graphEnd = graphPoints.StartTime.AddSeconds(graphPoints.TotalElapsedSeconds);
            if (XAxisReferential == XAxisValue.Time)
            {
                dataPoints = graphPoints;
            }
            else
            {
                dataPoints = tr.DistanceMetersTrack0(ReferenceTrailResult);
                //TBD (fix in DistTrack, as well as limiting for graph Start/End?) 
                //Make sure distance track (datapoints) has start/end for graphpoints, otherwise may about 30s valid data not be shown
                //It is not easy to check if the point already exists
                if (dataPoints.StartTime != graphStart)
                {
                    ITimeValueEntry<float> yValueEntry = dataPoints.GetInterpolatedValue(graphStart);
                    if (yValueEntry != null && !float.IsInfinity(yValueEntry.Value))
                    {
                        dataPoints.Add(graphStart, yValueEntry.Value);
                    }
                }
                if (graphEnd != dataPoints.StartTime.AddSeconds(dataPoints.TotalElapsedSeconds))
                {
                    ITimeValueEntry<float> yValueEntry = dataPoints.GetInterpolatedValue(graphEnd);
                    if (yValueEntry != null && !float.IsInfinity(yValueEntry.Value))
                    {
                        dataPoints.Add(graphEnd, yValueEntry.Value);
                    }
                }
            }
            float syncGraphOffset = LineChartUtil.GetSyncGraphOffset(graphPoints, refGraphPoints, SyncGraph);

            int oldElapsedEntry = int.MinValue;
            float oldXvalue = float.MinValue;
            float xOffset = 0;
            if (!(tr is ChildTrailResult) || this.TrailResults.Contains((tr as ChildTrailResult).ParentResult))
            {
                xOffset = tr.GetXOffset(XAxisReferential == XAxisValue.Time, this.ReferenceTrailResult);
                if (XAxisReferential != XAxisValue.Time)
                {
                    xOffset = (float)TrackUtil.DistanceConvertFrom(xOffset, this.ReferenceTrailResult);
                }
            }

            foreach (ITimeValueEntry<float> entry in dataPoints)
            {
                //The time is required to get the xvalue(time) or yvalue(dist)
                DateTime time = dataPoints.EntryDateTime(entry);

                //The x value in the graph, the actual time or distance
                float xValue;
                if (XAxisReferential == XAxisValue.Time)
                {
                    xValue = (float)tr.GetTimeResult(time);
                }
                else
                {
                    //Limit - only used for Distance

                    if (time < graphStart)
                    {
                        continue;
                    }
                    if (time > graphEnd)
                    {
                       break;
                    }

                    xValue = entry.Value;
                }
                //With "resync at Trail Points", the elapsed is adjusted to the reference at trail points
                //So at the end of each "subtrail", the track can be extended (elapsed jumps) 
                //or cut (elapsed is higher than next limit, then decreases at trail point)
                float nextXvalue = float.MaxValue;
                if (IsTrailPointOffset(tr))
                {
                    float offset = TrackUtil.GetChartResultsResyncOffset(XAxisReferential == XAxisValue.Time, tr, TrailPointResult(), xValue, out nextXvalue);
                    xValue += offset;
                }
                xValue += xOffset;
                uint elapsedEntry = entry.ElapsedSeconds;
                if (oldElapsedEntry < elapsedEntry &&
                    (!Data.Settings.SyncChartAtTrailPoints ||
                    oldXvalue < xValue && xValue <= nextXvalue))
                {
                    ITimeValueEntry<float> yValueEntry;
                    if (XAxisReferential == XAxisValue.Time)
                    {
                        yValueEntry = entry;
                    }
                    else
                    {
                        yValueEntry = graphPoints.GetInterpolatedValue(time);
                    }
                    //yValueEntry == null means that graphpoints are for a shorter time than datapoints, OK
                    //Infinity values gives garbled graphs
                    if (yValueEntry != null && !float.IsInfinity(yValueEntry.Value))
                    {
                        PointF point = new PointF(xValue, yValueEntry.Value + syncGraphOffset);
                        dataLine.Points.Add(elapsedEntry, point);
                    }
                    oldElapsedEntry = (int)elapsedEntry;
                    oldXvalue = xValue;
                }
            }
            return syncGraphOffset;
        }

        //From Overlay plugin
        private ChartDataSeries GetCategoryAverage(ChartDataSeries average,
                  IList<ChartDataSeries> list)
        {
            SortedList<float, bool> xs = new SortedList<float, bool>();
            foreach (ChartDataSeries series in list)
            {
                //Average graph is very slow with many points, limit them somehow
                //A reasonable value is close to the averaging time
                float xref = 15;
                if (XAxisReferential != XAxisValue.Time)
                {
                    //In distance mode, use points corresponding to time intervall at 5min/km
                    xref = (float)UnitUtil.Distance.ConvertFrom(xref*1000.0/300.0);
                }
                foreach (PointF point in series.Points.Values)
                {
                    float x = (float)(Math.Round(point.X / xref) * xref);
                    if (!xs.ContainsKey(x))
                    {
                        xs.Add(x, true);
                    }
                }
            }
            foreach (float x in xs.Keys)
            {
                int seen = 0;
                float y = 0;
                foreach (ChartDataSeries series in list)
                {
                    float theX = x;
                    float theY = series.GetYValueAtX(ref theX);
                    if (!theY.Equals(float.NaN))
                    {
                        y += theY;
                        seen++;
                    }
                }
                if (seen > 1 &&
                    average.Points.IndexOfKey(x) == -1)
                {
                    average.Points.Add(x, new PointF(x, y / seen));
                }
            }
            return average;
        }


        /*********************************************/
        private void SetupAxes()
        {
            smoothingLabel.Text = "";
            if (m_visible && ReferenceTrailResult != null)
            {
                // X axis
                LineChartUtil.SetupXAxisFormatter(XAxisReferential, MainChart.XAxis, ReferenceTrailResult.Activity);

                // Y axis
                MainChart.YAxisRight.Clear();
                foreach (LineChartTypes chartType in m_ChartTypes)
                {
                    CreateAxis(chartType, m_axisCharts.Count == 0);
                }
            }
        }

        private void CreateAxis(LineChartTypes chartType, bool left)
        {
            if ((m_trailResults == null || ReferenceTrailResult == null ||
                (chartType == LineChartTypes.DiffTime || chartType == LineChartTypes.DiffDist) &&
                m_trailResults.Count == 1 && m_trailResults == ReferenceTrailResult))
            {
                return;
            }
            LineChartTypes axisType = LineChartUtil.ChartToAxis(chartType);
            if (!m_axisCharts.ContainsKey(axisType))
            {
                IAxis axis;
                if (left)
                {
                    axis = MainChart.YAxis;
                }
                else
                {
                    axis = new RightVerticalAxis(MainChart);
                    MainChart.YAxisRight.Add(axis);
                }
                LineChartUtil.SetupYAxisFormatter(axisType, axis, ReferenceTrailResult.Activity);
                m_axisCharts.Add(axisType, axis);
            }
            if (!m_axisCharts.ContainsKey(chartType))
            {
                m_axisCharts.Add(chartType, m_axisCharts[axisType]);
            }
        }

        [DisplayName("X Axis value")]
        public XAxisValue XAxisReferential {
            get { return m_XAxisReferential; }
            set {
                m_XAxisReferential = value;
            }
        }

        [DisplayName("Y Axis value")]
        public LineChartTypes LeftChartType
        {
            get
            {
                if (m_ChartTypes == null || m_ChartTypes.Count == 0)
                {
                    return LineChartTypes.Unknown;
                }
                return m_ChartTypes[0];
            }
            set
            {
                ChartTypes = new List<LineChartTypes> { value };
            }
        }

        public IList<LineChartTypes> ChartTypes
        {
            get
            {
                return m_ChartTypes;
            }
            set
            {
                m_ChartTypes = value;
                //Select the first axis by default
                if (this.m_ChartTypes != null && this.m_ChartTypes.Count > 0)
                {
                    this.m_lastSelectedType = this.m_ChartTypes[0];
                }
                //Clear list of axis
                m_axisCharts = new Dictionary<LineChartTypes, IAxis>();
            }
        }

        public bool MultipleCharts
        {
            get
            {
                return m_multipleCharts;
            }
            set
            {
                m_multipleCharts = value;
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
                    if (this.ShowPage)
                    {
                        this.BeginUpdate();
                        SetupAxes();
                        SetupDataSeries();
                        this.EndUpdate(false);
                    }
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
                    m_hasValues = null;
                    if (value == null)
                    {
                        m_trailResults.Clear();
                    }
                    else
                    {
                        m_trailResults = value;
                    }
                    if (this.ShowPage)
                    {
                        this.BeginUpdate();
                        SetupAxes();
                        SetupDataSeries();
                        this.EndUpdate(false);
                    }
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

        void MainChart_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            this.m_CtrlPressed = false;
        }

        void MainChart_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            int? smoothStep = null;
            bool resetSmooth = false;
            bool refreshData = false;
            bool clearRefreshData = true;

            this.m_CtrlPressed = ((e.Modifiers & Keys.Control) > 0);

            if (e.KeyCode == Keys.Home)
            {
                smoothStep = 0;
                resetSmooth = true;
            }
            else if (e.KeyCode == Keys.End)
            {
                smoothStep = 0;
            }

            if (e.KeyCode == Keys.PageDown || e.KeyCode == Keys.PageUp)
            {
                smoothStep = 10;
                if (e.KeyCode == Keys.PageDown)
                {
                    smoothStep *= -1;
                }
            }
            else if (e.KeyCode == Keys.A)
            {
                refreshData = true;
                if (e.Modifiers == Keys.Control)
                {
                    Data.Settings.SmoothOverTrailPointsToggle();
                }
                else
                {
                    if (e.Modifiers == Keys.Shift)
                    {
                        if (SyncGraph <= SyncGraphMode.None)
                        {
                            SyncGraph = SyncGraphMode.Max;
                        }
                        else
                        {
                            SyncGraph--;
                        }
                    }
                    else
                    {
                        if (SyncGraph >= SyncGraphMode.Max)
                        {
                            SyncGraph = SyncGraphMode.None;
                        }
                        else
                        {
                            SyncGraph++;
                        }
                    }
                    //This tooltip is shown at setup - show explicitly here
                    if (SyncGraph == SyncGraphMode.None)
                    {
                        ShowGeneralToolTip(SyncGraph.ToString()); //TODO: Translate
                    }
                }
            }
            else if (e.KeyCode == Keys.C)
            {
                smoothStep = 1;
                m_lastSelectedType = LineChartTypes.Cadence;
            }
            else if (e.KeyCode == Keys.E)
            {
                smoothStep = 1;
                m_lastSelectedType = LineChartTypes.Elevation;
            }
            else if (e.KeyCode == Keys.H)
            {
                smoothStep = 1;
                m_lastSelectedType = LineChartTypes.HeartRateBPM;
            }
            else if (e.KeyCode == Keys.L)
            {
                refreshData = true;
                Data.Settings.ShowTrailPointsOnChart = (e.Modifiers == Keys.Shift);
            }
            else if (e.KeyCode == Keys.O)
            {
                m_lastSelectedType = LineChartTypes.DiffDistTime;
                if (e.Modifiers == (Keys.Control | Keys.Shift))
                {
                    smoothStep = 0;
                    //resetSmooth = true;
                    TrailResult tr = GetLastSelectedDiffResult();
                    if (tr != null && 1 == this.m_trailResults.Count)
                    {
                        tr = this.m_trailResults[0];
                    }
                    if (tr != null)
                    {
                        if (XAxisReferential == XAxisValue.Time)
                        {
                            DateTime t1 = tr.Activity.StartTime;
                            TimeSpan offset = TimeSpan.FromSeconds(tr.GetXOffset(XAxisReferential == XAxisValue.Time, this.ReferenceTrailResult));
                            //"pending offset" for now require to set the offset first
                            //offset += TimeSpan.FromSeconds(getSelectedInterval());

                            DateTime t2 = t1 + offset;
                            //if (this.ReferenceTrailResult != null && !tr.Equals(this.ReferenceTrailResult) && !tr.AnyOverlap(this.ReferenceTrailResult)
                            //    && tr.m_activityTrail.Trail.IsSplits)
                            //{
                            //    //_May_ be completely incorrect time, should not be time/of/day display
                            //    t2 -= (tr.StartTime - this.ReferenceTrailResult.StartTime);
                            //}
                            String s = "Adjust starttime on activity " + t1.ToLocalTime().ToString() + " to " + t2.ToLocalTime().ToString() + "?";
                            DialogResult popRes = MessageDialog.Show(string.Format(s,
                               CommonResources.Text.ActionYes, CommonResources.Text.ActionNo),
                               "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (popRes == DialogResult.Yes)
                            {
                                tr.Activity.StartTime = t2;
                            }
                        }
                        else
                        { //TBD, not needed?
                        }
                    }
                }
                else if ((e.Modifiers & Keys.Alt) > 0)
                {
                    smoothStep = (int)GetSelectedInterval();
                }
                else
                {
                    smoothStep = 1;
                }
            }
            else if (e.KeyCode == Keys.P)
            {
                smoothStep = 1;
                m_lastSelectedType = LineChartTypes.Power;
            }
            else if (e.KeyCode == Keys.R)
            {
                refreshData = true;
                clearRefreshData = false;
                if (e.Modifiers == Keys.Shift)
                {
                    refIsSelf = !refIsSelf;
                }
                else if (e.Modifiers == Keys.Control)
                {
                    Data.Settings.OnlyReferenceRight = !Data.Settings.OnlyReferenceRight;
                }
            }
            else if (e.KeyCode == Keys.S)
            {
                smoothStep = 1;
                m_lastSelectedType = LineChartTypes.Speed;
            }
            else if (e.KeyCode == Keys.T)
            {
                refreshData = true;
                Data.Settings.SyncChartAtTrailPoints = (e.Modifiers != Keys.Shift);
            }

            double axisVisibleMin = 0;
            double pixelsPerValue = 0;
            IList<LineChartTypes> charts = new List<LineChartTypes>();
            if (smoothStep != null)
            {
                refreshData = true;
                axisVisibleMin = this.MainChart.XAxis.OriginValue;
                pixelsPerValue = this.MainChart.XAxis.PixelsPerValue;
                if ((e.Modifiers & Keys.Shift) > 0)
                {
                    smoothStep = -smoothStep;
                }

                if (null == SetSmooth(smoothStep, smoothStep != 0, resetSmooth))
                {
                    refreshData = false;
                }
            }

            if (refreshData)
            {
                if (clearRefreshData)
                {
                    Controller.TrailController.Instance.Clear(true);
                }
                m_page.RefreshControlState();
                m_page.RefreshChart();
                if (pixelsPerValue > 0)
                {
                    this.MainChart.XAxis.OriginValue = axisVisibleMin;
                    this.MainChart.XAxis.PixelsPerValue = pixelsPerValue;
                }
            }

            if (smoothStep != null)
            {
                //Show smooth value once, change more than one axis if needed
                ShowSmoothToolTip();

                foreach (KeyValuePair<LineChartTypes, IAxis> kp in m_axisCharts)
                {
                    if (this.m_multipleCharts && charts.Contains(kp.Key))
                    {
                        kp.Value.LabelColor = Color.Black;
                    }
                    else
                    {
                        kp.Value.LabelColor = ColorUtil.ChartColor[kp.Key].LineNormal;
                    }
                }
            }
        }

        private TrailResult GetLastSelectedDiffResult()
        {
            TrailResult tr = null;
            int index = m_trailResults.Count - 1;
            if (LineChartUtil.IsDiffType(m_lastSelectedType) && index >= 0)
            {
                if (this.m_selectedDataSeries < 0 && m_trailResults.Contains(m_prevSelectedResult))
                {
                    tr = m_prevSelectedResult;
                }
                else
                {
                    if (this.m_selectedDataSeries >= 0)
                    {
                        //Series must be added in order, so they can be resolved to result here
                        index = this.SeriesIndexToResult(this.m_selectedDataSeries);
                    }
                    tr = m_trailResults[index];
                }
            }
            return tr;
        }

        //Combine Set&Get to minimize the funky reflection usage
        int GetSmooth()
        {
            return (int)SetSmooth(null, false, false);
        }

        //This would be so much easier with a macro or reference to property....
        private int? SetSmooth(string prop, int? val, bool isStep, bool resetSmooth)
        {
            ActivityInfoOptions t = TrailResult.TrailActivityInfoOptions;
            System.Reflection.PropertyInfo tInfo = t.GetType().GetProperty(prop);
            int currVal = (int)tInfo.GetValue(t, null);

            int ? newVal;
            if (val != null)
            {
                if (resetSmooth)
                {
                    ActivityInfoOptions a = new ActivityInfoOptions(true);
                    newVal = (int)a.GetType().GetProperty(prop).GetValue(a, null);
                }
                else
                {
                    newVal = val;
                    if (isStep)
                    {
                        newVal += currVal;
                    }
                }
                if (newVal < 0)
                {
                    newVal = 0;
                }
                if (currVal != newVal)
                {
                    tInfo.SetValue(t, (int)newVal, null);
                }
                else
                {
                    newVal = null;
                }
            }
            else
            {
                newVal = currVal;
            }

            return newVal;
        }

        //Sets the smoothing, returns value if setter is null or value changed
        private int? SetSmooth(int? val, bool isStep, bool resetSmooth)
        {
            int? newVal;
            if (!m_ChartTypes.Contains(m_lastSelectedType))
            {
                foreach (LineChartTypes chartType in m_ChartTypes)
                {
                    if (m_axisCharts[chartType] is LeftVerticalAxis)
                    {
                        m_lastSelectedType = chartType;
                        break;
                    }
                }
            }

            string smoothString = LineChartUtil.GetSmoothingString(m_lastSelectedType);
            if (!string.IsNullOrEmpty(smoothString))
            {
                newVal = SetSmooth(smoothString, val, isStep, resetSmooth);
            }
            else
            {
                TrailResult tr = GetLastSelectedDiffResult();

                if (tr != null)
                {
                    float currVal = tr.GetXOffset(XAxisReferential == XAxisValue.Time, this.ReferenceTrailResult);
                    if (val != null)
                    {
                        float valF;
                        if (resetSmooth)
                        {
                            valF = 0;
                        }
                        else
                        {
                            valF = (int)val;
                            if (isStep)
                            {
                                valF += currVal;
                            }
                        }
                        if (valF != currVal)
                        {
                            tr.SetXOffset(XAxisReferential == XAxisValue.Time, valF);
                            newVal = (int)valF;
                        }
                        else
                        {
                            newVal = null;
                        }
                    }
                    else
                    {
                        newVal = (int)currVal;
                    }
                }
                else
                {
                    //Not smoothing, no diff (selected)
                    newVal = 0;
                }
            }
            return newVal;
        }

        void ShowSmoothToolTip()
        {
            int val = GetSmooth();
            SetSmoothingPicker(val);

            if (!Data.Settings.ShowChartToolBar &&
                m_cursorLocationAtMouseMove != null)
            {
                summaryListToolTip.Show(
                    val.ToString(),
                    this,
                    new System.Drawing.Point(m_cursorLocationAtMouseMove.X +
                                  Cursor.Current.Size.Width / 2,
                                        m_cursorLocationAtMouseMove.Y),
                   summaryListToolTip.AutoPopDelay);
            }
        }

        private void ShowSpeedToolTip(TrailResult tr, IList<float[]> regions)
        {
            if (!(tr is SummaryTrailResult))
            {
                //TBD summary result, multiple result?
                float dist = 0;
                float time = 0;
                IValueRangeSeries<DateTime> res = TrackUtil.GetDateTimeFromChartResult(XAxisReferential == XAxisValue.Time, IsTrailPointOffset(tr), tr, this.ReferenceTrailResult, regions);
                foreach (IValueRange<DateTime> v in res)
                {
                    DateTime d1 = v.Lower;
                    DateTime d2 = v.Upper;

                    if (d2 > d1)
                    {
                        double t1 = tr.GetDistResult(d1);
                        double t2 = tr.GetDistResult(d2);
                        dist += (float)(t2 - t1);

                        t1 = tr.GetTimeResult(d1);
                        t2 = tr.GetTimeResult(d2);
                        time += (float)(t2 - t1);
                    }
                }
                if (time > 0)
                {
                    float speed = dist / time;
                    string s = UnitUtil.PaceOrSpeed.ToString(speed, tr.Activity, "U");
                    this.ShowGeneralToolTip(s);
                }
            }
        }

        public void ShowGeneralToolTip(string s)
        {
            summaryListToolTip.Show(s, this, //TODO: Relate to axis?
                new System.Drawing.Point(10 + Cursor.Current.Size.Width / 2, 10),
                summaryListToolTip.AutoPopDelay);
        }

        /*******************************************/

        void MainChart_MouseLeave(object sender, System.EventArgs e)
        {
            this.m_CtrlPressed = false;
        }

        void MainChart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.m_endSelect = false;
            if (!this.m_CtrlPressed)
            {
                this.m_MouseDownLocation = e.Location;
            }
            else
            {
                this.m_MouseDownLocation = Point.Empty;
            }
        }

        void MainChart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            m_cursorLocationAtMouseMove = e.Location;
        }

        void MainChart_SelectAxisLabel(object sender, ChartBase.AxisEventArgs e)
        {
            if (this.m_multipleCharts && (e.Axis is RightVerticalAxis || e.Axis is LeftVerticalAxis))
            {
                this.ClearSelectedRegions(true);
                //Select all charts for this axis
                for (int i = 0; i < MainChart.DataSeries.Count; i++)
                {
                    //For "single result" only select first series
                    if (MainChart.DataSeries[i].ValueAxis == e.Axis)
                    {
                        MainChart.DataSeries[i].AddSelecedRegion(
                            MainChart.DataSeries[i].XMin, MainChart.DataSeries[i].XMax);
                    }
                }

                foreach (LineChartTypes chartType in this.m_ChartTypes)
                {
                    if (m_axisCharts[chartType] == e.Axis)
                    {
                        //More than one chart could exist for the axis, only select the first
                        m_lastSelectedType = chartType;
                        m_axisCharts[m_lastSelectedType].LabelColor = Color.Black;
                        ShowSmoothToolTip();
                    }
                    else
                    {
                        m_axisCharts[chartType].LabelColor = ColorUtil.ChartColor[chartType].LineNormal;
                    }
                }
            }
        }

        /*******************************************/

        public bool BeginUpdate()
        {
            return this.MainChart.BeginUpdate();
        }

        public void EndUpdate(bool zoom)
        {
            if (this.ShowPage && zoom)
            {
                this.ZoomToData();
            }
            this.MainChart.EndUpdate();
            if (this.ShowPage)
            {
                //Select same region/range as before. Mostly interesting when recalc or adding graphs but not restricted when switching trail/activities
                //Must be done after EndUpdate, to recalc XAxis.MaxOriginFarValue
                SetSelectedResultRegions();
            }
        }
    }
}
