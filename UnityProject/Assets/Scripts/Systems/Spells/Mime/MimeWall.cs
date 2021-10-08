namespace Systems.Spells
{
	public class MimeWall : Spell
	{
		protected override string FormatInvocationMessage(Mind caster, string modPrefix)
		{
			return string.Format(SpellData.InvocationMessage, caster.ExpensiveName(), caster.ThemPronoun());
		}
		public override bool ValidateCast(Mind caster)
		{
			if (!base.ValidateCast(caster))
			{
				return false;
			}

			if (!caster.IsMiming)
			{
				Chat.AddExamineMsg(caster, "You must dedicate yourself to silence first!");
				return false;
			}

			return true;
		}
	}
}
