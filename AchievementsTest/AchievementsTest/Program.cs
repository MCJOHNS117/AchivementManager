using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AchievementFramework;

using Action = AchievementFramework.Action;

namespace AchievementsTest
{
	class Program
	{
		static void Main(string[] args)
		{
			AchievementManager.LoadAchievements("ExampleAchievements.xml", "");
			AchievementManager.NotifyOnAchievementComplete += AchievementCompleted;

			foreach(Achievement a in AchievementManager.AchievementList.Values)
			{
				Console.WriteLine(a.Name);
			}

			string input = "";

			while(true)
			{
				Console.Write("Enter a Action (Ex. EnemyDestroyed:1): ");
				input = Console.ReadLine();
				if(input == "!")
					break;
				AchievementManager.LogAction(ParseAction(input));
			}

			Console.WriteLine(AchievementManager.SaveAchievementProgress());

			Console.ReadKey();
		}

		public static Action ParseAction(string action)
		{
			Action result;

			string[] parsed = action.Split(':');
			result = new Action(parsed[0], Convert.ToInt32(parsed[1]));

			return result;
		}

		public static void AchievementCompleted(int id)
		{
			Console.WriteLine("You have earned " + AchievementManager.AchievementList[id].Id.ToString() + " Congratulations!");
		}
	}
}
