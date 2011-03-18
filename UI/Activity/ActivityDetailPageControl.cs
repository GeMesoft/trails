﻿/*
Copyright (C) 2009 Brendan Doherty
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

//Note: This module has been split in several classes, but there is still some interwining...

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;

using ZoneFiveSoftware.Common.Data.Fitness;
using ZoneFiveSoftware.Common.Visuals;
using ZoneFiveSoftware.Common.Visuals.Fitness;

#if ST_2_1
using ZoneFiveSoftware.Common.Visuals.Fitness.GPS;
#else
using ZoneFiveSoftware.Common.Visuals.Mapping;
#endif
#if ST_2_1
//IListItem, ListSettings
using ZoneFiveSoftware.SportTracks.Util;
using ZoneFiveSoftware.SportTracks.UI;
using ZoneFiveSoftware.SportTracks.UI.Forms;
using ZoneFiveSoftware.SportTracks.Data;
using TrailsPlugin.UI.MapLayers;
#else
using TrailsPlugin.UI.MapLayers;
using ZoneFiveSoftware.Common.Visuals.Util;
#endif
using TrailsPlugin.Data;
using TrailsPlugin.Utils;

namespace TrailsPlugin.UI.Activity {
	public partial class ActivityDetailPageControl : UserControl {

        private ITheme m_visualTheme =
#if ST_2_1
                PluginMain.GetApplication().VisualTheme;
#else
                PluginMain.GetApplication().SystemPreferences.VisualTheme;
#endif
        private CultureInfo m_culture =
#if ST_2_1
                new System.Globalization.CultureInfo("en");
#else
                PluginMain.GetApplication().SystemPreferences.UICulture;
#endif

		private Controller.TrailController m_controller;
		private bool m_isExpanded = false;

#if ST_2_1
        private Object m_view = null;
        private UI.MapLayers.MapControlLayer m_layer { get { return UI.MapLayers.MapControlLayer.Instance; } }
#else
        private IDetailPage m_DetailPage = null;
        private IDailyActivityView m_view = null;
        private TrailPointsLayer m_layer = null;
#endif

#if ST_2_1
        public ActivityDetailPageControl()
        {
#else
        public ActivityDetailPageControl(IDetailPage detailPage, IDailyActivityView view)
        {
            m_DetailPage = detailPage;
            m_view = view;
            m_layer = TrailPointsLayer.Instance(m_view);
#endif
            m_controller = Controller.TrailController.Instance;

			this.InitializeComponent();
			InitControls();
		}

		void InitControls()
        {
#if !ST_2_1
            this.ExpandSplitContainer.Panel2Collapsed = true;
#endif

            TrailSelector.SetControl(this, m_controller, m_view, m_layer);
            ResultList.SetControl(this, m_controller, m_view);
            MultiCharts.SetControl(this, m_controller, m_view);
#if ST_2_1
			SplitContainer sc = DailyActivitySplitter;
            if (sc != null)
            {
               sc.Panel2.Controls.Add(MultiCharts);
            }
#endif
		}

        public void UICultureChanged(CultureInfo culture)
        {
            m_culture = culture;

            this.TrailSelector.UICultureChanged(culture);
            this.ResultList.UICultureChanged(culture);
            this.MultiCharts.UICultureChanged(culture);
        }

        public void ThemeChanged(ITheme visualTheme)
        {
            m_visualTheme = visualTheme;
            TrailSelector.ThemeChanged(visualTheme);
            ResultList.ThemeChanged(visualTheme);
            MultiCharts.ThemeChanged(visualTheme);
        }

        public IList<IActivity> Activities
        {
            set
            {
                m_controller.Activities = value;
#if !ST_2_1
                m_layer.ClearOverlays();
#endif
                RefreshData();
                RefreshControlState();
            }
        }

        private bool _showPage = false;
        public bool HidePage()
        {
            _showPage = false;
#if !ST_2_1
            m_view.RouteSelectionProvider.SelectedItemsChanged -= new EventHandler(RouteSelectionProvider_SelectedItemsChanged);
#endif
            m_layer.HidePage();
            TrailSelector.ShowPage = false;
            ResultList.ShowPage = false;
            MultiCharts.ShowPage = false;
            return true;
        }

        public void ShowPage(string bookmark)
        {
            bool showPage = _showPage;
            _showPage = true;
            m_layer.ShowPage(bookmark);
            TrailSelector.ShowPage = true;
            ResultList.ShowPage = true;
            MultiCharts.ShowPage = true;
#if !ST_2_1
            //Avoid reregistering
            if (!showPage)
            {
                 m_view.RouteSelectionProvider.SelectedItemsChanged += new EventHandler(RouteSelectionProvider_SelectedItemsChanged);
            }
#endif
        }

        public void RefreshControlState() 
        {
            ResultList.RefreshControlState();
            TrailSelector.RefreshControlState();
        }

        public void RefreshData()
        {
            bool showPage = _showPage;
            HidePage(); //defer updates
            //Update list first, so not refresh changes selection
            ResultList.RefreshList();
            RefreshRoute(); 
            //Charts are refreshed when list is changed, no need for RefreshChart();
            if (showPage)
            {
                ShowPage("");
            }
        }
        public void RefreshChart()
        {
            MultiCharts.RefreshChart();
        }
        public IList<TrailResult> SelectedItems
        {
            get
            {
                return this.ResultList.SelectedItems;
            }
            //set { this.ResultList.SelectedItems = value; }
        }

        private void RefreshRoute()
        {
            if((! m_isExpanded || isReportView)
                && m_controller.CurrentActivityTrail != null)
            {
                m_layer.HighlightRadius = m_controller.CurrentActivityTrail.Trail.Radius;

                IList<TrailGPSLocation> points = new List<TrailGPSLocation>();
                //route
                foreach (TrailGPSLocation point in m_controller.CurrentActivityTrail.Trail.TrailLocations)
                {
                    points.Add(point);
                }
                //check for TrailOrdered - displayed status
                if (m_controller.CurrentActivityTrailDisplayed!=null)
                {
                    IList<TrailResult> results = m_controller.CurrentActivityTrailDisplayed.Results;
                    IDictionary<string, MapPolyline> routes = new Dictionary<string, MapPolyline>();
                    foreach (TrailResult tr in results)
                    {
                        //Do not map activities displayed already
                        if (ViewActivities == null || ViewActivities.Count == 0 || ViewActivities.Count > 1 ||
                            ViewActivities[0] != tr.Activity)
                        {
                            //Possibly limit no of Trails shown, it slows down Gmaps
                            TrailMapPolyline m = new TrailMapPolyline(tr);
                            m.Click += new MouseEventHandler(mapPoly_Click);
                            routes.Add(m.key, m);
                        }
                    }
                    m_layer.TrailRoutes = routes;
                }
                else
                {
                    m_layer.TrailRoutes = new Dictionary<string, MapPolyline>();
                }
                m_layer.MarkedTrailRoutes = new Dictionary<string, MapPolyline>();
                m_layer.TrailPoints = points;
            }
        }

        public IList<IActivity> ViewActivities
        {
            get
            {
                return CollectionUtils.GetAllContainedItemsOfType<IActivity>(m_view.SelectionProvider.SelectedItems);
            }
        }

        //Some views like mapping is only working in single view - there are likely better tests
//        public bool isSingleView
//        {
//            get
//            {
//#if !ST_2_1
//                if (CollectionUtils.GetSingleItemOfType<IActivity>(m_view.SelectionProvider.SelectedItems) == null)
//                {
//                    return false;
//                }
//#endif
//                return true;
//            }
//        }
        private bool isReportView
        {
            get
            {
            bool result = false;
#if !ST_2_1
            if (m_view.Id == GUIDs.ReportView)
            { 
                result = true;
            }
#endif
            return result;
            }
        }

        /*************************************************************************************************************/
        public int SetResultListHeight
        {
            set
            {
                this.LowerSplitContainer.SplitterDistance = value;
            }
        }
            public void MarkTrack(IList<TrailResultMarked> atr)
        {
            MarkTrack(atr, true);
        }
        public void MarkTrack(IList<TrailResultMarked> atr, bool markChart)
        {
#if !ST_2_1
            if (_showPage)
            {
                IActivity viewActivity = null;
                if (ViewActivities != null && ViewActivities.Count == 1)
                {
                    viewActivity = ViewActivities[0];
                }
                if (m_view != null &&
                    m_view.RouteSelectionProvider != null)
                {
                    //For activities drawn by default, use common marking
                    IList<TrailResultMarked> atr2 = new List<TrailResultMarked>();
                    foreach (TrailResultMarked trm in atr)
                    {
                        if (trm.trailResult.Activity == viewActivity)
                        {
                            atr2.Add(trm);
                        }
                    }
                    if (!markChart)
                    {
                        m_view.RouteSelectionProvider.SelectedItemsChanged -= new EventHandler(RouteSelectionProvider_SelectedItemsChanged);
                    }
                    //Only one activity, OK to merge selections on one track
                    TrailsItemTrackSelectionInfo result = TrailResultMarked.SelInfoUnion(atr2);
                    m_view.RouteSelectionProvider.SelectedItems = new IItemTrackSelectionInfo[] { result };
                    if (atr != null && atr.Count > 0)
                    {
                        m_layer.DoZoom(GPS.GetBounds(atr[0].trailResult.GpsPoints(result)));
                    }
                    if (!markChart)
                    {
                        m_view.RouteSelectionProvider.SelectedItemsChanged += new EventHandler(RouteSelectionProvider_SelectedItemsChanged);
                    }
                }
                IDictionary<string, MapPolyline> mresult = new Dictionary<string, MapPolyline>();
                foreach (TrailResultMarked trm in atr)
                {
                    foreach (TrailMapPolyline m in TrailMapPolyline.GetTrailMapPolyline(trm.trailResult, trm.selInfo))
                    {
                        if (trm.trailResult.Activity != viewActivity)
                        {
                            m.Click += new MouseEventHandler(mapPoly_Click);
                            if(!mresult.ContainsKey(m.key))
                            {
                                mresult.Add(m.key, m);
                            }
                        }
                    }
                }
                m_layer.MarkedTrailRoutes = mresult;
            }
#endif
        }

        void mapPoly_Click(object sender, MouseEventArgs e)
        {
            if (sender is TrailMapPolyline)
            {
                IList<TrailResult> result = new List<TrailResult>{(sender as TrailMapPolyline).TrailRes};
                this.EnsureVisible(result, true);
            }
        }

        public void EnsureVisible(IList<TrailResult> atr, bool chart)
        {
            ResultList.EnsureVisible(atr);
            if (chart)
            {
                MultiCharts.EnsureVisible(atr);
            }
        }
        public void SetSelectedRegions(IList<TrailResultMarked> atr)
        {
            MultiCharts.SetSelectedRegions(atr);
        }
#if ST_2_1
		private System.Windows.Forms.SplitContainer DailyActivitySplitter {
			get
            {
				Control c = this.Parent;
				while (c != null) {
                    if (c is ZoneFiveSoftware.SportTracks.UI.Views.Activities.ActivityDetailPanel) {
						return (System.Windows.Forms.SplitContainer)((ZoneFiveSoftware.SportTracks.UI.Views.Activities.ActivityDetailPanel)c).Controls[0];
                }
					c = c.Parent;
				}
                return null;
                //throw new Exception("Daily Activity Splitter not found");
			}
		}
#endif

        private void btnExpand_Click(object sender, EventArgs e)
        {
            this.LowerSplitContainer.Panel2.Controls.Remove(this.MultiCharts);
#if !ST_2_1
            this.ExpandSplitContainer.Panel2.Controls.Add(this.MultiCharts);
#else
            SplitContainer sc = DailyActivitySplitter;
            if (sc != null)
            {
#endif
            int width = this.UpperSplitContainer.Width;

            LowerSplitContainer.Panel2Collapsed = true;
#if ST_2_1
                if (sc.Panel2.Controls != null && sc.Panel2.Controls.Count<=1)
                {
                    sc.Panel2.Controls.Add(this.MultiCharts);
                }
                SplitterPanel p2 = DailyActivitySplitter.Panel2;
                sc.Panel2.Controls[0].Visible = false;
                MultiCharts.Width = p2.Width;
                MultiCharts.Height = p2.Height;
#else
            m_DetailPage.PageMaximized = true;
            this.ExpandSplitContainer.Panel2Collapsed = false;
            this.ExpandSplitContainer.SplitterDistance = width;
#endif
            m_isExpanded = true;
            MultiCharts.Expanded = m_isExpanded;
#if ST_2_1
 		    }
#endif
        }
		private void MultiCharts_Collapse(object sender, EventArgs e)
        {
#if !ST_2_1
            this.ExpandSplitContainer.Panel2.Controls.Remove(this.MultiCharts);
#endif
            this.LowerSplitContainer.Panel2.Controls.Add(this.MultiCharts);
            
            LowerSplitContainer.Panel2Collapsed = false;
#if ST_2_1
            SplitContainer sc = DailyActivitySplitter;
            if (sc != null)
            {
                sc.Panel2.Controls[0].Visible = true;
                if (sc.Panel2.Controls != null && sc.Panel2.Controls.Count > 0)
                {
                    sc.Panel2.Controls.Remove(this.MultiCharts);
                }
            }
#else
            this.ExpandSplitContainer.Panel2Collapsed = true;
            m_DetailPage.PageMaximized = false;
#endif
            m_isExpanded = false;
            MultiCharts.Expanded = m_isExpanded;
        }
        
#if !ST_2_1
        void RouteSelectionProvider_SelectedItemsChanged(object sender, EventArgs e)
        {
            if (sender is ISelectionProvider<IItemTrackSelectionInfo>)
            {
                //m_view.RouteSelectionProvider.SelectedItems
                ISelectionProvider<IItemTrackSelectionInfo> selected = sender as ISelectionProvider<IItemTrackSelectionInfo>;
                if (selected != null && selected.SelectedItems != null)
                {
                    MultiCharts.SetSelectedRange(
                      TrailsItemTrackSelectionInfo.SetAndAdjustFromSelection(selected.SelectedItems, this.ViewActivities));
                }
            }
        }
#endif
    }
}
