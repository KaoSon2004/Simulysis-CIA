var Markers = {
	defineMarkers(parentElement) {
		var defs = parentElement.append('svg:defs')

		// arrow head
		defs
			.append('svg:marker')
			.attr('id', 'filledTriangleMarker')
			.attr('viewBox', '0 -5 10 10')
			.attr('refX', 10)
			.attr('refY', 0)
			.attr('markerWidth', 10)
			.attr('markerHeight', 10)
			.attr('orient', 'auto')
			.attr('markerUnits', 'userSpaceOnUse')
			.append('path')
			.attr('d', 'M0,-5L10,0L0,5L3,0')
			.attr('fill', 'black')

		defs
			.append('svg:marker')
			.attr('id', 'triangleMarker')
			.attr('viewBox', '0 -5 5 10')
			.attr('refX', 5)
			.attr('refY', 0)
			.attr('markerWidth', 8)
			.attr('markerHeight', 8)
			.attr('orient', 'auto')
			.attr('markerUnits', 'userSpaceOnUse')
			.append('path')
			.attr('d', 'M0,-5L5,0L0,5')
			.attr('fill', 'transparent')
			.attr('stroke', 'black')

		defs
			.append('svg:marker')
			.attr('id', 'circleMarker')
			.attr('refX', 5)
			.attr('refY', 5)
			.attr('markerWidth', 8)
			.attr('markerHeight', 8)
			.attr('orient', 'auto')
			.attr('markerUnits', 'userSpaceOnUse')
			.append('circle')
			.attr('fill', 'black')
			.attr('r', 3)
			.attr('cx', 5)
			.attr('cy', 5)
	}
}

export default Markers
