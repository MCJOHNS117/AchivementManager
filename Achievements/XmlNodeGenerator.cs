using System.Xml.Linq;

namespace AchievementFramework
{
	public static class XmlNodeGenerator
	{
		public static XElement GenerateNode(Achievement Achievement)
		{
			XElement achievementNode = new XElement("Achievement");
			achievementNode.Add(new XElement("Name", Achievement.Name));
			achievementNode.Add(new XElement("Id", Achievement.Id));
			achievementNode.Add(new XElement("Description", Achievement.Description));
			achievementNode.Add(new XElement("Category", Achievement.Category));
			achievementNode.Add(new XElement("Subcategory", Achievement.Subcategory));
			achievementNode.Add(new XElement("IconId", Achievement.IconId));

			XElement progression = new XElement("Progression");
			foreach(string key in Achievement.Progress.Keys)
			{
				progression.Add(new XElement("Progress", new XAttribute("ActionType", key), new XAttribute("RequiredValue", Achievement.Progress[key].Required)));
			}
			achievementNode.Add(progression);

			if(Achievement.RequiredAchievementId.HasValue)
			{
				achievementNode.Add(new XElement("RequiredAchievementId", Achievement.RequiredAchievementId.Value));
			}
			else
			{
				achievementNode.Add(new XElement("RequiredAchievementId", "-1"));
			}

			achievementNode.Add(new XElement("Points", Achievement.Points));

			return achievementNode;
		}
	}
}
