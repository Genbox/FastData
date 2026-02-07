import {buildSortedUniqueArray} from "./common.js";

function countK16Nodes(length) {
  if (length <= 0) {
    return 0;
  }

  if (length <= 16) {
    return 1;
  }

  const remaining = length - 16;
  const baseChild = Math.floor(remaining / 17);
  const remainder = remaining % 17;
  let count = 1;

  for (let i = 0; i < 17; i += 1) {
    const childSize = baseChild + (i < remainder ? 1 : 0);
    count += countK16Nodes(childSize);
  }

  return count;
}

function buildK16Tree(data) {
  const nodeCount = countK16Nodes(data.length);
  const keys = new Array(nodeCount * 16).fill(Number.POSITIVE_INFINITY);
  const children = new Array(nodeCount * 17).fill(-1);
  const keyCounts = new Array(nodeCount).fill(0);
  const rangeStart = new Array(nodeCount).fill(-1);
  const rangeEnd = new Array(nodeCount).fill(-1);

  let nextNode = 0;

  function buildNode(start, length) {
    if (length === 0) {
      return -1;
    }

    const nodeIndex = nextNode;
    nextNode += 1;

    const keyBase = nodeIndex * 16;
    const childBase = nodeIndex * 17;

    rangeStart[nodeIndex] = start;
    rangeEnd[nodeIndex] = start + length;

    if (length <= 16) {
      for (let i = 0; i < length; i += 1) {
        keys[keyBase + i] = data[start + i];
      }

      keyCounts[nodeIndex] = length;
      return nodeIndex;
    }

    keyCounts[nodeIndex] = 16;

    const remaining = length - 16;
    const baseChild = Math.floor(remaining / 17);
    const remainder = remaining % 17;
    let cursor = start;

    for (let keyIndex = 0; keyIndex < 16; keyIndex += 1) {
      const childSize = baseChild + (keyIndex < remainder ? 1 : 0);
      const childIndex = buildNode(cursor, childSize);

      if (childIndex !== -1) {
        children[childBase + keyIndex] = childIndex;
      }

      cursor += childSize;
      keys[keyBase + keyIndex] = data[cursor];
      cursor += 1;
    }

    const lastChildSize = baseChild + (16 < remainder ? 1 : 0);
    const lastChildIndex = buildNode(cursor, lastChildSize);
    if (lastChildIndex !== -1) {
      children[childBase + 16] = lastChildIndex;
    }

    return nodeIndex;
  }

  const root = buildNode(0, data.length);

  return {
    root,
    keys,
    children,
    keyCounts,
    rangeStart,
    rangeEnd
  };
}

function createValueToIndexMap(data) {
  const map = new Map();

  for (let i = 0; i < data.length; i += 1) {
    map.set(data[i], i);
  }

  return map;
}

function baseModel(data, target) {
  const tree = buildK16Tree(data);

  return {
    data,
    target,
    tree,
    valueToIndex: createValueToIndexMap(data),
    node: tree.root,
    keyBase: -1,
    childBase: -1,
    keyCount: 0,
    scanCursor: 0,
    childSlot: 0,
    phase: "enter_node",
    activeKeySlot: null,
    visitedNodes: [],
    checkIndex: null,
    foundIndex: null,
    foundNode: null,
    low: undefined,
    high: undefined,
    mid: null,
    comparisons: 0,
    done: false,
    outcome: "idle",
    status: "Start at K16 tree root.",
    comparisonText: "",
    activeLine: 0
  };
}

export function createSixteenArySearch() {
  return {
    id: "sixteenAry",
    title: "16-ary Search",
    description: "K16 tree search: linearly scan up to 16 keys in a node, then descend to one child.",
    complexity: "O(log17 n) nodes, up to 16 key checks per node",
    supportsTreeView: false,
    pseudocode: [
      "node = root",
      "while node != -1",
      "  linearly scan node keys (up to 16)",
      "  if key == target: return found",
      "  choose child slot by comparisons and descend",
      "return -1"
    ],
    createModel(options) {
      const data = buildSortedUniqueArray(options.size, options.datasetMode, options.seed);
      return baseModel(data, options.target);
    },
    resetModel(model) {
      return baseModel(model.data.slice(), model.target);
    },
    step(model) {
      if (model.done) {
        return;
      }

      if (model.phase === "enter_node") {
        if (model.node === -1) {
          model.done = true;
          model.outcome = "not_found";
          model.status = "Reached empty child. Target not found.";
          model.comparisonText = "node == -1";
          model.checkIndex = null;
          model.activeKeySlot = null;
          model.activeLine = 5;
          return;
        }

        const node = model.node;
        if (model.visitedNodes.length === 0 || model.visitedNodes[model.visitedNodes.length - 1] !== node) {
          model.visitedNodes.push(node);
        }

        model.keyBase = node * 16;
        model.childBase = node * 17;
        model.keyCount = model.tree.keyCounts[node];
        model.scanCursor = 0;
        model.childSlot = 0;
        model.activeKeySlot = null;
        model.phase = "scan_keys";

        const start = model.tree.rangeStart[node];
        const endExclusive = model.tree.rangeEnd[node];
        model.low = start;
        model.high = endExclusive - 1;
        model.mid = null;

        model.status = `Node ${node}: scan ${model.keyCount} keys.`;
        model.comparisonText = `Node value range [${model.data[model.low]}, ${model.data[model.high]}]`;
        model.activeLine = 1;
        model.checkIndex = null;
        return;
      }

      if (model.phase === "scan_keys") {
        if (model.scanCursor >= model.keyCount) {
          model.phase = "descend";
          model.activeKeySlot = null;
          model.status = `No match in node ${model.node}. Descend child slot ${model.childSlot}.`;
          model.comparisonText = `child slot = ${model.childSlot}`;
          model.activeLine = 4;
          model.checkIndex = null;
          return;
        }

        const key = model.tree.keys[model.keyBase + model.scanCursor];
        const keyIndex = model.valueToIndex.get(key) ?? null;
        model.checkIndex = keyIndex;
        model.activeKeySlot = model.scanCursor;
        model.comparisons += 1;
        model.activeLine = 2;
        model.comparisonText = `Compare key ${key} with target ${model.target}`;

        if (model.target === key) {
          model.done = true;
          model.outcome = "found";
          model.foundIndex = keyIndex;
          model.foundNode = model.node;
          model.status = `Found in node ${model.node}.`;
          model.activeLine = 3;
          return;
        }

        if (model.target > key) {
          model.childSlot += 1;
          model.scanCursor += 1;
          model.status = `Target > ${key}. Continue node scan.`;
          return;
        }

        model.phase = "descend";
        model.activeKeySlot = null;
        model.status = `Target < ${key}. Descend child slot ${model.childSlot}.`;
        model.activeLine = 4;
        return;
      }

      const nextNode = model.tree.children[model.childBase + model.childSlot];
      model.node = nextNode;
      model.phase = "enter_node";
      model.activeKeySlot = null;
      model.checkIndex = null;
      model.status = `Descend to child ${nextNode}.`;
      model.comparisonText = "";
      model.activeLine = 4;
    }
  };
}