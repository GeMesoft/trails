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
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

using ZoneFiveSoftware.Common.Data.Measurement;
using ZoneFiveSoftware.Common.Visuals;
#if !ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness;
#endif
using TrailsPlugin.Data;

namespace TrailsPlugin.UI.Activity {
	public partial class MultiChartsControl : UserControl {

        public event System.EventHandler Expand;
        public event System.EventHandler Collapse;

        private bool m_expanded = false;
        private bool m_multipleGraphs = false;
        private bool m_multipleCharts = true;
        private bool _showPage;
        private ActivityDetailPageControl m_page;
        private Controller.TrailController m_controller;
        private IList<TrailLineChart> m_lineCharts;
        private TrailLineChart m_multiChart;

#if !ST_2_1
        private IDailyActivityView m_view = null;
#endif

		public MultiChartsControl() {
            InitializeComponent();
            InitControls();
        }
#if ST_2_1
        public void SetControl(ActivityDetailPageControl page, Controller.TrailController controller, Object view)
        {
#else
        public void SetControl(ActivityDetailPageControl page, Controller.TrailController controller, IDailyActivityView view)
        {
            m_view = view;
#endif
            m_page = page;
            m_controller = controller;
            foreach (TrailLineChart t in m_lineCharts)
            {
                t.SetControl(m_page, this);
            }
        }

        void InitControls()
        {
            //this.showToolBarMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Yeild16;
            this.speedPaceToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackGPS16;
            this.speedToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackGPS16;
            this.paceToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackGPS16;
            this.heartRateToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackHeartRate16;
            this.cadenceToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackCadence16;
            this.elevationToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackElevation16;
            this.gradeStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackElevation16;
            this.powerToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackPower16;
            this.distanceToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.TrackGPS16;
            this.timeToolStripMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Calendar16;

            //this.showToolBarMenuItem.Image = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.
            this.diffTimeToolStripMenuItem.Visible = false;
            this.diffDistToolStripMenuItem.Visible = false;

            //The panels and charts should probably be created manually instead
            m_lineCharts = new List<TrailLineChart>();
            foreach (Control t in this.ChartPanel.Controls)
            {
                if (t is TrailLineChart)
                {
                    TrailLineChart lChart = (TrailLineChart)t;
                    m_lineCharts.Add(lChart);
                    if(lChart.MultipleCharts)
                    {
                        m_multiChart=lChart;
                    }
                }
            }
            //speedChart.YAxisReferential = TrailLineChart.LineChartTypes.Speed;
            //paceChart.YAxisReferential = TrailLineChart.LineChartTypes.Pace;
            //heartrateChart.YAxisReferential = TrailLineChart.LineChartTypes.HeartRateBPM;
            //gradeChart.YAxisReferential = TrailLineChart.LineChartTypes.Grade;
            //elevationChart.YAxisReferential = TrailLineChart.LineChartTypes.Elevation;
            //cadenceChart.YAxisReferential = TrailLineChart.LineChartTypes.Cadence;
            //diffTime.YAxisReferential = TrailLineChart.LineChartTypes.diffTime;
            //diffDist.YAxisReferential = TrailLineChart.LineChartTypes.diffDist;
            this.Expanded = m_expanded;
            this.Resize += new System.EventHandler(TrailLineChart_Resize);
        }

        public void ThemeChanged(ITheme visualTheme)
        {
            this.ChartBanner.ThemeChanged(visualTheme);
            foreach (TrailLineChart t in m_lineCharts)
            {
                t.ThemeChanged(visualTheme);
            }
        }

        public void UICultureChanged(CultureInfo culture)
        {
            this.ChartBanner.Text = Properties.Resources.TrailChartsName;
            foreach (TrailLineChart t in m_lineCharts)
            {
                t.UICultureChanged(culture);
            }
            this.speedToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.Speed);
            this.paceToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.Pace);
            this.speedPaceToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.SpeedPace);
            this.elevationToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.Elevation);
            this.cadenceToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.Cadence);
            this.heartRateToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.HeartRateBPM);
            this.gradeStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.Grade);
            this.powerToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.Power);

            this.diffTimeToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.DiffTime);
            this.diffDistToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.DiffDist);
            this.diffDistTimeToolStripMenuItem.Text = TrailLineChart.ChartTypeString(TrailLineChart.LineChartTypes.DiffDistTime); //TODO: Difference

            this.timeToolStripMenuItem.Text = TrailLineChart.XAxisValueString(TrailLineChart.XAxisValue.Time);
            this.distanceToolStripMenuItem.Text = TrailLineChart.XAxisValueString(TrailLineChart.XAxisValue.Distance);
            this.showToolBarMenuItem.Text = Properties.Resources.UI_Activity_Menu_ShowToolBar;
        }

        public bool ShowPage
        {
            get { return _showPage; }
            set
            {
                _showPage = value;
                if (_showPage)
                {
                    RefreshChart();
                }
            }
        }

        public bool Expanded
        {
            set
            {
                 btnExpand.Text = "";
                //For some reason, the Designer moves this button out of the panel
                btnExpand.Left = this.ChartBanner.Right - 46;

                m_expanded = value;
                m_multipleGraphs = value;
                m_multipleCharts = !value;
                if (m_expanded)
                {
                    this.ChartBanner.Style = ZoneFiveSoftware.Common.Visuals.ActionBanner.BannerStyle.Header1;
                    this.ChartBanner.Text = Properties.Resources.TrailChartsName;
                    btnExpand.BackgroundImage = CommonIcons.LowerLeft;
                }
                else
                {
                    this.ChartBanner.Style = ZoneFiveSoftware.Common.Visuals.ActionBanner.BannerStyle.Header2;
                    btnExpand.BackgroundImage = CommonIcons.LowerHalf;
                }
                ShowPage = _showPage;
                //This could be changed to zoom to data only at changes
                foreach (TrailLineChart t in m_lineCharts)
                {
                    if (t.ShowPage)
                    {
                        t.ZoomToData();
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        private void RefreshRows()
        {
            int noOfGraphs = 0;
            for (int i = 0; i < m_lineCharts.Count; i++)
            {
                if (m_lineCharts[i].ShowPage)
                {
                    noOfGraphs++;
                }
            }
            int height = (ChartPanel.Height - (int)ChartPanel.RowStyles[0].Height);
            if (noOfGraphs > 0) { height = height / noOfGraphs; }
            //The first row is the banner, the following is the charts
            for (int i = 0; i < m_lineCharts.Count; i++)
            {
                ChartPanel.RowStyles[i+1].SizeType = SizeType.Absolute;
                if (m_lineCharts[i].ShowPage)
                {
                    ChartPanel.RowStyles[i+1].Height = height;
                }
                else
                {
                    ChartPanel.RowStyles[i+1].Height = 0;
                }
            }
            ShowChartToolBar = Data.Settings.ShowChartToolBar;
        }

        public void RefreshChart()
        {
            if (_showPage)
            {
                TrailLineChart.LineChartTypes speedPaceYaxis;
                if (m_controller.ReferenceActivity != null &&
                    m_controller.ReferenceActivity.Category.SpeedUnits.Equals(Speed.Units.Speed))
                {
                    speedPaceYaxis = TrailLineChart.LineChartTypes.Speed;
                }
                else
                {
                    speedPaceYaxis = TrailLineChart.LineChartTypes.Pace;
                }
                TrailLineChart.LineChartTypes diffYaxis = TrailLineChart.LineChartTypes.DiffDist;
                if (Data.Settings.XAxisValue == TrailLineChart.XAxisValue.Distance)
                {
                    diffYaxis = TrailLineChart.LineChartTypes.DiffTime;
                }

                m_multiChart.YAxisReferentials=new List<TrailLineChart.LineChartTypes>();
                multiChart.ShowPage = false;
                //TODO: Temporary handling. Cleanup and decide multiple graphs and charts
                TrailLineChart updateChart = m_multiChart;
                if (m_multipleCharts)
                {
                    m_multiChart.BeginUpdate();
                    m_multiChart.ShowPage = false;
                }
                foreach (TrailLineChart chart in m_lineCharts)
                {
                    bool visible = false;

                    if (!chart.MultipleCharts && (m_multipleGraphs || m_multipleCharts) &&
                        (Data.Settings.MultiChartType.Contains(chart.YAxisReferential) ||
                        chart.YAxisReferential == speedPaceYaxis &&
                        Data.Settings.MultiChartType.Contains(TrailLineChart.LineChartTypes.SpeedPace) ||
                        chart.YAxisReferential == diffYaxis &&
                        Data.Settings.MultiChartType.Contains(TrailLineChart.LineChartTypes.DiffDistTime)) ||
                        !m_multipleGraphs &&
                        (chart.YAxisReferential == Data.Settings.ChartType ||
                        chart.YAxisReferential == speedPaceYaxis &&
                            TrailLineChart.LineChartTypes.SpeedPace == Data.Settings.ChartType ||
                        chart.YAxisReferential == diffYaxis &&
                            TrailLineChart.LineChartTypes.DiffDistTime == Data.Settings.ChartType))
                    {
                        visible = true;
                    }

                    if (m_multipleCharts)
                    {
                        updateChart = m_multiChart;
                    }
                    else
                    {
                        updateChart = chart;
                        updateChart.BeginUpdate();
                    }
                    chart.ShowPage = false;
                    if (visible && ( !m_multipleCharts || 
                        !m_multiChart.YAxisReferentials.Contains(chart.YAxisReferential)))
                    {
                        if (!m_multipleCharts || updateChart.YAxisReferentials.Count == 0)
                        {
                            updateChart.XAxisReferential = Data.Settings.XAxisValue;
                            IList<Data.TrailResult> list = this.m_page.SelectedItems;
                            updateChart.ReferenceTrailResult = m_controller.ReferenceTrailResult;
                            updateChart.TrailResults = list;
                            if (!m_multipleGraphs && (!m_multipleCharts || updateChart.YAxisReferentials.Count == 1))
                            {
                                this.ChartBanner.Text = TrailLineChart.ChartTypeString(chart.YAxisReferential) + " / " +
                                TrailLineChart.XAxisValueString(chart.XAxisReferential);
                            }
                        }
                        if (updateChart.HasValues(chart.YAxisReferential))
                        {
                            if (m_multipleCharts)
                            {
                                m_multiChart.YAxisReferentials.Add(chart.YAxisReferential);
                            }
                            else
                            {
                                updateChart.ShowPage = visible;
                            }
                        }
                        else
                        {
                            if (!m_multipleCharts && visible && !updateChart.HasValues(chart.YAxisReferential))
                            {
                                chart.ShowPage = false;
                                m_multiChart.YAxisReferentials.Remove(chart.YAxisReferential);
                                //Replace empty chart
                                if (!m_multipleGraphs && chart.YAxisReferential != speedPaceYaxis)
                                {
                                    foreach (TrailLineChart replaceChart in m_lineCharts)
                                    {
                                        if (replaceChart.YAxisReferential == speedPaceYaxis)
                                        {
                                            replaceChart.BeginUpdate();
                                            replaceChart.ShowPage = false;
                                            replaceChart.XAxisReferential = Data.Settings.XAxisValue;
                                            IList<Data.TrailResult> list = this.m_page.SelectedItems;
                                            replaceChart.ReferenceTrailResult = m_controller.ReferenceTrailResult;
                                            replaceChart.TrailResults = list;
                                            if (!m_multipleGraphs)
                                            {
                                                this.ChartBanner.Text = TrailLineChart.ChartTypeString(replaceChart.YAxisReferential) + " / " +
                                                TrailLineChart.XAxisValueString(replaceChart.XAxisReferential);
                                            }
                                            replaceChart.ShowPage = visible;
                                            replaceChart.EndUpdate();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!m_multipleCharts)
                    {
                        updateChart.EndUpdate();
                    }
                }
                if (m_multipleCharts)
                {
                    if (!m_multiChart.AnyData())
                    {
                        m_multiChart.YAxisReferential = speedPaceYaxis;
                        m_multiChart.XAxisReferential = Data.Settings.XAxisValue;
                        IList<Data.TrailResult> list = this.m_page.SelectedItems;
                        m_multiChart.ReferenceTrailResult = m_controller.ReferenceTrailResult;
                        m_multiChart.TrailResults = list;
                        if (!m_multipleGraphs && (!m_multipleCharts || updateChart.YAxisReferentials.Count == 1))
                        {
                            this.ChartBanner.Text = TrailLineChart.ChartTypeString(m_multiChart.YAxisReferential) + " / " +
                            TrailLineChart.XAxisValueString(m_multiChart.XAxisReferential);
                        }
                    }
                    m_multiChart.ShowPage = true;
                    m_multiChart.EndUpdate();
                }
                RefreshRows();
                RefreshChartMenu();
            }
        }

        private bool setLineChartChecked(TrailLineChart.LineChartTypes t)
        {
            if (m_multipleGraphs || m_multipleCharts)
            {
                return Data.Settings.MultiChartType.Contains(t);
            }
            else
            {
                return Data.Settings.ChartType == t;
            }
        }
        void RefreshChartMenu()
        {
            //TODO: disable if track exists (or ref for diff). 
            speedToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.Speed);
            paceToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.Pace);
            speedPaceToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.SpeedPace);
            elevationToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.Elevation);
            cadenceToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.Cadence);
            heartRateToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.HeartRateBPM);
            gradeStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.Grade);
            powerToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.Power);

            diffTimeToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.DiffTime);
            diffDistToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.DiffDist);
            diffDistTimeToolStripMenuItem.Checked = setLineChartChecked(TrailLineChart.LineChartTypes.DiffDistTime);

            timeToolStripMenuItem.Checked = Data.Settings.XAxisValue == TrailLineChart.XAxisValue.Time;
            distanceToolStripMenuItem.Checked = Data.Settings.XAxisValue == TrailLineChart.XAxisValue.Distance;
            this.showToolBarMenuItem.Checked = Data.Settings.ShowChartToolBar;
        }

        void RefreshChart(TrailLineChart.LineChartTypes t)
        {
            if (m_multipleGraphs || m_multipleCharts)
            {
                Data.Settings.ToggleMultiChartType = t;
            }
            else
            {
                Data.Settings.ChartType = t;
            }
            RefreshChart();
        }

        void RefreshChart(TrailLineChart.XAxisValue t)
        {
            Data.Settings.XAxisValue = t;
            RefreshChart();
        }

        public void SetSelectedRegions(IList<TrailResultMarked> atr)
        {
            foreach (TrailLineChart chart in m_lineCharts)
            {
                chart.SetSelectedRegions(atr);
            }
        }
        //Used as callback when selected from chart - should be only for single activity
        public void SetSelectedRange(IList<IItemTrackSelectionInfo> asel)
        {
            foreach (TrailLineChart chart in m_lineCharts)
            {
                chart.SetSelectedRange(asel);
            }
        }
        public void SetSelectedResultRange(IList<float[]> regions)
        {
            foreach (TrailLineChart chart in m_lineCharts)
            {
                chart.SetSelectedResultRange(regions);
            }
        }
        public void SetSelectedResultRange(int i, IList<float[]> regions)
        {
            foreach (TrailLineChart chart in m_lineCharts)
            {
                chart.SetSelectedResultRange(i, true, regions);
            }
        }
        public void EnsureVisible(IList<TrailResult> atr)
        {
            foreach (TrailLineChart chart in m_lineCharts)
            {
                chart.EnsureVisible(atr);
            }
        }

        public bool ShowChartToolBar
        {
            set
            {
                if (ShowPage)
                {
                    foreach (TrailLineChart chart in m_lineCharts)
                    {
                        chart.ShowChartToolBar = value;
                    }
                    RefreshChartMenu();
                }
            }
        }

        /***************************************/
        private void speedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.Speed);
        }
        private void paceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.Pace);
        }
        private void speedPaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.SpeedPace);
        }

        private void elevationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.Elevation);
        }

        private void heartRateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.HeartRateBPM);
        }

        private void cadenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.Cadence);
        }

        private void gradeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.Grade);
        }
        private void powerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.Power);
        }

        private void diffTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.DiffTime);
        }
        private void diffDistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.DiffDist);
        }
        private void diffDistTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.LineChartTypes.DiffDistTime);
        }

        private void distanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.XAxisValue.Distance);
        }

        private void timeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshChart(TrailLineChart.XAxisValue.Time);
        }

        private void showToolBarMenuItem_Click(object sender, EventArgs e)
        {
            Data.Settings.ShowChartToolBar = !Data.Settings.ShowChartToolBar;
            ShowChartToolBar = Data.Settings.ShowChartToolBar;
        }

        /***************************************/
        private void btnExpand_Click(object sender, EventArgs e)
        {
            if (m_expanded)
            {
                Collapse(sender, e);
            }
            else
            {
                Expand(sender, e);
            }
        }
        void TrailLineChart_Resize(object sender, System.EventArgs e)
        {
            this.RefreshRows();
        }
        private void ChartBanner_MenuClicked(object sender, EventArgs e)
        {
            ChartBanner.ContextMenuStrip.Width = 100;
            ChartBanner.ContextMenuStrip.Show(ChartBanner.Parent.PointToScreen(new System.Drawing.Point(ChartBanner.Right - ChartBanner.ContextMenuStrip.Width - 2,
                ChartBanner.Bottom + 1)));
        }
    }
}
