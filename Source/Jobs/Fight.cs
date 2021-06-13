using RimWorld;
using Verse;

namespace Puppeteer
{
	public class FightEnemy : JobGiver_AIFightEnemies
	{
		readonly Pawn enemyTarget;

		public FightEnemy(Pawn enemyTarget)
		{
			this.enemyTarget = enemyTarget;
		}

		public override void UpdateEnemyTarget(Pawn pawn)
		{
			pawn.mindState.enemyTarget = enemyTarget;
		}
	}
}
