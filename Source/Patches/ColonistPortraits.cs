using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace Puppeteer
{
	/*[HarmonyPatch(typeof(PortraitsCache))]
	[HarmonyPatch("NewRenderTexture")]
	class PortraitsCache_NewRenderTexture_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var found = false;
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_I4_S && Convert.ToSByte(instruction.operand) == 24)
				{
					instruction.operand = 32;
					found = true;
				}
				yield return instruction;
			}
			if (found == false)
				Log.Error("Cannot find find operand 24 in PortraitsCache.NewRenderTexture");
		}
	}*/
}