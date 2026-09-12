using UnityEngine;
using US13.Managers;
using US13.Strings;
using US13.Systems.InGameEvents;

public class IncreaseEventFrequency : EventScriptBase
{
	public float MultiplyEventTimeBy = 0.5f;

	public override void OnEventStart()
	{
		if (AnnounceEvent)
		{
			var text = "we have detected blue space oddities around your station prepare for strangeness.";

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
		}

		if (FakeEvent) return;


		InGameEventsManager.Instance.TriggerEventInterval *= MultiplyEventTimeBy;

		base.OnEventStart();
	}

}
