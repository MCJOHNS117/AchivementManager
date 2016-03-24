using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AchievementFramework
{
	public class Achievement
	{
		//Name of the achievement, used to display
		public string Name { get; }
		//Internal achievement ID, used for storing progress
		public int Id { get; }
		//Description of the achievement, used for display						
		public string Description { get; }
		//Internal Icon ID, used to look-up the achievement icon			
		public int IconId { get; }
		//Current progress on the achievement
		public Dictionary<string, ActionProgress> Progress { get; private set; }
		//If this achievement requires a different achievement to activate, its ID is stored here
		public int? RequiredAchievementId { get; }
		//Points value of the achivement
		public int Points { get; }
		//True if this achievement has been unlocked already, false otherwise, 
		//Used to quickly eliminate this achievement when distributing actions			
		public bool Earned { get; set; }

		public Achievement()
		{

		}

		public Achievement(string name, int id, string description, int iconId, Dictionary<string, ActionProgress> progress,
			int points, bool earned, int? requiredAchievementId)
		{
			Name = name;
			Id = id;
			Description = description;
			IconId = iconId;
			Progress = progress;
			Points = points;
			Earned = earned;

			if(requiredAchievementId != null)
				RequiredAchievementId = requiredAchievementId;
		}

		//Handles Actions coming from the AchievementManager
		public void OnAction(Action action)
		{
			//First check that this achievement isnt earned already, 
			//and that if an achievement is required, it has been earned already.
			if(!Earned && (RequiredAchievementId.HasValue ? AchievementManager.IsAchievementEarned(RequiredAchievementId.Value) : true))
			{
				//Verify that the list of Actions contains this actions type
				//And that the Progress dictionary also contains this actions type
				if(Progress.ContainsKey(action.Type))
				{
					//Check that progress on this action is not already complete
					if(!Progress[action.Type].Completed)
					{
						//Now we can work with the progress on this action for this achievement
						ActionProgress progress = Progress[action.Type];
						progress.Current += action.Value;

						//The Action has been completed!
						if(progress.Current >= progress.Required)
						{
							progress.Current = progress.Required;
							progress.Completed = true;
						}
						else
						{
							progress.Completed = false;
						}

						//Update the progress list to reflect the new changes
						Progress[action.Type] = progress;
					}
				}
			}

			bool complete = true;
			foreach(ActionProgress ap in Progress.Values)
			{
				//If any one of the conditions is not met, we have not earned the achievement yet
				if(!ap.Completed)
					complete = false;
			}
			if(complete && !Earned)
			{
				Earned = true;
				AchievementManager.OnAchievementComplete(Id);
			}
		}
    }

	public struct ActionProgress
	{
		public int Current;
		public int Required;
		public bool Completed;
	}
}
