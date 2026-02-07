import {buildSortedUniqueArray} from "./common.js";

function baseModel(data, target) {
  return {
    data,
    target,
    visited: new Array(data.length).fill(false),
    checkIndex: null,
    foundIndex: null,
    low: 0,
    high: data.length - 1,
    mid: null,
    pivotIndices: [],
    comparisons: 0,
    done: false,
    outcome: "idle",
    status: "Start with low = 0 and high = n - 1.",
    comparisonText: "",
    activeLine: 0
  };
}

export function createBinarySearch() {
  return {
    id: "binary",
    title: "Binary Search",
    description: "Works on sorted arrays by repeatedly halving the search interval.",
    complexity: "Worst-case: O(log n)",
    pseudocode: [
      "while low <= high",
      "  mid = floor((low + high) / 2)",
      "  if arr[mid] == target: return mid",
      "  if target < arr[mid]: high = mid - 1",
      "  else: low = mid + 1",
      "return -1"
    ],
    createModel(options) {
      const data = buildSortedUniqueArray(options.size);
      return baseModel(data, options.target);
    },
    resetModel(model) {
      return baseModel(model.data.slice(), model.target);
    },
    step(model) {
      if (model.done) {
        return;
      }

      if (model.low > model.high) {
        model.done = true;
        model.outcome = "not_found";
        model.mid = null;
        model.checkIndex = null;
        model.status = "Target not found. Search interval is empty.";
        model.comparisonText = "low > high";
        model.activeLine = 5;
        return;
      }

      model.activeLine = 1;
      model.mid = Math.floor((model.low + model.high) / 2);
      model.checkIndex = model.mid;
      model.visited[model.mid] = true;
      model.comparisons += 1;

      const value = model.data[model.mid];
      model.comparisonText = `Compare arr[${model.mid}] = ${value} with target ${model.target}`;

      if (value === model.target) {
        model.done = true;
        model.outcome = "found";
        model.foundIndex = model.mid;
        model.status = `Found at index ${model.mid}.`;
        model.activeLine = 2;
        return;
      }

      if (model.target < value) {
        model.high = model.mid - 1;
        model.status = `Target is smaller than ${value}. Move high to ${model.high}.`;
        model.activeLine = 3;
        return;
      }

      model.low = model.mid + 1;
      model.status = `Target is greater than ${value}. Move low to ${model.low}.`;
      model.activeLine = 4;
    }
  };
}