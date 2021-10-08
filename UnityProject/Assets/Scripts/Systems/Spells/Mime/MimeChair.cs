using System.Linq;
using Objects;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;

namespace Spells
{
	public class MimeChair : Spell
	{
		protected override string FormatInvocationMessage(Mind caster, string modPrefix)
		{
			return string.Format(SpellData.InvocationMessage, caster.ExpensiveName(), caster.ThemPronoun());
		}

		public override bool CastSpellServer(Mind caster)
		{
			if (!base.CastSpellServer(caster))
			{
				return false;
			}

			var buckleable =
				MatrixManager.GetAt<BuckleInteract>(caster.BodyWorldPositionInt, true).FirstOrDefault();
			if (buckleable == null)
			{
				return false;
			}

			var directional = buckleable.GetComponent<Directional>();
			if (directional)
			{
				directional.FaceDirection(caster.CurrentDirection);
			}

			buckleable.BucklePlayer(caster);

			return true;
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