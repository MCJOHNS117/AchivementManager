using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AchievementFramework
{
	/*	TODO List:
	*	Rewrite the load and save acievement methods to use an XmlSerializer instead of hand coding everything
	*/
	public static class AchievementManager
	{
		//Achieveents are stored by their Id
		public static Dictionary<string, List<Achievement>> ActionListeners = new Dictionary<string, List<Achievement>>();

		public static Dictionary<Guid, Achievement> AchievementList = new Dictionary<Guid, Achievement>();

		public static int AchievementPoints
		{
			get
			{
				if(Initialized)
				{
					int result = 0;
					foreach(Achievement a in AchievementList.Values)
					{
						if(a.Earned)
							result += a.Points;
					}
					return result;
				}
				else
				{
					return 0;
				}
			}
		}
		public static int MaxAchievementPoints { get; private set; }

		private static Queue<Guid> QueuedCompletedAchievements = new Queue<Guid>();

		private static bool Initialized = false;
		public static bool ProcessingAction { get; private set; }

		public delegate void AchievementComplete(Guid id);
		public static AchievementComplete NotifyOnAchievementComplete;

		public static string AchievementsMasterFilename{ get; set; }
		private static XmlDocument MasterFileDocument;

		public static void CreateNewMasterFile(string path)
		{
			//Process the filename first to check for .xml, add if doesnt exist
			if(Path.GetExtension(path) == string.Empty)//There is no extension
			{
				//add an extension on the end
				path += ".xml";
			}

			AchievementsMasterFilename = path;

			//Create a new file using the path and filename
			File.Create(AchievementsMasterFilename);

			MasterFileDocument = new XmlDocument();
			MasterFileDocument.Load(AchievementsMasterFilename);
			MasterFileDocument.CreateXmlDeclaration("1.0", null, null);
			MasterFileDocument.Save(AchievementsMasterFilename);
		}

		public static void LoadAchievements(string achievementsMasterFilename, string achievementProgress)
		{
			ProcessingAction = false;
			//Set the Master Achievement Database filename
			AchievementsMasterFilename = achievementsMasterFilename;

			//Create the XML object
			XmlDocument achievementDoc = new XmlDocument();
			achievementDoc.Load(AchievementsMasterFilename);

			//get a list of achievements
			XmlNodeList achievements = achievementDoc.SelectNodes("/Achievements/Achievement");

			foreach(XmlNode achievement in achievements)
			{
				RegisterAchievement(achievement);
			}

			foreach(Achievement a in AchievementList.Values)
			{
				MaxAchievementPoints += a.Points;
			}

			Initialized = true;

			LoadAchievementProgress(achievementProgress);
		}

		private static void LoadAchievementProgress(string achievementProgress)
		{
			if(Initialized)
			{
				//Build a list of Achievement ID's that already have progress
				if(achievementProgress != string.Empty)
				{
					ProcessAchievementProgress(achievementProgress.Split(':').ToList());
				}
			}
			else
			{
				throw new InvalidOperationException("AchievemntManager is not Initialized!");
			}

			//All Achievements progress has been loaded. Now fill the ActionListener container
			FillActionListeners();
		}

		private static void FillActionListeners()
		{
			foreach(Achievement achievement in AchievementList.Values)
			{
				if(!achievement.Earned && IsAchievementEarned(achievement.RequiredAchievementId))
				{
					foreach(string actionType in achievement.Progress.Keys)
					{
						//Check if the ActionType is already registered
						if(ActionListeners.ContainsKey(actionType))
						{
							//ActionType is registered, add this achievement to its list
							ActionListeners[actionType].Add(achievement);
						}
						else
						{
							//Action type is not registered, register it and create a new list of achievements
							List<Achievement> tempList = new List<Achievement>();
							tempList.Add(achievement);
							ActionListeners.Add(actionType, tempList);
						}
					}
				}
			}
		}

		/*	Progress is encoded in the following manner:
		*	AchievementID,ActionType,Value
		*	Each achievements progress is seperated by a colon ':'
		*	So the first split in the LoadAchievements method splits the achievements into a list
		*	The second split takes each achievement and seperates its progress
		*	The number of ActionTypes for each achievement can be different. to figure out how many types
		*	a particular achievement has we first split the achievement using a comma ',' and count the members of the resulting string array
		*	Then subtract 1 from that number, and divide by 2
		*	Example: 3,EnemyShipDestroyed,745:4,EnemyAFrigateDestroyed,3,EnemyBFrigateDestroyed,5,EnemyCFrigateDestroyed,2
		*	This translates to:
		*	-Achievement ID 3 has an ActionType of EnemyShipDestroyed and the current progress is 745
		*	-Achievement ID 4 has 3 ActionTypes: EnemyAFrigateDestroyed with a progress of 3
		*					     EnemyBFrigateDestroyed with a progress of 5
		*					     EnemyCFrigateDestroyed with a progress of 2
		*/
		private static void ProcessAchievementProgress(List<string> achievementsProgressData)
		{
			foreach(string progress in achievementsProgressData)
			{
				//Split each achievement into its raw data
				string[] data = progress.Split(',');
				Guid achievementId = new Guid(data[0]);
				//determine the number of actiontypes for this achievement
				if(data.Length > 1) //Length greater then 1 means there is progress
				{
					//loop through the action types
					for(int i = 1; i < data.Length; i += 2)
					{
						Action a = new Action(data[i], Convert.ToInt32(data[i + 1]));
						if(AchievementList.ContainsKey(achievementId))
						{
							AchievementList[achievementId].OnAction(a);
						}
					}
				}
				else
				{
					//Data only has 1 element, this achievement has be unlocked already and progress does not need to be tracked
					if(AchievementList.ContainsKey(achievementId))
					{
						AchievementList[achievementId].Earned = true;
					}
				}
			}
		}

		//This method Adds and Achievement to the master file
		public static void AddAchievment(Achievement achievement)
		{
			MasterFileDocument.Load(AchievementsMasterFilename);

			XmlNode achivementNode = MasterFileDocument.CreateNode(XmlNodeType.Element, "Achivement", "Achievements/");

			MasterFileDocument.Save(AchievementsMasterFilename);
		}

		//This method registers a single achievment with the AchievmentManager
		private static void RegisterAchievement(XmlNode achievementNode)
		{
			//Pull the basics from the XML for each achievement
			string name = achievementNode["Name"].InnerText;
			Guid id = new Guid(achievementNode["ID"].InnerText);
			string description = achievementNode["Description"].InnerText;
			string category = achievementNode["Category"].InnerText;
			string subcategory = achievementNode["Subcategory"].InnerText;
			int iconId = Convert.ToInt32(achievementNode["IconID"].InnerText);
			int points = Convert.ToInt32(achievementNode["Points"].InnerText);

			Guid? requiredAchievement;

			if(achievementNode["RequiredAchievement"] != null)
			{
				requiredAchievement = new Guid(achievementNode["RequiredAchievement"].InnerText);
			}
			else
			{
				requiredAchievement = null;
			}				

			XmlNodeList actionProgressList = achievementNode["Progression"].ChildNodes;
			Dictionary<string, ActionProgress> actionProgress = new Dictionary<string, ActionProgress>();

			//Pull the progress information from the achievement XML
			foreach(XmlNode progress in actionProgressList)
			{
				//Create a blank ActionProgress to store the RequiredProgress
				ActionProgress ap = new ActionProgress();

				//Verify that the node contains both the ActionType and RequiredProgress attributes
				if(progress.Attributes["ActionType"] != null && progress.Attributes["RequiredValue"] != null)
				{
					//Set the RequiredProgress
					ap.Required = Convert.ToInt32(progress.Attributes["RequiredValue"].Value);

					//Verify the action isnt registered with the actionProgress
					if(!actionProgress.ContainsKey(progress.Attributes["ActionType"].Value))
					{
						actionProgress.Add(progress.Attributes["ActionType"].Value, ap);
					}
					else
					{
						//action type is registered twice, this is not allowed
						throw new IndexOutOfRangeException("ActionType: " + progress.Attributes["ActionType"].Value + " is already registered for AchievementID: " + id.ToString());
					}
				}
			}

			Achievement achievement = new Achievement(name, id, description, category, subcategory, iconId, actionProgress, points, false, requiredAchievement);

			//Add the achievement to the AchievementList if it is not already there
			if(!AchievementList.ContainsKey(achievement.Id))
			{
				AchievementList.Add(achievement.Id, achievement);
			}
			else
			{
				//Can not register an achievement twice with the same ID
				throw new IndexOutOfRangeException("AchievementList already contains Achievement ID: " + achievement.Id);
			}
		}

		public static string SaveAchievementProgress()
		{
			string achievementProgress = "";

			foreach(Achievement achievement in AchievementList.Values)
			{
				//If the achievement is earned, just add its ID to the progress and move on
				if(achievement.Earned)
				{
					achievementProgress += achievement.Id.ToString() + ":";
					continue;
				}
				//If the achievement is not earned, we need to add each actions progress to the list
				else
				{
					string actionList = "";
					
					foreach(string actionType in achievement.Progress.Keys)
					{
						if(achievement.Progress[actionType].Current > 0)
						{
							actionList += "," + actionType + ",";
							actionList += achievement.Progress[actionType].Current.ToString();
						}
					}

					//If the actionList contains actions whos values are not 0 add them to the progress record
					if(actionList != "")
					{
						achievementProgress += achievement.Id.ToString() + actionList + ":";
					}
					else //If there are no actions and the achievement has not been earned, skip logging this achievement
					{
						continue;
					}
				}
			}

			return achievementProgress;
		}

		//Process Actions here
		public static LogActionResults LogAction(Action action)
		{
			LogActionResults result = new LogActionResults();

			//Check that the actiontype is registered with the action listeners
			if(ActionListeners.ContainsKey(action.Type))
			{
				foreach(Achievement achievement in ActionListeners[action.Type].ToList())
				{
					if(AchievementList.ContainsKey(achievement.Id))
					{
						AchievementList[achievement.Id].OnAction(action);
					}
				}
				result.Status = LogActionResultsEnum.Ok;
				result.Message = "ActionType: " + action.Type + " Handled without issue.";		
            }
			else
			{
				//The action type in not registered
				//throw new IndexOutOfRangeException("ActionType: " + action.Type + " is not a registered type!");
				result.Status = LogActionResultsEnum.ActionTypeNotRegistered;
				result.Message = "Error: ActionType - " + action.Type + " is not a registered ActionType.";
			}

			//Listeners are removed as they are earned, so we need to update the listener 
			//container with any achievements that may have been opened
			while(QueuedCompletedAchievements.Count > 0)
			{
				Guid id = QueuedCompletedAchievements.Dequeue();

				//First remove achievement from ActionListeners
				foreach(string actionType in AchievementList[id].Progress.Keys)
				{
					//Check that this actionType is registered
					if(ActionListeners.ContainsKey(actionType))
					{
						ActionListeners[actionType].RemoveAll(a => a.Id == id);
					}
				}

				//Then check for achievements that required this acievement to be earned, and add them to the ActionListeners
				foreach(Achievement a in AchievementList.Values)
				{
					if(a.RequiredAchievementId != null)
					{
						if(id == a.RequiredAchievementId)
						{
							//Add a to actionlisteners
							foreach(string actionType in a.Progress.Keys)
							{
								//Check if the ActionType is already registered
								if(ActionListeners.ContainsKey(actionType))
								{
									//ActionType is registered, add this achievement to its list
									ActionListeners[actionType].Add(a);
								}
								else
								{
									//Action type is not registered, register it and create a new list of achievements
									List<Achievement> tempList = new List<Achievement>();
									tempList.Add(a);
									ActionListeners.Add(actionType, tempList);
								}
							}
						}
					}
				}
			}

			return result;
		}

		public static bool IsAchievementEarned(Guid? id)
		{
			bool result = false;

			if(id == null)
			{
				return true;
			}

			if(id.HasValue)
			{
				if(AchievementList.ContainsKey(id.Value))
				{
					result = AchievementList[id.Value].Earned;
				}
			}

			return result;
		}

		//Used by Achievement's to notify theyre complete
		public static void OnAchievementComplete(Guid id)
		{
			//Add much more here to regenerate ActionListeners list
			QueuedCompletedAchievements.Enqueue(id);

			if(NotifyOnAchievementComplete != null)
				NotifyOnAchievementComplete(id);
		}
	}

	public enum LogActionResultsEnum
	{
		Ok,
		ActionTypeNotRegistered,
	}

	public struct LogActionResults
	{
		public LogActionResultsEnum Status;
		public string Message;
	}
}
