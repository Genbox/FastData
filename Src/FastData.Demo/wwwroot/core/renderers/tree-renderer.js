import { COLORS } from "./colors.js";
import { drawComparisonsAndStatus } from "./overlays.js";

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function getTreeViewport(width, height) {
  const sidePadding = width < 980 ? 24 : 300;
  const top = width < 980 ? height * 0.52 : 86;
  const bottom = height - 48;

  return {
    left: sidePadding,
    right: width - sidePadding,
    top,
    bottom,
    width: Math.max(120, width - sidePadding * 2),
    height: Math.max(120, bottom - top)
  };
}

function drawTreeHeader(p, viewport, title) {
  p.fill(COLORS.muted);
  p.textAlign(p.CENTER, p.CENTER);
  p.textSize(15);
  p.text(title, viewport.left + viewport.width * 0.5, viewport.top - 22);
}

function pickNodeColor(node, state) {
  if (node.id === state.foundId) {
    return COLORS.found;
  }

  if (node.id === state.currentId) {
    return COLORS.checking;
  }

  if (state.visited.has(node.id)) {
    return COLORS.bound;
  }

  return COLORS.unvisited;
}

function drawGenericTree(p, nodes, edges, state, options) {
  const radius = options.radius;
  const labelSize = options.labelSize;
  const valueSize = options.valueSize;

  p.push();
  p.strokeWeight(1.2);

  for (let i = 0; i < edges.length; i += 1) {
    const edge = edges[i];
    const from = nodes[edge.from];
    const to = nodes[edge.to];
    const activeEdge = state.visited.has(from.id) && state.visited.has(to.id);

    p.stroke(activeEdge ? "rgba(113, 183, 255, 0.85)" : "rgba(164, 202, 210, 0.35)");
    p.line(from.x, from.y, to.x, to.y);
  }

  p.noStroke();

  for (let i = 0; i < nodes.length; i += 1) {
    const node = nodes[i];
    p.fill(pickNodeColor(node, state));
    p.circle(node.x, node.y, radius * 2);

    p.fill("#09151c");
    p.textAlign(p.CENTER, p.CENTER);
    p.textSize(valueSize);
    p.text(String(node.value), node.x, node.y - 1);

    p.fill(COLORS.muted);
    p.textSize(labelSize);
    p.text(node.label, node.x, node.y + radius + 11);
  }

  p.pop();
}

function buildBinaryDecisionTree(size) {
  const nodes = [];
  const edges = [];

  function build(low, high, depth, parent) {
    if (low > high) {
      return -1;
    }

    const mid = Math.floor((low + high) / 2);
    const id = nodes.length;
    nodes.push({ id, index: mid, depth, low, high, left: -1, right: -1, x: 0, y: 0 });

    if (parent !== -1) {
      edges.push({ from: parent, to: id });
    }

    const left = build(low, mid - 1, depth + 1, id);
    const right = build(mid + 1, high, depth + 1, id);
    nodes[id].left = left;
    nodes[id].right = right;
    return id;
  }

  const root = build(0, size - 1, 0, -1);
  return { root, nodes, edges };
}

function assignBinaryTreeLayout(tree, viewport) {
  const nodes = tree.nodes;
  if (nodes.length === 0 || tree.root === -1) {
    return;
  }

  let order = 0;

  function walk(nodeId) {
    if (nodeId === -1) {
      return;
    }

    const node = nodes[nodeId];
    walk(node.left);
    node.order = order;
    order += 1;
    walk(node.right);
  }

  walk(tree.root);

  const maxDepth = nodes.reduce((max, node) => Math.max(max, node.depth), 0);
  const yStep = maxDepth === 0 ? 0 : viewport.height / maxDepth;

  for (let i = 0; i < nodes.length; i += 1) {
    const node = nodes[i];
    const ratio = (node.order + 1) / (nodes.length + 1);
    node.x = viewport.left + ratio * viewport.width;
    node.y = viewport.top + node.depth * yStep;
  }
}

function drawBinaryTree(p, model) {
  const viewport = getTreeViewport(p.width, p.height);
  const tree = buildBinaryDecisionTree(model.data.length);
  assignBinaryTreeLayout(tree, viewport);

  const nodes = tree.nodes.map(node => ({
    id: node.index,
    x: node.x,
    y: node.y,
    value: model.data[node.index],
    label: `i=${node.index}`
  }));

  drawTreeHeader(p, viewport, "Binary decision tree");
  drawComparisonsAndStatus(p, model, viewport.top);

  drawGenericTree(
    p,
    nodes,
    tree.edges,
    {
      currentId: model.checkIndex,
      foundId: model.foundIndex,
      visited: new Set(model.visitedIndices || [])
    },
    {
      radius: clamp(16 - Math.floor(model.data.length / 36), 8, 15),
      labelSize: 9,
      valueSize: clamp(12 - Math.floor(model.data.length / 48), 7, 11)
    }
  );
}

function drawInterpolationTree(p, model) {
  const viewport = getTreeViewport(p.width, p.height);
  const tree = buildBinaryDecisionTree(model.data.length);
  assignBinaryTreeLayout(tree, viewport);

  const nodes = tree.nodes.map(node => ({
    id: node.index,
    x: node.x,
    y: node.y,
    value: model.data[node.index],
    label: `i=${node.index}`
  }));

  drawTreeHeader(p, viewport, "Interpolation on binary-style tree");
  drawComparisonsAndStatus(p, model, viewport.top);

  drawGenericTree(
    p,
    nodes,
    tree.edges,
    {
      currentId: model.checkIndex,
      foundId: model.foundIndex,
      visited: new Set(model.visitedIndices || [])
    },
    {
      radius: clamp(16 - Math.floor(model.data.length / 36), 8, 15),
      labelSize: 9,
      valueSize: clamp(12 - Math.floor(model.data.length / 48), 7, 11)
    }
  );
}

function buildEytzingerTree(data, viewport) {
  const nodes = [];
  const edges = [];
  let maxDepth = 0;

  for (let i = 0; i < data.length; i += 1) {
    const depth = Math.floor(Math.log2(i + 1));
    const levelStart = 2 ** depth - 1;
    const position = i - levelStart;
    const nodesAtLevel = Math.min(2 ** depth, data.length - levelStart);
    const x = viewport.left + ((position + 1) / (nodesAtLevel + 1)) * viewport.width;
    const y = viewport.top + depth;

    nodes.push({
      id: i,
      depth,
      x,
      y,
      value: data[i],
      label: `i=${i}`
    });

    if (i > 0) {
      edges.push({ from: Math.floor((i - 1) / 2), to: i });
    }

    maxDepth = Math.max(maxDepth, depth);
  }

  const yStep = maxDepth === 0 ? 0 : viewport.height / maxDepth;
  for (let i = 0; i < nodes.length; i += 1) {
    nodes[i].y = viewport.top + nodes[i].depth * yStep;
  }

  return { nodes, edges };
}

function drawEytzingerTree(p, model) {
  const viewport = getTreeViewport(p.width, p.height);
  const tree = buildEytzingerTree(model.data, viewport);
  drawTreeHeader(p, viewport, "Eytzinger heap-layout tree");
  drawComparisonsAndStatus(p, model, viewport.top);

  drawGenericTree(
    p,
    tree.nodes,
    tree.edges,
    {
      currentId: model.checkIndex,
      foundId: model.foundIndex,
      visited: new Set(model.visitedIndices || [])
    },
    {
      radius: clamp(15 - Math.floor(model.data.length / 34), 8, 14),
      labelSize: 9,
      valueSize: clamp(11 - Math.floor(model.data.length / 44), 7, 10)
    }
  );
}

function collectK16Children(tree, node) {
  const childBase = node * 17;
  const children = [];

  for (let i = 0; i < 17; i += 1) {
    const child = tree.children[childBase + i];
    if (child !== -1) {
      children.push(child);
    }
  }

  return children;
}

function buildK16TreeLayout(model, viewport) {
  const source = model.tree;
  if (!source || source.root === -1) {
    return { nodes: [], edges: [] };
  }

  const nodes = new Map();
  const edges = [];

  function visit(node, depth) {
    if (nodes.has(node)) {
      return;
    }

    const keyBase = node * 16;
    const keyCount = source.keyCounts[node];
    const firstKey = source.keys[keyBase];
    const lastKey = source.keys[keyBase + Math.max(0, keyCount - 1)];

    nodes.set(node, {
      id: node,
      depth,
      keyCount,
      firstKey,
      lastKey,
      children: collectK16Children(source, node),
      leafSpan: 1,
      xUnit: 0,
      x: 0,
      y: 0
    });

    const info = nodes.get(node);
    for (let i = 0; i < info.children.length; i += 1) {
      const child = info.children[i];
      edges.push({ from: node, to: child });
      visit(child, depth + 1);
    }
  }

  visit(source.root, 0);

  function computeLeafSpan(node) {
    const info = nodes.get(node);
    if (info.children.length === 0) {
      info.leafSpan = 1;
      return 1;
    }

    let span = 0;
    for (let i = 0; i < info.children.length; i += 1) {
      span += computeLeafSpan(info.children[i]);
    }

    info.leafSpan = Math.max(1, span);
    return info.leafSpan;
  }

  computeLeafSpan(source.root);

  let cursor = 0;
  function assignX(node) {
    const info = nodes.get(node);

    if (info.children.length === 0) {
      info.xUnit = cursor;
      cursor += 1;
      return info.xUnit;
    }

    let sum = 0;
    for (let i = 0; i < info.children.length; i += 1) {
      sum += assignX(info.children[i]);
    }

    info.xUnit = sum / info.children.length;
    return info.xUnit;
  }

  assignX(source.root);

  const outputNodes = [...nodes.values()];
  const maxDepth = outputNodes.reduce((max, node) => Math.max(max, node.depth), 0);
  const maxX = Math.max(1, cursor - 1);
  const yStep = maxDepth === 0 ? 0 : viewport.height / maxDepth;

  for (let i = 0; i < outputNodes.length; i += 1) {
    const node = outputNodes[i];
    node.x = viewport.left + (node.xUnit / maxX) * viewport.width;
    node.y = viewport.top + node.depth * yStep;
  }

  return { nodes: outputNodes, edges };
}

function drawK16Tree(p, model) {
  const viewport = getTreeViewport(p.width, p.height);
  const layout = buildK16TreeLayout(model, viewport);
  drawTreeHeader(p, viewport, "16-ary (K16) node tree");
  drawComparisonsAndStatus(p, model, viewport.top);

  if (layout.nodes.length === 0) {
    return;
  }

  const nodeLookup = new Map(layout.nodes.map(node => [node.id, node]));
  const visited = new Set(model.visitedNodes || []);
  const currentNode = model.node;
  const foundNode = model.foundNode;

  p.push();
  p.strokeWeight(1.2);

  for (let i = 0; i < layout.edges.length; i += 1) {
    const edge = layout.edges[i];
    const from = nodeLookup.get(edge.from);
    const to = nodeLookup.get(edge.to);

    if (!from || !to) {
      continue;
    }

    const activeEdge = visited.has(from.id) && visited.has(to.id);
    p.stroke(activeEdge ? "rgba(113, 183, 255, 0.85)" : "rgba(164, 202, 210, 0.35)");
    p.line(from.x, from.y, to.x, to.y);
  }

  p.noStroke();

  for (let i = 0; i < layout.nodes.length; i += 1) {
    const node = layout.nodes[i];
    const isFound = node.id === foundNode;
    const isCurrent = node.id === currentNode && !isFound;
    const isVisited = visited.has(node.id);
    const width = clamp(44 + node.keyCount * 2, 44, 76);
    const height = 26;

    let fill = COLORS.unvisited;
    if (isFound) {
      fill = COLORS.found;
    } else if (isCurrent) {
      fill = COLORS.checking;
    } else if (isVisited) {
      fill = COLORS.bound;
    }

    p.fill(fill);
    p.rect(node.x - width * 0.5, node.y - height * 0.5, width, height, 7);

    p.fill("#09151c");
    p.textAlign(p.CENTER, p.CENTER);
    p.textSize(9);
    const label = node.keyCount > 0 ? `${node.firstKey}..${node.lastKey}` : "empty";
    p.text(label, node.x, node.y - 1);

    p.fill(COLORS.muted);
    p.textSize(8);
    p.text(`n${node.id} (${node.keyCount})`, node.x, node.y + 15);

    if (isCurrent && model.activeKeySlot !== null && model.activeKeySlot !== undefined) {
      p.fill(COLORS.checking);
      p.textSize(8);
      p.text(`slot ${model.activeKeySlot}`, node.x, node.y + 25);
    }
  }

  p.pop();
}

export function drawSearchTree(p, model, algorithmId) {
  if (!model || !Array.isArray(model.data)) {
    return;
  }

  if (algorithmId === "binary") {
    drawBinaryTree(p, model);
    return;
  }

  if (algorithmId === "interpolation") {
    drawInterpolationTree(p, model);
    return;
  }

  if (algorithmId === "sixteenAry") {
    drawK16Tree(p, model);
    return;
  }

  if (algorithmId === "eytzinger") {
    drawEytzingerTree(p, model);

  }
}
