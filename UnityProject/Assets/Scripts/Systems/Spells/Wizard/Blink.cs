using UnityEngine;
using System.Collections;
using Systems.Teleport;

namespace Systems.Spells.Wizard
{
	public class Blink : Spell
	{
		public override bool CastSpellServer(Mind caster)
		{
			TeleportUtils.ServerTeleportRandom(caster.GameObjectBody, 8, 16, true, true);

			return true;
		}
	}
}
