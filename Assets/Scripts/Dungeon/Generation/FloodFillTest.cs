﻿public class FloodFillTest {
	private static bool[,] mark;

	private static void markBlock(Dungeon d, Position p) {
		mark[p.x, p.y] = true;

		var block = d.getBlock(p);

		if(block != null) {
			foreach(var dir in Direction.All) {
				if(block.isPassable(dir)) {
					var nd = p + dir;
					if(!mark[nd.x, nd.y]) {
						markBlock(d, nd);
					}
					
				}
			}
		}
	}

	public static bool IntegrityTest(Dungeon d) {
		mark = new bool[d.Width, d.Height];

		var free = d.firstBlockFreePos();

		markBlock(d, free);

		var blockCount = 0;
		var markCount = 0;

		d.ForEachExistingBlock(delegate(Position p, Block b) {
			blockCount++;

			if(mark[p.x, p.y]) {
				markCount++;
			}
		});

		return blockCount == markCount;
	}
}