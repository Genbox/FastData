import {buildSortedUniqueArray, toEytzinger} from "./common.js";

function baseModel(data, target) {
  return {
    data,
    target,
    checkIndex: null,
    foundIndex: null,
    low: undefined,
    high: undefined,
    mid: null,
    currentIndex: 0,
    comparisons: 0,
    visitedIndices: [],
    done: false,
    outcome: "idle",
    status: "Start at root index 0 in Eytzinger layout.",
    comparisonText: "",
    activeLine: 0
  };
}

export function createEytzingerSearch() {
  return {
    id: "eytzinger",
    title: "Eytzinger Search",
    description: "Searches a binary-search-tree layout stored in array (breadth-friendly memory order).",
    complexity: "Worst-case: O(log n)",
    supportsTreeView: true,
    pseudocode: [
      "i = 0",
      "while i < n",
      "  if a[i] == target: return i",
      "  if target < a[i]: i = 2*i + 1",
      "  else: i = 2*i + 2",
      "return -1"
    ],
    createModel(options) {
      const sorted = buildSortedUniqueArray(options.size, options.datasetMode, options.seed);
      const data = toEytzinger(sorted);
      return baseModel(data, options.target);
    },
    resetModel(model) {
      return baseModel(model.data.slice(), model.target);
    },
    step(model) {
      if (model.done) {
        return;
      }

      if (model.currentIndex >= model.data.length) {
        model.done = true;
        model.outcome = "not_found";
        model.status = "Walked past the last node. Target not found.";
        model.comparisonText = "Index is outside array bounds.";
        model.checkIndex = null;
        model.activeLine = 5;
        return;
      }

      const i = model.currentIndex;
      const node = model.data[i];
      model.checkIndex = i;
      model.comparisons += 1;

      if (model.visitedIndices.length === 0 || model.visitedIndices[model.visitedIndices.length - 1] !== i) {
        model.visitedIndices.push(i);
      }

      model.comparisonText = `Visit node ${i} with value ${node}. Compare to ${model.target}.`;
      model.activeLine = 2;

      if (node === model.target) {
        model.done = true;
        model.outcome = "found";
        model.foundIndex = i;
        model.status = `Found target at Eytzinger index ${i}.`;
        return;
      }

      if (model.target < node) {
        model.currentIndex = i * 2 + 1;
        model.status = `Target is smaller than ${node}. Move to left child ${model.currentIndex}.`;
        model.activeLine = 3;
        return;
      }

      model.currentIndex = i * 2 + 2;
      model.status = `Target is greater than ${node}. Move to right child ${model.currentIndex}.`;
      model.activeLine = 4;
    }
  };
}