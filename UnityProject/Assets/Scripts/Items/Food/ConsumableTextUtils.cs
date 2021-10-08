using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConsumableTextUtils
{
	public static void SendGenericConsumeMessage(Mind feeder, Mind eater,
		HungerState eaterHungerState, string consumableName, string eatVerb)
	{
		if (feeder == eater) //If you're eating it yourself.
		{
			switch (eaterHungerState)
			{
				case HungerState.Full:
					Chat.AddActionMsgToChat(eater, $"You cannot force any more of the {consumableName} to go down your throat!",
					$"{eater.ExpensiveName()} cannot force any more of the {consumableName} to go down {eater.TheirPronoun()} throat!");
					return; //Not eating!
				case HungerState.Normal:
					Chat.AddActionMsgToChat(eater, $"You unwillingly {eatVerb} the {consumableName}.", //"a bit of"
						$"{eater.ExpensiveName()} unwillingly {eatVerb}s the {consumableName}."); //"a bit of"
					break;
				case HungerState.Hungry:
					Chat.AddActionMsgToChat(eater, $"You {eatVerb} the {consumableName}.",
						$"{eater.ExpensiveName()} {eatVerb}s the {consumableName}.");
					break;
				case HungerState.Malnourished:
					Chat.AddActionMsgToChat(eater, $"You hungrily {eatVerb} the {consumableName}.",
						$"{eater.ExpensiveName()} hungrily {eatVerb}s the {consumableName}.");
					break;
				case HungerState.Starving:
					Chat.AddActionMsgToChat(eater, $"You hungrily {eatVerb} the {consumableName}, gobbling it down!",
						$"{eater.ExpensiveName()} hungrily {eatVerb}s the {consumableName}, gobbling it down!");
					break;
			}
		}
		else //If you're feeding it to someone else.
		{
			if (eaterHungerState == HungerState.Full)
			{
				Chat.AddActionMsgToChat(eater,
					$"{feeder.ExpensiveName()} cannot force any more of {consumableName} down your throat!",
					$"{feeder.ExpensiveName()} cannot force any more of {consumableName} down {eater.ExpensiveName()}'s throat!");
				return; //Not eating!
			}
			else
			{
				Chat.AddActionMsgToChat(eater,
					$"{feeder.ExpensiveName()} attempts to feed you {consumableName}.",
					$"{feeder.ExpensiveName()} attempts to feed {eater.ExpensiveName()} {consumableName}.");
			}
		}
	}

	public static void SendGenericForceFeedMessage(Mind feeder, Mind eater,
		HungerState eaterHungerState, string consumableName, string eatVerb)
	{
		Chat.AddActionMsgToChat(eater,
			$"{feeder.ExpensiveName()} forces you to {eatVerb} {consumableName}!",
			$"{feeder.ExpensiveName()} forces {eater.ExpensiveName()} to {eatVerb} {consumableName}!");
	}
}
