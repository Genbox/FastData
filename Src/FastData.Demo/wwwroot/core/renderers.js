const COLORS = {
  unvisited: "#4a6578",
  visited: "#3a4758",
  checking: "#71b7ff",
  found: "#6dd18c",
  bound: "#82f0d1",
  pivot: "#d8bbff",
  text: "#ecf4f1",
  muted: "#bcd1c9",
  bad: "#ea7f72"
};

function getLayout(width, height, length) {
  const sidePadding = width < 980 ? 24 : 300;
  const rightPadding = width < 980 ? 24 : 300;
  const top = height < 760 ? height * 0.48 : height * 0.56;
  const gap = 8;
  const available = Math.max(200, width - sidePadding - rightPadding);
  const rawCellWidth = (available - gap * (length - 1)) / length;
  const cellWidth = Math.max(18, Math.min(62, rawCellWidth));
  const rowWidth = cellWidth * length + gap * (length - 1);
  const startX = (width - rowWidth) * 0.5;

  return {
    top,
    gap,
    cellWidth,
    rowWidth,
    startX,
    cellHeight: Math.max(56, Math.min(130, height * 0.18))
  };
}

function getCellColor(index, model) {
  if (index === model.foundIndex) {
    return COLORS.found;
  }

  if (index === model.checkIndex) {
    return COLORS.checking;
  }

  if (Array.isArray(model.pivotIndices) && model.pivotIndices.includes(index)) {
    return COLORS.pivot;
  }

  if (model.low !== undefined && model.high !== undefined && index >= model.low && index <= model.high) {
    return index === model.mid ? COLORS.checking : COLORS.bound;
  }

  if (model.visited[index]) {
    return COLORS.visited;
  }

  return COLORS.unvisited;
}

function drawBounds(p, x, y, model) {
  if (model.low === undefined || model.high === undefined) {
    return;
  }

  p.textAlign(p.CENTER, p.TOP);
  p.textSize(13);

  p.fill(COLORS.bound);
  p.text(`low: ${model.low}`, x(model.low), y + 8);
  p.text(`high: ${model.high}`, x(model.high), y + 30);

  if (model.mid !== null && model.mid !== undefined) {
    p.fill(COLORS.text);
    p.text(`mid: ${model.mid}`, x(model.mid), y - 30);
  }
}

export function drawSearchArray(p, model) {
  const layout = getLayout(p.width, p.height, model.data.length);
  const baseY = layout.top;

  p.push();
  p.noStroke();

  for (let i = 0; i < model.data.length; i += 1) {
    const x = layout.startX + i * (layout.cellWidth + layout.gap);
    const value = model.data[i];
    const fillColor = getCellColor(i, model);
    const radius = Math.min(12, layout.cellWidth * 0.25);

    p.fill(fillColor);
    p.rect(x, baseY, layout.cellWidth, layout.cellHeight, radius);

    p.fill("#09151c");
    p.textAlign(p.CENTER, p.CENTER);
    p.textSize(Math.max(12, layout.cellWidth * 0.3));
    p.text(String(value), x + layout.cellWidth * 0.5, baseY + layout.cellHeight * 0.42);

    p.fill(COLORS.muted);
    p.textSize(12);
    p.text(String(i), x + layout.cellWidth * 0.5, baseY + layout.cellHeight + 18);
  }

  const centerX = p.width * 0.5;
  p.fill(COLORS.text);
  p.textAlign(p.CENTER, p.CENTER);
  p.textSize(22);
  p.text(`Target: ${model.target}`, centerX, baseY - 86);

  p.fill(model.outcome === "not_found" ? COLORS.bad : COLORS.muted);
  p.textSize(15);
  p.text(`Comparisons: ${model.comparisons}`, centerX, baseY - 56);

  const getCenter = index => layout.startX + index * (layout.cellWidth + layout.gap) + layout.cellWidth * 0.5;
  drawBounds(p, getCenter, baseY + layout.cellHeight + 28, model);

  if (Array.isArray(model.pivotIndices) && model.pivotIndices.length > 0) {
    p.fill(COLORS.pivot);
    p.textSize(13);
    p.textAlign(p.CENTER, p.TOP);
    p.text("pivot markers", centerX, baseY + layout.cellHeight + 52);
  }

  p.pop();
}
