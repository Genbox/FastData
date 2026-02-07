import { COLORS } from "./colors.js";
import { drawComparisonsAndStatus, drawMinMaxSideLabels } from "./overlays.js";

function getLayout(width, height, length) {
  const sidePadding = width < 980 ? 20 : 292;
  const rightPadding = width < 980 ? 20 : 292;
  const top = height < 760 ? height * 0.48 : height * 0.56;
  const gap = length > 80 ? 1 : length > 50 ? 2 : length > 30 ? 4 : 8;
  const available = Math.max(200, width - sidePadding - rightPadding);
  const rawCellWidth = (available - gap * (length - 1)) / length;
  const cellWidth = Math.max(2, Math.min(62, rawCellWidth));
  const rowWidth = cellWidth * length + gap * (length - 1);
  const startX = (width - rowWidth) * 0.5;
  const maxVisibleIndexes = width < 980 ? 14 : 26;

  return {
    top,
    gap,
    cellWidth,
    rowWidth,
    startX,
    cellHeight: Math.max(24, Math.min(130, Math.max(cellWidth * 2.8, height * 0.16))),
    showValues: cellWidth >= 16,
    showIndexes: cellWidth >= 10,
    indexLabelStride: Math.max(1, Math.ceil(length / maxVisibleIndexes))
  };
}

function getCellColor(index, model) {
  if (index === model.foundIndex) {
    return COLORS.found;
  }

  if (index === model.checkIndex) {
    return COLORS.checking;
  }

  if (model.low !== undefined && model.high !== undefined && index >= model.low && index <= model.high) {
    return index === model.mid ? COLORS.checking : COLORS.bound;
  }

  return COLORS.unvisited;
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

    if (layout.showValues) {
      p.fill("#09151c");
      p.textAlign(p.CENTER, p.CENTER);
      p.textSize(Math.max(9, layout.cellWidth * 0.24));
      p.text(String(value), x + layout.cellWidth * 0.5, baseY + layout.cellHeight * 0.5);
    }

    if (layout.showIndexes && i % layout.indexLabelStride === 0) {
      p.fill(COLORS.muted);
      p.textAlign(p.CENTER, p.CENTER);
      p.textSize(11);
      p.text(String(i), x + layout.cellWidth * 0.5, baseY + layout.cellHeight + 15);
    }
  }

  drawComparisonsAndStatus(p, model, baseY);
  drawMinMaxSideLabels(p, layout, baseY, model);
  p.pop();
}
