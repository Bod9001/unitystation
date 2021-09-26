namespace Systems.Spells
{
	public class MimeSpeech : Spell
	{
		protected override string FormatStillRechargingMessage(Mind caster)
		{
			return caster.IsMiming
				? "You can't break your vow of silence that fast!"
				: "You'll have to wait before you can give your vow of silence again!";
		}

		protected override string FormatInvocationMessageSelf(Mind caster)
		{
			return caster.IsMiming ? "You make a vow of silence." : "You break your vow of silence.";
		}

		public override bool CastSpellServer(Mind caster)
		{
			if (!base.CastSpellServer(caster))
			{
				return false;
			}

			caster.IsMiming = !caster.IsMiming;
			return true;
		}
	}
}
