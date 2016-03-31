﻿namespace AchievementFramework
{
	public class Action
	{
		public string Type { get; }
		public int Value { get; }

		public Action(string type, int value)
		{
			Type = type;
			Value = value;
		}
	}
}
