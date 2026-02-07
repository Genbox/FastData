import { COLORS } from "./colors.js";

function getVisibleBounds(model) {
  if (model.low === undefined || model.high === undefined) {
    return null;
  }

  if (!Array.isArray(model.data) || model.data.length === 0 || model.low > model.high) {
    return null;
  }

  const last = model.data.length - 1;
  const low = Math.max(0, Math.min(last, model.low));
  const high = Math.max(0, Math.min(last, model.high));
  return { low, high };
}

export function drawMinMaxSideLabels(p, layout, baseY, model) {
  const bounds = getVisibleBounds(model);
  if (!bounds) {
    return;
  }

  const leftX = layout.startX - 14;
  const rightX = layout.startX + layout.rowWidth + 14;
  const centerY = baseY + layout.cellHeight * 0.5;

  p.textSize(14);
  p.fill(COLORS.text);

  p.textAlign(p.RIGHT, p.CENTER);
  p.text(`min: ${model.data[bounds.low]}`, leftX, centerY);

  p.textAlign(p.LEFT, p.CENTER);
  p.text(`max: ${model.data[bounds.high]}`, rightX, centerY);
}

export function drawComparisonsAndStatus(p, model, anchorY) {
  const centerX = p.width * 0.5;
  p.fill(COLORS.text);
  p.textAlign(p.CENTER, p.CENTER);
  p.textSize(22);

  p.fill(COLORS.muted);
  p.textSize(15);
  p.text(`Comparisons: ${model.comparisons}`, centerX, anchorY - 56);

  let statusLabel = "Ready";
  let statusColor = COLORS.muted;

  if (model.outcome === "found") {
    statusLabel = "Found";
    statusColor = COLORS.found;
  } else if (model.outcome === "not_found") {
    statusLabel = "Not found";
    statusColor = COLORS.bad;
  } else if (model.comparisons > 0) {
    statusLabel = "Searching";
    statusColor = COLORS.checking;
  }

  p.textSize(16);
  p.textAlign(p.LEFT, p.CENTER);
  const statusPrefix = "Status: ";
  const prefixWidth = p.textWidth(statusPrefix);
  const statusWidth = p.textWidth(statusLabel);
  const statusStartX = centerX - (prefixWidth + statusWidth) * 0.5;

  p.fill(COLORS.muted);
  p.text(statusPrefix, statusStartX, anchorY - 34);
  p.fill(statusColor);
  p.text(statusLabel, statusStartX + prefixWidth, anchorY - 34);
}
