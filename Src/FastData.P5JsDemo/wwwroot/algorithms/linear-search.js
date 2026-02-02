import { buildRandomArray } from "./common.js";

function baseModel(data, target) {
  return {
    data,
    target,
    visited: new Array(data.length).fill(false),
    checkIndex: null,
    foundIndex: null,
    currentIndex: 0,
    comparisons: 0,
    pivotIndices: [],
    done: false,
    outcome: "idle",
    status: "Start scanning from index 0.",
    comparisonText: "",
    activeLine: 0
  };
}

export function createLinearSearch() {
  return {
    id: "linear",
    title: "Linear Search",
    description: "Checks each element left-to-right until the target is found or the array ends.",
    complexity: "Worst-case: O(n)",
    pseudocode: [
      "for i = 0 to n - 1",
      "  if arr[i] == target: return i",
      "return -1"
    ],
    createModel(options) {
      const data = buildRandomArray(options.size);
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
        model.checkIndex = null;
        model.status = "Target not found after scanning all elements.";
        model.comparisonText = "All positions checked.";
        model.activeLine = 2;
        return;
      }

      const i = model.currentIndex;
      model.checkIndex = i;
      model.visited[i] = true;
      model.comparisons += 1;
      model.activeLine = 1;

      const value = model.data[i];
      model.comparisonText = `Compare arr[${i}] = ${value} with target ${model.target}`;

      if (value === model.target) {
        model.done = true;
        model.outcome = "found";
        model.foundIndex = i;
        model.status = `Found at index ${i}.`;
        return;
      }

      model.currentIndex += 1;
      model.status = `No match at index ${i}. Move to ${model.currentIndex}.`;

      if (model.currentIndex >= model.data.length) {
        model.done = true;
        model.outcome = "not_found";
        model.checkIndex = null;
        model.status = "Target not found after scanning all elements.";
        model.activeLine = 2;
      }
    }
  };
}