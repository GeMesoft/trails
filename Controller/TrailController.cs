﻿using System;
using System.Collections.Generic;
using System.Text;
using ZoneFiveSoftware.Common.Data;
using ZoneFiveSoftware.Common.Data.GPS;
using ZoneFiveSoftware.Common.Data.Fitness;

namespace TrailsPlugin.Controller {
	class TrailController {
		static private TrailController m_instance;
		static public TrailController Instance {
			get {
				if (m_instance == null) {
					m_instance = new TrailController();
				}
				return m_instance;
			}
		}

		private IActivity m_currentActivity = null;
		private Data.ActivityTrail m_currentTrail = null;
		private string m_lastTrailId = null;
		private IList<Data.ActivityTrail> m_activityTrails = null;

		public IActivity CurrentActivity {
			get {
				return m_currentActivity;
			}
			set {
				if (m_currentActivity != value) {
					m_currentActivity = value;
					if (m_currentTrail != null) {
						m_lastTrailId = m_currentTrail.Trail.Id;
					}
					m_currentTrail = null;
				}
			}
		}

		public string CurrentTrailName {
			set {
				foreach (Data.ActivityTrail t in this.TrailsInBounds) {
					if (t.Trail.Name == value) {
						m_currentTrail = t;
						return;
					}
				}
				throw new Exception("Invalid trail name");
			}
		}

		public Data.ActivityTrail CurrentActivityTrail {
			get {
				if (m_currentTrail == null && m_currentActivity != null) {
					foreach (Data.ActivityTrail t in this.TrailsInBounds) {
						if (t.Trail.Id == m_lastTrailId) {
							m_currentTrail = t;
							break;
						}
					}
					if (m_currentTrail == null && this.TrailsInBounds.Count > 0) {
						m_currentTrail = TrailsInBounds[0];
					}
				}
				return m_currentTrail;
			}
		}


		public IList<Data.ActivityTrail> TrailsInBounds {
			get {
				if(m_activityTrails == null) {					
					if (m_currentActivity != null) {
						m_activityTrails = new List<Data.ActivityTrail>();
						IGPSBounds gpsBounds = GPSBounds.FromGPSRoute(m_currentActivity.GPSRoute);
						foreach (Data.Trail trail in PluginMain.Data.AllTrails.Values) {
							if (trail.IsInBounds(gpsBounds)) {
								m_activityTrails.Add(new Data.ActivityTrail(m_currentActivity, trail));
							}
						}
					}
				}
				return m_activityTrails;
			}
		}

		public IList<Data.ActivityTrail> TrailsWithResults {
			get {
				SortedList<long, Data.ActivityTrail> trails = new SortedList<long, Data.ActivityTrail>();
				if (m_currentActivity != null) {
					foreach (Data.ActivityTrail trail in this.TrailsInBounds) {
						IList<Data.TrailResult> results = trail.Results;
						if (results.Count > 0) {
							trails.Add(results[0].StartTime.Ticks, trail);
						}
					}
				}
				return trails.Values;
			}
		}

		public bool AddTrail(Data.Trail trail) {
			bool retval = PluginMain.Data.InsertTrail(trail);
			m_activityTrails = null;
			m_currentTrail = new TrailsPlugin.Data.ActivityTrail(m_currentActivity, trail);
			m_lastTrailId = trail.Id;
			return retval;
		}


		public bool UpdateTrail(Data.Trail trail) {
			bool retval = PluginMain.Data.UpdateTrail(trail);
			m_lastTrailId = trail.Id;
			m_activityTrails = null;
			return retval;
		}

		public bool DeleteCurrentTrail() {
			bool retval = PluginMain.Data.DeleteTrail(m_currentTrail.Trail);
			m_activityTrails = null;
			m_currentTrail = null;
			m_lastTrailId = null;
			return retval;

		}

	}
}
