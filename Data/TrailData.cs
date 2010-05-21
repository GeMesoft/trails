﻿/******************************************************************************

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
using System.Collections.Generic;
using ZoneFiveSoftware.Common.Data.Fitness;
using System.Xml;
using System.Xml.Serialization;
using ZoneFiveSoftware.Common.Data.GPS;

namespace TrailsPlugin.Data {
	public class TrailData {
        public TrailData()
        {
            defaults();
        }
        private static void defaults()
        {
            m_AllTrails = new SortedList<string, Data.Trail>();
        }
		private static SortedList<string, Data.Trail> m_AllTrails;

		public SortedList<string, Data.Trail> AllTrails {
			get {
				return m_AllTrails;
			}
		}

		public bool InsertTrail(Data.Trail trail) {
			foreach (Trail t in m_AllTrails.Values) {
				if (t.Name == trail.Name) {
					return false;
				}
			}
			trail.Id = System.Guid.NewGuid().ToString();
			m_AllTrails.Add(trail.Id, trail);
			PluginMain.WriteExtensionData();
			return true;
		}

		public bool UpdateTrail(Data.Trail trail) {
			foreach (Trail t in m_AllTrails.Values) {
				if (t.Name == trail.Name && t.Id != trail.Id) {
					return false;
				}
			}

			if (m_AllTrails.ContainsKey(trail.Id)) {
				m_AllTrails.Remove(trail.Id);
				m_AllTrails.Add(trail.Id, trail);
				PluginMain.WriteExtensionData();
				return true;
			} else {
				return false;
			}
		}
		public bool DeleteTrail(Data.Trail trail) {
			if (m_AllTrails.ContainsKey(trail.Id)) {
				m_AllTrails.Remove(trail.Id);
				PluginMain.WriteExtensionData();
				return true;
			} else {
				return false;
			}
		}

        private class xmlTags
        {
            public const string tTrails = "tTrails";
        }
        public static void ReadOptions(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, XmlElement pluginNode)
        {
            String attr;
            //m_AllTrails.Clear();
            attr = pluginNode.GetAttribute(xmlTags.tTrails);
            //if (attr.Length > 0) { m_defaultRadius = float.Parse(attr, NumberFormatInfo.InvariantInfo); }
            //FromXml(pluginNode);
            defaults();
            XmlDocument doc = new XmlDocument();
            if (attr == "")
            {
                attr = "<Trails/>";
            }
            doc.LoadXml(attr);

            foreach (XmlNode node in doc.DocumentElement.SelectNodes("Trail"))
            {
                Data.Trail trail = Data.Trail.FromXml(node);
                m_AllTrails.Add(trail.Id, trail);
            }
        }

        public static void WriteOptions(XmlDocument doc, XmlElement pluginNode)
        {
            //ToXml(xmlDoc);
            XmlNode trails = doc.CreateElement("Trails");
            foreach (Data.Trail trail in PluginMain.Data.AllTrails.Values)
            {
                trails.AppendChild(trail.ToXml(doc));
            }
            pluginNode.SetAttribute(xmlTags.tTrails, trails.OuterXml.ToString());

        }
        public void FromXml(XmlNode pluginNode)
        {
            defaults();
			foreach (XmlNode node in pluginNode.SelectNodes("Trails/Trail")) {
				Data.Trail trail = Data.Trail.FromXml(node);
				m_AllTrails.Add(trail.Id, trail);
			}

		}

		public XmlNode ToXml(XmlDocument doc) {
			XmlNode trails = doc.CreateElement("Trails");
			foreach (Data.Trail trail in PluginMain.Data.AllTrails.Values) {
				trails.AppendChild(trail.ToXml(doc));
			}
			return trails;
		}
	}
}
