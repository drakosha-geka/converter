using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace FlightPlan_Converter
{
	public partial class mainForm : Form
	{
		public bool CheckFileType(ref StreamReader _file, string _btn)
		{
			string temp;
			switch (_btn)
			{
				case "btnFS9": temp = _file.ReadLine();
					if (temp.Contains("xml"))
						return true;
					else
						return false;
				case "btnFSX": temp = _file.ReadLine();
					if (temp.Contains("flightplan"))
						return true;
					else
						return false;
			}
			return true;
		}

		public int ParseFS9(ref string _data, ref string _property, ref string _id, ref string _type)
		{
			int rez = 0;
			string [] SplitData;
			_property = _data.Substring(0, _data.IndexOf('='));
			_data = _data.Substring(_data.IndexOf('=') + 1).Trim(' ',',');
			SplitData = new string[_data.Length - _data.Replace(",", "").Length];
			SplitData = _data.Split(',', StringSplitOptions.RemoveEmptyEntries);
			switch (_property)
			{
				case "departure_id":
				case "destination_id":
					_id = _data.Substring(0, _data.IndexOf(','));
					_data = _data.Substring(_data.IndexOf(',') + 1).Trim().Replace('*', '°');
					rez = 1;
					break;
				case "title":
				case "description":
				case "type":
				case "routetype":
				case "cruising_altitude":

				case "AppVersion":
				case "departure_position":
				case "departure_name":
				case "destination_name": break;
				default:
					_property = _property.Substring(0, _property.Length - 2);
					_id = _data.Substring(0, _data.IndexOf(','));
					_data = _data.Substring(_data.IndexOf(',') + 1);
					_type = _data.Substring(0, _data.IndexOf(',')).Trim();
					_data = _data.Substring(_data.IndexOf(',') + 1, _data.Length - 2).
								Trim().TrimEnd(',').Replace('*', '°');
					rez = 2;
					break;
			}
			delete [] Spli
			return rez;
		}

		public void ParseFSX(string _data, ref string _lat, ref string _lon, ref string _alt)
		{
			_lat = _data.Substring(0, _data.IndexOf(',')).Replace('°', '*');
			_data = _data.Substring(_data.IndexOf(',') + 1);
			_lon = _data.Substring(0, _data.IndexOf(',')).Replace('°', '*').Trim();
			_alt = _data.Substring(_data.IndexOf(',') + 1).Trim();
		}

		public void ConvertCoords(ref string _lat, ref string _lon, bool toFSX = true)
		{
			double temp, min, sec;
			string tmpstr;
			if (toFSX)
			{
				tmpstr = _lat.Substring(_lat.IndexOf('°') + 2, _lat.Length - (_lat.IndexOf('°') + 3));
				temp = Convert.ToDouble(tmpstr);
				min = Math.Truncate(temp);
				sec = temp - min;
				sec *= 60;
				tmpstr = min.ToString("0") + "' " +
						sec.ToString("0.00") + '"';
				_lat = _lat.Substring(0, _lat.IndexOf('°') + 2) + tmpstr;

				tmpstr = _lon.Substring(_lon.IndexOf('°') + 2, _lon.Length - (_lon.IndexOf('°') + 3));
				temp = Convert.ToDouble(tmpstr);
				min = (int)Math.Truncate(temp);
				sec = temp - (double)min;
				sec *= 60;
				tmpstr = min.ToString("0") + "' " +
						sec.ToString("0.00") + '"';
				_lon = _lon.Substring(0, _lon.IndexOf('°') + 2) + tmpstr;
			}
			else
			{
				tmpstr = _lat.Substring(_lat.IndexOf('*') + 2, _lat.IndexOf("'") - (_lat.IndexOf('*') + 2));
				min = Convert.ToDouble(tmpstr);
				tmpstr = _lat.Substring(_lat.IndexOf("'") + 2, _lat.Length - (_lat.IndexOf("'") + 3));
				sec = Convert.ToDouble(tmpstr);
				min += sec / 60;
				tmpstr = min.ToString("00.00") + "'";
				_lat = _lat.Substring(0, _lat.IndexOf('*') + 2) + tmpstr;

				tmpstr = _lon.Substring(_lon.IndexOf('*') + 2, _lon.IndexOf("'") - (_lon.IndexOf('*') + 2));
				min = Convert.ToDouble(tmpstr);
				tmpstr = _lon.Substring(_lon.IndexOf("'") + 2, _lon.Length - (_lon.IndexOf("'") + 3));
				sec = Convert.ToDouble(tmpstr);
				min += sec / 60;
				tmpstr = min.ToString("00.00") + "'";
				_lon = _lon.Substring(0, _lon.IndexOf('*') + 2) + tmpstr;
			}
		}

		public string fileName;

		public struct Airport
		{
			public string id, lat, lon, alt;
		};

		public struct WayPoint
		{
			public string id, type, lat, lon, alt;
		};

		public enum RouteType
		{
			GPS, VOR, LowAlt, HighAlt
		}

		public List<WayPoint> waypoint;

		public Airport dep, dest;

		public string title, descrip, fltype, rtype, altit;

		public mainForm()
		{
			InitializeComponent();
		}

		private void btnLoad_Click(object sender, EventArgs e)
		{
			if (dlgLoad.ShowDialog() == DialogResult.OK)
			{
				fileName = dlgLoad.FileName;
				MessageBox.Show("Flight plan successfully loaded!", "Load flight plan",
								MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void btnFS9_Click(object sender, EventArgs e)
		{
			if (fileName != null)
			{
				StreamReader file = new StreamReader(fileName);
				StreamWriter _out;
				XmlDocument xml;

				if (!CheckFileType(ref file, btnFS9.Name))
				{
					MessageBox.Show("Wrong flight plan type!\n Please load FSX flight plan.",
									"Error - Flight plan type", MessageBoxButtons.OK, MessageBoxIcon.Error);
					file.Close();
					file.Dispose();
					return;
				}
				file.Close();
				file.Dispose();

				dlgSave.Filter = "Flight plan (FS2004/FS9 type)|*.pln";
				if (dlgSave.ShowDialog() == DialogResult.OK)
				{
					_out = new StreamWriter(dlgSave.FileName);
					_out.WriteLine("[flightplan]");
				}
				else return;

				WayPoint tmpWP = new WayPoint();
				waypoint = new List<WayPoint>();

				xml = new XmlDocument();
				xml.Load(fileName);
				rtype = "0";
				foreach (XmlNode item in xml.LastChild.LastChild.ChildNodes)
				{
					switch (item.Name)
					{
						case "Title": title = item.InnerText; break;
						case "Descr": descrip = item.InnerText; break;
						case "FPType": fltype = item.InnerText; break;
						case "RouteType": rtype = ((int)Enum.Parse(typeof(RouteType), item.InnerText)).ToString(); break;  //"1"
						case "CruisingAlt": altit = item.InnerText; break;
						case "DepartureID": dep.id = item.InnerText; break;
						case "DestinationID": dest.id = item.InnerText; break;
						case "DepartureLLA":
							ParseFSX(item.InnerText, ref dep.lat,
										ref dep.lon, ref dep.alt);
							ConvertCoords(ref dep.lat, ref dep.lon, false);
							break;
						case "DestinationLLA":
							ParseFSX(item.InnerText, ref dest.lat,
										ref dest.lon, ref dest.alt);
							ConvertCoords(ref dest.lat, ref dest.lon, false);
							break;
						case "ATCWaypoint":
							foreach (XmlNode childitem in item.ChildNodes)
							{
								switch (childitem.Name)
								{
									case "ATCWaypointType": tmpWP.type = childitem.InnerText.Substring(0, 1);
										break;
									case "WorldPosition":
										ParseFSX(childitem.InnerText, ref tmpWP.lat,
													ref tmpWP.lon, ref tmpWP.alt);
										ConvertCoords(ref tmpWP.lat, ref tmpWP.lon, false);
										break;
									case "ICAO": tmpWP.id = childitem.LastChild.InnerText; break;
								}
							}
							waypoint.Add(tmpWP);
							break;
					}
				}
				_out.WriteLine("title={0}", title);
				_out.WriteLine("description={0}", descrip);
				_out.WriteLine("type={0}", fltype);
				_out.WriteLine("routetype={0}", rtype);
				_out.WriteLine("cruising_altitude={0}", altit);
				_out.WriteLine("departure_id={0}, {1}, {2}, {3}", dep.id, dep.lat, dep.lon, dep.alt);
				_out.WriteLine("destination_id={0}, {1}, {2}, {3}", dest.id, dest.lat, dest.lon, dest.alt);
				foreach (WayPoint item in waypoint)
				{
					_out.WriteLine("waypoint.{0}={1}, {2}, {3}, {4}, {5},",
									waypoint.IndexOf(item), item.id, item.type, item.lat, item.lon, item.alt);
				}
				_out.Close();
				_out.Dispose();
				waypoint.Clear();
				MessageBox.Show("Flight plan successfully converted!", "Convert flight plan",
								MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else
				MessageBox.Show("Please choose flight plan to convert!",
									"Error - Plan doesn't open", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void btnFSX_Click(object sender, EventArgs e)
		{
			if (fileName != null)
			{
				StreamReader file = new StreamReader(fileName);
				XmlDocument _out;
				XmlElement child, root, flplan;

				if (!CheckFileType(ref file, btnFSX.Name))
				{
					MessageBox.Show("Wrong flight plan type!\n Please load FS2004 flight plan.",
									"Error - Flight plan type", MessageBoxButtons.OK, MessageBoxIcon.Error);
					file.Close();
					file.Dispose();
					return;
				}
				dlgSave.Filter = "Flight plan (FSX type)|*.pln";
				if (dlgSave.ShowDialog() == DialogResult.OK)
				{
					_out = new XmlDocument();
					_out.AppendChild(_out.CreateXmlDeclaration("1.0", "utf-8", null));
					root = _out.CreateElement("SimBase.Document");
					root.SetAttribute("Type", "AceXML");
					root.SetAttribute("version", "1,0");
					_out.AppendChild(root);
					child = _out.CreateElement("Descr");
					child.InnerText = "AceXML Document";
					root.AppendChild(child);
					flplan = _out.CreateElement("FlightPlan.FlightPlan");
					root.AppendChild(flplan);
					_out.Save(dlgSave.FileName);
				}
				else return;

				string data, property = null, id = null, type = null;
				WayPoint tmpWP;
				waypoint = new List<WayPoint>();
				while (!file.EndOfStream)
				{
					data = file.ReadLine();
					switch (ParseFS9(ref data, ref property, ref id, ref type))
					{
						case 0:
							switch (property)
							{
								case "title": title = data; break;
								case "description": descrip = data; break;
								case "type": fltype = data; break;
								case "routetype": rtype = data; break;
								case "cruising_altitude": altit = data; break;
							}
							break;
						case 1:
							switch (property)
							{
								case "departure_id":
									dep.id = id;
									dep.lat = data.Substring(0, data.IndexOf(',')).Trim();
									data = data.Substring(data.IndexOf(',') + 1);
									dep.lon = data.Substring(0, data.IndexOf(',')).Trim();
									data = data.Substring(data.IndexOf(',') + 1);
									dep.alt = data.Trim();
									ConvertCoords(ref dep.lat, ref dep.lon);
									break;
								case "destination_id":
									dest.id = id;
									dest.lat = data.Substring(0, data.IndexOf(',')).Trim();
									data = data.Substring(data.IndexOf(',') + 1);
									dest.lon = data.Substring(0, data.IndexOf(',')).Trim();
									data = data.Substring(data.IndexOf(',') + 1);
									dest.alt = data.Trim();
									ConvertCoords(ref dest.lat, ref dest.lon);
									break;
							}
							break;
						case 2:
							tmpWP.id = id;
							tmpWP.type = type;
							tmpWP.lat = data.Substring(0, data.IndexOf(',')).Trim();
							data = data.Substring(data.IndexOf(',') + 1);
							tmpWP.lon = data.Substring(0, data.IndexOf(',')).Trim();
							data = data.Substring(data.IndexOf(',') + 1);
							tmpWP.alt = data.Trim();
							ConvertCoords(ref tmpWP.lat, ref tmpWP.lon);
							waypoint.Add(tmpWP);
							break;
					}
				}
				file.Close();
				file.Dispose();
				root = _out.CreateElement("Title");
				root.InnerText = title;
				flplan.AppendChild(root);
				root = _out.CreateElement("FPType");
				root.InnerText = fltype;
				flplan.AppendChild(root);
				if (Convert.ToInt32(rtype) != 0)
				{
					root = _out.CreateElement("RouteType");
					root.InnerText = Enum.GetName(typeof(RouteType), Convert.ToInt32(rtype)); //"VOR"
					flplan.AppendChild(root);
				}
				root = _out.CreateElement("CruisingAlt");
				root.InnerText = altit;
				flplan.AppendChild(root);
				root = _out.CreateElement("DepartureID");
				root.InnerText = dep.id;
				flplan.AppendChild(root);
				root = _out.CreateElement("DepartureLLA");
				root.InnerText = dep.lat + "," + dep.lon + ',' + dep.alt;
				flplan.AppendChild(root);
				root = _out.CreateElement("DestinationID");
				root.InnerText = dest.id;
				flplan.AppendChild(root);
				root = _out.CreateElement("DestinationLLA");
				root.InnerText = dest.lat + "," + dest.lon + ',' + dest.alt;
				flplan.AppendChild(root);
				root = _out.CreateElement("Descr");
				root.InnerText = descrip;
				flplan.AppendChild(root);
				root = _out.CreateElement("AppVersion");
				flplan.AppendChild(root);
				child = _out.CreateElement("AppVersionMajor");
				child.InnerText = "10";
				root.AppendChild(child);
				child = _out.CreateElement("AppVersionBuild");
				child.InnerText = "61472";
				root.AppendChild(child);
				foreach (WayPoint item in waypoint)
				{
					root = _out.CreateElement("ATCWaypoint");
					root.SetAttribute("id", item.id);
					flplan.AppendChild(root);
					child = _out.CreateElement("ATCWaypointType");
					switch (item.type)
					{
						case "A": child.InnerText = "Airport"; break;
						case "N": child.InnerText = "NDB"; break;
						case "V": child.InnerText = "VOR"; break;
						case "I": child.InnerText = "Intersection"; break;
					}
					root.AppendChild(child);
					child = _out.CreateElement("WorldPosition");
					child.InnerText = item.lat + ',' + item.lon + ',' + item.alt;
					root.AppendChild(child);
					child = _out.CreateElement("ICAO");
					root.AppendChild(child);
					root = child;
					child = _out.CreateElement("ICAOIdent");
					child.InnerText = item.id;
					root.AppendChild(child);
				}
				_out.Save(dlgSave.FileName);
				waypoint.Clear();
				MessageBox.Show("Flight plan successfully converted!", "Convert flight plan",
								MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else
				MessageBox.Show("Please choose flight plan to convert!",
									"Error - Plan doesn't open", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void btnExit_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("You really want to close application?", "Exiting",
						MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				Application.Exit();
		}

		private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason != CloseReason.ApplicationExitCall)
			{
				e.Cancel = true;
				btnExit_Click(sender, e);
			}
		}
	}
}
