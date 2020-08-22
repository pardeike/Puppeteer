using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class Designator_OffLimits : Designator
	{
		public OffLimitsArea area = null;
		public bool? mode;

		public override int DraggableDimensions => 2;
		public override bool DragDrawMeasurements => false;

		public Designator_OffLimits(OffLimitsArea area, bool? mode)
		{
			this.area = area;
			this.mode = mode;
			defaultLabel = "Off Limits";
			soundDragChanged = null;
			useMouseIcon = true;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			return c.InBounds(Map);
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			if (mode.HasValue)
				area[c] = mode.Value;
			else
				area[c] = !area[c];
		}

		public override void SelectedUpdate()
		{
			if (mode != null)
				GenUI.RenderMouseoverBracket();
		}

		public override void ProcessInput(Event ev)
		{
			// cannot interact directly
		}

		protected override void FinalizeDesignationSucceeded()
		{
			base.FinalizeDesignationSucceeded();
		}

		public override void RenderHighlight(List<IntVec3> dragCells)
		{
			DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
		}

	}
}