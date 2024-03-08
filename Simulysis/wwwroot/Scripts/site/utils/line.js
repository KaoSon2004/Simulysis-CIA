var LineUtil = {
	getAllBranch(lines, branchDraws) {

		if (lines) {
			lines.forEach(line => {
				if (line.isBranch) {
					branchDraws.push(line)
				}
				return this.getAllBranch(line.branchDraws, branchDraws);
			})
		}
		return branchDraws;
	},
}

export default LineUtil